from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parents[2]/'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from matplotlib.path import Path as Polygon
from scipy.ndimage import map_coordinates
p=ROOT
before=json.loads((p/'before.json').read_text()); after=json.loads((p/'after.json').read_text())
core=Polygon(json.loads((ROOT.parent/'metadata.json').read_text())['protected_polygon'])
meta=json.loads((ROOT.parent/'metadata.json').read_text())
accepted=np.load(ROOT.parent/'accepted.npy').astype(np.float32)
crests=np.load(ROOT.parent/'crests.npy').astype(np.float32)
rows=[];changed=[];H={}
for t in before['terrains']:
    name=t['name'];n=t['res'];a=t['alpha']
    old=np.fromfile(p/'Before'/f'{name}.height.f32',np.float32).reshape(n,n)
    new=np.fromfile(p/'After'/f'{name}.height.f32',np.float32).reshape(n,n)
    H[name]=(old,new)
    f=p/'Prepared'/f'{name}.delta.f32'
    delta=np.fromfile(f,np.float32).reshape(n,n) if f.exists() else np.zeros_like(old)
    X,Z=np.meshgrid(t['p'][0]+np.arange(n)*1000/(n-1),t['p'][2]+np.arange(n)*1000/(n-1))
    protected=core.contains_points(np.c_[X.ravel(),Z.ravel()]).reshape(n,n)
    diff=new-old
    keep=map_coordinates(accepted,[(Z-meta['extent_xz'][2])/2,(X-meta['extent_xz'][0])/2],order=1,mode='constant',cval=0)>0
    assert np.array_equal(new[keep],old[keep]),name
    crest=map_coordinates(crests,[(Z-meta['extent_xz'][2])/2,(X-meta['extent_xz'][0])/2],order=1,mode='constant',cval=0)>0
    assert np.array_equal(new[crest],old[crest]),name
    row=dict(name=name,height_changed=int(np.count_nonzero(diff)),core_height_changes=int(np.count_nonzero(diff[protected])),outside_delta_changes=int(np.count_nonzero(diff[delta==0])),height_quantization_error_m=float(abs(new-(old+delta)).max()*1000))
    row['accepted_circle1_height_changes']=int(np.count_nonzero(diff[keep]))
    row['crest_height_changes']=int(np.count_nonzero(diff[crest]))
    if row['height_changed']:changed.append(name)
    if (p/'Before'/f'{name}.alpha.f32').exists():
        oa=np.fromfile(p/'Before'/f'{name}.alpha.f32',np.float32).reshape(a,a,3)
        na=np.fromfile(p/'After'/f'{name}.alpha.f32',np.float32).reshape(a,a,3)
        mf=p/'Prepared'/f'{name}.paint.u8'
        mask=np.fromfile(mf,'u1').reshape(a,a)!=0 if mf.exists() else np.zeros((a,a),bool)
        ad=np.max(abs(na-oa),axis=2)
        row.update(alpha_changed=int(np.count_nonzero(ad)),alpha_outside_mask_max=float(ad[~mask].max(initial=0)),alpha_outside_mask_changes=int(np.count_nonzero(ad[~mask])))
        assert row['alpha_outside_mask_max']==0,row
    assert row['core_height_changes']==0 and row['outside_delta_changes']==0,row
    assert row['height_quantization_error_m']<.016,row
    rows.append(row)
seams=[]
for i,t in enumerate(before['terrains']):
    for u in before['terrains'][i+1:]:
        if t['res']!=u['res']:continue
        dx=u['p'][0]-t['p'][0];dz=u['p'][2]-t['p'][2]
        if abs(abs(dx)-1000)<.01 and abs(dz)<.01:
            left,right=(t,u) if dx>0 else (u,t)
            b0,b1=[h[:,-1] for h in H[left['name']]];c0,c1=[h[:,0] for h in H[right['name']]]
        elif abs(abs(dz)-1000)<.01 and abs(dx)<.01:
            left,right=(t,u) if dz>0 else (u,t)
            b0,b1=[h[-1,:] for h in H[left['name']]];c0,c1=[h[0,:] for h in H[right['name']]]
        else:continue
        if t['name'] not in changed and u['name'] not in changed:continue
        seams.append(dict(a=t['name'],b=u['name'],before_max_m=float(abs(b0-c0).max()*1000),after_max_m=float(abs(b1-c1).max()*1000),gap_change_max_m=float(abs((b1-c1)-(b0-c0)).max()*1000)))
assert before['locks']==after['locks']
assert after['transformsUnchanged'] and after['auto']==before['auto'] and not after['generating']
report=dict(terrain_count=len(rows),changed_terrain_count=len(changed),unchanged_terrain_count=len(rows)-len(changed),transforms_unchanged=after['transformsUnchanged'],transform_count=before['transformCount'],locks_unchanged=True,core_height_changes=sum(r['core_height_changes'] for r in rows),outside_delta_changes=sum(r['outside_delta_changes'] for r in rows),alpha_outside_mask_changes=sum(r.get('alpha_outside_mask_changes',0) for r in rows),max_height_quantization_error_m=max(r['height_quantization_error_m'] for r in rows),rows=rows,seams=seams)
(p/'verification.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='rows'},indent=2))
