"""Read-only repro at the user's marked transition surfaces."""
from pathlib import Path
import json
import numpy as np
import sys
P=Path(__file__).parent
sys.path.insert(0,str(P.parent))
from build_preview import sample
A=P.parent/'Applied'
tiles=json.loads((A/'before.json').read_text(encoding='utf-8-sig'))['terrains']
probes=json.loads((P/'probes.json').read_text(encoding='utf-8-sig'))
results=[]
for probe in probes:
    t=next(t for t in tiles if t['name']==probe['tile'])
    n=t['res'];x,y,z=probe['p'];px,py,pz=t['p'];spacing=1000/(n-1)
    values={}
    for folder in ['Before','After']:
        a=np.fromfile(A/folder/(t['name']+'.f32'),dtype='<f4').reshape(n,n)
        def h(dx=0,dz=0):return float(sample(a,np.array((x+dx-px)/spacing),np.array((z+dz-pz)/spacing))*1000-710)
        gx=(h(spacing,0)-h(-spacing,0))/(2*spacing)
        gz=(h(0,spacing)-h(0,-spacing))/(2*spacing)
        values[folder]=dict(height=h(),slope_deg=float(np.rad2deg(np.arctan(np.hypot(gx,gz)))))
    results.append(dict(position=[x,z],tile=t['name'],before=values['Before'],after=values['After'],height_change_m=values['After']['height']-values['Before']['height'],rock_weight=probe['layers'][1]))
(P/'transition_measurements.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
for r in results:print(json.dumps(r))
bad=[r for r in results if r['after']['slope_deg']>40 and r['after']['slope_deg']-r['before']['slope_deg']>15 and r['rock_weight']>.9]
print('FAIL artificial steep rock bands at '+str(len(bad))+' sampled positions' if bad else 'No flagged steep-band samples')
raise SystemExit(1 if bad else 0)
