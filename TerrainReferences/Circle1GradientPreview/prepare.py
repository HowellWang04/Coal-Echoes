"""Create offline, metre-referenced inputs for the author's Poisson solver."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
PREVIOUS = ROOT.parent / 'PeripheralMountainsPreview'
sys.path.insert(0, str(PREVIOUS))
from build_preview import CORE, PIECES, sample, smooth

N = 1025
STEP = 2.0
X0, Z0 = -4148.0, -3834.0
xs, zs = X0 + np.arange(N)*STEP, Z0 + np.arange(N)*STEP
x, z = np.meshgrid(xs, zs)
live = json.loads((ROOT/'live.json').read_text(encoding='utf-8-sig'))
current = np.full((N,N), np.nan, dtype=np.float32)
before = np.full_like(current, np.nan)
for t in sorted(live['terrains'], key=lambda t:t['res']):
    px,py,pz=t['p']; sx,sy,sz=t['size']; n=t['res']
    ix=np.flatnonzero((xs>=px)&(xs<=px+sx))
    iz=np.flatnonzero((zs>=pz)&(zs<=pz+sz))
    if not len(ix) or not len(iz): continue
    for destination, folder in [(current,ROOT/'Live'),(before,PREVIOUS/'Applied/Before')]:
        a=np.fromfile(folder/(t['name']+'.f32'),dtype='<f4').reshape(n,n)
        destination[np.ix_(iz,ix)] = sample(a,(xs[ix][None,:]-px)/sx*(n-1),(zs[iz][:,None]-pz)/sz*(n-1))*sy+py
assert np.isfinite(current).all() and np.isfinite(before).all()

Image.MAX_IMAGE_PIXELS=None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    source=np.array(im.crop((4800,0,10012,10012)))
original=sample(source, x+4924.00049+4806-4800-.5, 2006+2085.999939-z-.5)-2073.599609375
assert (original>-710).all()

inside=np.zeros((N,N),dtype=bool)
distance=np.full((N,N),np.inf)
for p,q in zip(CORE,CORE[1:]+CORE[:1]):
    vx,vz=q[0]-p[0],q[1]-p[1]
    t=np.clip(((x-p[0])*vx+(z-p[1])*vz)/(vx*vx+vz*vz),0,1)
    distance=np.minimum(distance,np.hypot(x-p[0]-t*vx,z-p[1]-t*vz))
    if vz:
        inside ^= ((p[1]>z)!=(q[1]>z)) & (x<(q[0]-p[0])*(z-p[1])/vz+p[0])

# Native source B at its existing placement. No height averaging is used to
# derive the guidance field. The output's absolute elevation is constrained
# by the unchanged current terrain around the solve region.
p=PIECES[1]
dx=x-p['center'][0]; dz=z-p['center'][1]
c,s=np.cos(np.deg2rad(p['angle'])),np.sin(np.deg2rad(p['angle']))
sx=(c*dx+s*dz)/p['scale']+p['source_center'][0]
sy=-(-s*dx+c*dz)/p['scale']+p['source_center'][1]
outer=sample(source,sx-4800-.5,sy-.5)*p['scale']+p['offset']
assert (outer>-710).all()
coverage=smooth((sx-4900)/600)*smooth((9700-sx)/400)*smooth((sy-200)/400)*smooth((9700-sy)/400)

# The inner boundary follows the preserved core polygon. Elliptical side and
# outer limits contain this experiment to circle 1 and avoid a square patch.
radius=np.hypot((x+3210)/880,(z+2940)/710)
region=smooth((1-radius)/.24)
region *= smooth((distance-24)/90)
region[inside]=0
fixed=(region==0)
assert fixed[0,:].all() and fixed[-1,:].all() and fixed[:,0].all() and fixed[:,-1].all()

for name,array in [('current',current),('before',before),('original',original),('outer_source',outer),
                   ('protected',inside),('fixed',fixed),('region',region),('distance',distance),('source_coverage',coverage)]:
    np.save(ROOT/(name+'.npy'),array)

metadata=dict(resolution=N,spacing_m=STEP,extent_xz=[X0,xs[-1],Z0,zs[-1]],row_zero='south',
              height_units='Unity world Y metres',source_B_placement=p,protected_polygon=CORE,
              inner_fixed_guard_m=24,solve_region_ellipse=dict(center=[-3210,-2940],radius=[880,710]),
              context='Current live Main heights where available, otherwise live Draft heights',
              guide='Native 1m USGS source resampled to 2m. Current outer source B placement retained for controlled width comparison.',
              current_core_vs_before_max_m=float(np.abs(current[inside]-before[inside]).max()))

for name,width in [('A',450.0),('B',800.0)]:
    directory=ROOT/name; directory.mkdir(exist_ok=True)
    blend=smooth((distance-24)/width)*coverage
    blend[inside]=0
    # Gradients live on grid edges, in normalized Terrain height differences.
    edge_fields=[]
    for axis in [1,0]:
        if axis==1:
            w=.5*(blend[:,1:]+blend[:,:-1]); r=.5*(region[:,1:]+region[:,:-1])
        else:
            w=.5*(blend[1:,:]+blend[:-1,:]); r=.5*(region[1:,:]+region[:-1,:])
        g=(1-w)*np.diff(original,axis=axis)+w*np.diff(outer,axis=axis)
        edge_fields.append(((1-r)*np.diff(current,axis=axis)+r*g)/1000)
    gx,gz=edge_fields
    divergence=np.zeros((N,N),dtype=np.float64)
    divergence[:,:-1]+=gx; divergence[:,1:]-=gx
    divergence[:-1,:]+=gz; divergence[1:,:]-=gz
    count=np.full((N,N),4.0); count[[0,-1],:]-=1; count[:,[0,-1]]-=1
    alpha=(~fixed).astype('<f4')
    altitude=((current+710)/1000).astype('<f4')
    lap=(divergence/count).astype('<f4')
    alpha.tofile(directory/'alpha.f32'); altitude.tofile(directory/'altitude.f32'); lap.tofile(directory/'laplacian.f32')
    np.save(directory/'guidance_blend.npy',blend)
    (directory/'parameters.json').write_text(json.dumps(dict(width_m=width,description='Gradient blend distance outside the 24m core guard; solve footprint identical between candidates'),indent=2))

(ROOT/'metadata.json').write_text(json.dumps(metadata,indent=2))
paths=[PROJECT/'Assets/Scenes/Main Game Scene.unity']+[PROJECT/t['path'] for t in live['terrains']]
if not (ROOT/'asset-hashes-before.json').exists():
    hashes={str(p.relative_to(PROJECT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
    (ROOT/'asset-hashes-before.json').write_text(json.dumps(hashes,indent=2))
print(json.dumps(dict(current_range=[float(current.min()),float(current.max())],
                     solve_samples=int((~fixed).sum()),protected_samples=int(inside.sum()),
                     core_vs_before_max_m=metadata['current_core_vs_before_max_m']),indent=2))
