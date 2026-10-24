"""Rebuild a seamless scan texture as a larger seamless texture whose repeat is longer than
any guardhouse wall, so its stains and trowel marks no longer line up into a grid.

The output is a toroidal grid of cells; each cell copies a random offset of the (already
seamless) source, and neighbouring cells cross-fade over a narrow margin. Colour, normal and
roughness use the same cells so they stay registered. Normals are blended as vectors.

Usage: python break_tiling.py <scan_id> <source_tile_metres> <repeats>
  e.g. python break_tiling.py concrete_wall_007 2.16 3
Writes <scan_id>_BaseColor.jpg, _Normal.jpg and _HDRP_Mask.png (4096 px, covering
source_tile_metres * repeats) into the Unity folder. For concrete_wall_007 the colour is
weathered to 2026 first, and a clean 1980 copy goes to the Group Photo (see wall_eras.py).
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parents[1]
UNITY = HERE.parents[2] / 'Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/PolyHaven'
GROUP_PHOTO = HERE.parents[1] / 'Props/GroupPhoto/CleanGate'
OUT = 4096
CELLS = 8          # cells per side, each about 0.8 m on the wall
MARGIN = 72        # cross-fade width in output pixels
FLATTEN = .2       # share of the large-scale colour variation removed

scan_id, tile_m, repeats = sys.argv[1], float(sys.argv[2]), int(sys.argv[3])
src_px = round(OUT / repeats)  # source tile resampled so texel density is uniform in the output
rng = np.random.default_rng(sum(map(ord, scan_id)))  # fixed per scan so reruns match


def load(name, mode):
    img = Image.open(HERE / scan_id / f'{scan_id}_{name}_2k.jpg').convert(mode)
    return np.asarray(img.resize((src_px, src_px), Image.LANCZOS), np.float32) / 255


color = load('diff', 'RGB')
normal = load('nor_gl', 'RGB') * 2 - 1
rough = load('rough', 'L')[:, :, None]

# Large blotches are what the eye recognises when they come back, so soften the low
# frequencies of the colour toward the mean before reusing the scan (blur wraps, staying seamless).
def wrap_blur(img, sigma):
    k = np.fft.fftfreq(img.shape[0])
    g = np.exp(-2 * (np.pi * sigma) ** 2 * (k[:, None] ** 2 + k[None, :] ** 2))
    return np.real(np.fft.ifft2(np.fft.fft2(img, axes=(0, 1)) * g[:, :, None], axes=(0, 1)))

low = wrap_blur(color, src_px / 40)
color = color - FLATTEN * (low - color.mean(axis=(0, 1)))

cell = OUT // CELLS
size = cell + 2 * MARGIN
# Weight of a cell: 1 inside, smooth falloff across the margin on each side.
ramp = np.ones(size, np.float32)
t = (np.arange(MARGIN * 2, dtype=np.float32) + .5) / (MARGIN * 2)
ramp[:MARGIN * 2] = t * t * (3 - 2 * t)
ramp[-MARGIN * 2:] = ramp[:MARGIN * 2][::-1]
weight = (ramp[:, None] * ramp[None, :])[:, :, None]

acc = {k: np.zeros((OUT, OUT, 3 if k != 'rough' else 1), np.float32) for k in ('color', 'normal', 'rough')}
wsum = np.zeros((OUT, OUT, 1), np.float32)
for cy in range(CELLS):
    for cx in range(CELLS):
        oy, ox = rng.integers(0, src_px, 2)
        flip_y, flip_x = rng.integers(0, 2, 2).astype(bool)
        # Mirrored cells give each stain four looks; the normal's x or y flips with the image.
        ys = (oy + (-1 if flip_y else 1) * np.arange(size)) % src_px
        xs = (ox + (-1 if flip_x else 1) * np.arange(size)) % src_px
        sign = np.array([-1 if flip_x else 1, -1 if flip_y else 1, 1], np.float32)
        ty = (np.arange(size) + cy * cell - MARGIN) % OUT
        tx = (np.arange(size) + cx * cell - MARGIN) % OUT
        iy, ix = np.ix_(ty, tx)
        sy, sx = np.ix_(ys, xs)
        acc['color'][iy, ix] += color[sy, sx] * weight
        acc['normal'][iy, ix] += normal[sy, sx] * sign * weight
        acc['rough'][iy, ix] += rough[sy, sx] * weight
        wsum[iy, ix] += weight

color = acc['color'] / wsum
normal = acc['normal'] / wsum
normal /= np.linalg.norm(normal, axis=2, keepdims=True)
rough = acc['rough'] / wsum

if scan_id == 'concrete_wall_007':
    # The game shows this wall in 2026; the Group Photo render swaps in the 1980 copy by name.
    import wall_eras
    clean = Image.fromarray((wall_eras.clean(color, tile_m * repeats) * 255 + .5).astype(np.uint8))
    clean.resize((2048, 2048), Image.LANCZOS).save(GROUP_PHOTO / f'{scan_id}_BaseColor.png')
    color = wall_eras.aged(color, tile_m * repeats)

UNITY.mkdir(parents=True, exist_ok=True)
# JPG keeps the 4k colour and normal maps a few MB each; the mask needs alpha, so it stays PNG.
Image.fromarray((np.clip(color, 0, 1) * 255 + .5).astype(np.uint8)).save(UNITY / f'{scan_id}_BaseColor.jpg', quality=92)
Image.fromarray(((normal * .5 + .5) * 255 + .5).astype(np.uint8)).save(UNITY / f'{scan_id}_Normal.jpg', quality=95)
mask = np.ones((OUT, OUT, 4), np.float32)
mask[:, :, 0] = 0                 # metallic
mask[:, :, 3] = 1 - rough[:, :, 0]  # smoothness
Image.fromarray((mask * 255 + .5).astype(np.uint8), 'RGBA').save(UNITY / f'{scan_id}_HDRP_Mask.png')
print(scan_id, 'repeat', round(tile_m * repeats, 2), 'm at', round(OUT / (tile_m * repeats)), 'px/m')
