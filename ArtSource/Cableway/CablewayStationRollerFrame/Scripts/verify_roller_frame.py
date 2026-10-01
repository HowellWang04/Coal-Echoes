"""Reimport both deliveries, inspect dimensions and materials, render the GLB."""
import bpy
import json
import hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector

root=Path(__file__).resolve().parents[1]
report=json.loads((root/'verification.json').read_text(encoding='utf-8'))

def inspect():
    bpy.context.view_layer.update()
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name!='StudioGround']
    bounds=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
    tris=0
    for ob in objects:
        ob.data.calc_loop_triangles()
        tris+=len(ob.data.loop_triangles)
    mats={m for o in objects for m in o.data.materials if m}
    images={n.image for m in mats if m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image}
    for im in images:
        if im.size[0]>0:
            _=im.pixels[0]
    print('IMAGE_CHECK', [(im.name,im.filepath,im.has_data,list(im.size)) for im in images],flush=True)
    dims=[max(v[a] for v in bounds)-min(v[a] for v in bounds) for a in range(3)]
    def midpoint(name):
        ob=next(o for o in objects if o.name==name)
        return sum((ob.matrix_world@Vector(c) for c in ob.bound_box),Vector())/8
    frame_axis=(midpoint('ConcreteFooting_Rear')-midpoint('ConcreteFooting_Front')).normalized()
    wheel_axis=(midpoint('Sheave_03')-midpoint('Sheave_01')).normalized()
    parallel=abs(frame_axis.dot(wheel_axis))
    assert parallel>.99999,parallel
    return {'meshes':len(objects),'triangles':tris,'dimensions_xyz_m':dims,'frame_wheel_parallel_cosine':parallel,'maximum_dimension_error_m':max(abs(a-b) for a,b in zip(dims,report['source']['dimensions_xyz_m'])),'all_meshes_have_uv':all(len(o.data.uv_layers)>0 for o in objects),'materials':sorted(m.name for m in mats),'images':len(images),'all_images_loaded':all(im.has_data and im.size[0]>0 for im in images),'wheel_origins':{o.name:list(o.location) for o in objects if o.name.startswith('Sheave')}}

for ext in ['fbx','glb']:
    if ext=='fbx':
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(root/'Exports/CablewayStationRollerFrame.fbx'))
    else:
        bpy.ops.wm.open_mainfile(filepath=str(root/'Blender/CablewayStationRollerFrame.blend'))
        for o in list(bpy.data.collections['CablewayStationRollerFrame'].objects):
            bpy.data.objects.remove(o,do_unlink=True)
        bpy.ops.import_scene.gltf(filepath=str(root/'Exports/CablewayStationRollerFrame.glb'))
    info=inspect()
    assert info['meshes']==report['source']['mesh_count'],info
    assert info['triangles']==report['source']['triangles'],info
    assert info['maximum_dimension_error_m']<1e-4,info
    assert info['all_meshes_have_uv'] and info['all_images_loaded'] and info['images']>=9,info
    report[ext+'_reimport']=info

scene=bpy.context.scene
scene.render.filepath=str(root/'Previews/RollerFrame_GLB_Roundtrip.png')
bpy.ops.render.render(write_still=True)
pixels=[]
for filename in ['RollerFrame_Overall.png','RollerFrame_GLB_Roundtrip.png']:
    im=bpy.data.images.load(str(root/'Previews'/filename),check_existing=False)
    data=np.empty(im.size[0]*im.size[1]*4,dtype=np.float32);im.pixels.foreach_get(data)
    pixels.append(data.reshape((-1,4))[:,:3])
report['glb_render_mean_absolute_rgb_error']=float(np.mean(np.abs(pixels[0]-pixels[1])))
assert report['glb_render_mean_absolute_rgb_error']<.025,report['glb_render_mean_absolute_rgb_error']
report['file_sha256']={str(p.relative_to(root)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for folder in ['Blender','Exports','Textures','Previews'] for p in (root/folder).iterdir() if p.is_file() and p.suffix!='.blend1'}
report['verification_passed']=True
(root/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2),flush=True)
