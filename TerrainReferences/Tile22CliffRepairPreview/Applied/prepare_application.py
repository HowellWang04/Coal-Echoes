from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parents[2]/'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from scipy.ndimage import map_coordinates,distance_transform_edt
from matplotlib.path import Path as Polygon
p=ROOT
meta=json.loads((ROOT.parent/'metadata.json').read_text())
delta=np.load(ROOT.parent/'repair_delta_m.npy')/1000
distance=distance_transform_edt(delta==0)*2
core=Polygon(meta['protected_polygon'])
accepted=np.load(ROOT.parent/'accepted.npy').astype(np.float32)
crests=np.load(ROOT.parent/'crests.npy').astype(np.float32)
report=[]
for t in json.loads((p/'before.json').read_text())['terrains']:
    f=p/'Prepared'/f"{t['name']}.delta.f32"
    if not f.exists():continue
    n=t['res']; x=t['p'][0]+np.arange(n)*1000/(n-1);z=t['p'][2]+np.arange(n)*1000/(n-1)
    X,Z=np.meshgrid(x,z)
    expected=map_coordinates(delta,[(Z-meta["extent_xz"][2])/2,(X-meta["extent_xz"][0])/2],order=1,mode='constant',cval=0)
    actual=np.fromfile(f,np.float32).reshape(n,n)
    protect=core.contains_points(np.c_[X.ravel(),Z.ravel()]).reshape(n,n)
    error=float(abs(actual-expected).max()*1000)
    # Unity's float transforms round the local origin slightly at this extent.
    # Require the independent world-coordinate reference to agree within one native height quantum.
    assert error<1000/65535,(t['name'],error)
    assert np.all(actual[protect]==0)
    accept=map_coordinates(accepted,[(Z-meta['extent_xz'][2])/2,(X-meta['extent_xz'][0])/2],order=1,mode='constant',cval=0)>0
    assert np.all(actual[accept]==0)
    crest=map_coordinates(crests,[(Z-meta['extent_xz'][2])/2,(X-meta['extent_xz'][0])/2],order=1,mode='constant',cval=0)>0
    assert np.all(actual[crest]==0)
    old=np.fromfile(p/'Before'/f"{t['name']}.height.f32",np.float32).reshape(n,n)
    new=old+actual
    assert new.min()>=0 and new.max()<=1
    preview=np.fromfile(ROOT.parent/'NativePreview'/f"{t['name']}.f32",np.float32).reshape(n,n)
    assert float(abs(new-preview).max()*1000)<.016,t['name']
    changed=actual!=0
    if not changed.any():continue
    zi,xi=np.where(changed)
    a=t['alpha'];ax,az=np.meshgrid(t['p'][0]+(np.arange(a)+.5)*1000/a,t['p'][2]+(np.arange(a)+.5)*1000/a)
    padding=max(3*1000/(n-1),6)
    mask=map_coordinates(distance,[(az-meta["extent_xz"][2])/2,(ax-meta["extent_xz"][0])/2],order=1,mode='constant',cval=1e9)<=padding
    mask &= ~core.contains_points(np.c_[ax.ravel(),az.ravel()]).reshape(a,a)
    mask &= map_coordinates(accepted,[(az-meta['extent_xz'][2])/2,(ax-meta['extent_xz'][0])/2],order=1,mode='constant',cval=0)==0
    mask &= map_coordinates(crests,[(az-meta['extent_xz'][2])/2,(ax-meta['extent_xz'][0])/2],order=1,mode='constant',cval=0)==0
    mask.astype('u1').tofile(p/'Prepared'/f"{t['name']}.paint.u8")
    new.astype('f4').tofile(p/'Prepared'/f"{t['name']}.height.f32")
    row=dict(name=t['name'],changed=int(changed.sum()),delta_min_m=float(actual.min()*1000),delta_max_m=float(actual.max()*1000),sample_error_m=error,core_changed=0,height_rect=[int(xi.min()),int(zi.min()),int(xi.max()+1),int(zi.max()+1)],paint_pixels=int(mask.sum()))
    report.append(row)
(p/'prepared.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
