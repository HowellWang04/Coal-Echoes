from pathlib import Path
import json
import sys

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT.parent / 'WholeMapGradient800'
sys.path.insert(0, str(ROOT.parents[1] / 'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from scipy.ndimage import map_coordinates, distance_transform_edt
from matplotlib.path import Path as Polygon

meta = json.loads((SOURCE / 'metadata.json').read_text())
core = Polygon(meta['protected_polygon'])
accepted = np.load(SOURCE / 'accepted.npy').astype(np.float32)
delta = np.load(ROOT / 'repair_delta_m.npy') / 1000
distance = distance_transform_edt(delta == 0) * 2
rows = []
for t in json.loads((ROOT / 'before.json').read_text())['terrains']:
    name = t['name']; n = t['res']; a = t['alpha']
    x, z = np.meshgrid(t['p'][0] + np.arange(n) * 1000 / (n-1), t['p'][2] + np.arange(n) * 1000 / (n-1))
    coords = [(z-meta['extent_xz'][2])/2, (x-meta['extent_xz'][0])/2]
    actual = np.fromfile(ROOT / 'Prepared' / (name+'.delta.f32'), np.float32).reshape(n,n)
    expected = map_coordinates(delta, coords, order=1, mode='constant', cval=0)
    error = float(abs(actual-expected).max()*1000)
    assert error < .016, (name,error)
    protect = core.contains_points(np.c_[x.ravel(),z.ravel()]).reshape(n,n)
    keep = map_coordinates(accepted,coords,order=1,mode='constant',cval=0)>0
    assert np.all(actual[protect | keep] == 0), name
    assert actual.max() <= 0, name
    old = np.fromfile(ROOT / 'Before' / (name+'.height.f32'), np.float32).reshape(n,n)
    new = old+actual
    assert new.min() >= 0 and new.max() <= 1, name
    assert np.array_equal(new[actual==0],old[actual==0]), name
    if not np.any(actual):
        continue
    zi,xi = np.where(actual != 0)
    ax,az = np.meshgrid(t['p'][0]+(np.arange(a)+.5)*1000/a,t['p'][2]+(np.arange(a)+.5)*1000/a)
    ac = [(az-meta['extent_xz'][2])/2,(ax-meta['extent_xz'][0])/2]
    mask = map_coordinates(distance,ac,order=1,mode='constant',cval=1e9)<=max(3*1000/(n-1),6)
    mask &= ~core.contains_points(np.c_[ax.ravel(),az.ravel()]).reshape(a,a)
    mask &= map_coordinates(accepted,ac,order=1,mode='constant',cval=0)==0
    mask.astype('u1').tofile(ROOT/'Prepared'/(name+'.paint.u8'))
    new.astype('f4').tofile(ROOT/'Prepared'/(name+'.height.f32'))
    # No new near-vertical edges may be introduced by the lowering.
    limit = 1000/(n-1)*np.tan(np.radians(85))
    old_count=sum(int(np.count_nonzero(abs(np.diff(old.astype(float)*1000,axis=i))>limit)) for i in [0,1])
    new_count=sum(int(np.count_nonzero(abs(np.diff(new.astype(float)*1000,axis=i))>limit)) for i in [0,1])
    assert new_count <= old_count, (name,old_count,new_count)
    rows.append(dict(name=name,changed=int(np.count_nonzero(actual)),sample_error_m=error,
                     delta_min_m=float(actual.min()*1000),delta_max_m=float(actual.max()*1000),
                     height_rect=[int(xi.min()),int(zi.min()),int(xi.max()+1),int(zi.max()+1)],
                     paint_pixels=int(mask.sum()),old_edges_above_85=old_count,new_edges_above_85=new_count))
(ROOT/'prepared.json').write_text(json.dumps(rows,indent=2))
print(json.dumps(dict(terrains=len(rows),max_sample_error_m=max(r['sample_error_m'] for r in rows),
                     max_lowering_m=-min(r['delta_min_m'] for r in rows)),indent=2))
