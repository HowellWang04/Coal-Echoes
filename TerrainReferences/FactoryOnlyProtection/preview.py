from pathlib import Path
import sys,os,json
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
os.environ['MPLCONFIGDIR']=str(ROOT/'plot-cache')
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource
from scipy.ndimage import map_coordinates
factory=json.loads((ROOT/'factory.json').read_text())
a=np.fromfile(ROOT/'Before/Tile_1_-1_Main.height.f32',np.float32).reshape(1025,1025)*1100-710
b=np.fromfile(ROOT/'Prepared/Tile_1_-1_Main.height.f32',np.float32).reshape(1025,1025)*1100-710
fig,axs=plt.subplots(1,2,figsize=(13,6),layout='constrained')
for ax,h,title in zip(axs,[a,b],['Before','Factory only protection']):
 ax.imshow(LightSource(315,40).hillshade(h,dx=1000/1024,dy=1000/1024),origin='lower',extent=[-1924,-924,-1914,-914],cmap='gray');ax.set_title(title);ax.set_xlabel('World X');ax.set_ylabel('World Z')
fig.savefig(ROOT/'tile-comparison.png',dpi=140);plt.close(fig)
old=np.load(ROOT/'current.npy');sol=np.load(ROOT/'candidate.npy');blend=np.load(ROOT/'blend.npy');new=old*(1-blend)+sol*blend
fig,axs=plt.subplots(1,3,figsize=(18,7),layout='constrained')
extent=[-4996.00049,1147.99951,-3986,2158]
for ax,h,title in zip(axs,[old,new,new-old],['Before','After coarse surface','Height change meters']):
 im=ax.imshow(LightSource(315,40).hillshade(h[::3,::3],dx=6,dy=6) if title!='Height change meters' else h[::3,::3],origin='lower',extent=extent,cmap='gray' if title!='Height change meters' else 'RdBu_r')
 ax.add_patch(plt.Circle((factory['x'],factory['z']),factory['radius'],fill=False,color='lime',lw=1));ax.set_title(title);ax.set_xlim(-4800,-650);ax.set_ylim(-3300,1500)
 if title=='Height change meters':fig.colorbar(im,ax=ax)
fig.savefig(ROOT/'region-comparison.png',dpi=120)
# Same cross-sections as the diagnosis, now across the former boundary.
x=-1924.00049+np.arange(1025)*1000/1024;z=-1913.999939+np.arange(1025)*1000/1024
sections=[]
for z0,x0 in [(-1200,-1586.109865),(-1450,-1564.62549),(-1700,-1542.1645525)]:
 iz=np.argmin(abs(z-z0));ix=np.argmin(abs(x-x0))
 def slope(h):return [float(np.mean(np.diff(h[iz,ix-10:ix-3]))/(1000/1024)),float(np.mean(np.diff(h[iz,ix+3:ix+10]))/(1000/1024))]
 sections.append(dict(x=x0,z=z0,before=slope(a),after=slope(b)))
assert abs(sections[2]['after'][1]-sections[2]['after'][0])<.3,sections
(ROOT/'crease-check.json').write_text(json.dumps(sections,indent=2));print(json.dumps(sections,indent=2))
