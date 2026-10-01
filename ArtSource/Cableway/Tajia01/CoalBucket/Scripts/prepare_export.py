"""Apply the bucket components and unwrap one independent export mesh."""
import bpy
import math

scene=bpy.context.scene
source=bpy.data.collections['CoalBucket_Fabricated_Parts']
export=bpy.data.collections.new('CoalBucket_Export');scene.collection.children.link(export)
bpy.ops.object.select_all(action='DESELECT')
dg=bpy.context.evaluated_depsgraph_get()
clones=[]
for ob in source.objects:
    me=bpy.data.meshes.new_from_object(ob.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    clone=bpy.data.objects.new(ob.name+'_BucketExport',me);export.objects.link(clone)
    clone.matrix_world=ob.matrix_world;clone.select_set(True);clones.append(clone)
bpy.context.view_layer.objects.active=clones[0]
bpy.ops.object.join()
bucket=bpy.context.object;bucket.name='Tajia01_CoalBucket';bucket.data.name='CoalBucket_Mesh'
source.hide_render=True;source.hide_viewport=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.003,margin_method='FRACTION',area_weight=.5,correct_aspect=True,scale_to_bounds=True)
bpy.ops.object.mode_set(mode='OBJECT');bucket.data.uv_layers.active.name='CoalBucketUV'
bucket.data.calc_loop_triangles()
scene.render.bake.margin=4;scene.render.bake.margin_type='EXTEND'
scene.render.bake.use_clear=True;scene.render.bake.use_selected_to_active=False
print('Prepared',len(bucket.data.vertices),'vertices',len(bucket.data.loop_triangles),'triangles')
