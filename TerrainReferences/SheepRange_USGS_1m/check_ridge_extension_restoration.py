import json
from pathlib import Path
import numpy as np
from PIL import Image
folder=Path(__file__).parent
v=json.loads((folder/'ridge_extension_verification.json').read_text())
p=json.loads((folder/'ridge_extension.json').read_text())
old=json.loads((folder/'layout_config.json').read_text(encoding='utf-8-sig'))
Image.MAX_IMAGE_PIXELS=None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    h=np.array(im.crop((4806,2006,10006,8006)))
def original(x,z):
    ix=x+4924.00049-.5;iz=2085.999939-z-.5;c=int(ix);r=int(iz);fx=ix-c;fz=iz-r
    return ((h[r,c]*(1-fx)+h[r,c+1]*fx)*(1-fz)+(h[r+1,c]*(1-fx)+h[r+1,c+1]*fx)*fz)-old['source_base_elevation']-710
results=[];excluded=[]
for q in v['probes']:
    dx=q['x']-p['factory']['center'][0];dz=q['z']-p['factory']['center'][1]
    a=np.deg2rad(60);u=dx*np.cos(a)+dz*np.sin(a);w=-dx*np.sin(a)+dz*np.cos(a)
    factory_distance=np.hypot(max(abs(u)-272.5,0),max(abs(w)-190,0))
    tower_distance=min(np.hypot(q['x']-t['xz'][0],q['z']-t['xz'][1]) for t in p['towers'])
    if factory_distance<90 or tower_distance<36:
        excluded.append(q['kind']);continue
    results.append({**q,'native_y':float(original(q['x'],q['z'])),'difference_m':float(q['y']-original(q['x'],q['z']))})
report={'samples':len(results),'excluded_new_grading':excluded,'max_difference_m':max(abs(q['difference_m']) for q in results),'mean_absolute_difference_m':float(np.mean([abs(q['difference_m']) for q in results])),'points':results}
(folder/'ridge_extension_restoration_check.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='points'},indent=2))
