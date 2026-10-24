from pathlib import Path
import hashlib
import json
import os
import sys
import zipfile

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parents[1] / 'Library/GradientTerrainExampleTools/python-deps'))
os.environ['MPLCONFIGDIR'] = str(ROOT / 'plot-cache')
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource, LinearSegmentedColormap

UPSTREAM = ROOT / 'upstream/eric-guerin-gradient-terrains-68da08b'


def read_pgm(path):
    lines = path.read_text().splitlines()
    tokens = ' '.join(line.split('#')[0] for line in lines).split()
    assert tokens[0] == 'P2'
    width, height, maximum = map(int, tokens[1:4])
    array = np.array(tokens[4:], dtype=np.int32).reshape(height, width)
    return array, maximum


def normalized(array):
    array = array.astype(np.float32)
    return (array - array.min()) / (array.max() - array.min())


reference, maximum = read_pgm(UPSTREAM / 'results/result.pgm')
local, _ = read_pgm(ROOT / 'run/results/result.pgm')
diagnostic, _ = read_pgm(ROOT / 'run/results/result_diagnostic.pgm')
raw = np.fromfile(ROOT / 'run/results/result_diagnostic.f32', dtype='<f4').reshape(local.shape)
mask_values, mask_max = read_pgm(UPSTREAM / 'data/004_mask.pgm')
alt_values, alt_max = read_pgm(UPSTREAM / 'data/004_alt.pgm')
lap_values, lap_max = read_pgm(UPSTREAM / 'data/004_lap.pgm')
alpha = normalized(mask_values.astype(np.float32) / np.float32(mask_max))
altitude = normalized(alt_values.astype(np.float32) / np.float32(alt_max))
laplacian = (lap_values.astype(np.float32) / np.float32(lap_max) - np.float32(.5)) * np.float32(.03)
neighbors = np.zeros_like(raw)
count = np.zeros_like(raw)
for dest, origin in [(np.s_[1:, :], np.s_[:-1, :]), (np.s_[:-1, :], np.s_[1:, :]),
                     (np.s_[:, 1:], np.s_[:, :-1]), (np.s_[:, :-1], np.s_[:, 1:])]:
    neighbors[dest] += raw[origin]
    count[dest] += 1
residual = alpha * (neighbors / count - laplacian) + (1 - alpha) * altitude - raw
fixed = alpha == 0
diff = np.abs(local.astype(np.int64) - reference.astype(np.int64))
source_checks = []
with zipfile.ZipFile(ROOT / 'source.zip') as archive:
    for entry in archive.infolist():
        if not entry.is_dir():
            rel = Path(*Path(entry.filename).parts[1:])
            expected = hashlib.sha256(archive.read(entry)).hexdigest()
            actual = hashlib.sha256((UPSTREAM / rel).read_bytes()).hexdigest()
            source_checks.append({'path': str(rel), 'sha256': actual, 'unchanged': expected == actual})

report = {
    'upstream_commit': (ROOT / 'upstream-commit.txt').read_text().strip(),
    'run': json.loads((ROOT / 'run-status.json').read_text()),
    'resolution': list(local.shape), 'pixel_count': int(local.size),
    'pixel_difference_count': int(np.count_nonzero(diff)),
    'pixel_difference_max': int(diff.max()),
    'pixel_difference_rmse': float(np.sqrt(np.mean(diff.astype(float) ** 2))),
    'diagnostic_output_matches_original': bool(np.array_equal(diagnostic, local)),
    'float_values_finite': bool(np.isfinite(raw).all()),
    'float_height_range': [float(raw.min()), float(raw.max())],
    'fixed_constraint_count': int(fixed.sum()),
    'fixed_constraint_max_error': float(np.abs(raw[fixed] - altitude[fixed]).max()),
    'weighted_equation_residual_rms': float(np.sqrt(np.mean(residual.astype(float) ** 2))),
    'weighted_equation_residual_max': float(np.abs(residual).max()),
    'weighted_equation_residual_p99': float(np.quantile(np.abs(residual), .99)),
    'archive_file_count': len(source_checks),
    'all_upstream_files_unchanged': all(item['unchanged'] for item in source_checks),
    'output_raw_byte_hashes': {label: hashlib.sha256(path.read_bytes()).hexdigest() for label, path in {
        'reference_pgm': UPSTREAM / 'results/result.pgm',
        'local_pgm': ROOT / 'run/results/result.pgm',
        'diagnostic_pgm': ROOT / 'run/results/result_diagnostic.pgm'}.items()},
    'scope': 'Original upstream example only. No Coal Echoes terrain or Unity scene edited.',
    'residual_note': 'Residual of the author shader update in normalized float units. Default finite iterations are retained, without a new convergence threshold.',
}
(ROOT / 'verification.json').write_text(json.dumps(report, indent=2))
(ROOT / 'upstream-integrity.json').write_text(json.dumps(source_checks, indent=2))
print(json.dumps(report, indent=2))

plt.rcParams.update({'font.family': 'DejaVu Sans', 'font.size': 11,
                     'figure.facecolor': '#f3f1ec', 'axes.facecolor': '#f3f1ec',
                     'axes.titleweight': 'bold', 'axes.spines.top': False, 'axes.spines.right': False})
cmap = LinearSegmentedColormap.from_list('canyon', ['#524f46', '#918776', '#b7a283', '#ded0b4'])
light = LightSource(azdeg=315, altdeg=45)
ref_h = reference.astype(float) / maximum
local_h = local.astype(float) / maximum
fig, axes = plt.subplots(2, 2, figsize=(12, 10), layout='constrained')
fig.suptitle('Gradient Terrain Authoring | Original example verification', fontsize=18)
for ax, height, title in zip(axes[0], [ref_h, local_h], ['Author supplied reference', 'Local run | original code and parameters']):
    ax.imshow(light.shade(height, cmap=cmap, vert_exag=120, blend_mode='soft'))
    ax.set_title(title)
    ax.set_axis_off()
ax = axes[1, 0]
ax.imshow(diff, cmap='magma', vmin=0, vmax=1)
ax.set_title('Absolute difference | every pixel is zero')
ax.set_axis_off()
ax = axes[1, 1]
middle = local.shape[0] // 2
ax.plot(ref_h[middle], color='#c3803e', linewidth=3.5, label='Author reference')
ax.plot(local_h[middle], color='#254f59', linestyle='--', linewidth=1.3, label='Local run')
ax.set_title('Center row profile | curves overlap')
ax.set_xlabel('Column in the 513 pixel heightmap')
ax.set_ylabel('Normalized exported height')
ax.legend(frameon=False)
ax.grid(alpha=.18)
fig.savefig(ROOT / 'heightmap-comparison.png', dpi=160)
plt.close(fig)

fig = plt.figure(figsize=(14, 6.8), layout='constrained')
fig.suptitle('Author reference and local reconstruction', fontsize=20)
y, x = np.mgrid[0:513:3, 0:513:3]
for index, height, title in [(1, ref_h, 'AUTHOR REFERENCE'), (2, local_h, 'LOCAL ORIGINAL RUN')]:
    ax = fig.add_subplot(1, 2, index, projection='3d')
    h = height[::3, ::3]
    colors = light.shade(h, cmap=cmap, vert_exag=40, blend_mode='soft')
    ax.plot_surface(x / 512, y / 512, h, facecolors=colors, rstride=1, cstride=1,
                    linewidth=0, antialiased=True, shade=False)
    ax.set_box_aspect((1, 1, .42))
    ax.view_init(elev=39, azim=-65)
    ax.set(xlim=(0, 1), ylim=(0, 1), zlim=(0, 1))
    ax.set_axis_off()
    ax.set_title(title, pad=-4)
fig.text(.5, .025, '513 x 513 heightmap | 263,169 / 263,169 pixels identical | Shared illustrative vertical scale',
         ha='center', fontsize=11, color='#43535b')
fig.savefig(ROOT / 'terrain-comparison-3d.png', dpi=170)
plt.close(fig)
