from pathlib import Path
import json
import numpy as np
P=Path(__file__).parent
reports=[]
for t in json.loads((P/'tiles.json').read_text()):
    oldfile=P/(t['name']+'.before.a.f32')
    if not oldfile.exists(): continue
    n=t['n']; ar=t['ar']; px,py,pz=t['p']
    h=np.fromfile(P/(t['name']+'.h.f32'),'<f4').reshape(n,n)
    before=np.fromfile(P.parent.parent/'Applied'/'Before'/(t['name']+'.f32'),'<f4').reshape(n,n)
    a=np.fromfile(P/(t['name']+'.a.f32'),'<f4').reshape(ar,ar,3)
    old=np.fromfile(oldfile,'<f4').reshape(ar,ar,3)
    x=px+(np.arange(ar)[None,:]+.5)*1000/ar+2000
    z=pz+(np.arange(ar)[:,None]+.5)*1000/ar+1650
    along=.5*x+np.sqrt(.75)*z; across=-np.sqrt(.75)*x+.5*z
    d=np.hypot(np.maximum(abs(along)-272.5,0),np.maximum(abs(across)-190,0))
    region=d<90
    gz,gx=np.gradient(h*1000,1000/(n-1))
    slope=np.rad2deg(np.arctan(np.hypot(gx,gz)))
    stale=region&(slope<15)&(a[:,:,1]>.8)
    loc=np.argwhere(stale)
    probes=[]
    for zz,xx in loc[::max(1,len(loc)//5)][:5]:
        probes.append(dict(x=float(px+xx*1000/(n-1)),z=float(pz+zz*1000/(n-1)),slope=float(slope[zz,xx]),rock=float(a[zz,xx,1])))
    reports.append(dict(tile=t['name'],pixels=int(region.sum()),alpha_diff_from_backup=float(abs(a-old)[region].max()) if region.any() else 0,height_diff_from_backup_m=float(abs(h-before)[region].max()*1000) if region.any() else 0,flat_dark_pixels=int(stale.sum()),samples=probes))
(P/'old_stamp_check.json').write_text(json.dumps(reports,indent=2))
for r in reports: print(json.dumps(r))
bad=sum(r['flat_dark_pixels'] for r in reports)
print(f'FAIL stale rock stamp on {bad} low-slope samples' if bad else 'PASS no low-slope dark stamp')
raise SystemExit(1 if bad else 0)
