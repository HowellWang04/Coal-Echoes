from pathlib import Path
import json
import sys

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parents[2] / 'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np

survey = json.loads((ROOT / 'before.json').read_text())['terrains']
fields = {}
for t in survey:
    fields[t['name']] = tuple(np.fromfile(ROOT / folder / (t['name'] + '.height.f32'), np.float32)
                              .reshape(t['res'], t['res']).astype(float) * t['size'][1]
                              for folder in ['Before', 'After'])
old, new = fields['Tile_2_2_Main']
points = []
for record in json.loads((ROOT.parents[1] / 'Tile22CliffDiagnosis/reproduction.json').read_text())['steps']:
    a, b = (tuple(v) for v in record['indices'])
    points.append(dict(axis=record['axis'], before_m=abs(old[a] - old[b]), after_m=abs(new[a] - new[b])))
vertical = sum(int(np.count_nonzero(abs(np.diff(new, axis=i)) > (1000 / 1024) * np.tan(np.radians(85)))) for i in [0, 1])
assert vertical == 0
assert all(p['after_m'] < .32 for p in points)
lod = []
for t in survey:
    if not t['name'].endswith('_Main'):
        continue
    draft = t['name'].replace('_Main', '_Draft')
    a0, a1 = fields[t['name']]
    b0, b1 = fields[draft]
    mismatch = float(abs((a1-a0)[::8, ::8] - (b1-b0)).max())
    assert mismatch < .031, t['name']
    lod.append(dict(main=t['name'], draft=draft, correction_mismatch_m=mismatch))
report = dict(original_points=points, tile22_edges_above_85_degrees=vertical,
              max_lod_correction_mismatch_m=max(r['correction_mismatch_m'] for r in lod), lod=lod)
(ROOT / 'height-checks.json').write_text(json.dumps(report, indent=2))
print(json.dumps({k: v for k, v in report.items() if k != 'lod'}, indent=2))
