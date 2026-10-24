"""Export only the bucket assembly, using the cable contact as its origin."""
import bpy
import bmesh
import json
import struct
from pathlib import Path
from mathutils import Matrix,Vector

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia01\CoalBucket')
scene=bpy.data.scenes['Tajia01_CoalBucket_Rebuilt'];bpy.context.window.scene=scene
bucket=bpy.data.objects['Tajia01_CoalBucket']
factor=scene['authoring_to_export_scale'];pivot=Vector(scene['attachment_pivot_authoring'])
temporary=bpy.data.collections.new('Bucket_Temporary_Export');scene.collection.children.link(temporary)
scaled=bucket.copy();scaled.data=bucket.data.copy();scaled.name='Tajia01_CoalBucket_Standalone';temporary.objects.link(scaled)
scaled.data.transform(Matrix.Scale(factor,4)@Matrix.Translation(-pivot))
bpy.ops.object.select_all(action='DESELECT');scaled.select_set(True);bpy.context.view_layer.objects.active=scaled
bpy.context.view_layer.update();scaled.data.calc_loop_triangles()
bm=bmesh.new();bm.from_mesh(scaled.data)
report={
    'triangles':len(scaled.data.loop_triangles),
    'vertices':len(scaled.data.vertices),
    'material_slots':len(scaled.data.materials),
    'uv_layers':[u.name for u in scaled.data.uv_layers],
    'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),
    'zero_area_faces':sum(f.calc_area()<1e-14 for f in bm.faces),
    'export_dimensions_xyz':list(scaled.dimensions),
    'authoring_to_export_scale':factor,
    'attachment_pivot_authoring':list(pivot),
    'origin':'Cable contact center beneath trolley wheels',
    'texture_resolution':4096,
    'normal_strength':.45,
    'tower_included':False,
    'cable_included':False,
    'unity_verified':False,
}
bm.free()
assert report['nonmanifold_edges']==0
assert report['zero_area_faces']==0
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Exports'/'Tajia01_CoalBucket_v01.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,use_triangles=True,mesh_smooth_type='OFF',use_tspace=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,path_mode='RELATIVE',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Exports'/'Tajia01_CoalBucket_v01.glb'),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True,export_yup=True,export_normals=True,export_tangents=True,export_materials='EXPORT')
data=(ROOT/'Exports'/'Tajia01_CoalBucket_v01.glb').read_bytes();length=struct.unpack_from('<I',data,12)[0];gltf=json.loads(data[20:20+length])
assert len(gltf['scenes'])==1 and len(gltf['meshes'])==1 and len(gltf['materials'])==1
assert all(i['name'].startswith('CoalBucket_') for i in gltf['images'])
material=gltf['materials'][0]
assert 'normalTexture' in material and 'occlusionTexture' in material
assert 'baseColorTexture' in material['pbrMetallicRoughness'] and 'metallicRoughnessTexture' in material['pbrMetallicRoughness']
report['glb_meshes']=len(gltf['meshes']);report['glb_materials']=len(gltf['materials']);report['embedded_images']=[i['name'] for i in gltf['images']]
mesh=scaled.data;bpy.data.objects.remove(scaled,do_unlink=True);bpy.data.meshes.remove(mesh);bpy.data.collections.remove(temporary)
bucket.select_set(True);bpy.context.view_layer.objects.active=bucket
for image in bpy.data.images:
    if image.name.startswith('CoalBucket_') and image.size[0]>0:image.pack()
bpy.data.libraries.write(str(ROOT/'Blender'/'Tajia01_CoalBucket_v01.blend'),{scene},fake_user=True,compress=True)
(ROOT/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
