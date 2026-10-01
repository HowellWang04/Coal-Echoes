from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[1]
Image.MAX_IMAGE_PIXELS=None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    native=np.asarray(im.crop((4806,2006,10006,8006)),dtype=np.float32)
def height(x,z):
    ix=np.clip(np.asarray(x)+4924.00049-.5,0,5199); iz=np.clip(2085.999939-np.asarray(z)-.5,0,5999)
    c=ix.astype(int);r=iz.astype(int);fx=ix-c;fz=iz-r
    return ((native[r,c]*(1-fx)+native[r,np.minimum(c+1,5199)]*fx)*(1-fz)+(native[np.minimum(r+1,5999),c]*(1-fx)+native[np.minimum(r+1,5999),np.minimum(c+1,5199)]*fx)*fz)-1363.599609375-710
first=np.array([-2426.041,-1941.993]);second=np.array([-2452.7395,-1856.044])
old_dir=(second-first)/np.linalg.norm(second-first)
results=[]
for x in np.arange(-2570,-2200,3):
    for z in np.arange(-2360,-1930,3):
        c=np.array([x,z]);d=np.linalg.norm(first-c)
        if not 35<d<430: continue
        direction=(first-c)/d; right=np.array([direction[1],-direction[0]])
        angle=np.degrees(np.arccos(np.clip(np.dot(direction,old_dir),-1,1)))
        if angle>20:continue
        samples=np.array([c+direction*a+right*b for a in np.linspace(-5.7,5.7,9) for b in [-1.85,0,1.85]])
        hs=height(samples[:,0],samples[:,1]); ground=float(height(*c))
        if ground>-410:continue
        span=float(np.ptp(hs))
        # Station should remain below the first tower with a clear rising route.
        route=np.linspace(c+direction*8,first,50)
        routeheight=height(route[:,0],route[:,1])
        cableheight=np.linspace(float(hs.max())+4.7,-399,50)
        clearance=float(np.min(cableheight-routeheight))
        if clearance<2:continue
        results.append(dict(xz=c.tolist(),distance=float(d),angle=float(angle),heading=float(np.degrees(np.arctan2(direction[0],direction[1]))),ground=ground,span=span,clearance=clearance,score=span+0.001*d+.005*angle))
results.sort(key=lambda r:r['score'])
(root/'station_search.json').write_text(json.dumps(results[:40],indent=2))
x=np.linspace(-2640,-2110,530);z=np.linspace(-2420,-1780,640);X,Z=np.meshgrid(x,z);H=height(X,Z)
a=np.clip((H+510)/180,0,1);rgb=np.stack([80+150*a,100+130*a,60+150*a],axis=-1).astype('uint8')
rgb[np.abs((H+510)%5)<.25]=50
im=Image.fromarray(rgb[::-1]).resize((1060,1280));draw=ImageDraw.Draw(im)
def px(p):return ((p[0]+2640)*2,(2420+p[1])*-2+1280)
draw.line([px(first),px(second)],fill='red',width=4)
for i,r in enumerate(results[:8]):
    xx,zz=px(r['xz']);draw.ellipse((xx-4,zz-4,xx+4,zz+4),fill='blue');draw.text((xx+5,zz),str(i),fill='blue')
im.save(root/'Previews/StationSiteSearch.png')
print(json.dumps(results[:8],indent=2))
