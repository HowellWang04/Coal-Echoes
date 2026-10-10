"""Read-only reproduction of near-vertical height steps inside Tile 2,2."""
from pathlib import Path
import sys
import json
ROOT = Path(__file__).resolve().parent
HEIGHTS = Path(sys.argv[1]).resolve() if len(sys.argv)>1 else ROOT
sys.path.insert(0, str(ROOT.parents[1] / 'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np

survey = json.loads((ROOT / 'snapshot.json').read_text(encoding='utf-8-sig'))
terrains = {t['name']: t for t in survey['terrains']}
fields = {name: np.fromfile(HEIGHTS / (name + '.f32'), np.float32).reshape(t['res'], t['res']).astype(float) * t['size'][1] + t['p'][1] for name, t in terrains.items()}
name = 'Tile_2_2_Main'
t = terrains[name]
h = fields[name]
spacing = t['size'][0] / (t['res'] - 1)
records = []
for axis in [0, 1]:
    jumps = np.abs(np.diff(h, axis=axis))
    iz, ix = np.unravel_index(np.argmax(jumps), jumps.shape)
    iz2, ix2 = iz + (axis == 0), ix + (axis == 1)
    records.append(dict(axis='Z' if axis == 0 else 'X', jump_m=float(jumps[iz, ix]),
                        slope_deg=float(np.degrees(np.arctan(jumps[iz, ix] / spacing))),
                        indices=[[int(iz), int(ix)], [int(iz2), int(ix2)]],
                        heights_m=[float(h[iz, ix]), float(h[iz2, ix2])],
                        world_xz=[float(t['p'][0] + ix * spacing), float(t['p'][2] + iz * spacing)],
                        count_above_85_deg=int(np.count_nonzero(jumps > spacing * np.tan(np.radians(85))))))
seams = dict(west=float(abs(h[:, 0] - fields['Tile_1_2_Main'][:, -1]).max()),
             east=float(abs(h[:, -1] - fields['Tile_3_2_Main'][:, 0]).max()),
             south=float(abs(h[0, :] - fields['Tile_2_1_Main'][-1, :]).max()))
report = dict(spacing_m=spacing,steps=records,seams_m=seams,
              passes_no_internal_vertical_steps=all(r['slope_deg'] < 85 for r in records))
(HEIGHTS / 'reproduction.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
assert report['passes_no_internal_vertical_steps'], 'Tile 2,2 contains near-vertical internal height steps'
