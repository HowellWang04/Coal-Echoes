"""Transfer the original tower's albedo through closest-surface UV sampling."""
import bpy
import json
import time
import shutil
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia01')
SIZE=globals().get('TRANSFER_SIZE',1024)
status=ROOT/'Blender'/'legacy_transfer_status.txt'
status.write_text('preparing '+str(SIZE))
archive=ROOT/'Previous'/'Before_Legacy_Surface_Transfer'
if not archive.exists():
    for folder,pattern in [('Textures','*.png'),('Blender','*.blend'),('Exports','*'),('Previews','*.png')]:
        (archive/folder).mkdir(parents=True,exist_ok=True)
        for p in (ROOT/folder).glob(pattern):
            shutil.copy2(p,archive/folder/p.name)
scene=bpy.data.scenes['Tajia01_Tower_Rebuilt']
bpy.context.window.scene=scene
tower=bpy.data.objects['Tajia01_Tower']
portable=tower.data.materials[0]
source=bpy.data.objects['Tower_Legacy03_TextureSource']
source.data.calc_loop_triangles()
verts=[source.matrix_world@v.co for v in source.data.vertices]
tris=[tuple(t.vertices) for t in source.data.loop_triangles]
uvdata=source.data.uv_layers.active.data
tri_uv=[tuple(Vector((*uvdata[l].uv,0)) for l in t.loops) for t in source.data.loop_triangles]
bvh=BVHTree.FromPolygons(verts,tris,all_triangles=True)
source_lo=np.array([min(v[i] for v in verts) for i in range(3)])
source_hi=np.array([max(v[i] for v in verts) for i in range(3)])
target_lo=np.array([-3.1800000668,-1.3837999105,0])
target_hi=np.array([3.1800000668,1.2934999466,10.3149995804])

legacy=bpy.data.images.load(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\Assets\Art Resources\Environment\Cableway\texture\texture_pbr_20250901.002 1.png',check_existing=True)
pixels=np.empty(len(legacy.pixels),np.float32)
legacy.pixels.foreach_get(pixels)
texture=pixels.reshape(legacy.size[1],legacy.size[0],4)
original=bpy.data.images.load(str(archive/'Textures'/'Tower_BaseColor.png'),check_existing=False)
baseline=np.empty(len(original.pixels),np.float32)
original.pixels.foreach_get(baseline)
baseline=baseline.reshape(original.size[1],original.size[0],4)

mat=bpy.data.materials.new('Tower_PositionBake_Temporary')
n=mat.node_tree.nodes;l=mat.node_tree.links
out=next(x for x in n if x.type=='OUTPUT_MATERIAL')
geo=n.new('ShaderNodeNewGeometry')
offset=n.new('ShaderNodeVectorMath')
assert 'ADD' in {v.identifier for v in offset.bl_rna.properties['operation'].enum_items}
offset.operation='ADD';offset.inputs[1].default_value=(4,4,0)
l.new(geo.outputs['Position'],offset.inputs[0])
scale=n.new('ShaderNodeVectorMath');scale.operation='SCALE';scale.inputs['Scale'].default_value=1/12
l.new(offset.outputs[0],scale.inputs[0])
emit=n.new('ShaderNodeEmission');l.new(scale.outputs[0],emit.inputs[0]);l.new(emit.outputs[0],out.inputs['Surface'])
position=bpy.data.images.new('Tower_SurfaceTransfer_Position',SIZE,SIZE,alpha=True,float_buffer=True)
position.colorspace_settings.name='Non-Color'
target=n.new('ShaderNodeTexImage');target.image=position;n.active=target
tower.data.materials[0]=mat
bpy.ops.object.select_all(action='DESELECT');tower.select_set(True);bpy.context.view_layer.objects.active=tower
scene.cycles.samples=1
bpy.ops.object.bake(type='EMIT')
tower.data.materials[0]=portable
data=np.empty(SIZE*SIZE*4,np.float32);position.pixels.foreach_get(data)
data=data.reshape(-1,4)
valid=np.flatnonzero(data[:,:3].max(axis=1)>.001)
world=data[valid,:3]*12-np.array([4,4,0])
query=(world-target_lo)/(target_hi-target_lo)*(source_hi-source_lo)+source_lo
uv=np.empty((len(valid),2),np.float32)
distance=np.empty(len(valid),np.float32)
t0=time.time()
for i,q in enumerate(query):
    point,normal,idx,dist=bvh.find_nearest(q)
    a,b,c=tris[idx]
    result=barycentric_transform(point,verts[a],verts[b],verts[c],*tri_uv[idx])
    uv[i]=(result.x,result.y)
    distance[i]=dist
    if i%100000==0:
        status.write_text('transferring '+str(i)+' / '+str(len(valid)))
x=(uv[:,0]%1)*(legacy.size[0]-1);y=(uv[:,1]%1)*(legacy.size[1]-1)
ix=x.astype(int);iy=y.astype(int);fx=(x-ix)[:,None];fy=(y-iy)[:,None]
ix1=np.minimum(ix+1,legacy.size[0]-1);iy1=np.minimum(iy+1,legacy.size[1]-1)
sample=(texture[iy,ix]*(1-fx)+texture[iy,ix1]*fx)*(1-fy)+(texture[iy1,ix]*(1-fx)+texture[iy1,ix1]*fx)*fy
sample[:,3]=1
rows=np.minimum((np.arange(SIZE)+.5)*original.size[1]/SIZE,original.size[1]-1).astype(int)
cols=np.minimum((np.arange(SIZE)+.5)*original.size[0]/SIZE,original.size[0]-1).astype(int)
result=baseline[rows[:,None],cols[None,:]].reshape(-1,4).copy()
steel=world[:,2]>.245
result[valid[steel]]=sample[steel]
image=bpy.data.images.new('Tower_LegacyTransfer_'+str(SIZE),SIZE,SIZE,alpha=False)
image.colorspace_settings.name='sRGB';image.file_format='PNG'
image.pixels.foreach_set(result.ravel())
image.filepath_raw=str(ROOT/'Textures'/('Tower_LegacyTransfer_'+str(SIZE)+'.png'))
image.save();image.pack()
bs=next(x for x in portable.node_tree.nodes if x.type=='BSDF_PRINCIPLED')
base=bs.inputs['Base Color'].links[0].from_node
base.image=image
bpy.data.materials.remove(mat)
bpy.data.images.remove(position)
scene.cycles.samples=32
scene.render.filepath=str(ROOT/'Previews'/'Tower_LegacyTransfer_Overview.png')
bpy.ops.render.render(write_still=True)
report={'size':SIZE,'samples':len(valid),'seconds':time.time()-t0,'source_texture':legacy.filepath,'nearest_distance_p50_p95':np.quantile(distance,[.5,.95]).tolist()}
(ROOT/'legacy_surface_transfer.json').write_text(json.dumps(report,indent=2))
status.write_text('complete')
