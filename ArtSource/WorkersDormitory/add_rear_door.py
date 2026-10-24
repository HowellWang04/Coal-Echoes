"""Add the central rear entrance to the existing editable dormitory.

Run in WorkersDormitory.blend. Existing materials and front entrances are preserved.
"""
from pathlib import Path
import ast
import hashlib
import json
import math
import random
import shutil
import bpy
import bmesh
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
scene = bpy.data.scenes['WorkersDormitory']
bpy.context.window.scene = scene
asset = bpy.data.collections['WorkersDormitory_Asset']
root = bpy.data.objects['WorkersDormitory']
assert asset.objects.get('RearDoor_Hinge') is None, 'Rear entrance already exists'
materials = {m.name.removeprefix('Dorm_'): m for m in bpy.data.materials if m.name.startswith('Dorm_')}
batches = {}
rng = random.Random(19731004)
tree = ast.parse((SOURCE / 'build_dormitory.py').read_text(encoding='utf-8'))
helpers = ast.Module(body=[n for n in tree.body if
    isinstance(n, ast.ClassDef) and n.name == 'Mesh' or
    isinstance(n, ast.FunctionDef) and n.name in ('batch', 'box', 'wall')], type_ignores=[])
exec(compile(helpers, 'dormitory_helpers', 'exec'))

def fingerprint(obj):
    return hashlib.sha256(repr((
        [tuple(v.co) for v in obj.data.vertices],
        [tuple(p.vertices) for p in obj.data.polygons],
        [tuple(v.uv) for v in obj.data.uv_layers.active.data]
    )).encode()).hexdigest()

before = {o.name: fingerprint(o) for o in asset.objects if o.type == 'MESH'}
holes = [(x-.59, x+.59, .45+floor*3+.86, .45+floor*3+2.4)
         for floor in range(3) for x in (-14,-10,-6,-2,2,6,10,14)]
holes.append((-.70, .70, .49, 3.03))
bpy.data.objects.remove(asset.objects['RearMasonry__BuffBrick'], do_unlink=True)
wall('RearMasonry', 'X', 4.8, -16, 16, holes)

# Recess the ground-floor end of the centre light-blocking partition.
# The rear vestibule stays between the existing window pockets.
pockets = asset.objects['RoomPockets__Interior']
bm = bmesh.new()
bm.from_mesh(pockets.data)
unseen = set(bm.verts)
removed = 0
while unseen:
    seed = unseen.pop()
    component, todo = {seed}, [seed]
    while todo:
        v = todo.pop()
        for edge in v.link_edges:
            other = edge.other_vert(v)
            if other in unseen:
                unseen.remove(other)
                component.add(other)
                todo.append(other)
    if all(abs(v.co.x) < .081 for v in component) and max(v.co.z for v in component) > 9:
        bmesh.ops.delete(bm, geom=list(component), context='VERTS')
        removed += 1
assert removed == 1
bm.to_mesh(pockets.data)
bm.free()
box('RearVestibulePartition', 'Interior', (0,-.975,4.8), (.16,6.95,8.65))
box('RearVestibulePartition', 'Interior', (0,3.475,6.0625), (.16,1.95,6.125))

# One closed stair mesh avoids coincident sides on nested step boxes.
profile = [(5.025,0),(5.98,0),(5.98,.52/3),(5.70,.52/3),
           (5.70,.52*2/3),(5.42,.52*2/3),(5.42,.52),(5.025,.52)]
steps = batch('RearEntranceSteps', 'Concrete')
left = [(-.975,y,z) for y,z in profile]
right = [(.975,y,z) for y,z in profile]
steps.face(left, [(y/2.6,z/2.6) for _,y,z in left])
steps.face(list(reversed(right)), [(y/2.6,z/2.6) for _,y,z in reversed(right)])
for i in range(len(profile)):
    j = (i+1) % len(profile)
    points = [left[i],right[i],right[j],left[j]]
    axis = 2 if abs(profile[i][1]-profile[j][1]) < .00001 else 1
    steps.face(points, [(p[0]/2.6,p[1 if axis == 2 else 2]/2.6) for p in points])
for x in (-.77,.77):
    box('RearEntranceSurround', 'Concrete', (x,4.91,1.75), (.20,.67,2.50))
box('RearEntranceSurround', 'Concrete', (0,4.91,3.10), (1.74,.67,.20))
box('RearEntranceCanopy', 'Concrete', (0,5.28,3.34), (2.10,1.10,.16))
for x in (-.635,.635):
    box('RearEntranceFrame', 'FrameBrown', (x,4.67,1.76), (.065,.16,2.48))
box('RearEntranceFrame', 'FrameBrown', (0,4.67,2.9675), (1.205,.16,.065))
box('RearEntranceThreshold', 'Concrete', (0,4.82,.495), (1.34,.45,.07))
for (group, mat), mesh in batches.items():
    obj = mesh.make(group+'__'+mat, mat, asset, root, .008 if mat in ('Concrete','FrameBrown') else 0)
    if group == 'RearEntranceSteps':
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.00001)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(obj.data)
        bm.free()

batches = {}
hinge = bpy.data.objects.new('RearDoor_Hinge', None)
asset.objects.link(hinge)
hinge.parent = root
hinge.location = (-.597,4.60,.54)
hinge['opening_width_m'] = 1.205
hinge['opening_height_m'] = 2.415
hinge['open_angle_degrees'] = -95.0
hinge['default_closed'] = True
for x in (-.557,.557):
    box('RearDoorLeaf', 'FrameGreen', (x,4.97,1.7375), (.08,.09,2.365))
for z in (.595,1.77,2.88):
    box('RearDoorLeaf', 'FrameGreen', (0,4.97,z), (1.194,.09,.08))
box('RearDoorLeaf', 'FrameGreen', (0,4.97,1.1825), (1.034,.06,1.095))
for x in (-.267,.267):
    box('RearDoorLeaf', 'Glass2', (x,4.97,2.325), (.494,.018,1.03))
box('RearDoorLeaf', 'FrameBrown', (0,4.97,2.325), (.04,.09,1.03))
for y in (4.90,5.04):
    box('RearDoorLeaf', 'FrameGrey', (.43,y,1.55), (.08,.02,.22))
    batch('RearDoorLeaf', 'FrameGrey').rod((.43,y+.018,1.55),(.28,y+.018,1.55),.017,8)
for z in (.82,2.60):
    batch('RearDoorLeaf', 'FrameGrey').rod((-.60,4.98,z-.07),(-.60,4.98,z+.07),.025,10)
for (group, mat), mesh in batches.items():
    mesh.vertices = [(x,y-.32,z) for x,y,z in mesh.vertices]
    obj = mesh.make(group+'__'+mat, mat, asset, root, .006 if mat.startswith('Frame') else 0)
    obj.parent = hinge
    obj.location = -hinge.location
bpy.context.view_layer.update()

changed = {'RearMasonry__BuffBrick','RoomPockets__Interior'}
assert all(fingerprint(asset.objects[name]) == value for name,value in before.items() if name not in changed)

# Preserve the existing material-group node names and keep the new leaf separate.
export_scene = bpy.data.scenes.new('Dormitory_ExportTemporary')
export_root = bpy.data.objects.new('WorkersDormitory_Export', None)
export_scene.collection.objects.link(export_root)
export_hinge = bpy.data.objects.new('RearDoor_Hinge', None)
export_scene.collection.objects.link(export_hinge)
export_hinge.parent = export_root
export_hinge.location = hinge.location
dg = bpy.context.evaluated_depsgraph_get()
groups = {}
mesh_names = {
    'Dorm_BuffBrick': 'RearMasonry__BuffBrick.003',
    'Dorm_Concrete': 'Sills__Concrete.003',
    'Dorm_Interior': 'RoomPockets__Interior.003',
    'Dorm_Roof': 'Roof__Roof.003',
    'Dorm_SillRunoff.002': 'SillRunoff.005',
}
for suffix in ('FrameBrown','FrameGreen','FrameGrey','Glass0','Glass1','Glass2','Glass3','Glass4','Glass5'):
    mesh_names['Dorm_'+suffix] = 'BalconyInnerWindows__'+suffix+'.003'
for obj in asset.objects:
    if obj.type != 'MESH':
        continue
    mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(dg), depsgraph=dg)
    copy = bpy.data.objects.new(obj.name+'_Export', mesh)
    export_scene.collection.objects.link(copy)
    copy.matrix_world = obj.matrix_world.copy()
    key = 'Dorm_RearDoorLeaf' if obj.parent == hinge else mesh.materials[0].name
    groups.setdefault(key, []).append(copy)
bpy.context.window.scene = export_scene
for name, objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = objects[0]
    obj.name = name
    desired = mesh_names.get(name, name)
    previous = bpy.data.meshes.get(desired)
    if previous and previous != obj.data:
        previous.name = desired+'_Source'
    obj.data.name = desired
    world = obj.matrix_world.copy()
    obj.parent = export_hinge if name == 'Dorm_RearDoorLeaf' else export_root
    obj.matrix_world = world
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = export_root
bpy.ops.export_scene.gltf(filepath=str(SOURCE/'Exports/WorkersDormitory.glb'), export_format='GLB',
    use_selection=True, use_active_scene=True, export_apply=True, export_extras=True, export_animations=False)
bpy.ops.export_scene.fbx(filepath=str(SOURCE/'Exports/WorkersDormitory.fbx'), use_selection=True,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False)
temporary = list(export_scene.objects)
bpy.context.window.scene = scene
for obj in temporary:
    bpy.data.objects.remove(obj, do_unlink=True)
bpy.data.scenes.remove(export_scene)
old = bpy.context.preferences.filepaths.save_version
bpy.context.preferences.filepaths.save_version = 0
try:
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'WorkersDormitory.blend'))
finally:
    bpy.context.preferences.filepaths.save_version = old
target = SOURCE.parents[1]/'Assets/Art Resources/Environment/WorkersDormitory/WorkersDormitory.glb'
shutil.copyfile(SOURCE/'Exports/WorkersDormitory.glb', target)
report = {'unchanged_source_meshes':len(before)-2, 'changed_existing_meshes':sorted(changed),
    'door_clear_width_m':1.205, 'door_clear_height_m':2.415, 'default_closed':True,
    'doorway_floor_height_m':.52, 'interior_scope':'Rear vestibule only; existing interior is not playable'}
(SOURCE/'rear_door_revision.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
