"""Refine the same Poisson equation and produce offline checks and previews."""
from pathlib import Path
import json
import os
import sys
import time

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
sys.path.insert(0,str(ROOT.parent/'PeripheralMountainsPreview'))
os.environ['OPENBLAS_NUM_THREADS']='1'
os.environ['OMP_NUM_THREADS']='1'
os.environ['MPLCONFIGDIR']=str(ROOT/'plot-cache')
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.linalg import cg
from scipy.ndimage import gaussian_filter
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource,LinearSegmentedColormap
from build_preview import sample,CORE

meta=json.loads((ROOT/'metadata.json').read_text())
step=meta['spacing_m']; n=meta['resolution']; x0,x1,z0,z1=meta['extent_xz']
current=np.load(ROOT/'current.npy'); before=np.load(ROOT/'before.npy')
original=np.load(ROOT/'original.npy'); fixed=np.load(ROOT/'fixed.npy'); core=np.load(ROOT/'protected.npy')
region=np.load(ROOT/'region.npy'); distance=np.load(ROOT/'distance.npy')
free=~fixed
index=np.full((n,n),-1,dtype=np.int32); index[free]=np.arange(free.sum())
rows=[np.arange(free.sum(),dtype=np.int32)]; columns=[rows[0]]; values=[np.full(free.sum(),4.0)]
for left,right in [(index[:,:-1],index[:,1:]),(index[:-1,:],index[1:,:])]:
    valid=(left>=0)&(right>=0)
    a,b=left[valid],right[valid]
    rows.extend([a,b]); columns.extend([b,a]); values.extend([-np.ones(a.size),-np.ones(a.size)])
matrix=coo_matrix((np.concatenate(values),(np.concatenate(rows),np.concatenate(columns))),shape=(free.sum(),free.sum())).tocsr()
results={}
heights={'current':current}

def slope(h):
    gz,gx=np.gradient(h.astype(float),step)
    return np.rad2deg(np.arctan(np.hypot(gx,gz)))

probes=json.loads((ROOT.parent/'PeripheralMountainsPreview/Diagnosis/transition_measurements.json').read_text())[:2]
for name in ['A','B']:
    folder=ROOT/name
    altitude=np.fromfile(folder/'altitude.f32',dtype='<f4').reshape(n,n).astype(float)
    lap=np.fromfile(folder/'laplacian.f32',dtype='<f4').reshape(n,n).astype(float)
    initial=np.fromfile(folder/'author_result.f32',dtype='<f4').reshape(n,n).astype(float)
    # The author shader uses h = mean(neighbours) - laplacian.
    # Therefore the SPD free-node system is 4*h - sum(free neighbours)
    # = sum(fixed neighbours) - 4*laplacian. This only refines the original
    # equation after the upstream GPU initialization; no new terrain model.
    anchors=np.where(fixed,altitude,0)
    rhs=-4*lap
    rhs[1:,:]+=anchors[:-1,:]; rhs[:-1,:]+=anchors[1:,:]
    rhs[:,1:]+=anchors[:,:-1]; rhs[:,:-1]+=anchors[:,1:]
    b=rhs[free]
    iterations=[0]
    def tick(_): iterations[0]+=1
    started=time.perf_counter()
    solution,info=cg(matrix,b,x0=initial[free],rtol=1e-8,atol=0,maxiter=5000,callback=tick)
    if info!=0: raise RuntimeError(f'{name} refinement failed with code {info}')
    raw=altitude.copy(); raw[free]=solution
    output=(raw*1000-710).astype(np.float32)
    output[fixed]=current[fixed]
    assert np.array_equal(output[core],current[core])
    assert np.array_equal(output[fixed],current[fixed])
    assert np.isfinite(output).all()
    residual=(matrix@solution-b)/4*1000
    np.save(folder/'candidate_world_y.npy',output)
    output.astype('<f4').tofile(folder/'candidate_world_y.f32')
    np.save(folder/'change_m.npy',output-current)
    heights[name]=output
    slope_after=slope(output); slope_before=slope(before); slope_current=slope(current)
    detail_region=free&(distance>90)&(distance<260)&(region>.7)
    highpass=lambda h:h-gaussian_filter(h.astype(float),sigma=8/step)
    hp_original=highpass(original); hp_output=highpass(output)
    sample_checks=[]
    for probe in probes:
        x,z=probe['position']; u,v=np.array((x-x0)/step),np.array((z-z0)/step)
        sample_checks.append(dict(x=float(x),z=float(z),
            current_height_m=float(sample(current,u,v)),candidate_height_m=float(sample(output,u,v)),
            original_slope_deg=float(sample(slope_before,u,v)),
            current_slope_deg=float(sample(slope_current,u,v)),candidate_slope_deg=float(sample(slope_after,u,v))))
    checks=dict(width_m=json.loads((folder/'parameters.json').read_text())['width_m'],
        refinement='Conjugate gradient on identical author shader Poisson equation, float64 CPU refinement after unchanged upstream GPU solve',
        refinement_iterations=iterations[0],refinement_seconds=time.perf_counter()-started,
        equation_update_residual_max_m=float(abs(residual).max()),equation_update_residual_rms_m=float(np.sqrt(np.mean(residual**2))),
        author_to_refined_max_change_m=float(abs(raw-initial).max()*1000),
        protected_samples=int(core.sum()),protected_max_change_m=float(abs(output[core]-current[core]).max()),
        fixed_samples=int(fixed.sum()),fixed_max_change_m=float(abs(output[fixed]-current[fixed]).max()),
        changed_samples=int(np.count_nonzero(output!=current)),
        change_range_m=[float((output-current).min()),float((output-current).max())],
        world_height_range_m=[float(output.min()),float(output.max())],
        free_region_slope_p95_deg=float(np.quantile(slope_after[free],.95)),
        artificial_steep_sample_count=int(np.count_nonzero(free&(slope_after>40)&(slope_after-slope_before>15))),
        current_artificial_steep_sample_count=int(np.count_nonzero(free&(slope_current>40)&(slope_current-slope_before>15))),
        inner_detail_rms_original_m=float(np.sqrt(np.mean(hp_original[detail_region]**2))),
        inner_detail_rms_candidate_m=float(np.sqrt(np.mean(hp_output[detail_region]**2))),
        probes=sample_checks)
    (folder/'verification.json').write_text(json.dumps(checks,indent=2))
    results[name]=checks
    print(name,json.dumps(checks),flush=True)

(ROOT/'verification.json').write_text(json.dumps(results,indent=2))

plt.rcParams.update({'font.family':'Microsoft YaHei','font.size':11,'axes.unicode_minus':False,
                     'figure.facecolor':'#f2eee6','axes.facecolor':'#f2eee6'})
light=LightSource(azdeg=315,altdeg=45)
cmap=LinearSegmentedColormap.from_list('earth',['#746957','#af9876','#d4b996','#e9d8bd'])
extent=[x0,x1,z0,z1]
def shade(h):
    # Rows run south to north; reflect only for lighting, restore orientation.
    return light.shade(h[::-1],cmap=cmap,vert_exag=1,dx=step,dy=step,vmin=-650,vmax=-70,blend_mode='soft')[::-1]

fig,axes=plt.subplots(1,3,figsize=(17,6.4),layout='constrained')
fig.set_layout_engine('constrained',rect=(0,.06,1,.94))
fig.suptitle('圈 1 离线高度图候选 · 当前场景未修改',fontsize=20)
for ax,(name,title) in zip(axes,[('current','当前地形'),('A','候选 A · 450 米梯度过渡'),('B','候选 B · 800 米梯度过渡')]):
    ax.imshow(shade(heights[name]),origin='lower',extent=extent)
    ax.contour(np.linspace(x0,x1,n),np.linspace(z0,z1,n),core,[.5],colors=['#3bf1dc'],linewidths=1.3)
    ax.contour(np.linspace(x0,x1,n),np.linspace(z0,z1,n),free,[.5],colors=['#fff0ba'],linewidths=.9)
    for i,probe in enumerate(probes):
        px,pz=probe['position']; ax.plot(px,pz,'o',color='#d3293b',markersize=4); ax.text(px+22,pz+20,str(i+1),color='#d3293b',weight='bold')
    ax.set_title(title,fontsize=14); ax.set_xlabel('世界 X / 米'); ax.set_ylabel('世界 Z / 米')
fig.text(.5,.012,'青线内高度固定    浅黄线为本次局部求解范围    红点为原陡壁诊断位置    三图光照与高度配色一致',ha='center',fontsize=11)
fig.savefig(ROOT/'heightmap_candidates.png',dpi=160); plt.close(fig)

# Local angled view, same coordinates and physical axis proportions in all panels.
zi=np.flatnonzero((np.linspace(z0,z1,n)>=-3520)&(np.linspace(z0,z1,n)<=-2280))[::3]
xi=np.flatnonzero((np.linspace(x0,x1,n)>=-3990)&(np.linspace(x0,x1,n)<=-2410))[::3]
xx,zz=np.meshgrid(np.linspace(x0,x1,n)[xi],np.linspace(z0,z1,n)[zi])
fig=plt.figure(figsize=(18,7),layout='constrained')
fig.suptitle('圈 1 山脚对比 · 同视角与真实比例',fontsize=21)
for index,(name,title) in enumerate([('current','当前地形'),('A','A · 450 米'),('B','B · 800 米')],1):
    ax=fig.add_subplot(1,3,index,projection='3d')
    h=heights[name][np.ix_(zi,xi)]
    colors=shade(heights[name])[np.ix_(zi,xi)]
    ax.plot_surface(xx,zz,h,facecolors=colors,rstride=1,cstride=1,linewidth=0,antialiased=False,shade=False)
    ax.view_init(elev=35,azim=-110)
    ax.set(xlim=(xx.min(),xx.max()),ylim=(zz.min(),zz.max()),zlim=(-620,-80))
    ax.set_box_aspect((xx.ptp() if hasattr(xx,'ptp') else np.ptp(xx),np.ptp(zz),540))
    ax.set_axis_off(); ax.set_title(title,fontsize=17)
fig.text(.5,.025,'离线高度几何预览    无游戏材质与物体    本图每 6 米采样显示，候选文件为 2 米采样',ha='center',fontsize=12)
fig.savefig(ROOT/'terrain_candidates_3d.png',dpi=165); plt.close(fig)

fig,axes=plt.subplots(2,2,figsize=(13,9),layout='constrained')
fig.suptitle('圈 1 坡度与剖面对比',fontsize=20)
for ax,(name,title) in zip(axes[0],[('current','当前坡度'),('B','候选 B 坡度')]):
    im=ax.imshow(slope(heights[name]),origin='lower',extent=extent,vmin=0,vmax=70,cmap='magma')
    ax.contour(np.linspace(x0,x1,n),np.linspace(z0,z1,n),core,[.5],colors=['#3bf1dc'],linewidths=.9)
    ax.set_title(title); fig.colorbar(im,ax=ax,label='坡度 / 度')
for ax,probe in zip(axes[1],probes):
    px,pz=probe['position']; u=int(round((px-x0)/step)); mask=(np.linspace(z0,z1,n)>=-3450)&(np.linspace(z0,z1,n)<=-2400)
    for name,label,color in [('current','当前','#bd493e'),('A','A 450 米','#bb9738'),('B','B 800 米','#24746c')]:
        ax.plot(np.linspace(z0,z1,n)[mask],heights[name][mask,u],label=label,color=color)
    ax.plot(np.linspace(z0,z1,n)[mask],before[mask,u],label='改造前参考',color='#788388',linestyle=':',linewidth=1)
    ax.set_title(f'沿 X = {px:.0f} 米剖面'); ax.set_xlabel('世界 Z / 米'); ax.set_ylabel('世界高度 / 米'); ax.grid(alpha=.2); ax.legend(fontsize=9)
fig.savefig(ROOT/'slope_and_profiles.png',dpi=150); plt.close(fig)
