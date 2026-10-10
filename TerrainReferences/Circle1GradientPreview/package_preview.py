from pathlib import Path
import hashlib
import json
import os
import sys
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[1]
sys.path.insert(0,str(PROJECT/'Library/GradientTerrainExampleTools/python-deps'))
os.environ['MPLCONFIGDIR']=str(ROOT/'plot-cache')
import numpy as np
from PIL import Image
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.colors import LightSource,LinearSegmentedColormap

meta=json.loads((ROOT/'metadata.json').read_text())
checks=json.loads((ROOT/'verification.json').read_text())
fixed=np.load(ROOT/'fixed.npy'); current=np.load(ROOT/'current.npy')
from scipy.ndimage import binary_dilation
ring=binary_dilation(fixed)&~fixed
for name in ['A','B']:
    output=np.load(ROOT/name/'candidate_world_y.npy')
    encoded=np.rint((output+710)/1000*65535).astype(np.uint16)
    Image.fromarray(encoded[::-1]).save(ROOT/name/'height_16bit.png')
    checks[name]['png_encoding']=dict(world_y_min=-710,world_y_max=290,row_zero='north',quantization_step_m=1000/65535,
        note='For inspection only. Float files are authoritative; whole-PNG terrain import would quantize protected heights.')
    checks[name]['first_free_boundary_ring_change_p95_m']=float(np.quantile(abs(output[ring]-current[ring]),.95))
    checks[name]['first_free_boundary_ring_change_max_m']=float(abs(output[ring]-current[ring]).max())
    (ROOT/name/'verification.json').write_text(json.dumps(checks[name],indent=2))
(ROOT/'verification.json').write_text(json.dumps(checks,indent=2))

# Layout locator uses the previously saved full-map readback as context only.
previous=ROOT.parent/'PeripheralMountainsPreview'
overview=np.load(previous/'Applied/applied_height_4m.npy')
survey=json.loads((previous/'snapshot/survey.json').read_text(encoding='utf-8-sig'))
plt.rcParams.update({'font.family':'Microsoft YaHei','axes.unicode_minus':False,'font.size':11})
fig,ax=plt.subplots(figsize=(10,10),layout='constrained',facecolor='#f2eee6')
cmap=LinearSegmentedColormap.from_list('earth',['#716657','#bda380','#e2cfab'])
colors=LightSource(315,45).shade(overview,cmap=cmap,vert_exag=1,dx=4,dy=4,blend_mode='soft')
ax.imshow(colors,extent=[-4924,1076,-3914,2086])
x0,x1,z0,z1=meta['extent_xz']
ax.plot([x0,x1,x1,x0,x0],[z0,z0,z1,z1,z0],color='#dc433b',linewidth=2,label='离线预览取样窗口')
n=meta['resolution']
ax.contour(np.linspace(x0,x1,n),np.linspace(z0,z1,n),~fixed,[.5],colors=['#fff5c9'],linewidths=2)
points={p['name']:p['p'] for p in survey['points']}
towers=sorted(name for name in points if name.startswith('CablewayTower_'))
route=[points['CablewayFoothillStation']]+[points[name] for name in towers]+[points['CablewaySummitStation']]
ax.plot([p[0] for p in route],[p[2] for p in route],color='#ffdf50',linewidth=2)
for name in towers:
    px,_,pz=points[name]; ax.plot(px,pz,'o',color='#ffdf50',markersize=3)
for name,label in [('CablewayFoothillStation','索道下站'),('CablewaySummitStation','索道上站')]:
    px,_,pz=points[name]; ax.plot(px,pz,'s',color='#34e7cc',markersize=5)
    ax.annotate(label,(px,pz),xytext=(12,3),textcoords='offset points',bbox=dict(facecolor='#f2eee6',alpha=.9,edgecolor='none'))
ax.annotate('厂区',(-2300,-1860),xytext=(55,10),textcoords='offset points',arrowprops=dict(arrowstyle='->',color='#213a3a'),bbox=dict(facecolor='#f2eee6',edgecolor='none'))
ax.text(-3400,-3050,'圈 1\n本次局部候选',ha='center',color='#253632',bbox=dict(facecolor='#f2eee6',alpha=.9,edgecolor='none'))
ax.set_title('圈 1 在整张地图中的位置\n既有布局定位图，底图未应用候选',fontsize=17)
ax.set_xlabel('世界 X / 米'); ax.set_ylabel('世界 Z / 米')
fig.savefig(ROOT/'location.png',dpi=150); plt.close(fig)

before=json.loads((ROOT/'asset-hashes-before.json').read_text())
after={path:hashlib.sha256((PROJECT/path).read_bytes()).hexdigest() for path in before}
evidence=dict(checked_files=len(before),all_unchanged=before==after,
              changed_paths=[p for p in before if before[p]!=after[p]])
(ROOT/'asset-preservation.json').write_text(json.dumps(evidence,indent=2))
assert evidence['all_unchanged'],evidence
print(json.dumps(evidence))
