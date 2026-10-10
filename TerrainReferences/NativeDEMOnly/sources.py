"""The existing Sheep Range montage, sampled only inside valid GeoTIFF cells."""
from pathlib import Path
import sys
ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
sys.path.insert(0, str(PROJECT / 'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from scipy.ndimage import map_coordinates
from PIL import Image
Image.MAX_IMAGE_PIXELS = None
CORE = [(-4400,-2300),(-4220,650),(-3660,940),(-2750,870),(-2070,520),(-1660,-650),(-1560,-1810),(-1880,-2440),(-2920,-2540),(-3830,-2490)]
PIECES = [
    dict(id='A', source='407', source_center=[8200,3600], center=[-430,-680], scale=.70, angle=0, offset=-1580),
    dict(id='B', source='408', source_center=[6500,1550], center=[-2530,-3210], scale=.65, angle=90, offset=-1560),
    dict(id='C', source='407', source_center=[8300,7700], center=[-1820,1410], scale=.70, angle=70, offset=-1720),
]

def smooth(t):
    t = np.clip(t, 0, 1)
    return t*t*t*(t*(t*6-15)+10)

def load_sources():
    result = {}
    for key in ['406','407','408']:
        path=PROJECT/f'Library/NativeDEMOnlySources/USGS_1M_11_x65y{key}_NV_Southern_D23.tif' if key=='406' else Path(f'C:/Users/Guanhao/Downloads/USGS_1M_11_x65y{key}_NV_Southern_D23.tif')
        with Image.open(path) as im:
            a = np.array(im)
            a[(a == float(im.tag_v2[42113])) | ~np.isfinite(a)] = np.nan
            path=PROJECT/f'Library/NativeDEMOnlySources/USGS_1M_11_x66y{key}_NV_Southern_D23.tif'
            with Image.open(path) as neighbor:
                assert neighbor.tag_v2[33550] == im.tag_v2[33550]
                assert abs(neighbor.tag_v2[33922][3] - im.tag_v2[33922][3] - 10000) < .001
                assert abs(neighbor.tag_v2[33922][4] - im.tag_v2[33922][4]) < .001
                east=np.array(neighbor)
                east[(east==float(neighbor.tag_v2[42113])) | ~np.isfinite(east)] = np.nan
                # The nominal tiles overlap by 12 pixels. Use the same physical grid.
                a=np.concatenate([a[:,:10006],east[:,6:]],axis=1)
            result[key] = a
    return result

def sample(a, x, z):
    return map_coordinates(a, np.broadcast_arrays(z,x), order=1, mode='constant', cval=np.nan)

def dem_sample(sources,key,x,y):
    result=sample(sources[key],x,y)
    if key=='407':
        if np.any(y<6):result=np.where(y<6,sample(sources['408'],x,y+10000),result)
        if np.any(y>=10006):result=np.where(y>=10006,sample(sources['406'],x,y-10000),result)
    elif key=='408' and np.any(y>=10006):
        result=np.where(y>=10006,sample(sources['407'],x,y-10000),result)
    return result

def fields(sources, x, z):
    yield dem_sample(sources,'408', x+4924.00049+4806-.5,4091.999939-z-.5)-2073.599609375
    for p in PIECES:
        dx=x-p['center'][0]; dz=z-p['center'][1]
        c,s=np.cos(np.deg2rad(p['angle'])),np.sin(np.deg2rad(p['angle']))
        sx=(c*dx+s*dz)/p['scale']+p['source_center'][0]-.5
        sz=-(-s*dx+c*dz)/p['scale']+p['source_center'][1]-.5
        yield dem_sample(sources,p['source'],sx,sz)*p['scale']+p['offset']

if __name__ == '__main__':
    import json
    x=-4996.00049+np.arange(769)[None,:]*8
    z=-3986+np.arange(769)[:,None]*8
    hs=np.stack(list(fields(load_sources(),x,z)))
    valid=np.isfinite(hs)
    hole=~valid.any(axis=0)
    zz,xx=np.where(hole)
    report=dict(uncovered=int(hole.sum()),total=int(hole.size),ranges=[dict(source=k,valid=int(v.sum()),min=float(h[v].min()),max=float(h[v].max())) for k,h,v in zip(['Center','A','B','C'],hs,valid)])
    if hole.any(): report['uncovered_extent']=[float(x[0,xx].min()),float(x[0,xx].max()),float(z[zz,0].min()),float(z[zz,0].max())]
    print(json.dumps(report,indent=2))
    np.save(ROOT/'audit_fields.npy',hs)
