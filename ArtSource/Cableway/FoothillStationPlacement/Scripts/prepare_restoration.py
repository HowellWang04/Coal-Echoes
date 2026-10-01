"""Remove only the recorded factory grading from the existing DEM import."""
from pathlib import Path
import json, shutil, hashlib
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parents[1]
project=root.parents[2]
history=project/'TerrainReferences/SheepRange_USGS_1m'
cfg=json.loads((history/'layout_config.json').read_text(encoding='utf-8-sig'))
plan=json.loads((history/'ridge_extension.json').read_text())
tiles=json.loads((root/'terrain_before.json').read_text())
Image.MAX_IMAGE_PIXELS=None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as image:
    native=np.asarray(image.crop((4806,2006,10006,8006)),dtype=np.float32)

def original(x,z):
    ix=np.clip(np.asarray(x)+4924.00049-.5,0,5199)
    iz=np.clip(2085.999939-np.asarray(z)-.5,0,5999)
    c=ix.astype(int);r=iz.astype(int);fx=ix-c;fz=iz-r
    return ((native[r,c]*(1-fx)+native[r,np.minimum(c+1,5199)]*fx)*(1-fz)+(native[np.minimum(r+1,5999),c]*(1-fx)+native[np.minimum(r+1,5999),np.minimum(c+1,5199)]*fx)*fz)-cfg['source_base_elevation']-710

cx,cz=plan['factory']['center'];angle=np.deg2rad(plan['factory']['angle'])
def distance(x,z):
    dx=x-cx;dz=z-cz
    u=dx*np.cos(angle)+dz*np.sin(angle);v=-dx*np.sin(angle)+dz*np.cos(angle)
    return np.hypot(np.maximum(abs(u)-272.5,0),np.maximum(abs(v)-190,0))

oldpath=root/'Before/SheepRange_Height.raw'
newpath=root/'RestoredFactory_Height.raw'
shutil.copy2(oldpath,newpath)
n=6144
old=np.memmap(oldpath,dtype='<u2',mode='r',shape=(n,n))
new=np.memmap(newpath,dtype='<u2',mode='r+',shape=(n,n))
x=np.arange(n,dtype=np.float32)*(6000/n)+cfg['world_origin_xz'][0]
z=x[::-1]-cfg['world_origin_xz'][0]+cfg['world_origin_xz'][1]
changed=0;maximum=0
for row,north in enumerate(z):
    cols=np.where(distance(x,north)<90)[0]
    if not len(cols):continue
    xx=x[cols];h=original(xx,north)
    for t in plan['towers']:
        tx,tz=t['xz'];d=np.hypot(xx-tx,north-tz)
        a=np.clip((36-d)/22,0,1);w=a*a*(3-2*a)
        h+=w*(t['ground_y']-h)
    values=np.rint(np.clip((h+710)/1000,0,65535/65536)*65536).astype('<u2')
    diff=values.astype(np.int32)-old[row,cols].astype(np.int32)
    changed+=int(np.count_nonzero(diff));maximum=max(maximum,float(abs(diff).max()*1000/65536))
    new[row,cols]=values
new.flush()

# Apply the input-map difference to live terrain samples, preserving local residuals.
# The matrix importer flips the rows and maps its 6144 samples over 6000 metres.
def raw_sample(raw,xx,zz):
    px=np.clip((xx-cfg['world_origin_xz'][0])/6000*(n-1),0,n-1.001)
    pz=np.clip((zz-cfg['world_origin_xz'][1])/6000*(n-1),0,n-1.001)
    c=px.astype(int);lo=pz.astype(int);fx=px-c;fz=pz-lo;r=n-1-lo
    return ((raw[r,c]*(1-fx)+raw[r,c+1]*fx)*(1-fz)+(raw[r-1,c]*(1-fx)+raw[r-1,c+1]*fx)*fz)/65536

patches=[]
for t in tiles:
    if not t['affected']:continue
    res=t['resolution'];p=t['pos'];size=t['size']
    xx=p[0]+np.arange(res)*size[0]/(res-1)
    zz=p[2]+np.arange(res)*size[2]/(res-1)
    X,Z=np.meshgrid(xx,zz)
    before=np.fromfile(root/'Before'/(t['file']+'.f32'),dtype='<f4').reshape((res,res))
    delta=(raw_sample(new,X,Z)-raw_sample(old,X,Z))*1000/size[1]
    # Keep every tower footing and its existing transition entirely unchanged.
    protected=np.zeros_like(delta,dtype=bool)
    for tower in plan['towers']:
        protected |= np.hypot(X-tower['xz'][0],Z-tower['xz'][1])<=36
    delta[protected]=0
    after=(before.astype(np.float64)+delta).astype('<f4')
    changed_mask=after!=before
    after.tofile(root/(t['file']+'_restored.f32'))
    patches.append({**t,'changed_samples':int(changed_mask.sum()),'max_change_m':float(np.max(abs(after-before))*size[1]),'tower_footing_changes':int(np.count_nonzero(changed_mask&protected)),'patch':t['file']+'_restored.f32'})

# Candidate stations follow the extension downhill from the first two towers.
first=np.array(plan['towers'][0]['xz']);second=np.array(plan['towers'][1]['xz'])
direction=(second-first)/np.linalg.norm(second-first)
right=np.array([direction[1],-direction[0]])
candidates=[]
for d in np.arange(35,221,5):
    for offset in [-8,-4,0,4,8]:
        center=first-direction*d+right*offset
        footprint=np.array([center+direction*a+right*b for a in np.linspace(-5.6,5.6,9) for b in [-1.85,0,1.85]])
        heights=original(footprint[:,0],footprint[:,1])
        # Select for a small foundation height spread, downhill from the first tower.
        candidates.append({'xz':center.tolist(),'distance_from_first_tower':float(d),'lateral_offset':offset,'height_span':float(heights.max()-heights.min()),'native_ground':float(original(*center)),'heading_degrees':float(np.degrees(np.arctan2(direction[0],direction[1])))})
candidates.sort(key=lambda q:q['height_span']+.006*abs(q['distance_from_first_tower']-100)+.12*abs(q['lateral_offset']))
report={'source':'Original USGS DEM using the same crop and elevation conversion as the saved layout','restored_region':'Only the recorded 545 by 380 metre factory grading and 90 metre transition','raw_changed_samples':changed,'raw_max_change_m':maximum,'raw_before_sha256':hashlib.sha256(oldpath.read_bytes()).hexdigest(),'raw_after_sha256':hashlib.sha256(newpath.read_bytes()).hexdigest(),'patches':patches,'station_candidates':candidates[:15],'tower_footings_preserved':True,'method':'Live terrain plus restored-minus-current input-map delta; no graph regeneration or new leveling'}
(root/'restoration_plan.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k not in ['patches','station_candidates']},indent=2))
print(json.dumps(candidates[:5],indent=2))
