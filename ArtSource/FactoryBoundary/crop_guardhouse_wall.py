"""Crop the 3 m wall into the short segment between Pillar_030 and the guardhouse.

blender --background --factory-startup --python-exit-code 1 --python ArtSource/FactoryBoundary/crop_guardhouse_wall.py
Bricks keep their size, UVs and per-brick tint; the open cut end sits inside the guardhouse wall.
"""

from pathlib import Path

import bpy
import bmesh

MODELS = Path(__file__).resolve().parents[2] / 'Assets/Art Resources/Environment/Gate Area/FactoryBoundary/Models'
# Unity local X 0.359 is the module's finished end; keep 0.82 m of wall from it.
CUT_X = 0.359 + 0.82

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(MODELS / 'FactoryWall_3m.glb'))
result = []
# The gate-side infill shows no whitewash, so this segment matches it.
for original in [o for o in bpy.context.scene.objects if o.type == 'MESH' and not o.name.endswith('Whitewash')]:
    mesh = original.data.copy()
    mesh.transform(original.matrix_world)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    # Blender X is Unity -X after glTF import.
    bmesh.ops.bisect_plane(bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces), dist=.00001,
                           plane_co=(-CUT_X, 0, 0), plane_no=(-1, 0, 0), clear_outer=True)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    if not mesh.polygons:
        bpy.data.meshes.remove(mesh)
        continue
    obj = bpy.data.objects.new('GuardhouseWall_' + original.name.rsplit('_', 1)[-1], mesh)
    bpy.context.scene.collection.objects.link(obj)
    result.append(obj)
for obj in list(bpy.context.scene.objects):
    if obj not in result:
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.object.select_all(action='SELECT')
# The importer renames the tint layer, so export whatever name it was given.
color = next(o.data.color_attributes.active_color.name for o in result if o.data.color_attributes.active_color)
bpy.ops.export_scene.gltf(filepath=str(MODELS / 'FactoryWall_GuardhouseShort.glb'), export_format='GLB',
    use_selection=True, export_apply=True, export_yup=True, export_animations=False, export_image_format='NONE',
    export_vertex_color='NAME', export_vertex_color_name=color, export_all_vertex_colors=False)
print('GUARDHOUSE_WALL', color, [(o.name, len(o.data.polygons), round(o.dimensions.x, 3)) for o in result], flush=True)
