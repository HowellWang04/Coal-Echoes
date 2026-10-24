"""Export the current guardhouse with an independent door pivot."""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes')
SOURCE = ROOT / 'ArtSource/Guardhouse'
ASSET = ROOT / 'Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt'
source_scene = bpy.context.scene
door = bpy.data.objects['Door_Hinge']
door.rotation_euler.z = 0
bpy.context.view_layer.update()
collection = bpy.data.collections['Guardhouse | game meshes']
meshes = [o for o in collection.objects if o.type == 'MESH']
deps = bpy.context.evaluated_depsgraph_get()
report = {'source_meshes': len(meshes), 'triangles': 0, 'nonmanifold_meshes': []}
for obj in meshes:
    ev = obj.evaluated_get(deps)
    mesh = ev.to_mesh()
    mesh.calc_loop_triangles()
    report['triangles'] += len(mesh.loop_triangles)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    count = sum(not e.is_manifold for e in bm.edges)
    if count:
        report['nonmanifold_meshes'].append({'name': obj.name, 'edges': count})
    bm.free()
    ev.to_mesh_clear()

def tree(objects):
    verts, faces = [], []
    deps = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        start = len(verts)
        verts.extend(ev.matrix_world @ v.co for v in mesh.vertices)
        faces.extend(tuple(start + i for i in p.vertices) for p in mesh.polygons)
        ev.to_mesh_clear()
    return BVHTree.FromPolygons(verts, faces)

moving = [o for o in door.children if o.name.startswith(('Door_Stile', 'Door_SolidRail', 'Door_RecessedPanel'))]
fixed = [o for o in meshes if o.name.startswith(('FrontWall', 'DoorFrame', 'TransomRail', 'Threshold', 'InteriorFloor'))]
report['door_sweep'] = []
for angle in (0, -15, -45, -85, -100):
    door.rotation_euler.z = math.radians(angle)
    bpy.context.view_layer.update()
    intersections = len(tree(moving).overlap(tree(fixed)))
    report['door_sweep'].append({'degrees': angle, 'intersections': intersections})
assert all(c['intersections'] == 0 for c in report['door_sweep'])
door.rotation_euler.z = 0
bpy.context.view_layer.update()
deps = bpy.context.evaluated_depsgraph_get()
# Samples follow the final resized door, including its keyway and inset cylinder.
key = bpy.data.objects['MortiseLock_InsetCylinder']
key_world = key.matrix_world.translation
report['keyhole_rays'] = []
for label, dx, dz in [('round_hole',0,.005),('stem',0,-.011),('cylinder_face',.017,.005)]:
    origin = Vector((key_world.x + dx, -2.6, key_world.z + dz * door.scale.z))
    hit, loc, normal, index, obj, matrix = source_scene.ray_cast(deps, origin, Vector((0,1,0)), distance=.4)
    report['keyhole_rays'].append({'sample': label, 'hit': obj.name if hit else None, 'y': round(loc.y,6)})
assert report['keyhole_rays'][0]['hit'] == 'MortiseLock_DarkInterior'
assert report['keyhole_rays'][1]['hit'] == 'MortiseLock_DarkInterior'
assert report['keyhole_rays'][2]['hit'] == 'MortiseLock_InsetCylinder'
report['keyhole_depth_metres'] = round(report['keyhole_rays'][0]['y'] - report['keyhole_rays'][2]['y'],6)

export_scene = bpy.data.scenes.new('Guardhouse_Export')
export_root = bpy.data.objects.new('Guardhouse_Game', None)
export_scene.collection.objects.link(export_root)
export_hinge = bpy.data.objects.new('Door_Hinge_Game', None)
export_scene.collection.objects.link(export_hinge)
export_hinge.parent = export_root
export_hinge.location = door.matrix_world.translation
export_hinge['open_angle_degrees'] = -100.0
export_hinge['clear_opening_metres'] = 1.205

groups = {
    'Guardhouse_Shell': [o for o in meshes if o.parent not in (door,bpy.data.objects['Roof_Removable'])],
    'Guardhouse_Roof': list(bpy.data.objects['Roof_Removable'].children),
    'Guardhouse_DoorLeaf': list(door.children),
}
for name, objects in groups.items():
    verts, faces, uvs, material_indices, smooth = [], [], [], [], []
    materials = []
    parent = export_hinge if name == 'Guardhouse_DoorLeaf' else export_root
    pivot = parent.location if parent != export_root else Vector((0,0,0))
    for obj in objects:
        if obj.type != 'MESH':
            continue
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        offset = len(verts)
        verts.extend(ev.matrix_world @ v.co - pivot for v in mesh.vertices)
        for poly in mesh.polygons:
            faces.append(tuple(offset + i for i in poly.vertices))
            material = mesh.materials[poly.material_index]
            if material not in materials:
                materials.append(material)
            material_indices.append(materials.index(material))
            smooth.append(poly.use_smooth)
            uvs.append([tuple(mesh.uv_layers.active.data[i].uv) if mesh.uv_layers.active else (0,0) for i in poly.loop_indices])
        ev.to_mesh_clear()
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    for material in materials:
        mesh.materials.append(material)
    uv = mesh.uv_layers.new(name='UVMap')
    for poly, coords, material_index, is_smooth in zip(mesh.polygons, uvs, material_indices, smooth):
        poly.material_index = material_index
        poly.use_smooth = is_smooth
        for i, coord in zip(poly.loop_indices, coords):
            uv.data[i].uv = coord
    obj = bpy.data.objects.new(name, mesh)
    export_scene.collection.objects.link(obj)
    obj.parent = parent

bpy.context.window.scene = export_scene
bpy.ops.object.select_all(action='SELECT')
export_path = ASSET / 'Guardhouse_Rebuilt.glb'
print('Export format choices', [i.identifier for i in bpy.ops.export_scene.gltf.get_rna_type().properties['export_format'].enum_items])
bpy.ops.export_scene.gltf(filepath=str(export_path), export_format='GLB', use_selection=True, use_active_scene=True, export_yup=True, export_apply=True, export_extras=True, export_animations=False)
report['export_meshes'] = len(groups)
report['export_bytes'] = export_path.stat().st_size
bpy.context.window.scene = source_scene
for obj in list(export_scene.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
bpy.data.scenes.remove(export_scene)
(SOURCE / 'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
