"""Validate the corrected solve and its offline sampling on all 58 native terrains."""
from pathlib import Path
import sys
import json
ROOT=Path(__file__).resolve().parent
OLD=ROOT.parent/'WholeMapGradient800'
sys.path.insert(0,str(ROOT.parents[1]/'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from scipy.ndimage import map_coordinates

meta=json.loads((ROOT/'metadata.json').read_text())
survey=json.loads((OLD/'before.json').read_text())
live=json.loads((ROOT/'live-before.json').read_text())
assert all(t['matchesPrevious'] for t in live['terrains'])
candidate=np.load(ROOT/'candidate.npy')
previous=np.load(OLD/'candidate.npy')
core=np.load(ROOT/'protected.npy');accepted=np.load(ROOT/'accepted.npy');crests=np.load(ROOT/'crests.npy')
free=np.load(ROOT/'free.npy')
for name in ['protected','accepted','crests','free','current','base','fade']:
    assert np.array_equal(np.load(ROOT/(name+'.npy')),np.load(OLD/(name+'.npy'))),name
correction=candidate.astype(float)-previous
assert np.all(correction[~free]==0)
np.save(ROOT/'repair_delta_m.npy',correction.astype(np.float32))
(ROOT/'NativePreview').mkdir(exist_ok=True)
fields={};rows=[]
for t in survey['terrains']:
    name=t['name'];n=t['res'];px,py,pz=t['p'];sx,sy,sz=t['size']
    old=np.fromfile(OLD/'After'/(name+'.height.f32'),np.float32).reshape(n,n)
    X,Z=np.meshgrid(px+np.arange(n)*sx/(n-1),pz+np.arange(n)*sz/(n-1))
    coords=[(Z-meta['extent_xz'][2])/2,(X-meta['extent_xz'][0])/2]
    delta=map_coordinates(correction,coords,order=1,mode='constant',cval=0)
    new=(old.astype(float)+delta/sy).astype(np.float32)
    assert np.all(new>=0) and np.all(new<=1),name
    assert np.array_equal(new[delta==0],old[delta==0]),name
    for mask in [core,accepted,crests]:
        # Only count samples wholly within the fixed mask, excluding mixed border cells.
        protected=map_coordinates(mask.astype(float),coords,order=1,mode='constant',cval=0)>=1-1e-9
        assert np.array_equal(new[protected],old[protected]),name
    new.tofile(ROOT/'NativePreview'/(name+'.f32'))
    fields[name]=(old.astype(float)*sy+py,new.astype(float)*sy+py)
    step=sx/(n-1)
    def jumps(h):
        return max(float(abs(np.diff(h,axis=i)).max()) for i in [0,1])
    ja,jb=[jumps(h) for h in fields[name]]
    rows.append(dict(name=name,max_correction_m=float(abs(delta).max()),changed_samples=int(np.count_nonzero(new!=old)),
                     old_max_adjacent_jump_m=ja,new_max_adjacent_jump_m=jb,
                     old_max_adjacent_slope_deg=float(np.degrees(np.arctan(ja/step))),
                     new_max_adjacent_slope_deg=float(np.degrees(np.arctan(jb/step)))))
seams=[]
for i,t in enumerate(survey['terrains']):
    for u in survey['terrains'][i+1:]:
        if t['res']!=u['res']:continue
        dx=u['p'][0]-t['p'][0];dz=u['p'][2]-t['p'][2]
        if abs(abs(dx)-1000)<.01 and abs(dz)<.01:
            left,right=(t,u) if dx>0 else (u,t)
            a0,a1=[h[:,-1] for h in fields[left['name']]];b0,b1=[h[:,0] for h in fields[right['name']]]
        elif abs(abs(dz)-1000)<.01 and abs(dx)<.01:
            left,right=(t,u) if dz>0 else (u,t)
            a0,a1=[h[-1,:] for h in fields[left['name']]];b0,b1=[h[0,:] for h in fields[right['name']]]
        else:continue
        gap_change=float(abs((a1-b1)-(a0-b0)).max())
        assert gap_change<.001,(t['name'],u['name'],gap_change)
        seams.append(dict(a=t['name'],b=u['name'],before_m=float(abs(a0-b0).max()),after_m=float(abs(a1-b1).max()),gap_change_m=gap_change))
lod=[]
for t in survey['terrains']:
    if not t['name'].endswith('_Main'):continue
    draft=t['name'].replace('_Main','_Draft')
    if draft not in fields:continue
    a0,a1=fields[t['name']];b0,b1=fields[draft]
    lod.append(dict(main=t['name'],draft=draft,before_m=float(abs(a0[::8,::8]-b0).max()),
                    after_m=float(abs(a1[::8,::8]-b1).max()),
                    gap_change_m=float(abs((a1-a0)[::8,::8]-(b1-b0)).max())))
    assert lod[-1]['gap_change_m']<.001,lod[-1]
report=dict(offline_only=True,terrain_count=len(rows),fixed_changed=int(np.count_nonzero(correction[~free])),
            core_changed=int(np.count_nonzero(correction[core])),accepted_changed=int(np.count_nonzero(correction[accepted])),
            crests_changed=int(np.count_nonzero(correction[crests])),correction_range_m=[float(correction.min()),float(correction.max())],
            rows=rows,seams=seams,lod=lod)
(ROOT/'verification.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k not in ['rows','seams','lod']},indent=2))
print(json.dumps([r for r in rows if r['name'].startswith('Tile_2_2')],indent=2))
print('Maximum same-resolution seam gap change metres',max(r['gap_change_m'] for r in seams))
print('Maximum Main Draft correction mismatch metres',max(r['gap_change_m'] for r in lod))
