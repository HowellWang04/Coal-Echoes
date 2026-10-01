"""Restore the native USGS DEM inside the 11 flat tower pads of the Assets RAW.

Uses the same sampling as prepare_ridge_extension.py. Writes the restored RAW and the
height delta in metres, after confirming that the RAW matches the native DEM outside the pads.
"""
import json
from pathlib import Path
import numpy as np
from PIL import Image

OUT = Path(__file__).parent
ROOT = OUT.parents[1]
old = json.loads((OUT / 'layout_config.json').read_text(encoding='utf-8-sig'))
selection = json.loads((OUT / 'ridge_extension_points.json').read_text(encoding='utf-8-sig'))
Image.MAX_IMAGE_PIXELS = None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    source = np.array(im.crop((4806, 2006, 10006, 8006)))
assert np.all(source > -999000)


def sample(x, z):
    ix = np.clip(np.asarray(x) + 4924.00049 - .5, 0, 5199)
    iz = np.clip(2085.999939 - np.asarray(z) - .5, 0, 5999)
    c = ix.astype(int)
    r = iz.astype(int)
    fx = ix - c
    fz = iz - r
    return ((source[r, c] * (1 - fx) + source[r, np.minimum(c + 1, 5199)] * fx) * (1 - fz) + (source[np.minimum(r + 1, 5999), c] * (1 - fx) + source[np.minimum(r + 1, 5999), np.minimum(c + 1, 5199)] * fx) * fz) - old['source_base_elevation'] - 710


n = 6144
step = 6000 / n
x = np.arange(n, dtype=np.float32) * step + old['world_origin_xz'][0]
z = x[::-1] - old['world_origin_xz'][0] + old['world_origin_xz'][1]
height = np.empty((n, n), np.float32)
for i, north in enumerate(z):
    height[i] = sample(x, north)
# The factory grading was already removed from the Assets RAW by the foothill station task,
# so outside the pads the current RAW should equal the native DEM.
native = np.rint(np.clip((height + 710) / 1000, 0, 65535 / 65536) * 65536).astype('<u2')
current = np.fromfile(ROOT / 'Assets/TerrainData/SheepRangeDEM/SheepRange_Height.raw', '<u2').reshape(n, n)
pad = np.zeros((n, n), bool)
for tx, tz in selection['points']:
    rows = np.where(abs(z - tz) < 36)[0]
    cols = np.where(abs(x - tx) < 36)[0]
    pad[np.ix_(rows, cols)] |= np.hypot(x[cols][None, :] - tx, z[rows][:, None] - tz) < 36
diff = np.abs(native.astype(np.int32) - current.astype(np.int32)) * 1000 / 65536
report = {
    'outside_pads_max_diff_m': float(diff[~pad].max()),
    'outside_pads_pixels_over_2cm': int((diff[~pad] > .02).sum()),
    'inside_pads_max_diff_m': float(diff[pad].max()),
    'inside_pads_mean_diff_m': float(diff[pad].mean()),
    'pads': len(selection['points']),
}
print(json.dumps(report, indent=2))
assert report['outside_pads_max_diff_m'] < .02, 'current RAW has other edits outside the pads'
restored = current.copy()
restored[pad] = native[pad]
restored.tofile(OUT / 'restore_tower_pads_height.raw')
# Float delta in metres for applying to live terrain, stored only around the pads.
np.save(OUT / 'restore_tower_pads_delta.npy', ((restored.astype(np.float32) - current.astype(np.float32)) * 1000 / 65536))
(OUT / 'restore_tower_pads.json').write_text(json.dumps(report, indent=2))
