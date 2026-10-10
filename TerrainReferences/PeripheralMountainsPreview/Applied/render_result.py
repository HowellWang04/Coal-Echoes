from pathlib import Path
import json
import numpy as np
import sys
P=Path(__file__).parent
sys.path.insert(0,str(P.parent))
import build_preview as preview
data=json.loads((P/'before.json').read_text(encoding='utf-8-sig'))
xs=np.linspace(-4924,1076,1501,dtype=np.float32)
zs=np.linspace(2086,-3914,1501,dtype=np.float32)
height=np.full((1501,1501),np.nan,dtype=np.float32)
for tile in data['terrains']:
    if not tile['active']:continue
    x,y,z=tile['p'];sx,sy,sz=tile['size'];n=tile['res']
    ix=np.where((xs>=x-.01)&(xs<=x+sx+.01))[0]
    iz=np.where((zs>=z-.01)&(zs<=z+sz+.01))[0]
    a=np.fromfile(P/'After'/(tile['name']+'.f32'),dtype='<f4').reshape(n,n)
    height[np.ix_(iz,ix)]=preview.sample(a,(xs[ix][None,:]-x)/sx*(n-1),(zs[iz][:,None]-z)/sz*(n-1))*sy+y
assert np.isfinite(height).all()
mask=np.memmap(P/'import_mask.f32',dtype='<f4',mode='r',shape=(6144,6144))
mask=preview.sample(mask,(xs[None,:]+4924.00049)*6144/6000,(zs[:,None]+3914)*6144/6000)
original=np.load(P.parent/'current_height_4m.npy')
preview.OUT=P
preview.render(original,height,[mask],applied=True)
np.save(P/'applied_height_4m.npy',height)
