from pathlib import Path
import os,sys,json
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
os.environ['MPLCONFIGDIR']=str(ROOT/'plot-cache')
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource,LinearSegmentedColormap
plt.rcParams['font.sans-serif']=['Microsoft YaHei']
plt.rcParams['axes.unicode_minus']=False
meta=json.loads((ROOT/'metadata.json').read_text())
before=np.load(ROOT/'before.npy');after=np.load(ROOT/'candidate.npy')
extent=meta['extent_xz'];x=np.linspace(extent[0],extent[1],len(after));z=np.linspace(extent[2],extent[3],len(after))
palette=LinearSegmentedColormap.from_list('sand',['#c09865','#af916b','#dbccae'])
light=LightSource(315,40)
fig,axes=plt.subplots(1,2,figsize=(16,8),layout='constrained')
core=np.array(meta['protected_polygon']+[meta['protected_polygon'][0]])
for ax,h,title in zip(axes,[before,after],['修改前','实测地形延伸到地图边界']):
    rgb=light.shade(h[::3,::3],palette,dx=6,dy=6,vert_exag=1,vmin=-710,vmax=390,blend_mode='soft')
    ax.imshow(rgb,origin='lower',extent=extent)
    ax.plot(core[:,0],core[:,1],color='#29dcd2',lw=1)
    ax.set_xlim(-4924,1076);ax.set_ylim(-3914,2086);ax.set_title(title);ax.set_xlabel('X 米');ax.set_ylabel('Z 米')
fig.suptitle('高度表面预览    青线内为中心保护区    不含 Unity 材质')
fig.savefig(ROOT/'comparison.png',dpi=130);plt.close(fig)
fig=plt.figure(figsize=(16,8),layout='constrained')
ix=np.arange(36,3037,12);xx,zz=np.meshgrid(x[ix],z[ix])
for i,(h,title) in enumerate(zip([before,after],['修改前','实测地形与梯度过渡'])):
    ax=fig.add_subplot(1,2,i+1,projection='3d');yy=h[np.ix_(ix,ix)]
    colors=light.shade(yy,palette,dx=24,dy=24,vert_exag=1,vmin=-710,vmax=390,blend_mode='soft')
    ax.plot_surface(xx,zz,yy,facecolors=colors,shade=False,linewidth=0,antialiased=False,rcount=len(ix),ccount=len(ix))
    ax.set_box_aspect((6000,6000,1100));ax.set_zlim(-710,390);ax.view_init(42,-62);ax.set_title(title)
fig.savefig(ROOT/'comparison_3d.png',dpi=130);plt.close(fig)
print('Previews written')
