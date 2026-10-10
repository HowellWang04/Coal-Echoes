"""Offline northern mountain lowering candidates, sampled from verified Terrain readbacks."""
from pathlib import Path
import json
import os
import sys

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
SOURCE = ROOT.parent / 'WholeMapGradient800'
sys.path.insert(0, str(PROJECT / 'Library/GradientTerrainExampleTools/python-deps'))
os.environ['MPLCONFIGDIR'] = str(ROOT / 'plot-cache')
import numpy as np
from scipy.ndimage import map_coordinates, distance_transform_edt, binary_erosion
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource, LinearSegmentedColormap

plt.rcParams['font.sans-serif'] = ['Microsoft YaHei']
plt.rcParams['axes.unicode_minus'] = False
meta = json.loads((SOURCE / 'metadata.json').read_text())
snapshot = json.loads((ROOT / 'snapshot.json').read_text(encoding='utf-8-sig'))
assert len(snapshot['terrains']) == 58
assert all(t['matchesPrevious'] for t in snapshot['terrains'])
n = meta['resolution']
step = meta['spacing_m']
x0, x1, z0, z1 = meta['extent_xz']
xs = x0 + np.arange(n) * step
zs = z0 + np.arange(n) * step
x, z = xs[None, :], zs[:, None]
current = np.full((n, n), np.nan, np.float32)
for t in sorted(snapshot['terrains'], key=lambda item: item['res']):
    px, py, pz = t['p']
    sx, sy, sz = t['size']
    resolution = t['res']
    ix = np.flatnonzero((xs >= px - .001) & (xs <= px + sx + .001))
    iz = np.flatnonzero((zs >= pz - .001) & (zs <= pz + sz + .001))
    field = np.fromfile(SOURCE / 'After' / (t['name'] + '.height.f32'), np.float32).reshape(resolution, resolution)
    coords = np.broadcast_arrays((zs[iz, None] - pz) / sz * (resolution - 1),
                                 (xs[None, ix] - px) / sx * (resolution - 1))
    current[np.ix_(iz, ix)] = map_coordinates(field, coords, order=1, mode='nearest') * sy + py

valid = np.isfinite(current)
core = np.load(SOURCE / 'protected.npy')
accepted = np.load(SOURCE / 'accepted.npy')
def smooth(t):
    t = np.clip(t, 0, 1)
    return t * t * t * (t * (t * 6 - 15) + 10)

# A constant vertical translation in the mountain body; only its perimeter changes slope.
# Keep the central polygon and a 24m guard unchanged, with 800m for its approach.
distance = distance_transform_edt(~core) * step
weight = smooth((x + 3450) / 800) * smooth((450 - x) / 800)
weight = weight * smooth((z - 100) / 800) * smooth((distance - 24) / 800)
# Return to the pre-montage relief at the foot instead of digging a depression into it.
# Blend across the first 400m above that earlier relief.
base = np.load(SOURCE / 'base.npy')
rise = np.maximum(current - base, 0)
weight = weight * smooth(rise / 400)
# Keep the low southern apron near its current elevation while translating the crown.
# Blend across world Y -450m to 0m; the mountain crown receives the full translation.
weight = weight * smooth((current + 450) / 450)
weight[~valid | core | accepted] = 0
current[~valid] = -710
plateau = valid & (weight == 1)
interior = binary_erosion(plateau)
fields = [current.astype(np.float64)]
reports = []
for amount in [120, 200]:
    candidate = current.astype(np.float64) - amount * weight
    unchanged = weight == 0
    delta = candidate - current
    dz, dx = np.gradient(delta, step)
    assert np.array_equal(candidate[unchanged], current[unchanged])
    assert candidate[valid].min() >= -710
    assert np.all(candidate[valid] >= np.minimum(current, base)[valid] - 1e-5)
    assert np.max(np.abs(delta[plateau] + amount)) < 1e-9
    assert np.max(np.hypot(dx, dz)[interior]) < 1e-9
    np.save(ROOT / f'candidate_{amount}m.npy', candidate.astype(np.float32))
    reports.append(dict(lowering_m=amount,changed_samples=int(np.count_nonzero(delta)),
                        protected_changed=int(np.count_nonzero(delta[core])),
                        accepted_changed=int(np.count_nonzero(delta[accepted])),
                        plateau_slope_delta_max=float(np.max(np.hypot(dx, dz)[interior])),
                        plateau_samples=int(plateau.sum()),
                        min_height_m=float(candidate[valid].min()),
                        max_height_m=float(candidate[valid].max())))
    fields.append(candidate)
np.save(ROOT / 'current.npy', current)
np.save(ROOT / 'weight.npy', weight.astype(np.float32))
record = dict(source='WholeMapGradient800/After verified against all 58 live terrains',
              extent_xz=meta['extent_xz'],spacing_m=step,scene_modified=False,
              approach_width_m=800,guard_m=24,foot_height_band_above_base_m=400,
              southern_apron_height_band_world_y_m=[-450,0],candidates=reports)
(ROOT / 'verification.json').write_text(json.dumps(record, indent=2))
print(json.dumps(record), flush=True)

cmap = LinearSegmentedColormap.from_list('sandstone', ['#94724f', '#c29a65', '#b89876', '#d7c8a8'])
light = LightSource(315, 40)
titles = ['当前山体', '主体下移 120 米', '主体下移 200 米']
xa, xb, za, zb = -3600, 650, -300, 2080
ix = np.arange(np.searchsorted(xs, xa), np.searchsorted(xs, xb), 5)
iz = np.arange(np.searchsorted(zs, za), np.searchsorted(zs, zb), 5)
X, Z = np.meshgrid(xs[ix], zs[iz])
fig = plt.figure(figsize=(18, 7), layout='constrained')
for col, (h, title) in enumerate(zip(fields, titles)):
    ax = fig.add_subplot(1, 3, col + 1, projection='3d')
    Y = h[np.ix_(iz, ix)]
    colors = light.shade(Y, cmap, vmin=-710, vmax=290, dx=step*5, dy=step*5, vert_exag=1, blend_mode='soft')
    ax.plot_surface(X, Z, Y, facecolors=colors, rstride=1, cstride=1, shade=False,
                    linewidth=0, antialiased=False, rasterized=True)
    ax.view_init(40, -65)
    ax.set_box_aspect((xb-xa, zb-za, 1000))
    ax.set_xlim(xa, xb)
    ax.set_ylim(za, zb)
    ax.set_zlim(-710, 290)
    ax.set_axis_off()
    ax.set_title(title, fontsize=16)
fig.suptitle('北侧山体降低候选 · 真实高宽比例 · 相同视角与光照', fontsize=19)
fig.text(.5, .025, '离线高度预览，不含 Unity 材质    主体保持起伏，山脚渐变衔接，中心山区固定', ha='center', fontsize=12)
fig.savefig(ROOT / 'comparison_3d.png', dpi=160)
plt.close(fig)

fig, axes = plt.subplots(1, 2, figsize=(14, 7), layout='constrained')
crop = current[np.ix_(iz, ix)]
rgb = light.shade(crop, cmap, vmin=-710, vmax=290, dx=step*5, dy=step*5, vert_exag=1, blend_mode='soft')
axes[0].imshow(rgb, origin='lower', extent=[xs[ix[0]], xs[ix[-1]], zs[iz[0]], zs[iz[-1]]])
axes[0].contour(X, Z, weight[np.ix_(iz, ix)], [.01, .999], colors=['#ffc85c', '#ef7557'], linewidths=1.5)
axes[0].contour(X, Z, core[np.ix_(iz, ix)], [.5], colors=['#21e4ce'], linewidths=1.2)
profile_x = -1350
axes[0].axvline(profile_x, color='white', lw=1, ls=':')
axes[0].set_title('红线内为等量下移区域，黄线为 1% 权重轮廓\n青线为中心保护区，白线为剖面位置')
axes[0].set_xlabel('Unity X / 米')
axes[0].set_ylabel('Unity Z / 米')
j = np.argmin(abs(xs-profile_x))
k = np.flatnonzero((zs >= -200) & (zs <= 2080))
for h, title, color in zip(fields, titles, ['#5e6267', '#ce8b29', '#bc4b36']):
    axes[1].plot(zs[k], h[k,j], label=title, color=color)
axes[1].set_title('同一位置的南北剖面')
axes[1].set_xlabel('Unity Z / 米')
axes[1].set_ylabel('世界高度 / 米')
axes[1].grid(alpha=.2)
axes[1].legend()
fig.savefig(ROOT / 'region_and_profile.png', dpi=150)
plt.close(fig)
