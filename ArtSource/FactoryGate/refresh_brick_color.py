"""Recolor existing brick materials while preserving the approved mesh exactly."""
from pathlib import Path
import hashlib
import json
import shutil
import sys
import os

import bpy

SOURCE = Path(__file__).resolve().parent
ASSET = SOURCE.parent.parent / 'Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt'
TEXTURES = ASSET / 'Textures'
STAGING = SOURCE / 'BrickColorStaging'
BACKUP = SOURCE / 'Previous/Before_Individual_Brick_Colors'
sys.path.insert(0, str(SOURCE))
import texture_maps


def mesh_digest():
    result = {}
    for obj in bpy.data.collections['Factory Gate Asset'].objects:
        if obj.type == 'MESH':
            data = ([tuple(v.co) for v in obj.data.vertices],
                    [tuple(p.vertices) for p in obj.data.polygons],
                    [tuple(uv.uv) for uv in obj.data.uv_layers.active.data])
            result[obj.name] = hashlib.sha256(repr(data).encode()).hexdigest()
    return result


STAGING.mkdir(exist_ok=True)
BACKUP.mkdir(parents=True, exist_ok=True)
for path in (SOURCE/'FactoryGate_Rebuilt.blend', ASSET/'FactoryGate_Rebuilt.glb',
             ASSET/'FactoryGate_Rebuilt.fbx',
             TEXTURES/'Brick_BaseColor.png', TEXTURES/'BrickWeathered_BaseColor.png',
             SOURCE/'Previews/Gate_Hero.png', SOURCE/'Previews/Gate_Masonry_Detail.png'):
    destination = BACKUP/path.name
    if not destination.exists():
        shutil.copy2(path, destination)

bpy.ops.wm.open_mainfile(filepath=str(BACKUP/'FactoryGate_Rebuilt.blend'))
before = mesh_digest()
texture_maps.mineral(STAGING, 'Brick', (.565, .425, .305), 921, (9, 4.5), 2048)
for name in ('Brick', 'BrickWeathered'):
    mat = bpy.data.materials[name]
    node = next(n for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.label == 'BaseColor')
    node.image = bpy.data.images.load(str(STAGING/f'{name}_BaseColor.png'), check_existing=False)
    node.image.name = f'{name}_BaseColor_Recolored'
    node.image.colorspace_settings.name = 'sRGB'

tint_counts = texture_maps.tint_individual_bricks(bpy.data.collections['Factory Gate Asset'])

# Only base color changes. Keep generated data maps byte-identical to the originals.
for suffix in ('Normal', 'ORM', 'HDRPMask'):
    assert (STAGING/f'Brick_{suffix}.png').read_bytes() == (TEXTURES/f'Brick_{suffix}.png').read_bytes(), suffix
assert before == mesh_digest()
for name in ('Brick', 'BrickWeathered'):
    shutil.copy2(STAGING/f'{name}_BaseColor.png', STAGING/f'{name}_BaseColor.publish.png')
    os.replace(STAGING/f'{name}_BaseColor.publish.png', TEXTURES/f'{name}_BaseColor.png')
    node = next(n for n in bpy.data.materials[name].node_tree.nodes if n.type == 'TEX_IMAGE' and n.label == 'BaseColor')
    node.image.filepath = str(TEXTURES/f'{name}_BaseColor.png')

bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.data.collections['Factory Gate Asset'].objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects['FactoryGate_Rebuilt']
bpy.ops.export_scene.gltf(filepath=str(STAGING/'FactoryGate_Rebuilt.glb'), export_format='GLB',
    use_selection=True, export_yup=True, export_animations=False, export_extras=True, export_tangents=True,
    export_vertex_color='NAME', export_vertex_color_name='BrickTint', export_all_vertex_colors=False)
os.replace(STAGING/'FactoryGate_Rebuilt.glb', ASSET/'FactoryGate_Rebuilt.glb')
# Export at the final location so relative texture paths remain valid.
bpy.ops.export_scene.fbx(filepath=str(ASSET/'FactoryGate_Rebuilt.fbx'), use_selection=True,
    object_types={'EMPTY','MESH'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    bake_space_transform=False, add_leaf_bones=False, bake_anim=False, path_mode='RELATIVE')
# Repack all referenced images; remove the two superseded orphaned color images.
for im in list(bpy.data.images):
    if im.users == 0 and im.source == 'FILE':
        bpy.data.images.remove(im)
bpy.ops.file.pack_all()
for name in ('Brick', 'BrickWeathered'):
    node = next(n for n in bpy.data.materials[name].node_tree.nodes if n.type == 'TEX_IMAGE' and n.label == 'BaseColor')
    node.image.filepath = '//'+Path(os.path.relpath(TEXTURES/f'{name}_BaseColor.png',SOURCE)).as_posix()
bpy.context.scene.camera = bpy.data.objects['Hero']
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(STAGING/'FactoryGate_Rebuilt.blend'), relative_remap=False)
os.replace(STAGING/'FactoryGate_Rebuilt.blend', SOURCE/'FactoryGate_Rebuilt.blend')
report = {'mesh_and_uv_unchanged': before == mesh_digest(), 'mesh_count': len(before),
          'data_maps_unchanged': True, 'changed_materials': ['Brick', 'BrickWeathered'],
          'individual_brick_tints': tint_counts,
          'backup': str(BACKUP.relative_to(SOURCE)), 'play_mode_verified': False}
(SOURCE/'brick_color_revision.json').write_text(json.dumps(report,indent=2),encoding='utf-8')

prefs = bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type = 'OPTIX'
prefs.get_devices()
for device in prefs.devices:
    device.use = device.type == 'OPTIX'
scene = bpy.context.scene
scene.cycles.device = 'GPU'
for camera, filename, resolution in (
    ('Hero','Gate_Hero.png',(1800,1200)),
    ('Front','Gate_Front.png',(1800,1150)),
    ('Masonry Detail','Gate_Masonry_Detail.png',(1300,1400)),
    ('Rear','Gate_Rear.png',(1800,1200)),
    ('Whitewash Detail','Gate_Whitewash_Detail.png',(1300,1400))):
    scene.camera = bpy.data.objects[camera]
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.filepath = str(SOURCE/'Previews'/filename)
    bpy.ops.render.render(write_still=True)
    print('RENDER_COMPLETE',filename,flush=True)
print('BRICK_COLOR_REFRESH_COMPLETE',json.dumps(report),flush=True)
