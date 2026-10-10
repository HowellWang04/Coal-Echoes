from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw

P=Path(__file__).parent
tiles=json.loads((P/'tiles.json').read_text())
h=np.zeros((3073,2049),np.float32)
a=np.zeros_like(h)
old=np.zeros_like(h)
for t in tiles:
    px,py,pz=t['p']; n=t['n']; ar=t['ar']
    x=round((px+2924.00049)/1000)*1024
    z=round((pz+2914)/1000)*1024
    hh=np.fromfile(P/(t['name']+'.h.f32'),'<f4').reshape(n,n)*1000-710
    aa=np.fromfile(P/(t['name']+'.a.f32'),'<f4').reshape(ar,ar,3)
    oo=np.fromfile(P.parent.parent/'Applied'/'Before'/(t['name']+'.f32'),'<f4').reshape(n,n)*1000-710
    h[z:z+n,x:x+n]=hh;a[z:z+n,x:x+n]=aa[:,:,1];old[z:z+n,x:x+n]=oo
gz,gx=np.gradient(h,1000/1024)
slope=np.rad2deg(np.arctan(np.hypot(gx,gz)))
shade=np.clip((.7-.6*gx+.4*gz)/np.sqrt(1+gx*gx+gz*gz),0,1)
panels=[('Actual surface',shade),('Dark rock weight',a),('Slope 0 to 60 deg',np.clip(slope/60,0,1)),('Height change -200 to 200m',np.clip((h-old+200)/400,0,1))]
canvas=Image.new('RGB',(1600,670),'#202020');draw=ImageDraw.Draw(canvas)
for i,(label,data) in enumerate(panels):
    im=Image.fromarray(np.uint8(np.flipud(data)*255)).convert('RGB').resize((400,600))
    canvas.paste(im,(400*i,40));draw.text((400*i+10,10),label,fill='white')
    for wx in [-2500,-2000,-1500,-1000]:
        xx=400*i+(wx+2924.00049)/2000*400
        draw.line((xx,40,xx,640),fill=(100,70,50),width=1)
    for wz in [-2000,-1000,0]:
        yy=40+(86-wz)/3000*600
        draw.line((400*i,yy,400*i+400,yy),fill=(100,70,50),width=1)
draw.text((10,648),'Bounds X -2924 to -924, Z -2914 to 86; north up. Grid X 500m, Z 1000m.',fill='white')
canvas.save(P/'maps.png')
