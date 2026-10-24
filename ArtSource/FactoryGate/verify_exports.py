"""Reimport delivered GLB/FBX and inspect their geometry and texture references."""

from pathlib import Path
import json
import math
import struct
import sys
import numpy as np

import bpy
from mathutils import Vector

SOURCE=Path(__file__).resolve().parent
ASSET=SOURCE.parent.parent/'Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt'
expected=json.loads((SOURCE/'asset_report.json').read_text(encoding='utf-8'))
sys.path.insert(0,str(SOURCE))
import texture_maps


def inspect_corrosion():
    bpy.context.view_layer.update()
    openings=[]
    solid_controls=[]
    raised_vertices=0
    for side,seed,label in ((-1,104,'Left'),(1,682,'Right')):
        obj=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(label+'_GateLeaf_GatePaint'))
        inv=obj.matrix_world.inverted()
        direction=(inv.to_3x3() @ Vector((0,1,0))).normalized()
        edge=.068+texture_maps.SHEET_WIDTH
        x0,x1=(-edge,-.068) if side<0 else (.068,edge)
        def blocked(u,v):
            origin=inv @ Vector((x0+u*(x1-x0),-1,.23+v*2.815))
            return obj.ray_cast(origin,direction)[0]
        for u,v,ru,rv,_ in texture_maps.corrosion_holes(seed):
            openings.append(not blocked(u,v))
        solid_controls.append(blocked(.55,.62))
        for vert in obj.data.vertices:
            co=obj.matrix_world @ vert.co
            u,v=(co.x-x0)/(x1-x0),(co.z-.23)/2.815
            sheet_y=-.008+.0025*math.sin(u*14*texture_maps.STRETCH)*math.sin(v*11)
            raised_vertices+=co.y<sheet_y-.001
    return {'tested_hole_centers':len(openings),'clear_through_hole_centers':sum(openings),
            'solid_sheet_control_rays_hit':all(solid_controls),'raised_paint_vertices':raised_vertices}


def inventory():
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world @ Vector(c) for o in objects for c in o.bound_box]
    dims=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
    triangles=0
    missing_uv=[]
    bad_normals=[]
    for o in objects:
        o.data.calc_loop_triangles()
        triangles+=len(o.data.loop_triangles)
        if not o.data.uv_layers:
            missing_uv.append(o.name)
        if any(not all(math.isfinite(x) for x in v.normal) or v.normal.length<.8 for v in o.data.vertices):
            bad_normals.append(o.name)
    return {'size_xyz_blender':dims,'triangles':triangles,'mesh_objects':len(objects),
            'missing_uv':missing_uv,'bad_vertex_normals':bad_normals,
            'missing_image_files':[im.filepath for im in bpy.data.images if im.source=='FILE' and not im.packed_file and not Path(bpy.path.abspath(im.filepath)).is_file()]}


checks={}
binary=(ASSET/'FactoryGate_Rebuilt.glb').read_bytes()
magic,version,length=struct.unpack_from('<III',binary)
json_len=struct.unpack_from('<I',binary,12)[0]
gltf=json.loads(binary[20:20+json_len])
checks['glb_container_valid']=magic==0x46546c67 and version==2 and length==len(binary)
checks['all_gltf_primitives_have_uv_and_normals']=all(
    'TEXCOORD_0' in p['attributes'] and 'NORMAL' in p['attributes']
    for m in gltf['meshes'] for p in m['primitives'])
checks['gltf_materials']=[{'name':m.get('name'),'baseColorFactor':m.get('pbrMetallicRoughness',{}).get('baseColorFactor',[1,1,1,1]),
    'normal': 'normalTexture' in m,'metallicRoughness':'metallicRoughnessTexture' in m.get('pbrMetallicRoughness',{})} for m in gltf['materials']]
checks['glb_bytes']=len(binary)
checks['all_gltf_primitives_have_tangents']=all('TANGENT' in p['attributes'] for m in gltf['meshes'] for p in m['primitives'])
brick_images={m['name']:gltf['textures'][m['pbrMetallicRoughness']['baseColorTexture']['index']]['source']
              for m in gltf['materials'] if m['name'] in ('Brick','BrickWeathered')}
checks['weathered_brick_texture_is_distinct']=brick_images['Brick']!=brick_images['BrickWeathered']

for suffix in ('glb','fbx'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if suffix=='glb':
        bpy.ops.import_scene.gltf(filepath=str(ASSET/'FactoryGate_Rebuilt.glb'))
    else:
        bpy.ops.import_scene.fbx(filepath=str(ASSET/'FactoryGate_Rebuilt.fbx'))
    result=inventory()
    result['corrosion_geometry']=inspect_corrosion()
    result['dimensions_match']=all(abs(a-b)<.002 for a,b in zip(result['size_xyz_blender'],expected['size_xyz_blender']))
    result['triangle_count_matches']=result['triangles']==expected['triangles']
    checks[suffix]=result

# Render imported GLB in the source's same lighting and camera. This verifies
# the delivered textured asset instead of merely rendering authoring materials.
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'FactoryGate_Rebuilt.blend'))
collection=bpy.data.collections['Factory Gate Asset']
for o in list(collection.objects):
    bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.import_scene.gltf(filepath=str(ASSET/'FactoryGate_Rebuilt.glb'))
scene=bpy.context.scene
scene.camera=bpy.data.objects['Hero']
scene.render.resolution_x,scene.render.resolution_y=1800,1200
scene.render.filepath=str(SOURCE/'Previews/Gate_GLB_Roundtrip.png')
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX'
prefs.get_devices()
for device in prefs.devices:
    device.use=device.type=='OPTIX'
scene.cycles.device='GPU'
bpy.ops.render.render(write_still=True)
pixels=[]
for name in ('Gate_Hero.png','Gate_GLB_Roundtrip.png'):
    image=bpy.data.images.load(str(SOURCE/'Previews'/name),check_existing=False)
    data=np.empty(len(image.pixels),dtype=np.float32)
    image.pixels.foreach_get(data)
    pixels.append(data.reshape(-1,4)[:,:3])
checks['source_vs_glb_render_mean_absolute_difference']=float(np.mean(np.abs(pixels[0]-pixels[1])))
checks['glb_roundtrip_render']='Previews/Gate_GLB_Roundtrip.png'
checks['unity_editor_verified']=False
(SOURCE/'verification.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
print('VERIFICATION',json.dumps(checks),flush=True)
assert checks['glb_container_valid']
assert checks['all_gltf_primitives_have_uv_and_normals']
assert checks['all_gltf_primitives_have_tangents']
assert checks['weathered_brick_texture_is_distinct']
assert checks['source_vs_glb_render_mean_absolute_difference']<.012
for suffix in ('glb','fbx'):
    assert checks[suffix]['dimensions_match'],suffix
    assert checks[suffix]['triangle_count_matches'],suffix
    assert not checks[suffix]['missing_uv'],suffix
    assert not checks[suffix]['bad_vertex_normals'],suffix
    assert not checks[suffix]['missing_image_files'],suffix
    assert checks[suffix]['corrosion_geometry']['clear_through_hole_centers']==16,suffix
    assert checks[suffix]['corrosion_geometry']['solid_sheet_control_rays_hit'],suffix
    assert checks[suffix]['corrosion_geometry']['raised_paint_vertices']>100,suffix
print('EXPORT_CHECKS_PASSED',flush=True)
