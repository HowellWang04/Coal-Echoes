"""Export only the tower, at the original model height, and record mesh checks."""
import bpy
import bmesh
import json
import struct
from pathlib import Path
from mathutils import Matrix

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia01')
scene=bpy.context.scene
tower=bpy.data.objects['Tajia01_Tower']
original=bpy.data.objects['material']
original_points=[original.matrix_world@v.co for v in original.data.vertices]
original_height=max(v.z for v in original_points)-min(v.z for v in original_points)
points=[tower.matrix_world@v.co for v in tower.data.vertices]
authoring_height=max(v.z for v in points)-min(v.z for v in points)
factor=original_height/authoring_height
temp_collection=bpy.data.collections.new('Temporary_Export_Scale')
scene.collection.children.link(temp_collection)
scaled=tower.copy();scaled.data=tower.data.copy()
scaled.name='Tajia01_Tower_Standalone'
temp_collection.objects.link(scaled)
scaled.data.transform(Matrix.Scale(factor,4))
bpy.ops.object.select_all(action='DESELECT')
scaled.select_set(True);bpy.context.view_layer.objects.active=scaled
scaled.data.calc_loop_triangles()
bm=bmesh.new();bm.from_mesh(scaled.data)
report={
    'name':scaled.name,
    'vertices':len(scaled.data.vertices),
    'triangles':len(scaled.data.loop_triangles),
    'material_slots':len(scaled.data.materials),
    'uv_layers':[u.name for u in scaled.data.uv_layers],
    'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),
    'zero_area_faces':sum(f.calc_area()<1e-14 for f in bm.faces),
    'authoring_to_export_scale':factor,
    'original_fbx_height':original_height,
    'bucket_included':False,
    'unity_inspector_verified':False,
    'normal_strength':next(n for n in tower.data.materials[0].node_tree.nodes if n.type=='NORMAL_MAP').inputs['Strength'].default_value,
    'texture_resolution':4096,
}
bm.free()
bpy.context.view_layer.update()
report['export_dimensions_xyz']=list(scaled.dimensions)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Exports'/'Tajia01_Tower_v01.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,use_triangles=True,mesh_smooth_type='OFF',use_tspace=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,path_mode='RELATIVE',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Exports'/'Tajia01_Tower_v01.glb'),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True,export_yup=True,export_normals=True,export_tangents=True,export_materials='EXPORT')
glb=(ROOT/'Exports'/'Tajia01_Tower_v01.glb').read_bytes()
json_size=struct.unpack_from('<I',glb,12)[0]
gltf=json.loads(glb[20:20+json_size])
assert len(gltf['scenes'])==1 and len(gltf['meshes'])==1 and len(gltf['materials'])==1
assert all(i['name'].startswith('Tower_') for i in gltf['images'])
report['glb_scenes']=len(gltf['scenes'])
report['glb_meshes']=len(gltf['meshes'])
report['glb_materials']=len(gltf['materials'])
report['glb_embedded_images']=[i['name'] for i in gltf['images']]
mesh=scaled.data
bpy.data.objects.remove(scaled,do_unlink=True)
bpy.data.meshes.remove(mesh)
bpy.data.collections.remove(temp_collection)
tower.select_set(True);bpy.context.view_layer.objects.active=tower
for image in bpy.data.images:
    if image.name.startswith('Tower_') and image.size[0]>0:
        image.pack()
scene['authoring_to_export_scale']=factor
scene['export_dimensions_xyz']=report['export_dimensions_xyz']
scene['normal_strength']=report['normal_strength']
scene['export_files']='Exports/Tajia01_Tower_v01.fbx and Exports/Tajia01_Tower_v01.glb'
bpy.data.libraries.write(str(ROOT/'Blender'/'Tajia01_Tower_v01.blend'),{scene},fake_user=True,compress=True)
(ROOT/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report))
