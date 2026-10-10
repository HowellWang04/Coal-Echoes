"""Isolate the height-validity cutoff without writing any Unity assets."""
from pathlib import Path
import json
import sys
import os
ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parents[1] / 'Library/GradientTerrainExampleTools/python-deps'))
sys.path.insert(0, str(ROOT.parent / 'PeripheralMountainsPreview'))
os.environ['MPLCONFIGDIR'] = str(ROOT / 'plot-cache')
import numpy as np
from PIL import Image
from build_preview import sample, smooth
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
plt.rcParams['font.sans-serif'] = ['Microsoft YaHei']
plt.rcParams['axes.unicode_minus'] = False

whole = ROOT.parent / 'WholeMapGradient800'
meta = json.loads((whole / 'metadata.json').read_text())
base = np.load(whole / 'base.npy')
saved = np.load(whole / 'original.npy')
iz0, iz1 = 2860, 3015
ix0, ix1 = 2100, 2220
xs = meta['extent_xz'][0] + np.arange(ix0,ix1)*2
zs = meta['extent_xz'][2] + np.arange(iz0,iz1)*2
x,z = xs[None,:], zs[:,None]
Image.MAX_IMAGE_PIXELS=None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    source=np.array(im.crop((4800,0,10012,10012)))
sx=x+4924.00049+4806
sy=4091.999939-z
raw=sample(source,sx-4800-.5,sy-.5)
native=raw-2073.599609375
coverage=smooth((10000-sx)/200)*smooth((sx-4900)/200)*smooth((sy-200)/200)*smooth((9800-sy)/200)
old_weight=coverage*(native > -710)
b=base[iz0:iz1,ix0:ix1]
old=native*old_weight+b*(1-old_weight)
# Every DEM sample here is real positive elevation; -710 is a Unity range limit.
assert raw.min() > 1300
new=native*coverage+b*(1-coverage)
assert abs(old-saved[iz0:iz1,ix0:ix1]).max()<.0001
axis=1
flips=(native[:,:-1]>-710)!=(native[:,1:]>-710)
oldj=abs(np.diff(old,axis=1));newj=abs(np.diff(new,axis=1))
r,c=np.unravel_index(np.argmax(np.where(flips,oldj,0)),oldj.shape)
report=dict(raw_dem_range_m=[float(raw.min()),float(raw.max())],
            threshold_world_y_m=-710,
            saved_input_match_max_m=float(abs(old-saved[iz0:iz1,ix0:ix1]).max()),
            cut_crossing_edges=int(flips.sum()),
            max_crossing_jump_before_m=float(oldj[flips].max()),
            max_crossing_jump_without_cut_m=float(newj[flips].max()),
            horizontal_spacing_m=2,
            example=dict(world_xz=[float(xs[c]),float(zs[r])],raw_dem_m=raw[r,c:c+2].tolist(),
                         native_world_y_m=native[r,c:c+2].tolist(),
                         weight=old_weight[r,c:c+2].tolist(),
                         current_guidance=old[r,c:c+2].tolist(),
                         guidance_without_cut=new[r,c:c+2].tolist()),
            probe_only=True)
(ROOT/'source-probe.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
assert report['max_crossing_jump_before_m']>50
assert report['max_crossing_jump_without_cut_m']<3

fig,axes=plt.subplots(1,2,figsize=(13,5),layout='constrained')
terrain=np.fromfile(ROOT/'Tile_2_2_Main.f32',np.float32).reshape(1025,1025)*1000-710
axes[0].imshow(terrain,origin='lower',extent=[-924.0005,75.9995,1086.00012,2086.00012],cmap='terrain')
jump=np.zeros_like(terrain)
jump[:,:-1]=abs(np.diff(terrain,axis=1))
jump[:-1,:]=np.maximum(jump[:-1,:],abs(np.diff(terrain,axis=0)))
overlay=np.ma.masked_where(jump<10,jump)
axes[0].imshow(overlay,origin='lower',extent=[-924.0005,75.9995,1086.00012,2086.00012],cmap='autumn',vmin=10,vmax=50)
axes[0].plot([-663.2583], [1951.2345], 'wo', ms=5)
axes[0].set_title('Tile 2,2 内部异常台阶\n红黄色为相邻采样高差超过 10 米的位置')
axes[0].set_xlabel('Unity X / 米');axes[0].set_ylabel('Unity Z / 米')
axes[1].plot(xs,old[r],color='#bb4e35',label='当前输入，存在高度硬切换')
axes[1].plot(xs,new[r],color='#348c81',label='仅去掉高度硬切换的输入实验')
axes[1].axhline(-710,color='gray',ls=':',label='误用的有效性阈值')
axes[1].set_title('原始坡度引导高度剖面\n此处先发生突变，随后被求解器保留下来')
axes[1].set_xlabel('Unity X / 米');axes[1].set_ylabel('世界高度 / 米');axes[1].legend()
fig.savefig(ROOT/'diagnosis.png',dpi=150)
plt.close(fig)
