"""Trace the measured step through saved construction stages and solver input."""
from pathlib import Path
import json
import sys
ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parents[1] / 'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from scipy.ndimage import map_coordinates

repro = json.loads((ROOT / 'reproduction.json').read_text())
t = next(t for t in json.loads((ROOT / 'snapshot.json').read_text())['terrains'] if t['name'] == 'Tile_2_2_Main')
paths = dict(before_peripheral=ROOT.parent/'PeripheralMountainsPreview/Applied/Before/Tile_2_2_Main.f32',
             after_peripheral=ROOT.parent/'Circle1GradientPreview/Applied800/Before/Tile_2_2_Main.height.f32',
             before_wholemap=ROOT.parent/'WholeMapGradient800/Before/Tile_2_2_Main.height.f32',
             after_wholemap=ROOT/'Tile_2_2_Main.f32')
report = {}
for stage, path in paths.items():
    h = np.fromfile(path, np.float32).reshape(1025,1025).astype(float)*1000-710
    records = []
    for point in repro['steps']:
        a,b = point['indices']
        records.append(dict(axis=point['axis'],heights_m=[h[tuple(a)],h[tuple(b)]],jump_m=float(abs(h[tuple(b)]-h[tuple(a)]))))
    report[stage]=dict(at_repro=records,max_internal_jump_m=max(float(abs(np.diff(h,axis=i)).max()) for i in [0,1]))
whole=ROOT.parent/'WholeMapGradient800'
meta=json.loads((whole/'metadata.json').read_text())
for field in ['current','candidate','base','original','free','crests','fade']:
    h=np.load(whole/(field+'.npy')).astype(float)
    entries=[]
    for point in repro['steps']:
        coords=np.array(point['indices'],float)
        coords[:,0]=(t['p'][2]+coords[:,0]*repro['spacing_m']-meta['extent_xz'][2])/2
        coords[:,1]=(t['p'][0]+coords[:,1]*repro['spacing_m']-meta['extent_xz'][0])/2
        values=map_coordinates(h,coords.T,order=1,mode='nearest')
        entries.append(dict(axis=point['axis'],values=values.tolist(),difference=float(values[1]-values[0])))
    report[field]=entries
(ROOT/'trace.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
