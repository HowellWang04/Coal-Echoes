from sources import *
import json
from matplotlib.path import Path as Polygon
phase=sys.argv[1] if len(sys.argv)>1 else 'Prepared'
before=json.loads((ROOT/'before.json').read_text(encoding='utf-8-sig'))
poly=Polygon(CORE)
rows=[];heights={};old_heights={}
for t in before['terrains']:
    name=t['name'];n=t['res'];a=t['alpha'];px,py,pz=t['p']
    old=np.fromfile(ROOT/'Before'/f'{name}.height.f32',np.float32).reshape(n,n)
    new=np.fromfile(ROOT/phase/f'{name}.height.f32',np.float32).reshape(n,n)
    expected=np.fromfile(ROOT/'Prepared'/f'{name}.height.f32',np.float32).reshape(n,n)
    x,z=np.meshgrid(px+np.arange(n)*1000/(n-1),pz+np.arange(n)*1000/(n-1))
    core=poly.contains_points(np.c_[x.ravel(),z.ravel()]).reshape(n,n)
    world_delta=new.astype(float)*1100-old.astype(float)*1000
    core_error=float(abs(world_delta[core]).max(initial=0))
    if n==1025:assert core_error<.02,(name,core_error)
    quant_error=float(abs(new.astype(float)-expected).max()*1100)
    assert quant_error<.017,(name,quant_error)
    row=dict(name=name,core_error_m=core_error,quantization_error_m=quant_error,changed_samples=int(np.count_nonzero(abs(world_delta)>.02)))
    if phase=='After':
        oa=np.fromfile(ROOT/'Before'/f'{name}.alpha.f32',np.float32).reshape(a,a,3)
        na=np.fromfile(ROOT/'After'/f'{name}.alpha.f32',np.float32).reshape(a,a,3)
        mask=np.fromfile(ROOT/'Prepared'/f'{name}.paint.u8','u1').reshape(a,a)>0
        row['alpha_outside_mask_changes']=int(np.count_nonzero(na[~mask]!=oa[~mask]))
        assert row['alpha_outside_mask_changes']==0,row
    rows.append(row);heights[name]=new;old_heights[name]=old
seams=[];lod=[]
for i,t in enumerate(before['terrains']):
    for u in before['terrains'][i+1:]:
        dx=u['p'][0]-t['p'][0];dz=u['p'][2]-t['p'][2]
        if t['res']!=u['res']:
            if abs(dx)<.01 and abs(dz)<.01:
                main,draft=(t,u) if t['res']==1025 else (u,t)
                delta=float(abs(heights[main['name']][::8,::8]-heights[draft['name']]).max()*1100)
                assert delta<.001,(main['name'],delta)
                lod.append(delta)
            continue
        if abs(abs(dx)-1000)<.01 and abs(dz)<.01:
            left,right=(t,u) if dx>0 else (u,t)
            delta=float(abs(heights[left['name']][:,-1]-heights[right['name']][:,0]).max()*1100)
        elif abs(abs(dz)-1000)<.01 and abs(dx)<.01:
            left,right=(t,u) if dz>0 else (u,t)
            delta=float(abs(heights[left['name']][-1,:]-heights[right['name']][0,:]).max()*1100)
        else:continue
        assert delta<.001,(left['name'],right['name'],delta)
        seams.append(delta)
report=dict(phase=phase,terrains=len(rows),main_core_error_max_m=max(r['core_error_m'] for r in rows if r['name'].endswith('Main')),draft_core_realign_max_m=max(r['core_error_m'] for r in rows if r['name'].endswith('Draft')),height_quantization_error_max_m=max(r['quantization_error_m'] for r in rows),seam_pairs=len(seams),seam_gap_max_m=max(seams),lod_pairs=len(lod),lod_common_sample_gap_max_m=max(lod),rows=rows)
if phase=='After':
    after=json.loads((ROOT/'after.json').read_text())
    assert after['transformsUnchanged'] and after['locks']==before['locks']
    assert after['auto']==before['auto'] and not after['generating']
    report.update(transforms_unchanged=True,transform_count=before['transformCount'],locks_unchanged=True)
(ROOT/(phase.lower()+'-verification.json')).write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='rows'},indent=2))
