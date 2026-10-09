"""Reimport the delivered GLB and FBX in isolated scenes, then render previews."""
from pathlib import Path
import json
import bpy
from mathutils import Vector

SOURCE=Path(__file__).resolve().parent
source_scene=bpy.data.scenes['WorkersDormitory']
expected=json.loads((SOURCE/'asset_report.json').read_text(encoding='utf-8'))
report={}
for extension in ('glb','fbx'):
    scene=bpy.data.scenes.new('Verify_'+extension)
    bpy.context.window.scene=scene
    if extension=='glb':bpy.ops.import_scene.gltf(filepath=str(SOURCE/'Exports/WorkersDormitory.glb'))
    else:bpy.ops.import_scene.fbx(filepath=str(SOURCE/'Exports/WorkersDormitory.fbx'))
    bpy.context.view_layer.update()
    meshes=[ob for ob in scene.objects if ob.type=='MESH']
    corners=[ob.matrix_world@Vector(c) for ob in meshes for c in ob.bound_box]
    dimensions=[round(max(p[i] for p in corners)-min(p[i] for p in corners),5) for i in range(3)]
    data={'mesh_count':len(meshes),'triangles':sum(sum(len(p.vertices)-2 for p in ob.data.polygons) for ob in meshes),'dimensions_m':dimensions,'all_meshes_have_uv':all(bool(ob.data.uv_layers) for ob in meshes),'all_meshes_have_materials':all(bool(ob.data.materials) for ob in meshes),'unwanted_objects':[ob.name for ob in scene.objects if ob.type in ('CAMERA','LIGHT') or 'Cube'==ob.name or 'Preview' in ob.name]}
    assert data['mesh_count']==14,data
    assert data['all_meshes_have_uv'] and data['all_meshes_have_materials'],data
    assert not data['unwanted_objects'],data
    assert all(abs(a-b)<.02 for a,b in zip(sorted(dimensions),sorted(expected['building_dimensions_m']))),data
    report[extension]=data
    if extension=='glb':
        scene.world=source_scene.world
        for ob in bpy.data.collections['WorkersDormitory_PreviewOnly'].objects:scene.collection.objects.link(ob)
        scene.camera=bpy.data.objects['Dorm_Hero']
        scene.render.engine='CYCLES'
        scene.cycles.samples=32
        scene.cycles.use_denoising=True
        scene.render.resolution_x=1800
        scene.render.resolution_y=1120
        scene.render.resolution_percentage=100
        scene.view_settings.exposure=.65
        scene.render.image_settings.file_format='PNG'
        scene.render.filepath=str(SOURCE/'Previews/Dormitory_GLB_Verified.png')
        bpy.ops.render.render(write_still=True)
    bpy.context.window.scene=source_scene
    bpy.data.scenes.remove(scene)
(SOURCE/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
names=['Hero','Front','Rear','Detail']
if 'Dorm_Entrance' in bpy.data.objects:names.append('Entrance')
for name in names:
    source_scene.camera=bpy.data.objects['Dorm_'+name]
    source_scene.render.filepath=str(SOURCE/'Previews'/('Dormitory_'+name+'.png'))
    bpy.ops.render.render(write_still=True)
    print('RENDER_COMPLETE',name,flush=True)
print('VERIFY_COMPLETE',json.dumps(report),flush=True)
