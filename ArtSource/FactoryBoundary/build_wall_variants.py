"""Build six 9 m wall modules with identical geometry and different brick tints."""
from pathlib import Path
import hashlib
import json
import random
import sys

import bpy

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parent.parent
MODELS = ROOT / 'Assets/Art Resources/Environment/Gate Area/FactoryBoundary/Models'
sys.path.insert(0, str(SOURCE.parent / 'FactoryGate'))
import build_gate as gate
import texture_maps

bpy.ops.wm.open_mainfile(filepath=str(SOURCE / 'FactoryBoundary.blend'))
for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
gate.materials = {name: bpy.data.materials[name]
                  for name in ('Brick', 'BrickWeathered', 'Concrete', 'Mortar')}
gate.groups = {}
gate.rng = random.Random(1973)
collection = bpy.data.collections.new('FactoryWall_9m_01')
bpy.context.scene.collection.children.link(collection)
gate.asset_collection = collection
wall = gate.empty('FactoryWall_9m_01')
wall['grid_spacing'] = 9.0
wall['socket_end_blender'] = [-9, 0, 0]
x0, x1 = -8.625, -.375
gate.builder(wall, 'Concrete').box(((x0+x1)/2, 0, .14),
                                 (x1-x0, .62, .28), 1/4.5, .008, offset=(.5, 0))
gate.brickwork(wall, x0, x1, -.25, .25, .28, 3.46, False)
for (name, mat), (mesh, parent) in list(gate.groups.items()):
    mesh.object(f'{name}_{mat}', mat, parent,
                {'Brick': 0, 'BrickWeathered': 0, 'Concrete': .012, 'Mortar': .001}[mat], 1)
for obj in collection.objects:
    if obj.type != 'MESH':
        continue
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for modifier in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    modifier = obj.modifiers.new('Stable export triangulation', 'TRIANGULATE')
    modifier.quad_method = 'FIXED'
    bpy.ops.object.modifier_apply(modifier=modifier.name)

variants = [(collection, wall)]
for index in range(2, 7):
    name = f'FactoryWall_9m_{index:02}'
    target = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(target)
    parent = wall.copy()
    parent.name = name
    target.objects.link(parent)
    for original in wall.children:
        obj = original.copy()
        obj.data = original.data.copy()
        obj.name = original.name.replace('FactoryWall_9m_01', name)
        obj.parent = parent
        target.objects.link(obj)
    variants.append((target, parent))

report = []
for index, (target, parent) in enumerate(variants):
    tint_counts = texture_maps.tint_individual_bricks(target)
    geometry = hashlib.sha256()
    colors = hashlib.sha256()
    triangles = 0
    for obj in sorted(parent.children, key=lambda o: o.data.materials[0].name):
        mesh = obj.data
        mesh.calc_loop_triangles()
        triangles += len(mesh.loop_triangles)
        geometry.update(repr([tuple(v.co) for v in mesh.vertices]).encode())
        geometry.update(repr([tuple(p.vertices) for p in mesh.polygons]).encode())
        layer = mesh.color_attributes.get('BrickTint')
        if layer:
            colors.update(repr([tuple(c.color) for c in layer.data]).encode())
    bpy.ops.object.select_all(action='DESELECT')
    for obj in target.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parent
    # Unity prefabs reuse the existing gate materials and textures.
    bpy.ops.export_scene.gltf(filepath=str(MODELS / f'{parent.name}.glb'),
        export_format='GLB', use_selection=True, export_yup=True,
        export_animations=False, export_extras=True, export_tangents=True,
        export_image_format='NONE', export_vertex_color='NAME',
        export_vertex_color_name='BrickTint', export_all_vertex_colors=False)
    report.append({'name': parent.name, 'triangles': triangles,
                   'geometry_hash': geometry.hexdigest(), 'color_hash': colors.hexdigest(),
                   'brick_colors': tint_counts})
    parent.location.y = index * 2

assert len({r['geometry_hash'] for r in report}) == 1
assert len({r['color_hash'] for r in report}) == 6
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'FactoryWall_9m_Variants.blend'))
(SOURCE / 'wall_variants_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('WALL_VARIANTS_COMPLETE', json.dumps(report), flush=True)
