"""Offline proposal only. Reads the live-scene snapshot and source DEMs."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).parent
Image.MAX_IMAGE_PIXELS = None
SURVEY = json.loads((OUT / 'snapshot/survey.json').read_text(encoding='utf-8-sig'))
FONT = 'C:/Windows/Fonts/msyh.ttc'

def font(size):
    return ImageFont.truetype(FONT, size)

def sample(a, x, y):
    x = np.clip(x, 0, a.shape[1]-1.001)
    y = np.clip(y, 0, a.shape[0]-1.001)
    i, j = x.astype(int), y.astype(int)
    u, v = x-i, y-j
    return (a[j,i]*(1-u)+a[j,i+1]*u)*(1-v)+(a[j+1,i]*(1-u)+a[j+1,i+1]*u)*v

def relief(h, spacing, contours=False):
    gy,gx = np.gradient(h, spacing)
    light = np.clip((-.5*gx-.6*gy+.62)/np.sqrt(1+gx*gx+gy*gy), 0, 1)
    t = np.clip((h+650)/850, 0, 1)
    low,high = np.array([191,167,124]),np.array([135,139,130])
    rgb = (low[None,None,:]*(1-t[:,:,None])+high[None,None,:]*t[:,:,None])*(.48+.78*light[:,:,None])
    if contours:
        bands=np.floor(h/50)
        edge=np.zeros_like(h,dtype=bool)
        edge[1:,:] |= bands[1:,:]!=bands[:-1,:]
        edge[:,1:] |= bands[:,1:]!=bands[:,:-1]
        rgb[edge]*=.82
    return Image.fromarray(np.clip(rgb,0,255).astype('uint8'))

def prepare():
    xs=np.linspace(-4924,1076,1501,dtype=np.float32)
    zs=np.linspace(2086,-3914,1501,dtype=np.float32)
    h=np.full((len(zs),len(xs)),np.nan,dtype=np.float32)
    for t in SURVEY['tiles']:
        x,y,z=t['p']; sx,sy,sz=t['size']; n=t['res']
        ix=np.where((xs>=x-.01)&(xs<=x+sx+.01))[0]
        iz=np.where((zs>=z-.01)&(zs<=z+sz+.01))[0]
        if not len(ix) or not len(iz): continue
        a=np.fromfile(OUT/'snapshot'/t['file'],dtype='<f4').reshape(n,n)
        h[np.ix_(iz,ix)]=sample(a,(xs[ix][None,:]-x)/sx*(n-1),(zs[iz][:,None]-z)/sz*(n-1))*sy+y
    assert np.isfinite(h).all()
    np.save(OUT/'current_height_4m.npy',h)
    im=relief(h,4)
    d=ImageDraw.Draw(im)
    for p in SURVEY['points']:
        x,y,z=p['p'];u=(x+4924)/4;v=(2086-z)/4
        d.ellipse((u-4,v-4,u+4,v+4),fill='red')
    im.save(OUT/'current_survey.png')
    for suffix in ['408','407']:
        with Image.open(f'C:/Users/Guanhao/Downloads/USGS_1M_11_x65y{suffix}_NV_Southern_D23.tif') as source:
            a=np.array(source.resize((1001,1001),Image.Resampling.NEAREST))
        np.save(OUT/f'source_{suffix}_10m.npy',a)
        valid=a>0
        im=relief(np.where(valid,a-2073.599609375,-700),10)
        black=Image.new('RGB',im.size,'#202b30');im=Image.composite(im,black,Image.fromarray((valid*255).astype('uint8')))
        d=ImageDraw.Draw(im)
        for k in range(0,1001,100):
            d.line((k,0,k,1000),fill='#777777');d.line((0,k,1000,k),fill='#777777')
            d.text((k+3,3),str(k*10),font=font(18),fill='white')
            d.text((3,k+3),str(k*10),font=font(18),fill='white')
        im.save(OUT/f'source_{suffix}_overview.png')

CORE = [(-4400,-2300),(-4220,650),(-3660,940),(-2750,870),(-2070,520),(-1660,-650),(-1560,-1810),(-1880,-2440),(-2920,-2540),(-3830,-2490)]
PIECES = [
    dict(id='A',source='407',source_center=[8200,3600],center=[-430,-680],scale=.70,angle=0,radius=[1390,2250],offset=-1580),
    dict(id='B',source='408',source_center=[6500,1550],center=[-2530,-3210],scale=.65,angle=90,radius=[2100,920],offset=-1560),
    dict(id='C',source='407',source_center=[8300,7700],center=[-1820,1410],scale=.70,angle=70,radius=[1970,1000],offset=-1520),
]

def smooth(t):
    t=np.clip(t,0,1)
    return t*t*(3-2*t)

def compose():
    h=np.load(OUT/'current_height_4m.npy')
    x=np.linspace(-4924,1076,1501)[None,:]
    z=np.linspace(2086,-3914,1501)[:,None]
    def pixel(p): return ((p[0]+4924)/4,(2086-p[1])/4)
    maskimage=Image.new('L',(1501,1501),0)
    ImageDraw.Draw(maskimage).polygon([pixel(p) for p in CORE],fill=255)
    protected=np.array(maskimage)>0
    distance=np.full_like(h,1e9)
    for p,q in zip(CORE,CORE[1:]+CORE[:1]):
        vx,vz=q[0]-p[0],q[1]-p[1]
        t=np.clip(((x-p[0])*vx+(z-p[1])*vz)/(vx*vx+vz*vz),0,1)
        distance=np.minimum(distance,np.sqrt((x-p[0]-t*vx)**2+(z-p[1]-t*vz)**2))
    keep=smooth(distance/360);keep[protected]=0
    result=h.copy(); weights=[]
    for p in PIECES:
        a=np.load(OUT/f"source_{p['source']}_10m.npy")
        dx=x-p['center'][0];dz=z-p['center'][1]
        theta=np.deg2rad(p['angle']); c,s=np.cos(theta),np.sin(theta)
        sx=(c*dx+s*dz)/p['scale']+p['source_center'][0]
        sy=-(-s*dx+c*dz)/p['scale']+p['source_center'][1]
        valid=(sx>=0)&(sx<10000)&(sy>=0)&(sy<10000)
        source=sample(a,sx/10.002,sy/10.002)
        valid &= source>0
        target=source*p['scale']+p['offset']
        radius=np.sqrt((dx/p['radius'][0])**2+(dz/p['radius'][1])**2)
        # Fade before source coverage ends, so missing DEM cells never form cliffs.
        coverage=smooth((sx-4900)/600)*smooth((9700-sx)/400)*smooth((sy-200)/400)*smooth((9700-sy)/400)
        irregular=.035*np.sin(dx/310+dz/460)+.025*np.sin(dx/720-dz/280)
        w=smooth((1-radius+irregular)/.42)*keep*valid*coverage
        result=result*(1-w)+target*w
        weights.append(w)
    result[protected]=h[protected]
    np.save(OUT/'proposal_height_4m.npy',result.astype(np.float32))
    np.save(OUT/'protected_mask.npy',protected)
    np.save(OUT/'change_mask.npy',np.max(weights,axis=0).astype(np.float32))
    low=float(np.floor(min(h.min(),result.min())))
    high=float(np.ceil(max(h.max(),result.max())))
    Image.fromarray(np.round((result-low)/(high-low)*65535).astype('uint16')).save(OUT/'proposal_height_16bit.png')
    for point in SURVEY['points']:
        u,v=pixel((point['p'][0],point['p'][2]))
        assert protected[round(v),round(u)],point['name']
    for building in SURVEY['buildings']:
        for px in [building['min'][0],building['max'][0]]:
            for pz in [building['min'][2],building['max'][2]]:
                u,v=pixel((px,pz));assert protected[round(v),round(u)],building['name']
    checks=dict(protected_samples=int(protected.sum()),protected_max_delta_m=float(np.abs(result[protected]-h[protected]).max()),resolution_m=4,source_fragment_resolution_m=10,extent_xz=[-4924,1076,-3914,2086],height_units='Unity world Y meters',pieces=PIECES,protected_polygon=CORE,scene_snapshot=SURVEY['scene'],scene_dirty_at_snapshot=SURVEY['sceneDirty'],stations=2,towers=10,buildings=26,max_raise_m=float((result-h).max()),max_lower_m=float((result-h).min()))
    checks['height_png_encoding']=dict(min_world_y=low,max_world_y=high,row_zero='north +Z',note='preview quantization only; use float32 npy for exact core preservation')
    checks['all_survey_points_and_building_bounds_protected']=True
    (OUT/'proposal.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
    render(h,result,weights)

def render(h,proposal,weights,applied=False):
    cream='#f4f0e8'; ink='#273938'; muted='#66736c'; cyan='#55ebde'; orange='#ed9b55'
    page=Image.new('RGB',(2780,1950),cream);d=ImageDraw.Draw(page)
    d.text((65,42),'外围山体落地结果' if applied else '外围山体围合方案',font=font(56),fill=ink)
    d.text((68,117),'方案 01   中心山体保留 · 外围真实 DEM 片段组合 · '+('实际 Terrain 回读' if applied else '当前场景定位'),font=font(25),fill=muted)
    size=1550;ox,oy=65,200
    terrain=relief(proposal,4,True).resize((size,size),Image.Resampling.LANCZOS)
    page.paste(terrain,(ox,oy));d=ImageDraw.Draw(page)
    def pt(p):return (ox+(p[0]+4924)/6000*size,oy+(2086-p[1])/6000*size)
    def label(pos,text,color=ink,fs=24):
        box=d.textbbox((0,0),text,font=font(fs));w=box[2]+22;hh=box[3]-box[1]+18
        d.rounded_rectangle((pos[0],pos[1],pos[0]+w,pos[1]+hh),radius=5,fill=cream)
        d.text((pos[0]+11,pos[1]+4),text,font=font(fs),fill=color)
    d.line([pt(p) for p in CORE+[CORE[0]]],fill=cyan,width=4)
    label(pt((-4000,-150)),'中心保留区',ink,28)
    label(pt((-4400,-610)),'蓝线内采样高度变化为 0 米',ink,21)
    for p,w in zip(PIECES,weights):
        edge=(w>.05);boundary=np.zeros_like(edge);boundary[1:,:]|=edge[1:,:]!=edge[:-1,:];boundary[:,1:]|=edge[:,1:]!=edge[:,:-1]
        yy,xx=np.where(boundary)
        # Sparse contour marks keep terrain legible.
        for j in range(0,len(xx),9):
            u=ox+xx[j]/1500*size;v=oy+yy[j]/1500*size
            d.ellipse((u-1,v-1,u+1,v+1),fill=orange)
    label(pt((-730,-150)),'A  东侧长山脊',ink,27)
    label(pt((-3350,-3240)),'B  南侧错位山口',ink,27)
    label(pt((-2210,1610)),'C  北侧背景山体',ink,27)
    # Proposed turning exit remains separate from all actual object locations.
    exitroute=[(-1900,-2280),(-1530,-2500),(-1190,-2770),(-780,-2940),(-390,-2980)]
    for a,b in zip(exitroute,exitroute[1:]):d.line([pt(a),pt(b)],fill='#f9eed2',width=5)
    label(pt((-1600,-2690)),'预留出山通道',ink,21)
    points={p['name']:p['p'] for p in SURVEY['points']}
    towers=sorted([n for n in points if n.startswith('CablewayTower_')])
    route=[points['CablewayFoothillStation']]+[points[n] for n in towers]+[points['CablewaySummitStation']]
    d.line([pt((p[0],p[2])) for p in route],fill='#ffe479',width=5)
    for p in route:
        u,v=pt((p[0],p[2]));d.ellipse((u-4,v-4,u+4,v+4),fill='#ffe479',outline=ink,width=1)
    # Factory outline is a padded hull of measured building bounds, not a legal boundary.
    corners=[]
    for b in SURVEY['buildings']:
        mn,mx=b['min'],b['max'];corners.extend([(mn[0]-15,mn[2]-15),(mx[0]+15,mn[2]-15),(mx[0]+15,mx[2]+15),(mn[0]-15,mx[2]+15)])
    def hull(points):
        pts=sorted(set(points))
        def cross(o,a,b):return (a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0])
        lo=[];hi=[]
        for p in pts:
            while len(lo)>=2 and cross(lo[-2],lo[-1],p)<=0:lo.pop()
            lo.append(p)
        for p in reversed(pts):
            while len(hi)>=2 and cross(hi[-2],hi[-1],p)<=0:hi.pop()
            hi.append(p)
        return lo[:-1]+hi[:-1]
    factory=hull(corners)
    d.line([pt(p) for p in factory+[factory[0]]],fill='#ff886a',width=4)
    label(pt((-2290,-2020)),'洗煤厂',ink,23)
    label(pt((-3200,-740)),'上站',ink,22)
    label(pt((-2980,-1970)),'下站',ink,22)
    # Compass and scale.
    d.line((ox+size-65,oy+110,ox+size-65,oy+50),fill=cream,width=4)
    d.polygon([(ox+size-65,oy+35),(ox+size-76,oy+57),(ox+size-54,oy+57)],fill=cream)
    d.text((ox+size-90,oy+120),'北',font=font(26),fill=cream)
    u,v=ox+40,oy+size-56;length=size/6
    d.line((u,v,u+length,v),fill=cream,width=6)
    d.text((u,v-38),'0                       1 km',font=font(21),fill=cream)
    # Detail inset is anchored to current measured XZ positions.
    ix,iy,iw,ih=1670,255,1040,1170
    bounds=(-3070,-1580,-2240,-565)
    xx=np.linspace(bounds[0],bounds[1],iw)[None,:];zz=np.linspace(bounds[3],bounds[2],ih)[:,None]
    inset=sample(h,(xx+4924)/4,(2086-zz)/4)
    page.paste(relief(inset,(bounds[1]-bounds[0])/iw,True),(ix,iy));d=ImageDraw.Draw(page)
    d.text((ix,195),'工厂与索道定位详图',font=font(32),fill=ink)
    def ip(p):return (ix+(p[0]-bounds[0])/(bounds[1]-bounds[0])*iw,iy+(bounds[3]-p[1])/(bounds[3]-bounds[2])*ih)
    d.line([ip(p) for p in factory+[factory[0]]],fill='#ff886a',width=4)
    for b in SURVEY['buildings']:
        mn,mx=b['min'],b['max'];u,v=ip((mn[0],mx[2]));u2,v2=ip((mx[0],mn[2]))
        d.rectangle((u,v,u2,v2),fill='#a95443',outline='#fbc5a9',width=1)
    d.line([ip((p[0],p[2])) for p in route],fill='#ffe479',width=5)
    for name in towers:
        p=points[name];u,v=ip((p[0],p[2]));n=int(name[-2:])
        d.polygon([(u,v-8),(u+8,v),(u,v+8),(u-8,v)],fill='#ffe479',outline=ink)
        side=-1 if n%2==0 else 1
        tx=u+27 if side>0 else u-112
        d.line((u+side*8,v,u+side*23,v),fill=cream,width=2)
        label((tx,v-19),f'{n:02d} 号塔',ink,21)
    for name,title,shift in [('CablewaySummitStation','上站  旧装煤站',(28,-54)),('CablewayFoothillStation','下站  山脚站棚',(-218,30))]:
        p=points[name];u,v=ip((p[0],p[2]));d.rectangle((u-10,v-10,u+10,v+10),fill='#70eee0',outline=ink,width=2);label((u+shift[0],v+shift[1]),title,ink,24)
    p=points['FactoryGate'];u,v=ip((p[0],p[2]));d.ellipse((u-7,v-7,u+7,v+7),fill='#ff886a',outline=ink);label((u-105,v-55),'现有厂门',ink,22)
    label(ip((-2190,-1990)),'洗煤厂建筑区',ink,24)
    d.text((ix,iy+ih+20),'红色轮廓按 26 栋厂房包围盒外扩绘制',font=font(22),fill=muted)
    d.text((ix,iy+ih+54),'黄色折线连接塔架位置，仅表示索道走向',font=font(22),fill=muted)
    d.text((ix,iy+ih+88),'两站与 10 座塔架均读取自当前主场景',font=font(22),fill=muted)
    d.text((ix,iy+ih+155),'本轮预览重点',font=font(29),fill=ink)
    for k,line in enumerate(['东侧补足围合，保留厂前谷地空间','南侧用交错山脚留出弯折的出入通道','北侧形成远景层次，中心山脊和索道保持']):
        d.text((ix,iy+ih+206+k*42),line,font=font(23),fill=muted)
    d.text((65,1790),'青色实线  中心保留范围      橙色点线  外围'+('修改' if applied else '候选片段影响')+'范围',font=font(26),fill=ink)
    d.text((65,1840),'底图为'+('Unity 地形回读' if applied else '离线高程组合预览')+' · 50 米等高线 · 图上北为 Unity +Z · '+('已保存主场景' if applied else '尚未写入 Unity 地形'),font=font(24),fill=muted)
    d.text((65,1885),'中心保留区约 767 万个原生高度采样值未变。外围已使用原始 DEM 细节，主地形接缝不超过约 3 厘米。' if applied else '中心采用当前 Terrain 高度快照，外围候选使用现有 USGS 数据的 10 米采样。保留边界与山口走向供本轮审阅。',font=font(22),fill=muted)
    page.save(OUT/'terrain_proposal.png')
    comparison=Image.new('RGB',(2460,1370),cream);cd=ImageDraw.Draw(comparison)
    for j,(title,a) in enumerate([('修改前地形' if applied else '当前地形',h),('外围山体已应用' if applied else '外围山体候选',proposal)]):
        offset=40+j*1220
        cd.text((offset,25),title,font=font(38),fill=ink)
        comparison.paste(relief(a,4).resize((1180,1180)),(offset,100))
    cd.text((40,1300),'同一范围与同一光照，仅供比较山体形态。中心保留区采样高程完全相同。',font=font(25),fill=muted)
    comparison.save(OUT/'terrain_comparison.png')

if __name__ == '__main__':
    if not (OUT/'current_height_4m.npy').exists(): prepare()
    compose()
