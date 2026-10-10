"""Retain native relief at Terrain spacing and package identical Main/Draft sources."""
from sources import *
import json
from scipy.ndimage import distance_transform_edt
meta=json.loads((ROOT/'metadata.json').read_text())
before=json.loads((ROOT/'before.json').read_text(encoding='utf-8-sig'))
candidate=np.load(ROOT/'candidate.npy')
free=np.load(ROOT/'free.npy').astype(np.float32)
old_coarse=np.load(ROOT/'before.npy')
native_coarse=np.load(ROOT/'native.npy')
keep=np.load(ROOT/'keep.npy')
detail_fade=smooth(distance_transform_edt(keep>0)*2/32).astype(np.float32)
weights=[np.load(ROOT/f'weight_{i}.npy') for i in range(4)]
sources=load_sources()
(ROOT/'Prepared').mkdir(exist_ok=True)
offset=[-2031.25,-3031.25]; size=6062.5
world_origin=[-2924.00049+offset[0],-913.999939+offset[1]]
reports=[]
atlases={}
guides={}
invalid_weight_max=0.
for lod,n,spacing in [('Main',6209,1000/1024),('Draft',777,1000/128)]:
    if lod=='Draft':
        atlas=atlases['Main'][::8,::8].copy()
        atlases[lod]=atlas;guides[lod]=guides['Main'][::8,::8]
        atlas.tofile(ROOT/'Prepared'/'draft_height.f32')
        reports.append(dict(lod=lod,resolution=n,spacing_m=spacing,world_range_m=[float(atlas.min()*1100-710),float(atlas.max()*1100-710)]))
        continue
    xs=world_origin[0]+np.arange(n)*spacing
    zs=world_origin[1]+np.arange(n)*spacing
    atlas=np.empty((n,n),np.float32)
    guide=np.empty((n,n),np.float32)
    old=np.empty((n,n),np.float32)
    # The only use of edge extension here is a temporary current-height field;
    # final border heights come from valid measured DEMs.
    for r in range(0,n,64):
        coords=np.broadcast_arrays((zs[r:r+64,None]+3986)/2,(xs[None,:]+4996.00049)/2)
        old[r:r+64]=map_coordinates(old_coarse,coords,order=1,mode='nearest')
    tiles=before['terrains'] if lod=='Main' else [t for t in before['terrains'] if t['res']==129]
    for t in sorted(tiles,key=lambda t:t['res']):
        px,py,pz=t['p'];tn=t['res']
        col=round((px+4924.00049)/1000);row=round((pz+3914)/1000)
        count=1024 if lod=='Main' else 128
        margin=32 if lod=='Main' else 4
        xx=margin+col*count;zz=margin+row*count
        arr=np.fromfile(ROOT/'Before'/f"{t['name']}.height.f32",np.float32).reshape(tn,tn)
        if tn!=count+1:
            coords=np.meshgrid(np.linspace(0,tn-1,count+1),np.linspace(0,tn-1,count+1),indexing='ij')
            arr=map_coordinates(arr,coords,order=1,mode='nearest')
        old[zz:zz+count+1,xx:xx+count+1]=arr*1000+py
    for r in range(0,n,64):
        zz=zs[r:r+64,None];xx=xs[None,:]
        coords=np.broadcast_arrays((zz+3986)/2,(xx+4996.00049)/2)
        get=lambda a:map_coordinates(a,coords,order=1,mode='nearest')
        k=get(keep);fade=get(detail_fade)
        coarse=get(candidate)
        native=np.zeros_like(coarse)
        valid_weight=np.zeros_like(coarse)
        for i,h in enumerate(fields(sources,xx,zz)):
            w=get(weights[i])
            w=np.divide(w,k,out=np.zeros_like(w),where=k>0)
            invalid=~np.isfinite(h)
            invalid_weight_max=max(invalid_weight_max,float(w[invalid].max(initial=0)))
            w[invalid]=0
            native+=np.nan_to_num(h)*w
            valid_weight+=w
        assert np.all(valid_weight[k>0]>0),(lod,r)
        native=np.divide(native,valid_weight,out=np.zeros_like(native),where=valid_weight>0)
        guide[r:r+64]=native
        residual=(old[r:r+64]-get(old_coarse))*(1-fade)+(native-get(native_coarse))*fade
        result=coarse+residual
        result[k==0]=old[r:r+64][k==0]
        atlas[r:r+64]=(result+710)/1100
        if r%1024==0:print(lod,'rows',r,n,flush=True)
    assert atlas.min()>=0 and atlas.max()<=1,(lod,atlas.min(),atlas.max())
    atlas.tofile(ROOT/'Prepared'/f'{lod.lower()}_height.f32')
    atlases[lod]=atlas
    guides[lod]=guide
    reports.append(dict(lod=lod,resolution=n,spacing_m=spacing,world_range_m=[float(atlas.min()*1100-710),float(atlas.max()*1100-710)]))
    del old
del sources
edits=[]
for t in before['terrains']:
    name=t['name'];n=t['res'];a=t['alpha'];px,py,pz=t['p']
    lod='Main' if n==1025 else 'Draft'
    col=round((px+4924.00049)/1000);row=round((pz+3914)/1000)
    margin=32 if n==1025 else 4;xx=margin+col*(n-1);zz=margin+row*(n-1)
    new=atlases[lod][zz:zz+n,xx:xx+n].copy()
    guide=guides[lod][zz:zz+n,xx:xx+n]
    old=np.fromfile(ROOT/'Before'/f'{name}.height.f32',np.float32).reshape(n,n)
    # Exclude the protected core from material edits, including a small sample guard.
    ax,az=np.meshgrid(px+(np.arange(a)+.5)*1000/a,pz+(np.arange(a)+.5)*1000/a)
    mask=map_coordinates(keep,[(az+3986)/2,(ax+4996.00049)/2],order=1,mode='nearest')>0
    if lod=='Draft':mask[:]=True  # Draft now follows the Main samples, including the core.
    mask.astype('u1').tofile(ROOT/'Prepared'/f'{name}.paint.u8')
    new.tofile(ROOT/'Prepared'/f'{name}.height.f32')
    limits=1000/(n-1)*np.tan(np.deg2rad(85))
    hx,hz=np.meshgrid(px+np.arange(n)*1000/(n-1),pz+np.arange(n)*1000/(n-1))
    transitioning=map_coordinates(free,[(hz+3986)/2,(hx+4996.00049)/2],order=1,mode='nearest')>0
    steep=0;introduced=0
    for axis in [0,1]:
        high=abs(np.diff(new.astype(float)*1100,axis=axis))>limits
        edge_transition=(transitioning[1:]|transitioning[:-1]) if axis==0 else (transitioning[:,1:]|transitioning[:,:-1])
        # A very steep face already present in the measured source is retained.
        # Reject a new step whose local height difference is not explained by it.
        deviation=abs(np.diff(new.astype(float)*1100,axis=axis)-np.diff(guide.astype(float),axis=axis))
        steep+=int(high.sum());introduced+=int(np.count_nonzero(high&edge_transition&(deviation>2*1000/(n-1))))
    assert introduced==0,(name,'transition edges above 85 degrees',introduced)
    edits.append(dict(name=name,changed=int(np.count_nonzero(abs(new*1100-710-(old*1000-710))>.02)),height_rect=[0,0,n,n],paint_pixels=int(mask.sum()),native_edges_above_85=steep,transition_edges_above_85=introduced))
(ROOT/'prepared.json').write_text(json.dumps(edits,indent=2))
report=dict(offset=offset,size=size,base_y=-710,height_range=1100,atlases=reports,terrain_count=len(edits),core_world_height_tolerance_m=.02,discarded_invalid_weight_max=invalid_weight_max)
(ROOT/'atlas.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report),flush=True)
