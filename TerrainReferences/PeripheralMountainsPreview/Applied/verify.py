from pathlib import Path
import json
import numpy as np
import sys
sys.path.insert(0,str(Path(__file__).parent.parent))
from build_preview import CORE

P=Path(__file__).parent
before=json.loads((P/'before.json').read_text(encoding='utf-8-sig'))
tiles=before['terrains']
def load(folder):
    return {t['name']:np.fromfile(P/folder/(t['name']+'.f32'),dtype='<f4').reshape(t['res'],t['res']) for t in tiles}
old,after=load('Before'),load('After')
seams=[];lod=[];protected_count=0;protected_changed=0;protected_max=0
for t in tiles:
    name=t['name'];px,py,pz=t['p'];n=t['res']
    x=px+np.arange(n)[None,:]*1000/(n-1)
    z=pz+np.arange(n)[:,None]*1000/(n-1)
    inside=np.zeros((n,n),dtype=bool)
    for p,q in zip(CORE,CORE[1:]+CORE[:1]):
        if q[1]!=p[1]:inside^=((p[1]>z)!=(q[1]>z))&(x<(q[0]-p[0])*(z-p[1])/(q[1]-p[1])+p[0])
    delta=abs(after[name]-old[name])
    protected_count+=int(inside.sum());protected_changed+=int(np.count_nonzero(delta[inside]))
    protected_max=max(protected_max,float(np.max(delta[inside],initial=0)*1000))
    if name.endswith('_Main'):
        dn=name.replace('_Main','_Draft')
        if dn in after:lod.append(dict(tile=name,before=float(abs(old[name][::8,::8]-old[dn]).max()*1000),after=float(abs(after[name][::8,::8]-after[dn]).max()*1000)))
    for q in tiles:
        if t['res']!=q['res']:continue
        dx,dy,dz=np.array(q['p'])-t['p']
        axis=0 if abs(dx-1000)<.01 and abs(dz)<.01 else (1 if abs(dz-1000)<.01 and abs(dx)<.01 else -1)
        if axis<0:continue
        other=q['name']
        def edge(v):return v[name][:,-1]-v[other][:,0] if axis==0 else v[name][-1,:]-v[other][0,:]
        seams.append(dict(a=name,b=other,before=float(abs(edge(old)).max()*1000),after=float(abs(edge(after)).max()*1000)))
result=dict(protected_samples=protected_count,protected_changed=protected_changed,protected_max_delta_m=protected_max,seams=seams,lod=lod,max_seam_before_m=max(t['before'] for t in seams),max_seam_after_m=max(t['after'] for t in seams),max_lod_before_m=max(t['before'] for t in lod),max_lod_after_m=max(t['after'] for t in lod))
(P/'verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ['seams','lod']}))
print(json.dumps(sorted(seams,key=lambda t:t['after'],reverse=True)[:6]))
