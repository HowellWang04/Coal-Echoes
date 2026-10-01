"""Save and export the current approved station from its live authoring scene."""
import bpy, bmesh, json
from mathutils import Vector

ns=bpy.app.driver_namespace['station_build']
scene=ns['SCENE'];root=ns['ROOT'];objects=ns['objects']
bpy.context.window.scene=scene
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:ob.select_set(True)
bpy.context.view_layer.objects.active=objects[0]

# Refresh packed bytes after painting or saving image pixels. Reusing an
# existing packed buffer can make both exporters embed an older texture.
for kind in ['Steel','Cladding','Concrete']:
    for suffix in ['BaseColor','Roughness','Metallic','Normal','ORM','HDRP_Mask']:
        image=bpy.data.images['Station_'+kind+'_'+suffix]
        if image.packed_file:image.unpack(method='REMOVE')
        image.reload()
        image.pack()

def inspect_meshes(meshes):
    bounds=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    result={'mesh_count':len(meshes),'dimensions_xyz_m':[max(p[a] for p in bounds)-min(p[a] for p in bounds) for a in range(3)],'objects':[]}
    for ob in meshes:
        mesh=ob.data;mesh.calc_loop_triangles()
        bm=bmesh.new();bm.from_mesh(mesh)
        result['objects'].append({'name':ob.name,'triangles':len(mesh.loop_triangles),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'zero_area_faces':sum(f.calc_area()<1e-10 for f in bm.faces),'uv_layers':len(mesh.uv_layers),'materials':[m.name for m in mesh.materials]})
        bm.free()
    result['total_triangles']=sum(o['triangles'] for o in result['objects'])
    return result

report={'asset':'CablewayStationShell','variant':'Compact single slope open shelter','source':inspect_meshes(objects),'unity_modified':False,'unity_runtime_tested':False,'open_side_railing':False,'blender_version':bpy.app.version_string}
assert all(o['nonmanifold_edges']==0 and o['zero_area_faces']==0 and o['uv_layers']==1 for o in report['source']['objects'])
ns['final_report']=report
ns['inspect_meshes']=inspect_meshes

bpy.ops.export_scene.fbx(filepath=str(root/'Exports/CablewayStationShell.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_tspace=True,mesh_smooth_type='FACE',bake_anim=False,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(root/'Exports/CablewayStationShell.glb'),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True,export_yup=True,export_normals=True,export_tangents=True,export_materials='EXPORT')

images={bpy.data.images['Station_'+kind+'_'+suffix] for kind in ['Steel','Cladding','Concrete'] for suffix in ['BaseColor','Roughness','Metallic','Normal','ORM','HDRP_Mask']}
bpy.data.libraries.write(str(root/'Blender/CablewayStationShell.blend'),{scene}|images,fake_user=True,compress=True)
(root/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
