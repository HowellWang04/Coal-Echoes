from pathlib import Path
import sys,os,json,time
SOURCE=Path(__file__).resolve().parent
ROOT=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else SOURCE
sys.path.insert(0,str(SOURCE.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
os.environ['OPENBLAS_NUM_THREADS']='1';os.environ['OMP_NUM_THREADS']='1'
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.linalg import cg

meta=json.loads((ROOT/'metadata.json').read_text());n=meta['resolution']
free=np.load(ROOT/'free.npy');fixed=~free;current=np.load(ROOT/'current.npy')
index=np.full((n,n),-1,np.int32);index[free]=np.arange(free.sum())
# A soft height reference prevents distant mountains drifting while releasing
# every former hard constraint outside the factory. The smoothing scale is 40 m.
screen=(meta['spacing_m']/40.)**2
rows=[np.arange(free.sum(),dtype=np.int32)];cols=[rows[0]];values=[np.full(free.sum(),4.+screen)]
for left,right in [(index[:,:-1],index[:,1:]),(index[:-1,:],index[1:,:])]:
    valid=(left>=0)&(right>=0);a,b=left[valid],right[valid]
    rows.extend([a,b]);cols.extend([b,a]);values.extend([-np.ones(a.size),-np.ones(a.size)])
matrix=coo_matrix((np.concatenate(values),(np.concatenate(rows),np.concatenate(cols))),shape=(free.sum(),free.sum())).tocsr()
del rows,cols,values,index,a,b,valid,left,right
folder=ROOT/'Solver'
altitude=np.fromfile(folder/'altitude.f32',np.float32).reshape(n,n).astype(float)
lap=np.fromfile(folder/'laplacian.f32',np.float32).reshape(n,n).astype(float)
initial=np.fromfile(folder/'author_result.f32',np.float32).reshape(n,n).astype(float)
anchors=np.where(fixed,altitude,0);rhs=-4*lap+screen*altitude
rhs[1:]+=anchors[:-1];rhs[:-1]+=anchors[1:];rhs[:,1:]+=anchors[:,:-1];rhs[:,:-1]+=anchors[:,1:]
b=rhs[free];iterations=[0];started=time.perf_counter()
def tick(_):
    iterations[0]+=1
    if iterations[0]%200==0:print('iteration',iterations[0],'seconds',round(time.perf_counter()-started,1),flush=True)
solution,info=cg(matrix,b,x0=initial[free],rtol=1e-8,atol=0,maxiter=7000,callback=tick)
if info:raise RuntimeError(f'CG did not converge {info}')
output=current.copy();output[free]=(solution*1000-710).astype(np.float32)
residual=(matrix@solution-b)/4*1000
delta=(output-current)/1000
(ROOT/'Prepared').mkdir(exist_ok=True);delta.astype('f4').tofile(ROOT/'Prepared/gradient_delta.f32')
np.save(ROOT/'candidate.npy',output)
report=dict(iterations=iterations[0],seconds=time.perf_counter()-started,residual_max_m=float(abs(residual).max()),max_raise_m=float((output-current).max()),max_lower_m=float((output-current).min()),range_m=[float(output.min()),float(output.max())],fixed_changed=int(np.count_nonzero(output[fixed]!=current[fixed])),author_refinement_max_m=float(abs(solution-initial[free]).max()*1000),soft_height_reference_scale_m=40)
(ROOT/'solver-verification.json').write_text(json.dumps(report,indent=2));print(json.dumps(report),flush=True)
assert report['fixed_changed']==0
assert output.min()>=-710 and output.max()<=390,report
