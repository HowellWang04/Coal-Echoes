"""Scientific before and after figures from the native-resolution offline candidate."""
from pathlib import Path
import sys
import os
import json
ROOT=Path(__file__).resolve().parent
OLD=ROOT.parent/'WholeMapGradient800'
sys.path.insert(0,str(ROOT.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
os.environ['MPLCONFIGDIR']=str(ROOT/'plot-cache')
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource,LinearSegmentedColormap,TwoSlopeNorm
plt.rcParams['font.sans-serif']=['Microsoft YaHei']
plt.rcParams['axes.unicode_minus']=False

survey=json.loads((OLD/'before.json').read_text())
t=next(t for t in survey['terrains'] if t['name']=='Tile_2_2_Main')
n=t['res'];step=1000/(n-1)
before=np.fromfile(OLD/'After/Tile_2_2_Main.height.f32',np.float32).reshape(n,n)*1000-710
after=np.fromfile(ROOT/'NativePreview/Tile_2_2_Main.f32',np.float32).reshape(n,n)*1000-710
xs=t['p'][0]+np.arange(n)*step;zs=t['p'][2]+np.arange(n)*step
light=LightSource(310,45)
cmap=LinearSegmentedColormap.from_list('sandstone',['#a78257','#c9a470','#aa8764','#ded1b3'])
fig=plt.figure(figsize=(15,7),layout='constrained')
indices=np.arange(0,n,4)
X,Z=np.meshgrid(xs[indices],zs[indices])
for col,(h,title) in enumerate([(before,'当前地形，仍有垂直台阶'),(after,'修正输入后的离线候选')]):
    ax=fig.add_subplot(1,2,col+1,projection='3d')
    Y=h[np.ix_(indices,indices)]
    rgb=light.shade(Y,cmap,vmin=-710,vmax=30,dx=step*4,dy=step*4,vert_exag=1,blend_mode='soft')
    ax.plot_surface(X,Z,Y,facecolors=rgb,rstride=1,cstride=1,shade=False,linewidth=0,antialiased=False,rasterized=True)
    ax.view_init(38,155)
    ax.set_box_aspect((1000,1000,740))
    ax.set_xlim(xs[0],xs[-1]);ax.set_ylim(zs[0],zs[-1]);ax.set_zlim(-710,30)
    ax.set_axis_off();ax.set_title(title,fontsize=16)
fig.suptitle('Tile 2,2 断层修复对比 · 相同视角与真实高宽比例',fontsize=19)
fig.text(.5,.025,'只显示高度表面，不含 Unity 材质    本次没有降低北侧山顶，正式场景未应用',ha='center',fontsize=12)
fig.savefig(ROOT/'comparison_3d.png',dpi=160);plt.close(fig)

fig,axes=plt.subplots(1,3,figsize=(17,5.5),layout='constrained')
repro=json.loads((ROOT.parent/'Tile22CliffDiagnosis/reproduction.json').read_text())
for ax,h,title in zip(axes[:2],[before,after],['修复前的相邻高度差','候选的相邻高度差']):
    jumps=np.zeros_like(h)
    jumps[:,:-1]=abs(np.diff(h,axis=1))
    jumps[:-1]=np.maximum(jumps[:-1],abs(np.diff(h,axis=0)))
    im=ax.imshow(jumps,origin='lower',extent=[xs[0],xs[-1],zs[0],zs[-1]],cmap='magma',vmin=0,vmax=50)
    ax.axhline(zs[886],color='#43d8e4',lw=.9,ls=':')
    ax.set_title(title);ax.set_xlabel('Unity X / 米');ax.set_ylabel('Unity Z / 米')
fig.colorbar(im,ax=axes[:2],shrink=.7,label='每约 0.98 米的高度变化 / 米')
row=886;mask=(xs>-710)&(xs<-610)
axes[2].plot(xs[mask],before[row,mask],color='#bb523c',label='当前地形')
axes[2].plot(xs[mask],after[row,mask],color='#27847f',label='修复候选')
axes[2].set_xlabel('Unity X / 米');axes[2].set_ylabel('世界高度 / 米')
axes[2].set_title('截图中异常位置的剖面')
axes[2].grid(alpha=.2);axes[2].legend()
fig.savefig(ROOT/'steps_and_profile.png',dpi=150);plt.close(fig)

meta=json.loads((ROOT/'metadata.json').read_text());x0,x1,z0,z1=meta['extent_xz']
correction=np.load(ROOT/'repair_delta_m.npy')
core=np.load(ROOT/'protected.npy');crests=np.load(ROOT/'crests.npy');accepted=np.load(ROOT/'accepted.npy')
xx=np.linspace(x0,x1,correction.shape[1])[::4];zz=np.linspace(z0,z1,correction.shape[0])[::4]
fig,ax=plt.subplots(figsize=(8,8),layout='constrained')
limit=max(abs(correction.min()),abs(correction.max()))
im=ax.imshow(correction[::4,::4],origin='lower',extent=[x0,x1,z0,z1],cmap='RdBu_r',vmin=-limit,vmax=limit)
for m,col in [(core,'#19aa88'),(accepted,'#9152c1'),(crests,'#303030')]:
    ax.contour(xx,zz,m[::4,::4],[.5],colors=[col],linewidths=.8)
ax.plot([xs[0],xs[-1],xs[-1],xs[0],xs[0]],[zs[0],zs[0],zs[-1],zs[-1],zs[0]],color='#e39024',lw=1.5)
ax.text(xs[0]+60,zs[-1]-120,'Tile 2,2',color='#94540b')
ax.set_xlim(-4924,1076);ax.set_ylim(-3914,2086)
ax.set_title('相对当前地形的修正范围\n绿色中心、紫色圈 1、黑色固定山脊均保持原高')
ax.set_xlabel('Unity X / 米');ax.set_ylabel('Unity Z / 米')
fig.colorbar(im,ax=ax,shrink=.7,label='高度修正 / 米')
fig.savefig(ROOT/'correction_map.png',dpi=150);plt.close(fig)
