"""Extend the accepted gradient-domain terrain transition without touching its fixed region."""
from pathlib import Path
import os,sys,json
SOURCE=Path(__file__).resolve().parent
ROOT=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else SOURCE
ROOT.mkdir(parents=True,exist_ok=True)
PROJECT=SOURCE.parents[1]
sys.path.insert(0,str(PROJECT/'Library/GradientTerrainExampleTools/python-deps'))
sys.path.insert(0,str(SOURCE.parent/'PeripheralMountainsPreview'))
os.environ['OPENBLAS_NUM_THREADS']='1'
import numpy as np
from scipy.ndimage import distance_transform_edt,map_coordinates
from PIL import Image
from build_preview import CORE,PIECES,sample,smooth

N=3073; STEP=2.; X0=-4996.00049;Z0=-3986.
xs=X0+np.arange(N)*STEP;zs=Z0+np.arange(N)*STEP
x=xs[None,:];z=zs[:,None];shape=(N,N)
current=np.full(shape,np.nan,np.float32);base=current.copy()
survey=json.loads((SOURCE/'before.json').read_text())
for t in sorted(survey['terrains'],key=lambda t:t['res']):
    px,py,pz=t['p'];sx,sy,sz=t['size'];n=t['res']
    ix=np.flatnonzero((xs>=px-.001)&(xs<=px+sx+.001));iz=np.flatnonzero((zs>=pz-.001)&(zs<=pz+sz+.001))
    if not len(ix) or not len(iz):continue
    for dest,path in [(current,SOURCE/'Before'/f"{t['name']}.height.f32"),(base,SOURCE.parent/'PeripheralMountainsPreview/Applied/Before'/f"{t['name']}.f32")]:
        a=np.fromfile(path,np.float32).reshape(n,n)
        dest[np.ix_(iz,ix)]=sample(a,(xs[ix][None,:]-px)/sx*(n-1),(zs[iz][:,None]-pz)/sz*(n-1))*sy+py
valid=np.isfinite(current)
indices=distance_transform_edt(~valid,return_distances=False,return_indices=True)
current[~valid]=current[tuple(indices[:,~valid])];base[~valid]=base[tuple(indices[:,~valid])]
del indices
inside=np.zeros(shape,bool);distance=np.full(shape,1e9,np.float32)
for p,q in zip(CORE,CORE[1:]+CORE[:1]):
    vx,vz=q[0]-p[0],q[1]-p[1]
    t=np.clip(((x-p[0])*vx+(z-p[1])*vz)/(vx*vx+vz*vz),0,1)
    distance=np.minimum(distance,np.hypot(x-p[0]-t*vx,z-p[1]-t*vz)).astype(np.float32)
    if vz:inside ^= ((p[1]>z)!=(q[1]>z))&(x<vx*(z-p[1])/vz+p[0])
Image.MAX_IMAGE_PIXELS=None
sources={};source_valid={};native_range=[float('inf'),float('-inf')]
for key in ['407','408']:
    with Image.open(f'C:/Users/Guanhao/Downloads/USGS_1M_11_x65y{key}_NV_Southern_D23.tif') as im:
        sources[key]=np.array(im.crop((4800,0,10012,10012)))
        nodata=float(im.tag_v2[42113])
        source_valid[key]=np.isfinite(sources[key])&(sources[key]!=nodata)
# Only use measured DEM where valid; the pre-montage terrain supplies the edge beyond DEM coverage.
original=np.empty(shape,np.float32);outer=[np.empty(shape,np.float32) for _ in PIECES];weights=[np.empty(shape,np.float32) for _ in PIECES];old_influence=np.zeros(shape,np.float32)
keep=smooth((distance-24)/800);keep[inside]=0
border=smooth((x+4924.00049)/450)*smooth((1075.99951-x)/550)*smooth((z+3914)/450)*smooth((2086-z)/950)
for row in range(0,N,64):
    zz=z[row:row+64];sl=np.s_[row:row+64]
    sx=x+4924.00049+4806;sy=4091.999939-zz
    native=sample(sources['408'],sx-4800-.5,sy-.5)-2073.599609375
    native_weight=smooth((10000-sx)/200)*smooth((sx-4900)/200)*smooth((sy-200)/200)*smooth((9800-sy)/200)
    # DEM validity comes from its NoData metadata, not Unity's world-height floor.
    measured=sample(source_valid['408'],sx-4800-.5,sy-.5)>1-1e-7
    used=np.broadcast_to(native_weight,native.shape)>0
    assert measured[used].all(), 'DEM coverage includes NoData; feather that data boundary before solving'
    if used.any():native_range=[min(native_range[0],float(native[used].min())),max(native_range[1],float(native[used].max()))]
    original[sl]=native*native_weight+base[sl]*(1-native_weight)
    remaining=np.ones((len(zz),N))
    for i,p in enumerate(PIECES):
        dx=x-p['center'][0];dz=zz-p['center'][1];c,s=np.cos(np.deg2rad(p['angle'])),np.sin(np.deg2rad(p['angle']))
        sx=(c*dx+s*dz)/p['scale']+p['source_center'][0];sy=-(-s*dx+c*dz)/p['scale']+p['source_center'][1]
        native=sample(sources[p['source']],sx-4800-.5,sy-.5)
        coverage=smooth((sx-4900)/600)*smooth((9700-sx)/400)*smooth((sy-200)/400)*smooth((9700-sy)/400)*(native>0)
        h=native*p['scale']+p['offset'];h=np.where(h>110,110+170*np.tanh((h-110)/170),h)
        outer[i][sl]=h
        radius=np.hypot(dx/p['radius'][0],dz/p['radius'][1])
        irregular=.035*np.sin(dx/310+dz/460)+.025*np.sin(dx/720-dz/280)
        # Match the original footprint and use 800m for its lateral gradient mixing as well.
        edge_m=(1-radius+irregular)*min(p['radius'])
        weights[i][sl]=smooth(edge_m/800)*coverage*keep[sl]*border[sl]
        old=smooth((1-radius+irregular)/.42)*coverage*border[sl]
        remaining*=1-old
    old_influence[sl]=1-remaining
    print('sources',row,N,flush=True) if row%512==0 else None
del sources,source_valid

# Preserve every part of the approved circle 1 footprint, plus a small sampling guard.
accepted=np.load(SOURCE.parent/'Circle1GradientPreview/region.npy')>0
accept=map_coordinates(accepted.astype(np.float32),np.broadcast_arrays((z+3834)/2,(x+4148)/2),order=1,mode='constant',cval=0)>0
accepted_distance=distance_transform_edt(~accept)*STEP
support=old_influence>.0001
outside_distance=distance_transform_edt(~support)*STEP
map_edge=np.minimum(np.minimum(x+4924.00049,1075.99951-x),np.minimum(z+3914,2086-z))
free=(outside_distance<200)&(distance>24)&~inside&valid&(map_edge>24)&(accepted_distance>24)
free[[0,-1],:]=False;free[:,[0,-1]]=False
fade=smooth((200-outside_distance)/160)*smooth((distance-24)/90)*smooth((map_edge-24)/120)*smooth((accepted_distance-24)/160)
# Hold the interior crests of the three existing outer mountain groups.
# Without these anchors the large connected Poisson domain can lose their elevation.
gz,gx=np.gradient(current,STEP);gentle=np.hypot(gx,gz)<np.tan(np.deg2rad(35))
crest=np.zeros(shape,bool);crest_stats=[]
for p in PIECES:
    part=(np.hypot((x-p['center'][0])/p['radius'][0],(z-p['center'][1])/p['radius'][1])<.85)&((current-base)>100)&free
    part &= (z<500)&(z> -2500) if p['id']=='A' else ((z< -2500) if p['id']=='B' else (z>650))
    peak=float(current[part].max())
    anchor=part&(current>peak-100)&gentle
    crest|=anchor
    crest_stats.append(dict(piece=p['id'],peak_m=peak,anchor_samples=int(anchor.sum())))
crest_distance=distance_transform_edt(~crest)*STEP
free &= crest_distance>24
fade*=smooth((crest_distance-24)/160)
fade[~free]=0
fields=[]
for axis in [1,0]:
    edge=lambda a:(a[:,1:]+a[:,:-1])*.5 if axis==1 else (a[1:,:]+a[:-1,:])*.5
    gradient=np.diff(original,axis=axis).astype(np.float64)
    for h,w in zip(outer,weights):
        ew=edge(w);gradient=gradient*(1-ew)+np.diff(h,axis=axis)*ew
    r=edge(fade)
    fields.append(((1-r)*np.diff(current,axis=axis)+r*gradient)/1000)
gx,gz=fields;divergence=np.zeros(shape,np.float64)
divergence[:,:-1]+=gx;divergence[:,1:]-=gx;divergence[:-1,:]+=gz;divergence[1:,:]-=gz
(ROOT/'Solver').mkdir(exist_ok=True)
free.astype('f4').tofile(ROOT/'Solver/alpha.f32');((current+710)/1000).astype('f4').tofile(ROOT/'Solver/altitude.f32');(divergence/4).astype('f4').tofile(ROOT/'Solver/laplacian.f32')
for name,a in [('current',current),('base',base),('original',original),('protected',inside),('accepted',accept),('crests',crest),('free',free),('fade',fade),('distance',distance),('valid',valid)]:np.save(ROOT/(name+'.npy'),a)
meta=dict(resolution=N,spacing_m=STEP,extent_xz=[X0,float(xs[-1]),Z0,float(zs[-1])],protected_polygon=CORE,width_m=800,guard_m=24,free_samples=int(free.sum()),core_samples=int(inside.sum()),accepted_samples=int(accept.sum()),crest_anchors=crest_stats,row_zero='south',height_units='Unity world Y metres',pieces=PIECES)
meta['native_guidance_world_range_m']=native_range
meta['native_validity']='TIFF NoData checked across every bilinear footprint with nonzero coverage; no Unity height cutoff'
(ROOT/'metadata.json').write_text(json.dumps(meta,indent=2))
print(json.dumps(meta),flush=True)
