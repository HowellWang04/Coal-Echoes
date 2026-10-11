from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
phase=sys.argv[1] if len(sys.argv)>1 else 'Prepared'
meta=json.loads((ROOT/'before.json').read_text(encoding='utf-8-sig'));factory=json.loads((ROOT/'factory.json').read_text());edits={r['name']:r for r in json.loads((ROOT/'prepared.json').read_text())}
heights={};baseline_heights={};rows=[]
for t in meta['terrains']:
 name=t['name'];n=t['res'];a=t['alpha'];px,py,pz=t['p']
 old=np.fromfile(ROOT/'Before'/f'{name}.height.f32',np.float32).reshape(n,n)
 new=np.fromfile(ROOT/phase/f'{name}.height.f32',np.float32).reshape(n,n)
 expected=np.fromfile(ROOT/'Prepared'/f'{name}.height.f32',np.float32).reshape(n,n)
 x,z=np.meshgrid(px+np.arange(n)*1000/(n-1),pz+np.arange(n)*1000/(n-1))
 fixed=np.hypot(x-factory['x'],z-factory['z'])<=factory['radius']
 assert np.array_equal(new[fixed],old[fixed]),name
 outside=expected==old
 assert np.array_equal(new[outside],old[outside]),('Outside modified',name)
 err=float(abs(new-expected).max()*1100)
 assert err<.017,(name,err)
 row=dict(name=name,quantization_error_m=err,factory_changed=0,outside_changed=0)
 if phase=='After':
  mask=np.fromfile(ROOT/'Prepared'/f'{name}.paint.u8','u1').reshape(a,a)>0
  oa=np.fromfile(ROOT/'Before'/f'{name}.alpha.f32',np.float32).reshape(a,a,3)
  na=np.fromfile(ROOT/'After'/f'{name}.alpha.f32',np.float32).reshape(a,a,3)
  assert np.array_equal(oa[~mask],na[~mask]),('Materials outside',name)
  row['material_outside_changed']=0
 heights[name]=new;baseline_heights[name]=old;rows.append(row)
seams=[];lod=[]
for i,t in enumerate(meta['terrains']):
 for u in meta['terrains'][i+1:]:
  dx=u['p'][0]-t['p'][0];dz=u['p'][2]-t['p'][2]
  if t['res']!=u['res']:
   if abs(dx)<.01 and abs(dz)<.01:
    main,draft=(t,u) if t['res']==1025 else (u,t)
    delta=float(abs(heights[main['name']][::8,::8]-heights[draft['name']]).max()*1100);baseline=float(abs(baseline_heights[main['name']][::8,::8]-baseline_heights[draft['name']]).max()*1100);assert delta<=baseline+.001,(main['name'],delta,baseline);lod.append(delta)
   continue
  if abs(abs(dx)-1000)<.01 and abs(dz)<.01:
   left,right=(t,u) if dx>0 else (u,t);delta=float(abs(heights[left['name']][:,-1]-heights[right['name']][:,0]).max()*1100);baseline=float(abs(baseline_heights[left['name']][:,-1]-baseline_heights[right['name']][:,0]).max()*1100)
  elif abs(abs(dz)-1000)<.01 and abs(dx)<.01:
   left,right=(t,u) if dz>0 else (u,t);delta=float(abs(heights[left['name']][-1,:]-heights[right['name']][0,:]).max()*1100);baseline=float(abs(baseline_heights[left['name']][-1,:]-baseline_heights[right['name']][0,:]).max()*1100)
  else:continue
  assert delta<=baseline+.001,(left['name'],right['name'],delta,baseline);seams.append(delta)
report=dict(phase=phase,terrains=len(rows),factory_changed=0,outside_changed=0,seams_not_worsened=True,seam_pairs=len(seams),seam_max_m=max(seams),lod_pairs=len(lod),lod_max_m=max(lod),quantization_max_m=max(r['quantization_error_m'] for r in rows),rows=rows)
if phase=='After':
 after=json.loads((ROOT/'after.json').read_text());assert after['transformsUnchanged'] and after['locks']==meta['locks'];assert after['auto']==meta['auto'] and not after['generating'];report.update(transforms_unchanged=True,locks_unchanged=True)
(ROOT/(phase.lower()+'-verification.json')).write_text(json.dumps(report,indent=2));print(json.dumps({k:v for k,v in report.items() if k!='rows'},indent=2))

