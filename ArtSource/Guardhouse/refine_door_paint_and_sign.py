"""Calm the painted timber wear and hang the nameplate beside the front door.

Run after repair_surfaces.py in the open source Blender scene, then run
export_guardhouse.py.
"""
import ast
import math
from pathlib import Path
import bpy
import bmesh
import numpy as np
from mathutils.bvhtree import BVHTree

SOURCE = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Guardhouse')
TEXTURES = SOURCE.parents[1] / 'Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/Textures'
asset = bpy.data.collections['Guardhouse | game meshes']
shell = bpy.data.objects['Architecture']
door = bpy.data.objects['Door_Hinge']
tree = ast.parse((SOURCE / 'build_guardhouse.py').read_text(encoding='utf-8'))
helpers = ast.Module(body=[n for n in tree.body if isinstance(n, ast.FunctionDef)
                          and n.name in ('noise', 'save_image', 'box', 'cylinder')], type_ignores=[])
exec(compile(helpers, str(SOURCE / 'build_guardhouse.py'), 'exec'))

# The old mask mixed in fine noise, so orange wood showed through as dense
# rust-like specks. Paint now lifts only in a few broad clusters: the kick
# zone and around the lever, exposing grey weathered timber.
w, h = 1024, 1536
u, v = np.meshgrid(np.linspace(0, 1, w), np.linspace(0, 1, h))
broad = noise(w, h, 5, 8, 31)
patch = noise(w, h, 26, 40, 32)
chip = noise(w, h, 110, 165, 33)
grain = noise(w, h, 290, 9, 74)
field = broad * .45 + patch * .4 + chip * .15
risk = .24 + .35 * np.exp(-v / .05) + .36 * np.exp(-((u - .93) / .06) ** 2 - ((v - .52) / .07) ** 2)
wear = field < risk
rim = ~wear & (field < risk + .015)
paint = np.array([.28, .36, .355]) + (broad[:, :, None] - .5) * .05 + (grain[:, :, None] - .5) * .03
paint *= (1 - .16 * np.exp(-v / .14))[:, :, None]
paint[rim] *= .82
wood = np.array([.37, .345, .30]) + (grain[:, :, None] - .5) * .08
paint[wear] = wood[wear]
height = grain * .12 + chip * .02 + (~wear) * .10
dy, dx = np.gradient(height)
normal = np.stack((-dx * 3, -dy * 3, np.ones_like(dx)), axis=2)
normal /= np.linalg.norm(normal, axis=2)[:, :, None]

timber = bpy.data.materials['GH_BlueGrey_PaintedWood']
for node in timber.node_tree.nodes:
    if node.type != 'TEX_IMAGE':
        continue
    name = node.image.name
    old = node.image
    node.image = save_image(name, normal * .5 + .5, True) if name.endswith('_Normal') else save_image(name, paint)
    bpy.data.images.remove(old)
    node.image.name = name
timber.diffuse_color = (*np.mean(paint, axis=(0, 1)), 1)
print('Paint wear', round(wear.mean(), 4), 'kick zone', round(wear[v < .06].mean(), 3),
      'lever', round(wear[(abs(u - .93) < .06) & (abs(v - .52) < .07)].mean(), 3))

# A vertical enamel plate on the blank wall right of the entrance, clear of
# the door frame (outer edge x 2.0775) and the building corner (x 2.8).
ink = bpy.data.materials['GH_PlaqueLettering']
enamel = bpy.data.materials['GH_IvoryEnamel']
for name in ('Gatehouse_EnamelPlaque', 'Gatehouse_PlaqueFace', 'GatehouseSign_Lettering', 'Plaque_Fixing', 'Plaque_Fixing.001'):
    bpy.data.objects.remove(bpy.data.objects[name], do_unlink=True)
x, z = 2.44, 1.72
plate = [box('Gatehouse_EnamelPlaque', (x, -2.357, z), (.24, .026, .72), ink, bevel=.009),
         box('Gatehouse_PlaqueFace', (x, -2.374, z), (.206, .012, .686), enamel, bevel=.006)]
font = bpy.data.fonts.load(r'C:\Windows\Fonts\simhei.ttf', check_existing=True)
curve = bpy.data.curves.new('GatehouseSignText', 'FONT')
curve.body = '门\n卫\n室'
curve.font = font
curve.size = .14
curve.space_line = 1.1
curve.align_x = 'CENTER'
curve.align_y = 'CENTER'
curve.extrude = .0003
label = bpy.data.objects.new('GatehouseSign_Lettering', curve)
asset.objects.link(label)
label.parent = shell
label.location = (x, -2.382, z)
label.rotation_euler.x = math.pi / 2
curve.materials.append(ink)
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active = label
label.select_set(True)
bpy.ops.object.convert(target='MESH')
bm = bmesh.new()
bm.from_mesh(label.data)
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000001)
bm.to_mesh(label.data)
bm.free()
label.select_set(False)
plate.append(label)
for fz in (z - .31, z + .31):
    plate.append(cylinder('Plaque_Fixing', (x, -2.388, fz), .006, .004, bpy.data.materials['GH_OxidizedIron'], axis='Y'))
bpy.context.view_layer.update()

def world_bounds(obj):
    ev = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    points = [ev.matrix_world @ v.co for v in ev.to_mesh().vertices]
    ev.to_mesh_clear()
    return [min(p[i] for p in points) for i in range(3)], [max(p[i] for p in points) for i in range(3)]

def bvh(objects):
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

low, high = world_bounds(plate[0])
text_low, text_high = world_bounds(label)
face_low, face_high = world_bounds(plate[1])
print('Plate x', round(low[0], 4), round(high[0], 4), 'z', round(low[2], 4), round(high[2], 4))
print('Lettering x', round(text_low[0], 4), round(text_high[0], 4), 'z', round(text_low[2], 4), round(text_high[2], 4))
assert low[0] > 2.0775 + .15 and high[0] < 2.8 - .15
assert face_low[0] < text_low[0] and text_high[0] < face_high[0]
assert z - .31 + .01 < text_low[2] and text_high[2] < z + .31 - .01
others = [o for o in asset.objects if o.type == 'MESH' and o not in plate and not o.name.startswith('FrontWall_')]
plate_tree = bvh(plate)
touching = [o.name for o in others if plate_tree.overlap(bvh([o]))]
assert not touching, touching

old = bpy.context.preferences.filepaths.save_version
bpy.context.preferences.filepaths.save_version = 0
try:
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'Guardhouse_Rebuilt.blend'), compress=True)
finally:
    bpy.context.preferences.filepaths.save_version = old
print('Door paint refreshed; nameplate moved beside the entrance')
