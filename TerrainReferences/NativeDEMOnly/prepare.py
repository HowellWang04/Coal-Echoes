"""Replace the procedural apron with surveyed DEMs and solve only their joins."""
import os
os.environ['OPENBLAS_NUM_THREADS']='1'
from sources import *
import json
from scipy.ndimage import distance_transform_edt
N=3073; STEP=2.; X0=-4996.00049; Z0=-3986.
x=X0+np.arange(N)[None,:]*STEP
z=Z0+np.arange(N)[:,None]*STEP
shape=(N,N)
before=json.loads((ROOT/'before.json').read_text(encoding='utf-8-sig'))
current=np.full(shape,np.nan,np.float32)
for t in sorted(before['terrains'],key=lambda t:t['res']):
    px,py,pz=t['p']; n=t['res']
    ix=np.flatnonzero((x[0]>=px-.001)&(x[0]<=px+1000+.001))
    iz=np.flatnonzero((z[:,0]>=pz-.001)&(z[:,0]<=pz+1000+.001))
    a=np.fromfile(ROOT/'Before'/f"{t['name']}.height.f32",np.float32).reshape(n,n)
    # A sub-millimetre coordinate discrepancy at tile corners is rounded to the edge.
    current[np.ix_(iz,ix)]=map_coordinates(a,np.broadcast_arrays(np.clip((z[iz]-pz)*(n-1)/1000,0,n-1),np.clip((x[:,ix]-px)*(n-1)/1000,0,n-1)),order=1,mode='nearest')*1000+py
valid=np.isfinite(current)
indices=distance_transform_edt(~valid,return_distances=False,return_indices=True)
current[~valid]=current[tuple(indices[:,~valid])]
del indices
inside=np.zeros(shape,bool); distance=np.full(shape,np.inf,np.float32)
for p,q in zip(CORE,CORE[1:]+CORE[:1]):
    vx,vz=q[0]-p[0],q[1]-p[1]
    t=np.clip(((x-p[0])*vx+(z-p[1])*vz)/(vx*vx+vz*vz),0,1)
    distance=np.minimum(distance,np.hypot(x-p[0]-t*vx,z-p[1]-t*vz)).astype(np.float32)
    if vz:inside^=((p[1]>z)!=(q[1]>z))&(x<vx*(z-p[1])/vz+p[0])
keep=smooth((distance-24)/800).astype(np.float32); keep[inside]=0
sources=load_sources()
hs=[np.empty(shape,np.float32) for _ in range(4)]
for row in range(0,N,64):
    for dest,field in zip(hs,fields(sources,x,z[row:row+64])):dest[row:row+64]=field
    if row%512==0:print('source rows',row,N,flush=True)
del sources
coverage=[]
for h in hs:
    measured=np.isfinite(h)
    # Fade to another measured source before any NoData or raster edge is reached.
    coverage.append(smooth(distance_transform_edt(measured)*STEP/450).astype(np.float32))
assert np.maximum.reduce(coverage).min()>0,'A boundary still has no measured source'
a=smooth((x+2100)/800)*smooth((z+3200)/800)*smooth((1400-z)/800)*coverage[1]
b=smooth((-z-2250)/800)*coverage[2]
c=smooth((z-350)/800)*coverage[3]
weights=[coverage[0]*(1-a)*(1-b)*(1-c),a*(1-b)*(1-c),b*(1-c),c]
total=sum(weights)
# Continue another measured fragment where the original center raster has NoData.
remaining=1-total
coverage_total=sum(coverage)
weights=[w+remaining*cov/coverage_total for w,cov in zip(weights,coverage)]
total=sum(weights)
assert total.min()>1e-5,'Source ownership has a gap'
weights=[(w/total).astype(np.float32) for w in weights]
for h,w in zip(hs,weights):assert np.isfinite(h[w>0]).all()
native=sum(np.nan_to_num(h)*w for h,w in zip(hs,weights)).astype(np.float32)
# The current field participates only beside the protected gameplay core.
# All fixed outer interiors and every map edge derive from measured data.
weights=[w*keep for w in weights]+[1-keep]
hs.append(current)
initial=sum(np.nan_to_num(h)*w for h,w in zip(hs,weights)).astype(np.float32)
initial[keep==0]=current[keep==0]
mix=1-np.maximum.reduce(weights)
free=(mix>1e-7)&(keep>0)
free[[0,-1],:]=False;free[:,[0,-1]]=False
div=np.zeros(shape,np.float64)
for axis in [1,0]:
    edge=lambda a:(a[:,1:]+a[:,:-1])*.5 if axis==1 else (a[1:,:]+a[:-1,:])*.5
    coverage_edge=edge(keep)
    native_g=sum(edge(w)*np.diff(np.nan_to_num(h),axis=axis) for w,h in zip(weights[:4],hs[:4]))
    # Current heights constrain the protected boundary, but the transition's
    # gradients come exclusively from measured DEMs, including the old apron.
    g=np.divide(native_g,coverage_edge,out=np.diff(current,axis=axis).copy(),where=coverage_edge>0)/1000
    if axis==1:div[:,:-1]+=g;div[:,1:]-=g
    else:div[:-1]+=g;div[1:]-=g
(ROOT/'Solver').mkdir(exist_ok=True)
free.astype('f4').tofile(ROOT/'Solver/alpha.f32')
((initial+710)/1000).astype('f4').tofile(ROOT/'Solver/altitude.f32')
(div/4).astype('f4').tofile(ROOT/'Solver/laplacian.f32')
for name,value in [('current',initial),('before',current),('free',free),('keep',keep),('core',inside),('native',native),('owner',np.argmax(weights,axis=0).astype(np.uint8)),('valid',valid)]:np.save(ROOT/(name+'.npy'),value)
for i,(h,w) in enumerate(zip(hs[:4],weights[:4])):
    np.save(ROOT/f'source_{i}.npy',h);np.save(ROOT/f'weight_{i}.npy',w)
meta=dict(resolution=N,spacing_m=STEP,extent_xz=[X0,float(x[0,-1]),Z0,float(z[-1,0])],protected_polygon=CORE,width_m=800,guard_m=24,free_samples=int(free.sum()),height_range_m=[float(initial.min()),float(initial.max())],source_weights=['Center','A','B','C','Protected current'],procedural_sources=0,northern_vertical_offset_m=-200,pieces=PIECES)
(ROOT/'metadata.json').write_text(json.dumps(meta,indent=2))
print(json.dumps(meta),flush=True)
