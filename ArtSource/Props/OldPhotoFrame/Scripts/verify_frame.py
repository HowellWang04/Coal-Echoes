"""Check exported geometry and render the self-contained GLB after reimport."""
import bpy
import json
import hashlib
import struct
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
report=json.loads((ROOT/'verification.json').read_text(encoding='utf-8'))
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Blender/OldPhotoFrame.blend'))
source_images=[im for im in bpy.data.images if im.source=='FILE']
report['packed_source_images']=len(source_images)
report['all_source_images_packed']=all(im.packed_file is not None for im in source_images)
assert report['all_source_images_packed']
original=list(bpy.data.collections['OldPhotoFrame'].objects)
for ob in original:bpy.data.objects.remove(ob,do_unlink=True)

def geometry(objects):
    bpy.context.view_layer.update()
    points=[ob.matrix_world@Vector(co) for ob in objects for co in ob.bound_box]
    for ob in objects:ob.data.calc_loop_triangles()
    return {'mesh_count':len(objects),'triangles':sum(len(ob.data.loop_triangles) for ob in objects),
            'dimensions_xyz_m':[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)],
            'all_meshes_have_uvs':all(len(ob.data.uv_layers)>0 for ob in objects)}

for ext in ['fbx','glb']:
    before=set(bpy.data.objects)
    path=ROOT/'Exports'/('OldPhotoFrame.'+ext)
    if ext=='fbx':bpy.ops.import_scene.fbx(filepath=str(path))
    else:bpy.ops.import_scene.gltf(filepath=str(path))
    added=set(bpy.data.objects)-before
    meshes=[ob for ob in added if ob.type=='MESH']
    result=geometry(meshes)
    result['triangles_match']=result['triangles']==report['source']['triangles']
    result['dimensions_match']=all(abs(a-b)<1e-5 for a,b in zip(result['dimensions_xyz_m'],report['source']['dimensions_xyz_m']))
    result['mesh_count_matches']=result['mesh_count']==report['source']['mesh_count']
    assert result['triangles_match'] and result['dimensions_match'] and result['mesh_count_matches'] and result['all_meshes_have_uvs'],result
    result['materials']=sorted(set(m.name for ob in meshes for m in ob.data.materials if m))
    result['missing_texture_nodes']=[]
    for ob in meshes:
        for mat in ob.data.materials:
            if mat and mat.use_nodes:
                for node in mat.node_tree.nodes:
                    if node.type=='TEX_IMAGE':
                        if node.image:
                            # FBX image data is lazily loaded; reading a pixel forces decode.
                            pixels=node.image.pixels
                            decoded=len(pixels)>0 and float(pixels[0])>=0
                        else:decoded=False
                        if not decoded:
                            result['missing_texture_nodes'].append({'material':mat.name,'path':node.image.filepath if node.image else None})
    assert not result['missing_texture_nodes'],result
    report[ext+'_roundtrip']=result
    if ext=='glb':
        raw=path.read_bytes()
        size=struct.unpack_from('<I',raw,12)[0]
        gltf=json.loads(raw[20:20+size])
        report['glb_extensions']=gltf.get('extensionsUsed',[])
        assert 'KHR_materials_transmission' in report['glb_extensions']
        report['glb_images_embedded']=all('bufferView' in im for im in gltf.get('images',[]))
        assert report['glb_images_embedded']
        scene=bpy.context.scene
        prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
        for d in prefs.devices:d.use=d.type=='OPTIX'
        scene.render.filepath=str(ROOT/'Previews/OldPhotoFrame_GLB_Roundtrip.png')
        bpy.ops.render.render(write_still=True)
    for ob in added:bpy.data.objects.remove(ob,do_unlink=True)

report['delivery_sha256']={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for folder in ['Blender','Exports','Textures','Previews'] for p in sorted((ROOT/folder).glob('*')) if p.is_file() and p.suffix!='.blend1'}
(ROOT/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ROUNDTRIP_CHECKS_PASSED',json.dumps({k:report[k] for k in ['fbx_roundtrip','glb_roundtrip','glb_extensions','all_source_images_packed']}),flush=True)
