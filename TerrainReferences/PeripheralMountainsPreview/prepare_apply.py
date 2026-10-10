"""Prepare the approved peripheral terrain at source resolution, offline."""
from pathlib import Path
import json
import numpy as np
from PIL import Image
from build_preview import CORE, PIECES, smooth, sample

ROOT=Path(__file__).parent
OUT=ROOT/'Applied'
OUT.mkdir(exist_ok=True)
Image.MAX_IMAGE_PIXELS=None
before=json.loads((OUT/'before.json').read_text(encoding='utf-8-sig'))
sources={}
for name in ['407','408']:
    with Image.open(f'C:/Users/Guanhao/Downloads/USGS_1M_11_x65y{name}_NV_Southern_D23.tif') as im:
        sources[name]=np.array(im.crop((4800,0,10012,10012)))

def fields(x,z):
    shape=np.broadcast_shapes(x.shape,z.shape)
    inside=np.zeros(shape,dtype=bool)
    distance=np.full(shape,1e9)
    for p,q in zip(CORE,CORE[1:]+CORE[:1]):
        vx,vz=q[0]-p[0],q[1]-p[1]
        t=np.clip(((x-p[0])*vx+(z-p[1])*vz)/(vx*vx+vz*vz),0,1)
        distance=np.minimum(distance,np.hypot(x-p[0]-t*vx,z-p[1]-t*vz))
        if vz:
            inside ^= ((p[1]>z)!=(q[1]>z)) & (x<(q[0]-p[0])*(z-p[1])/vz+p[0])
    keep=smooth((distance-4)/356);keep[inside]=0
    # A zero border joins existing terrain beyond the preview square continuously.
    border=smooth((x+4924.00049)/450)*smooth((1075.99951-x)/550)*smooth((z+3914)/450)*smooth((2086-z)/950)
    remaining=np.ones(shape,dtype=np.float64);contribution=np.zeros(shape,dtype=np.float64)
    for p in PIECES:
        dx=x-p['center'][0];dz=z-p['center'][1]
        theta=np.deg2rad(p['angle']);c,s=np.cos(theta),np.sin(theta)
        sx=(c*dx+s*dz)/p['scale']+p['source_center'][0]
        sy=-(-s*dx+c*dz)/p['scale']+p['source_center'][1]
        source=sample(sources[p['source']],sx-4800-.5,sy-.5)
        coverage=smooth((sx-4900)/600)*smooth((9700-sx)/400)*smooth((sy-200)/400)*smooth((9700-sy)/400)
        radius=np.hypot(dx/p['radius'][0],dz/p['radius'][1])
        irregular=.035*np.sin(dx/310+dz/460)+.025*np.sin(dx/720-dz/280)
        w=smooth((1-radius+irregular)/.42)*keep*coverage*border*(source>0)
        target=(source*p['scale']+p['offset']+710)/1000
        # Fit only the highest new peaks into the existing 1000 m Terrain range.
        target=np.where(target>.82,.82+.17*np.tanh((target-.82)/.17),target)
        remaining*=1-w
        contribution=contribution*(1-w)+target*w
    alpha=1-remaining
    target=np.divide(contribution,alpha,out=np.zeros_like(contribution),where=alpha>1e-12)
    return target.astype('float32'),alpha.astype('float32'),inside

n=6144
height=np.memmap(OUT/'import_height.f32',dtype='<f4',mode='w+',shape=(n,n))
mask=np.memmap(OUT/'import_mask.f32',dtype='<f4',mode='w+',shape=(n,n))
xs=-4924.00049+np.arange(n)*6000/n
zs=-3914+np.arange(n)*6000/n
for row in range(0,n,48):
    hh,aa,_=fields(xs[None,:],zs[row:row+48,None])
    height[row:row+48]=hh;mask[row:row+48]=aa
height.flush();mask.flush()

def imported(x,z):
    return sample(height,(x+4924.00049)*n/6000,(z+3914)*n/6000),sample(mask,(x+4924.00049)*n/6000,(z+3914)*n/6000)

(OUT/'Prepared').mkdir(exist_ok=True)
stats=[]
for t in before['terrains']:
    nres=t['res'];px,py,pz=t['p'];sx,sy,sz=t['size']
    x=px+np.arange(nres)[None,:]*sx/(nres-1)
    z=pz+np.arange(nres)[:,None]*sz/(nres-1)
    target,alpha=imported(x,z)
    old=np.fromfile(OUT/'Before'/f"{t['name']}.f32",dtype='<f4').reshape(nres,nres)
    new=(old*(1-alpha)+target*alpha).astype('<f4')
    untouched=alpha==0
    new[untouched]=old[untouched]
    assert np.all((new>=0)&(new<=1)),t['name']
    count=int(np.count_nonzero(new!=old))
    if count:
        new.tofile(OUT/'Prepared'/f"{t['name']}.f32")
        alpha.astype('<f4').tofile(OUT/'Prepared'/f"{t['name']}.mask.f32")
    stats.append(dict(name=t['name'],changed_samples=count,max_change_m=float(abs(new-old).max()*1000),outside_mask_unchanged=bool(np.array_equal(new[untouched],old[untouched]))))
(OUT/'preparation.json').write_text(json.dumps(dict(resolution=6144,origin=[-4924.00049,-3914],size=6000,source_sample_m=1,import_row_order='south to north',edge_transition_m=dict(west=450,east=550,south=450,north=950),peak_compression_world_y_start=110,peak_compression_asymptote=280,protected_guard_m=4,tiles=stats),ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(changed_tiles=sum(t['changed_samples']>0 for t in stats),unchanged_tiles=sum(t['changed_samples']==0 for t in stats),max_change_m=max(t['max_change_m'] for t in stats))))
