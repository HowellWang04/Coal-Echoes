from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from scipy.ndimage import map_coordinates
from matplotlib.path import Path as Polygon
p=ROOT/'Applied800'
meta=json.loads((ROOT/'metadata.json').read_text())
delta=np.fromfile(p/'Prepared/gradient_delta.f32',np.float32).reshape(1025,1025)
core=Polygon(meta['protected_polygon'])
for t in json.loads((p/'before.json').read_text())['terrains']:
    f=p/'Prepared'/f"{t['name']}.delta.f32"
    if not f.exists():continue
    n=t['res']; x=t['p'][0]+np.arange(n)*1000/(n-1);z=t['p'][2]+np.arange(n)*1000/(n-1)
    X,Z=np.meshgrid(x,z)
    expected=map_coordinates(delta,[(Z+3834)/2,(X+4148)/2],order=1,mode='constant',cval=0)
    actual=np.fromfile(f,np.float32).reshape(n,n)
    protect=core.contains_points(np.c_[X.ravel(),Z.ravel()]).reshape(n,n)
    print(t['name'], 'error m',float(abs(actual-expected).max()*1000),'rms',float(np.sqrt(np.mean((actual-expected)**2))*1000),'core',float(abs(actual[protect]).max(initial=0)*1000),'expected min',float(expected.min()*1000))
