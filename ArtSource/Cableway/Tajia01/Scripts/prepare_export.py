"""Create an applied single-mesh export and a unique texture atlas."""
import bpy
import math
import os
import json

ROOT = r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia01'
scene=bpy.context.scene
source=bpy.data.collections['Tower_Fabricated_Parts']
export=bpy.data.collections.new('Tower_Export')
scene.collection.children.link(export)
bpy.ops.object.select_all(action='DESELECT')
dg=bpy.context.evaluated_depsgraph_get()
clones=[]
for ob in source.objects:
    me=bpy.data.meshes.new_from_object(ob.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    clone=bpy.data.objects.new(ob.name+'_Export',me)
    export.objects.link(clone)
    clone.matrix_world=ob.matrix_world
    clones.append(clone)
    clone.select_set(True)
bpy.context.view_layer.objects.active=clones[0]
bpy.ops.object.join()
tower=bpy.context.object
tower.name='Tajia01_Tower'
tower.data.name='Tajia01_Tower_Mesh'
source.hide_render=True
source.hide_viewport=True
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.0015,margin_method='FRACTION',area_weight=.5,correct_aspect=True,scale_to_bounds=True)
bpy.ops.object.mode_set(mode='OBJECT')
tower.data.uv_layers.active.name='TowerUV'
tower.data.calc_loop_triangles()
print('EXPORT',len(tower.data.vertices),'vertices',len(tower.data.loop_triangles),'triangles',len(tower.data.materials),'materials')
try:
    scene.render.engine='CYCLES'
except TypeError as e:
    print(e)
    raise
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.refresh_devices()
types=prefs.get_device_types(bpy.context)
print('COMPUTE_TYPES',types)
if any(t[0]=='OPTIX' for t in types):
    prefs.compute_device_type=next(t[0] for t in types if t[0]=='OPTIX')
    prefs.refresh_devices()
    for device in prefs.devices:
        device.use=device.type=='OPTIX'
    device_ids={i.identifier for i in scene.cycles.bl_rna.properties['device'].enum_items}
    scene.cycles.device=next(v for v in device_ids if v=='GPU')
scene.cycles.samples=8
scene.render.bake.use_selected_to_active=False
scene.render.bake.margin=8
margin_ids={i.identifier for i in scene.render.bake.bl_rna.properties['margin_type'].enum_items}
scene.render.bake.margin_type=next(v for v in margin_ids if v=='EXTEND')
scene.render.bake.use_clear=True
os.makedirs(ROOT+'\\Textures',exist_ok=True)
print('Ready for texture baking')
