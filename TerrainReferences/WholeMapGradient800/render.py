from pathlib import Path
import sys,os,json
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
os.environ['MPLCONFIGDIR']=str(ROOT/'plot-cache')
import numpy as np
from scipy.ndimage import binary_erosion,gaussian_filter
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource,LinearSegmentedColormap
plt.rcParams['font.sans-serif']=['Microsoft YaHei'];plt.rcParams['axes.unicode_minus']=False
meta=json.loads((ROOT/'metadata.json').read_text());x0,x1,z0,z1=meta['extent_xz'];step=meta['spacing_m']
current=np.load(ROOT/'current.npy');candidate=np.load(ROOT/'candidate.npy');base=np.load(ROOT/'base.npy');free=np.load(ROOT/'free.npy');core=np.load(ROOT/'protected.npy');accepted=np.load(ROOT/'accepted.npy');valid=np.load(ROOT/'valid.npy')
crests=np.load(ROOT/'crests.npy')
assert np.array_equal(candidate[core|accepted|crests],current[core|accepted|crests])
def slope(h):
    dz,dx=np.gradient(h,step);return np.rad2deg(np.arctan(np.hypot(dx,dz)))
sc,sa,sb=[slope(h) for h in [current,candidate,base]]
first=free&~binary_erosion(free)
report=dict(core_changed=int(np.count_nonzero(candidate[core]!=current[core])),accepted_changed=int(np.count_nonzero(candidate[accepted]!=current[accepted])),artificial_steep_screen_before=int(np.count_nonzero(free&(sc>40)&(sc-sb>15))),artificial_steep_screen_after=int(np.count_nonzero(free&(sa>40)&(sa-sb>15))),first_free_row_delta_max_m=float(abs(candidate[first]-current[first]).max()),slope_free_p99_before=float(np.percentile(sc[free],99)),slope_free_p99_after=float(np.percentile(sa[free],99)))
(ROOT/'candidate-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report),flush=True)
cmap=LinearSegmentedColormap.from_list('terrain_coal',['#aa865a','#d0ae7a','#96826b','#ded4bb']);light=LightSource(315,40)
extent=[x0,x1,z0,z1];xx=np.linspace(x0,x1,current.shape[1])[::2];zz=np.linspace(z0,z1,current.shape[0])[::2]
survey=json.loads((ROOT.parent/'PeripheralMountainsPreview/snapshot/survey.json').read_text(encoding='utf-8-sig'))
fig,axes=plt.subplots(1,2,figsize=(16,9),layout='constrained')
for ax,h,title in zip(axes,[current,candidate],['应用前，保留已确认圈 1','全图外围 800 米梯度过渡']):
    rgb=light.shade(h[::2,::2],cmap,vmin=-710,vmax=290,dx=4,dy=4,vert_exag=1,blend_mode='soft')
    ax.imshow(rgb,origin='lower',extent=extent)
    ax.contour(xx,zz,core[::2,::2],[.5],colors=['#21e4ce'],linewidths=1)
    ax.contour(xx,zz,accepted[::2,::2],[.5],colors=['#df9aff'],linewidths=1)
    points={p['name']:p['p'] for p in survey['points']};towers=sorted(k for k in points if k.startswith('CablewayTower_'))
    route=[points['CablewayFoothillStation']]+[points[k] for k in towers]+[points['CablewaySummitStation']]
    ax.plot([p[0] for p in route],[p[2] for p in route],'.-',color='#ffe57c',lw=1,ms=3)
    for name,label in [('CablewayFoothillStation','下站'),('CablewaySummitStation','上站')]:
        p=points[name];ax.text(p[0]+45,p[2],label,color='white',fontsize=10,bbox=dict(fc='#2c3437',alpha=.8,pad=2))
    for building in survey['buildings']:
        mn,mx=building['min'],building['max'];ax.plot([mn[0],mx[0],mx[0],mn[0],mn[0]],[mn[2],mn[2],mx[2],mx[2],mn[2]],color='#ff9474',lw=.6)
    ax.text(-2200,-2220,'洗煤厂',color='white',fontsize=10,bbox=dict(fc='#2c3437',alpha=.8,pad=2))
    ax.set_title(title);ax.set_xlabel('Unity X / 米');ax.set_ylabel('Unity Z / 米');ax.set_xlim(-4924,1076);ax.set_ylim(-3914,2086)
fig.suptitle('青线内中心固定 · 紫线内圈 1 固定 · 黄色为两站与十座塔架',fontsize=15)
fig.savefig(ROOT/'height_comparison.png',dpi=160);plt.close(fig)

windows=[('北侧圈 3',(-3300,300,0,2070),40,-65),('东侧接合',(-1900,1020,-2500,300),38,-135),('南侧剩余过渡',(-2600,700,-3880,-2300),40,-65)]
fig=plt.figure(figsize=(17,10),layout='constrained')
for col,(label,(xa,xb,za,zb),elev,azim) in enumerate(windows):
    ix=np.arange(max(0,round((xa-x0)/step)),min(current.shape[1],round((xb-x0)/step)),4);iz=np.arange(max(0,round((za-z0)/step)),min(current.shape[0],round((zb-z0)/step)),4)
    X,Z=np.meshgrid(x0+ix*step,z0+iz*step)
    for row,(h,title) in enumerate([(current,'应用前'),(candidate,'800 米方案')]):
        ax=fig.add_subplot(2,3,row*3+col+1,projection='3d');Y=h[np.ix_(iz,ix)]
        colors=light.shade(Y,cmap,vmin=-710,vmax=290,dx=8,dy=8,vert_exag=1,blend_mode='soft')
        ax.plot_surface(X,Z,Y,facecolors=colors,rstride=1,cstride=1,shade=False,linewidth=0,antialiased=False,rasterized=True)
        ax.view_init(elev,azim);ax.set_box_aspect((xb-xa,zb-za,1000));ax.set_zlim(-710,290);ax.set_axis_off();ax.set_title(label+' · '+title)
fig.suptitle('实际比例高度表面 · 同视角同光照 · 不含 Unity 材质',fontsize=15);fig.savefig(ROOT/'terrain_comparison_3d.png',dpi=140);plt.close(fig)
