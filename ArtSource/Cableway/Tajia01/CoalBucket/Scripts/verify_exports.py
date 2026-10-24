"""Reimport the delivered bucket files and compare an actual export render."""
import bpy
import json
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia01\CoalBucket')
report=json.loads((ROOT/'verification.json').read_text(encoding='utf-8'))
source=bpy.data.scenes['Tajia01_CoalBucket_Rebuilt']
for extension in ('fbx','glb'):
    scene=bpy.data.scenes.new('CoalBucket_'+extension.upper()+'_Roundtrip');bpy.context.window.scene=scene
    path=str(ROOT/'Exports'/('Tajia01_CoalBucket_v01.'+extension))
    if extension=='fbx':bpy.ops.import_scene.fbx(filepath=path)
    else:bpy.ops.import_scene.gltf(filepath=path)
    meshes=[o for o in scene.objects if o.type=='MESH'];assert len(meshes)==1
    obj=meshes[0];obj.data.calc_loop_triangles()
    points=[obj.matrix_world@Vector(p) for p in obj.bound_box]
    dims=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    error=max(abs(a-b) for a,b in zip(dims,report['export_dimensions_xyz']))
    assert len(obj.data.loop_triangles)==report['triangles'] and error<1e-5 and len(obj.data.uv_layers)>0
    report[extension+'_roundtrip']={'mesh_count':1,'triangles':len(obj.data.loop_triangles),'material_slots':len(obj.data.materials),'dimensions_xyz':dims,'maximum_dimension_error':error,'uv_present':True}
    if extension=='glb':
        obj.scale*=1/report['authoring_to_export_scale'];obj.location+=Vector(report['attachment_pivot_authoring'])
        scene.collection.children.link(bpy.data.collections['CoalBucket_Preview_Stage']);scene.camera=source.camera;scene.world=source.world
        scene.render.engine='CYCLES';scene.cycles.device=source.cycles.device;scene.cycles.samples=48;scene.cycles.use_denoising=True
        scene.render.resolution_x=1200;scene.render.resolution_y=1400;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
        scene.render.filepath=str(ROOT/'Previews'/'CoalBucket_v01_ExportCheck.png');bpy.ops.render.render(write_still=True)
        def pixels(path):
            image=bpy.data.images.load(str(path),check_existing=False);data=np.empty(len(image.pixels),dtype=np.float32);image.pixels.foreach_get(data);bpy.data.images.remove(image);return data.reshape(-1,4)[:,:3]
        diff=np.abs(pixels(ROOT/'Previews'/'CoalBucket_v01_Overview.png')-pixels(ROOT/'Previews'/'CoalBucket_v01_ExportCheck.png'))
        report['render_roundtrip_mean_absolute_rgb_error']=float(diff.mean())
        camera=scene.camera;old_location=camera.location.copy();old_rotation=camera.rotation_euler.copy();old_scale=camera.data.ortho_scale
        for name,location,target,scale,resolution in [
            ('Detail',(4,-6,3.7),(0,-.05,1.25),2.95,(1400,1100)),
            ('Trolley',(3,-5,4.9),(0,0,3.79),1.45,(1200,1000)),
            ('Rear',(-6,9,5.6),(0,0,2.03),4.95,(1200,1400)),
        ]:
            camera.location=location;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
            scene.render.resolution_x,scene.render.resolution_y=resolution
            scene.render.filepath=str(ROOT/'Previews'/('CoalBucket_v01_'+name+'.png'));bpy.ops.render.render(write_still=True)
        camera.location=old_location;camera.rotation_euler=old_rotation;camera.data.ortho_scale=old_scale
bpy.context.window.scene=source
with bpy.data.libraries.load(str(ROOT/'Blender'/'Tajia01_CoalBucket_v01.blend'),link=False) as (saved,unused):report['saved_blend_scenes']=list(saved.scenes)
(ROOT/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
