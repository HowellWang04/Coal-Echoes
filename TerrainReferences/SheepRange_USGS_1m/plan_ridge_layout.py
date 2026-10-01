"""Read the native DEM and prepare a layout preview without modifying Unity."""
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).parent
survey = json.loads((OUT/'ridge_scene_survey.json').read_text(encoding='utf-8-sig'))['result']
config = json.loads((OUT/'layout_config.json').read_text(encoding='utf-8-sig'))
Image.MAX_IMAGE_PIXELS = None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    dem = np.array(im.crop((4806,2006,10006,8006)))

def sample(x,z):
    ix = np.asarray(x)+4924.00049-.5
    iz = 2085.999939-np.asarray(z)-.5
    if np.any(ix<0) or np.any(ix>=5199) or np.any(iz<0) or np.any(iz>=5999):
        raise ValueError('Outside native DEM')
    c=ix.astype(int);r=iz.astype(int);fx=ix-c;fz=iz-r
    return ((dem[r,c]*(1-fx)+dem[r,c+1]*fx)*(1-fz)+(dem[r+1,c]*(1-fx)+dem[r+1,c+1]*fx)*fz)-config['source_base_elevation']-710

towers={t['name']:t for t in survey['towers'] if t['name'].startswith('tajia')}
low=np.array(towers['tajia03 (10)']['pos'])[::2]
high=np.array(towers['tajia03 (7)']['pos'])[::2]
direction=(high-low)/np.linalg.norm(high-low)
ts=np.arange(-900,801,50)
pts=low+ts[:,None]*direction
print(json.dumps({'anchors':[low.tolist(),high.tolist()],'direction':direction.tolist(),'profile':[{'t':int(t),'x':float(p[0]),'z':float(p[1]),'y':float(sample(*p))} for t,p in zip(ts,pts)]},indent=2))

bounds=(-3200,-500,-2800,-800)
width=1350;height=1000
xs=np.linspace(bounds[0],bounds[1],width);zs=np.linspace(bounds[3],bounds[2],height)
h=sample(xs[None,:],zs[:,None]).astype(np.float32)
gz,gx=np.gradient(h,2,2)
light=np.clip((.6*gx-.4*gz+.707)/np.sqrt(gx*gx+gz*gz+1),0,1)
rgb=np.stack([(.32+.68*light)*225,(.32+.68*light)*200,(.32+.68*light)*155],axis=-1)
bands=np.floor(h/20);cont=np.zeros_like(h,dtype=bool);cont[1:,:]|=bands[1:,:]!=bands[:-1,:];cont[:,1:]|=bands[:,1:]!=bands[:,:-1]
rgb[cont]*=.72
canvas=Image.fromarray(rgb.astype(np.uint8));draw=ImageDraw.Draw(canvas)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
def pt(p): return ((p[0]-bounds[0])/(bounds[1]-bounds[0])*width,(bounds[3]-p[1])/(bounds[3]-bounds[2])*height)
draw.line([pt(p) for p in pts],fill='#ffffff',width=3)
for t,p in zip(ts,pts):
    if t%100==0:draw.text(pt(p),str(t),font=font,fill='#202020',stroke_width=1,stroke_fill='white')
for name,t in towers.items():
    x,y=pt(np.array(t['pos'])[::2]);colour='#ff4466' if name in ['tajia03 (10)','tajia03 (7)'] else '#ffce4b'
    draw.ellipse((x-6,y-6,x+6,y+6),fill=colour,outline='black',width=1)
    draw.text((x+8,y),name,font=font,fill=colour,stroke_width=1,stroke_fill='black')
origin=np.array(config['core_world_origin_xz']);r=config['factory_rect_local']
draw.rectangle((pt(origin+[r[0],r[3]]),pt(origin+[r[1],r[2]])),outline='#40e8ff',width=4)
draw.text((20,20),'Native DEM | 20 m contours | north up',font=font,fill='white',stroke_width=2,stroke_fill='black')
canvas.save(OUT/'ridge_analysis.png')

candidates=[]
for start in [-150,-100,-50,0]:
    p=low+start*direction
    for east in [0,50,100]:
        rect=[p[0]-20+east,p[0]+525+east,p[1]-370,p[1]+10]
        xx=np.linspace(rect[0],rect[1],110);zz=np.linspace(rect[2],rect[3],77)
        terrain=sample(xx[None,:],zz[:,None])
        y=float(np.median(terrain))
        candidates.append({'start_t':start,'east_offset':east,'rect':rect,'y':y,'range':[float(terrain.min()),float(terrain.max())], 'max_edit':float(np.max(abs(terrain-y)))})
(OUT/'ridge_factory_candidates.json').write_text(json.dumps(candidates,indent=2))

best=[]
for cx in np.arange(-2200,-1649,50):
    for cz in np.arange(-2150,-1349,50):
        for angle in [0,30,60,90]:
            a=np.deg2rad(angle);u=np.array([np.cos(a),np.sin(a)]);v=np.array([-np.sin(a),np.cos(a)])
            xx=np.linspace(-272.5,272.5,29)[None,:];zz=np.linspace(-190,190,21)[:,None]
            x=cx+xx*u[0]+zz*v[0];z=cz+xx*u[1]+zz*v[1]
            hh=sample(x,z);y=float(np.median(hh));maxedit=float(np.max(abs(hh-y)))
            center=np.array([cx,cz]);start=low-100*direction
            q=start-center; closest=center+np.clip(q@u,-272.5,272.5)*u+np.clip(q@v,-190,190)*v
            gap=float(np.linalg.norm(start-closest))
            # Favor the near foot while limiting excavation of original rock ridges.
            score=maxedit+gap*.11
            corners=[(center+su*272.5*u+sv*190*v).tolist() for su,sv in [(-1,-1),(1,-1),(1,1),(-1,1)]]
            best.append({'center':[int(cx),int(cz)],'angle':angle,'y':y,'max_edit':maxedit,'foot_gap':gap,'score':score,'corners':corners})
best.sort(key=lambda b:b['score'])
(OUT/'ridge_factory_search.json').write_text(json.dumps(best[:20],indent=2))

# Candidate only. The user's two horizontal anchor positions are preserved.
factory=best[0]
fine_t=np.arange(440,561,.5);fine_p=low+fine_t[:,None]*direction
end_t=float(fine_t[np.argmax(sample(fine_p[:,0],fine_p[:,1]))])
anchor_t=float(np.linalg.norm(high-low))
route_t=np.array([-90,-45,0,50,anchor_t,170,240,360,440,end_t])
route=low+route_t[:,None]*direction
ys=sample(route[:,0],route[:,1])
old_end=next(t for t in config['towers'] if t['name']=='tajia03 (11)')
proposal={'status':'preview only, not applied','factory':factory,'lower_anchor':'tajia03 (10)',
          'upper_anchor':'tajia03 (7)','route_length':float(end_t+90),'route_gain':float(ys[-1]-ys[0]),
          'peak_above_previous_endpoint':float(ys[-1]-old_end['ground_y']),
          'route':[{'number':i+1,'t':float(t),'world_xz':p.tolist(),'raw_ground_y':float(y)} for i,(t,p,y) in enumerate(zip(route_t,route,ys))],
          'restoration':'Recreate original DEM outside new factory and footing masks, disable superseded location locks, then rebuild protection at new locations.'}
(OUT/'ridge_layout_proposal.json').write_text(json.dumps(proposal,indent=2))

W,H=1800,1350;map_w,map_h=1250,1000
bounds=(-3000,-1400,-2350,-1070)
xs=np.linspace(bounds[0],bounds[1],map_w);zs=np.linspace(bounds[3],bounds[2],map_h)
h=sample(xs[None,:],zs[:,None]).astype(np.float32)
gz,gx=np.gradient(h,1.28,1.28)
shade=np.clip((.6*gx-.4*gz+.707)/np.sqrt(gx*gx+gz*gz+1),0,1)
rgb=np.stack([(.36+.64*shade)*211,(.36+.64*shade)*194,(.36+.64*shade)*158],axis=-1)
bands=np.floor(h/20);cont=np.zeros_like(h,dtype=bool);cont[1:,:]|=bands[1:,:]!=bands[:-1,:];cont[:,1:]|=bands[:,1:]!=bands[:,:-1];rgb[cont]*=.80
canvas=Image.new('RGB',(W,H),'#14212a');canvas.paste(Image.fromarray(rgb.astype(np.uint8)),(0,0));draw=ImageDraw.Draw(canvas)
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',24);small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',20);title=ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc',32)
def pt(p):return ((p[0]-bounds[0])/(bounds[1]-bounds[0])*map_w,(bounds[3]-p[1])/(bounds[3]-bounds[2])*map_h)
draw.polygon([pt(p) for p in factory['corners']],fill='#295b65',outline='#66f0e8',width=5)
for i in range(0,len(config['towers'])-1):
    a=np.array(config['towers'][i]['world_xz']);b=np.array(config['towers'][i+1]['world_xz'])
    for t in np.arange(0,1,.14):
        p=a+(b-a)*t;q=a+(b-a)*min(t+.07,1)
        draw.line([pt(p),pt(q)],fill='#a7b1bb',width=3)
for t in config['towers']:
    px,py=pt(t['world_xz'])
    if 0<px<map_w and 0<py<map_h:
        draw.line((px-5,py-5,px+5,py+5),fill='#cad0d8',width=2);draw.line((px-5,py+5,px+5,py-5),fill='#cad0d8',width=2)
draw.line([pt(p) for p in route],fill='#ffd45c',width=5)
for i,(p,t) in enumerate(zip(route,route_t)):
    px,py=pt(p);anchor=abs(t)<.01 or abs(t-anchor_t)<.01
    draw.ellipse((px-9,py-9,px+9,py+9),fill='#ff567c' if anchor else '#ffd45c',outline='#14212a',width=2)
    draw.text((px+13,py-15),str(i+1),font=font,fill='#ffffff',stroke_width=2,stroke_fill='#14212a')
draw.text((22,20),'小山脊布置候选 · 真实 DEM 俯视图',font=title,fill='white',stroke_width=2,stroke_fill='#14212a')
draw.text((22,65),'北向上   等高距20米   仅预览，尚未移动',font=small,fill='white',stroke_width=2,stroke_fill='#14212a')
fp=pt(factory['center']);draw.text((fp[0]-85,fp[1]-30),'拟建厂区平台',font=font,fill='#c1fffb')
draw.text((fp[0]-83,fp[1]+8),'545米 × 380米',font=small,fill='#c1fffb')
start=pt(route[0]);draw.text((start[0]+30,start[1]+15),'下站候选',font=small,fill='white',stroke_width=2,stroke_fill='#14212a')
draw.text((pt(route[-1])[0]+30,pt(route[-1])[1]-35),'更高的脊顶落点',font=small,fill='white',stroke_width=2,stroke_fill='#14212a')
draw.line((30,950,30+200/(bounds[1]-bounds[0])*map_w,950),fill='white',width=6);draw.text((30,960),'200米',font=small,fill='white')
draw.rectangle((map_w,0,W,map_h),fill='#14212a')
x=1280;y=35
rows=[('布局说明',title,'white'),('粉色点  你刚移动的两座架子',small,'#ff7896'),('黄色线  新索道候选路线',small,'#ffd45c'),('青色区  工厂整体迁移候选',small,'#66f0e8'),('灰色线  原路线，旧塔脚待恢复',small,'#bec9d3'),('',small,'white'),(f'新线路约 {proposal["route_length"]:.0f} 米',font,'white'),(f'地面起终点高差约 {proposal["route_gain"]:.0f} 米',font,'white'),(f'脊顶比原终点高约 {proposal["peak_above_previous_endpoint"]:.0f} 米',font,'white'),('保留两个标记的水平位置',small,'#bec9d3'),('其余架子沿直线投影布置',small,'#bec9d3'),('',small,'white'),('厂区按原占地整体旋转布置',font,'#66f0e8'),('平台距下站约200米',small,'#bec9d3'),('需另接一小段上站道路',small,'#bec9d3'),('采样估计最大整平约33米',small,'#bec9d3'),('最终平台边缘随建筑范围收紧',small,'#bec9d3'),('',small,'white'),('迁移后的恢复与锁',title,'white'),('旧厂区和旧塔脚回到原始 DEM',small,'#bec9d3'),('移除本轮旧位置锁的保护作用',small,'#bec9d3'),('新平台和新塔脚完成后再设锁',small,'#bec9d3'),('',small,'white'),('待验收',title,'white'),('缆绳净空、平台土方与道路坡度',small,'#bec9d3'),('此图不代表最终生成效果',small,'#bec9d3')]
for text,f,c in rows:
    draw.text((x,y),text,font=f,fill=c);y+=32 if f!=title else 44
# Longitudinal terrain profile, using raw survey heights rather than the edited Terrain.
left,right,top,bottom=90,1700,1080,1270
fine_t=np.linspace(route_t[0],route_t[-1],1200);fine=low+fine_t[:,None]*direction;hh=sample(fine[:,0],fine[:,1])
ymin=float(np.floor(hh.min()/20)*20);ymax=float(np.ceil((hh.max()+35)/20)*20)
def prof(t,v):return (left+(t-route_t[0])/(route_t[-1]-route_t[0])*(right-left),bottom-(v-ymin)/(ymax-ymin)*(bottom-top))
for level in np.arange(ymin,ymax+1,20):
    yy=prof(route_t[0],level)[1];draw.line((left,yy,right,yy),fill='#31404a');draw.text((10,yy-10),str(int(level)),font=small,fill='#a7b7c5')
draw.line([prof(t,v) for t,v in zip(fine_t,hh)],fill='#cda982',width=4)
for i,(t,v) in enumerate(zip(route_t,ys)):
    a=prof(t,v);b=prof(t,v+31.5);draw.line([a,b],fill='#ffd45c',width=3);draw.text((b[0]-9,b[1]-27),str(i+1),font=small,fill='white')
draw.text((90,1015),'沿线纵断面 · 棕色为原始地面 · 黄色塔架高度示意，缆绳尚未验算',font=font,fill='white')
draw.text((90,1300),'0米',font=small,fill='#b6c7d5');draw.text((1590,1300),f'{proposal["route_length"]:.0f}米',font=small,fill='#b6c7d5')
canvas.save(OUT/'ridge_layout_proposal.png')
