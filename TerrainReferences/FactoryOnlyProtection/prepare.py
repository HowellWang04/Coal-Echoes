"""Release the former gameplay core; keep only the live factory lock fixed."""
from pathlib import Path
import sys, os, json
ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
OLD = ROOT.parent / 'NativeDEMOnly'
sys.path.insert(0, str(PROJECT / 'Library/GradientTerrainExampleTools/python-deps'))
os.environ['OPENBLAS_NUM_THREADS'] = '1'
import numpy as np
from scipy.ndimage import map_coordinates, distance_transform_edt

def smooth(t):
    t = np.clip(t, 0, 1)
    return t*t*t*(t*(t*6-15)+10)

N = 3073
STEP = 2.
X0, Z0 = -4996.00049, -3986.
x = X0 + np.arange(N)[None, :]*STEP
z = Z0 + np.arange(N)[:, None]*STEP
factory = json.loads((ROOT/'factory.json').read_text())
atlas = np.fromfile(ROOT/'Before/main_height.f32', np.float32).reshape(6209, 6209)
coords = np.broadcast_arrays((z+3945.249939)/(1000/1024), (x+4955.25049)/(1000/1024))
current = map_coordinates(atlas, coords, order=1, mode='nearest')*1100-710
del atlas

# The old mask limits this repair's extent only. Its polygon is no longer fixed.
repair = np.load(OLD/'keep.npy') < 1
repair[[0,-1],:] = False
repair[:,[0,-1]] = False
outside_distance = distance_transform_edt(repair)*STEP
factory_distance = np.hypot(x-factory['x'], z-factory['z'])-factory['radius']
blend = smooth(factory_distance/160)*smooth(outside_distance/160)
free = repair & (factory_distance > 0)

hs = [np.load(OLD/f'source_{i}.npy') for i in range(4)]
coverage = [smooth(distance_transform_edt(np.isfinite(h))*STEP/450).astype(np.float32) for h in hs]
a = smooth((x+2100)/800)*smooth((z+3200)/800)*smooth((1400-z)/800)*coverage[1]
b = smooth((-z-2250)/800)*coverage[2]
c = smooth((z-350)/800)*coverage[3]
weights = [coverage[0]*(1-a)*(1-b)*(1-c), a*(1-b)*(1-c), b*(1-c), c]
remaining = 1-sum(weights)
coverage_total = sum(coverage)
weights = [w+remaining*cov/coverage_total for w,cov in zip(weights,coverage)]
total = sum(weights)
weights = [(w/total).astype(np.float32) for w in weights]
native = sum(np.nan_to_num(h)*w for h,w in zip(hs,weights)).astype(np.float32)

# Blend slope guidance continuously at the new factory boundary.
div = np.zeros((N,N), np.float64)
for axis in [1,0]:
    edge = lambda a: (a[:,1:]+a[:,:-1])*.5 if axis==1 else (a[1:,:]+a[:-1,:])*.5
    native_g = sum(edge(w)*np.diff(np.nan_to_num(h),axis=axis) for w,h in zip(weights,hs))
    g = (np.diff(current,axis=axis)*(1-edge(blend)) + native_g*edge(blend))/1000
    if axis==1:
        div[:,:-1]+=g; div[:,1:]-=g
    else:
        div[:-1]+=g; div[1:]-=g

(ROOT/'Solver').mkdir(exist_ok=True)
free.astype('f4').tofile(ROOT/'Solver/alpha.f32')
((current+710)/1000).astype('f4').tofile(ROOT/'Solver/altitude.f32')
(div/4).astype('f4').tofile(ROOT/'Solver/laplacian.f32')
for name,value in [('current',current),('before',current),('free',free),('blend',blend),('native',native),('repair',repair)]:
    np.save(ROOT/(name+'.npy'),value)
for i,w in enumerate(weights): np.save(ROOT/f'weight_{i}.npy',w)
meta = dict(resolution=N,spacing_m=STEP,extent_xz=[X0,float(x[0,-1]),Z0,float(z[-1,0])],factory=factory,
            boundary_slope_fade_m=160,free_samples=int(free.sum()),former_core_fixed=False)
(ROOT/'metadata.json').write_text(json.dumps(meta,indent=2))
print(json.dumps(meta),flush=True)
