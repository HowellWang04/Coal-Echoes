"""Rebuild DEM from its original source with only the current layout grading."""
import json
from pathlib import Path
import numpy as np
from PIL import Image

OUT=Path(__file__).parent
old=json.loads((OUT/'layout_config.json').read_text(encoding='utf-8-sig'))
before=json.loads((OUT/'ridge_revision_before.json').read_text(encoding='utf-8-sig'))['result']
proposal=json.loads((OUT/'ridge_layout_proposal.json').read_text())
names=['tajia03','tajia03 (1)','tajia03 (10)','tajia01','tajia03 (7)','tajia03 (3)','tajia03 (11)','tajia03 (4)','tajia03 (5)','tajia03 (6)']
fixed=['tajia03 (10)','tajia03 (7)','tajia03 (11)']
live={t['name']:t for t in before['towers']}
points=[[-2426.041,-1941.993],[-2439,-1899],live[fixed[0]]['pos'][::2],[-2490,-1815],live[fixed[1]]['pos'][::2],[-2509,-1698],live[fixed[2]]['pos'][::2],[-2510,-1510],[-2540,-1430],[-2603.586,-1370.433]]
Image.MAX_IMAGE_PIXELS=None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    source=np.array(im.crop((4806,2006,10006,8006)))
assert np.all(source>-999000)
def sample(x,z):
    ix=np.clip(np.asarray(x)+4924.00049-.5,0,5199);iz=np.clip(2085.999939-np.asarray(z)-.5,0,5999)
    c=ix.astype(int);r=iz.astype(int);fx=ix-c;fz=iz-r
    return ((source[r,c]*(1-fx)+source[r,np.minimum(c+1,5199)]*fx)*(1-fz)+(source[np.minimum(r+1,5999),c]*(1-fx)+source[np.minimum(r+1,5999),np.minimum(c+1,5199)]*fx)*fz)-old['source_base_elevation']-710
n=6144;step=6000/n
x=np.arange(n,dtype=np.float32)*step+old['world_origin_xz'][0]
z=x[::-1]-old['world_origin_xz'][0]+old['world_origin_xz'][1]
height=np.empty((n,n),np.float32)
for i,north in enumerate(z):height[i]=sample(x,north)
factory=proposal['factory'];cx,cz=factory['center'];fy=factory['y']
a=np.deg2rad(factory['angle']);u=np.array([np.cos(a),np.sin(a)]);v=np.array([-np.sin(a),np.cos(a)])
# Recompute from native heights. No prior factory rectangle or tower pad is retained.
max_factory=0.
for i,north in enumerate(z):
    dx=x-cx;dz=north-cz
    along=dx*u[0]+dz*u[1];across=dx*v[0]+dz*v[1]
    dist=np.hypot(np.maximum(abs(along)-272.5,0),np.maximum(abs(across)-190,0))
    t=np.clip(1-dist/90,0,1);w=t*t*(3-2*t)
    edit=w*(fy-height[i]);max_factory=max(max_factory,float(abs(edit).max()));height[i]+=edit
towers=[]
for number,(name,p) in enumerate(zip(names,points),1):
    tx,tz=p;yy=float(sample(tx,tz))
    rows=np.where(abs(z-tz)<36)[0];cols=np.where(abs(x-tx)<36)[0]
    dist=np.hypot(x[cols][None,:]-tx,z[rows][:,None]-tz)
    t=np.clip((36-dist)/22,0,1);w=t*t*(3-2*t)
    sub=height[np.ix_(rows,cols)];height[np.ix_(rows,cols)]=sub+w*(yy-sub)
    towers.append({'number':number,'name':name,'xz':[float(tx),float(tz)],'ground_y':yy,'fixed_xz':name in fixed})
# Verify the central support-line clearance before committing any asset changes.
clearance=[]
for i in range(len(points)-1):
    f=np.linspace(0,1,201);p=np.array(points[i])[None,:]*(1-f[:,None])+np.array(points[i+1])[None,:]*f[:,None]
    gy=sample(p[:,0],p[:,1]);wire=(towers[i]['ground_y']+31)*(1-f)+(towers[i+1]['ground_y']+31)*f-3*4*f*(1-f)
    clearance.append(float(np.min(wire-gy)))
revision={'status':'prepared','factory':factory,'factory_blend_m':90,'max_factory_edit_m':max_factory,
          'old_factory_center':[old['factory_world_xz'][0]-87.5,old['factory_world_xz'][1]-55],
          'old_factory_y':old['factory_y'],'unity_yaw_delta':-60,'towers':towers,
          'route_length_m':float(np.linalg.norm(np.diff(np.array(points),axis=0),axis=1).sum()),
          'route_gain_m':towers[-1]['ground_y']-towers[0]['ground_y'],
          'estimated_cable_center_clearance_m':min(clearance),'span_clearances_m':clearance,
          'tower_pad_radius_m':14,'tower_pad_transition_m':22}
assert min(clearance)>5, 'Route needs cable clearance adjustment'
(OUT/'ridge_revision.json').write_text(json.dumps(revision,indent=2))
# Stage the RAW outside Assets. Unity imports it explicitly after old locks are disabled.
np.rint(np.clip((height+710)/1000,0,65535/65536)*65536).astype('<u2').tofile(OUT/'ridge_height_staged.raw')
print(json.dumps({k:v for k,v in revision.items() if k!='towers'},indent=2))
