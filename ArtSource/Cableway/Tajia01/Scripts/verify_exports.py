"""Reimport FBX and GLB and render the exported asset, including its rear."""
import bpy
import json
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia01')
report=json.loads((ROOT/'verification.json').read_text(encoding='utf-8'))
source=bpy.data.scenes['Tajia01_Tower_Rebuilt']
for extension in ('fbx','glb'):
    scene=bpy.data.scenes.new('Tower_'+extension.upper()+'_Roundtrip')
    bpy.context.window.scene=scene
    path=str(ROOT/'Exports'/('Tajia01_Tower_v01.'+extension))
    if extension=='fbx':
        bpy.ops.import_scene.fbx(filepath=path)
    else:
        bpy.ops.import_scene.gltf(filepath=path)
    imported=[o for o in scene.objects if o.type=='MESH']
    assert len(imported)==1,(extension,len(imported))
    obj=imported[0]
    obj.data.calc_loop_triangles()
    world_points=[obj.matrix_world@Vector(p) for p in obj.bound_box]
    actual=[max(p[i] for p in world_points)-min(p[i] for p in world_points) for i in range(3)]
    error=max(abs(a-b) for a,b in zip(actual,report['export_dimensions_xyz']))
    checks={
        'mesh_count':len(imported),
        'triangles':len(obj.data.loop_triangles),
        'material_slots':len(obj.data.materials),
        'dimensions_xyz':actual,
        'maximum_dimension_error':error,
        'uv_present':len(obj.data.uv_layers)>0,
    }
    assert checks['triangles']==report['triangles']
    assert error<1e-5
    assert checks['uv_present']
    report[extension+'_roundtrip']=checks
    if extension=='glb':
        obj.scale*=1/report['authoring_to_export_scale']
        scene.collection.children.link(bpy.data.collections['Preview_Stage'])
        scene.camera=source.camera
        scene.world=source.world
        scene.render.engine='CYCLES'
        scene.cycles.device=source.cycles.device
        scene.cycles.samples=48
        scene.cycles.use_denoising=True
        scene.render.resolution_x=1200
        scene.render.resolution_y=1400
        scene.render.resolution_percentage=100
        scene.render.image_settings.file_format='PNG'
        scene.render.filepath=str(ROOT/'Previews'/'Tower_v01_ExportCheck.png')
        bpy.ops.render.render(write_still=True)
        def pixels(path):
            image=bpy.data.images.load(str(path),check_existing=False)
            data=np.empty(len(image.pixels),dtype=np.float32)
            image.pixels.foreach_get(data)
            bpy.data.images.remove(image)
            return data.reshape(-1,4)[:,:3]
        reference=pixels(ROOT/'Previews'/'Tower_v01_Overview.png')
        exported=pixels(ROOT/'Previews'/'Tower_v01_ExportCheck.png')
        diff=np.abs(reference-exported)
        report['render_roundtrip_mean_absolute_rgb_error']=float(diff.mean())
        report['render_roundtrip_p99_rgb_error']=float(np.quantile(diff,.99))
        camera=scene.camera
        old_location=camera.location.copy()
        old_rotation=camera.rotation_euler.copy()
        old_scale=camera.data.ortho_scale
        camera.location=(8,-14,8)
        camera.rotation_euler=(Vector((.75,-.2,4.4))-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.ortho_scale=4.0
        scene.render.resolution_x=1400;scene.render.resolution_y=1100
        scene.render.filepath=str(ROOT/'Previews'/'Tower_v01_Detail.png')
        bpy.ops.render.render(write_still=True)
        camera.location=(-12,22,12.5)
        camera.rotation_euler=(Vector((0,0,5.1))-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.ortho_scale=12.25
        scene.render.resolution_x=1200;scene.render.resolution_y=1400
        scene.render.filepath=str(ROOT/'Previews'/'Tower_v01_Rear.png')
        bpy.ops.render.render(write_still=True)
        camera.location=old_location;camera.rotation_euler=old_rotation;camera.data.ortho_scale=old_scale
bpy.context.window.scene=source
with bpy.data.libraries.load(str(ROOT/'Blender'/'Tajia01_Tower_v01.blend'),link=False) as (saved,unused):
    report['saved_blend_scenes']=list(saved.scenes)
    report['saved_blend_objects']=len(saved.objects)
(ROOT/'verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report))
