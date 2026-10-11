"""Bake the repaired surface, retaining fine DEM relief and the factory exactly."""
from pathlib import Path
import sys, os, json
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[1]
sys.path.insert(0,str(PROJECT/'Library/GradientTerrainExampleTools/python-deps'))
sys.path.insert(0,str(ROOT.parent/'NativeDEMOnly'))
import sources as dem
import numpy as np
from scipy.ndimage import map_coordinates, binary_dilation

factory=json.loads((ROOT/'factory.json').read_text())
before=json.loads((ROOT/'before.json').read_text(encoding='utf-8-sig'))
candidate=np.load(ROOT/'candidate.npy')
blend=np.load(ROOT/'blend.npy')
native_coarse=np.load(ROOT/'native.npy')
weights=[np.load(ROOT/f'weight_{i}.npy') for i in range(4)]
old=np.fromfile(ROOT/'Before/main_height.f32',np.float32).reshape(6209,6209)
atlas=old.copy()
sources=dem.load_sources()
n=6209;spacing=1000/1024
xs=-4955.25049+np.arange(n)*spacing
zs=-3945.249939+np.arange(n)*spacing
coverage=np.zeros((n,n),bool)
for row in range(0,n,64):
    zz=zs[row:row+64,None];xx=xs[None,:]
    coords=np.broadcast_arrays((zz+3986)/2,(xx+4996.00049)/2)
    get=lambda a:map_coordinates(a,coords,order=1,mode='nearest')
    fade=get(blend)
    fade[np.hypot(xx-factory['x'],zz-factory['z'])<=factory['radius']+1]=0
    if not np.any(fade):continue
    native=np.zeros_like(fade);total=np.zeros_like(fade)
    for w,h in zip(weights,dem.fields(sources,xx,zz)):
        weight=get(w);weight[~np.isfinite(h)]=0
        native+=np.nan_to_num(h)*weight;total+=weight
    native=np.divide(native,total,out=np.zeros_like(native),where=total>0)
    target=get(candidate)+native-get(native_coarse)
    result=(old[row:row+64]*1100-710)*(1-fade)+target*fade
    normalized=((result+710)/1100).astype(np.float32)
    active=fade>0
    atlas[row:row+64][active]=normalized[active]
    coverage[row:row+64]=active
    if row%1024==0:print('rows',row,n,flush=True)
del sources
assert np.isfinite(atlas).all() and atlas.min()>=0 and atlas.max()<=1
(ROOT/'Prepared').mkdir(exist_ok=True)
atlas.tofile(ROOT/'Prepared/main_height.f32')
atlas[::8,::8].copy().tofile(ROOT/'Prepared/draft_height.f32')
records=[]
for t in before['terrains']:
    name=t['name'];n=t['res'];a=t['alpha'];px,py,pz=t['p']
    step=1 if n==1025 else 8
    ix=round((px+4955.25049)/spacing);iz=round((pz+3945.249939)/spacing)
    new=atlas[iz:iz+1025:step,ix:ix+1025:step].copy()
    baseline=old[iz:iz+1025:step,ix:ix+1025:step]
    current=np.fromfile(ROOT/'Before'/f'{name}.height.f32',np.float32).reshape(n,n)
    changed=new!=baseline
    # Do not re-quantize existing terrain in the factory or outside this repair.
    new[~changed]=current[~changed]
    hx,hz=np.meshgrid(px+np.arange(n)*1000/(n-1),pz+np.arange(n)*1000/(n-1))
    assert np.array_equal(new[np.hypot(hx-factory['x'],hz-factory['z'])<=factory['radius']],current[np.hypot(hx-factory['x'],hz-factory['z'])<=factory['radius']])
    new.tofile(ROOT/'Prepared'/f'{name}.height.f32')
    # Recompute local slope/cavity materials, with a sampling halo.
    paint=binary_dilation(changed,iterations=max(1,int(np.ceil(32/(1000/(n-1))))))
    ax,az=np.meshgrid(px+(np.arange(a)+.5)*1000/a,pz+(np.arange(a)+.5)*1000/a)
    paint=map_coordinates(paint.astype(float),[(az-pz)*(n-1)/1000,(ax-px)*(n-1)/1000],order=0)>0
    paint &= np.hypot(ax-factory['x'],az-factory['z'])>factory['radius']+1
    paint &= map_coordinates(blend,[(az+3986)/2,(ax+4996.00049)/2],order=1,mode='nearest')>0
    paint.astype('u1').tofile(ROOT/'Prepared'/f'{name}.paint.u8')
    delta=(new-current)*1100
    records.append(dict(name=name,changed=int(np.count_nonzero(changed)),paint_pixels=int(paint.sum()),max_raise_m=float(delta.max()),max_lower_m=float(delta.min())))
(ROOT/'prepared.json').write_text(json.dumps(records,indent=2))
report=dict(changed_terrains=sum(r['changed']>0 for r in records),max_raise_m=max(r['max_raise_m'] for r in records),max_lower_m=min(r['max_lower_m'] for r in records),factory=factory,former_core_fixed=False)
(ROOT/'atlas.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report),flush=True)
