"""Coal Echoes guardhouse. Run in Blender's Python context."""
from pathlib import Path
import math
import random
import json

import bpy
import bmesh
import numpy as np
from mathutils import Vector

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes')
SOURCE = ROOT / 'ArtSource/Guardhouse'
ASSET = ROOT / 'Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt'
TEXTURES = ASSET / 'Textures'
for folder in (SOURCE / 'Previews', TEXTURES):
    folder.mkdir(parents=True, exist_ok=True)
rng = random.Random(73)

scene = bpy.data.scenes.new('Guardhouse_Rebuilt')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
asset = bpy.data.collections.new('Guardhouse | game meshes')
scene.collection.children.link(asset)
stage = bpy.data.collections.new('Presentation | excluded from export')
scene.collection.children.link(stage)

def empty(name, location=(0, 0, 0), parent=None):
    obj = bpy.data.objects.new(name, None)
    asset.objects.link(obj)
    obj.location = location
    obj.parent = parent
    return obj

root = empty('Guardhouse_Rebuilt')
shell = empty('Architecture', parent=root)
roof = empty('Roof_Removable', parent=root)
door = empty('Door_Hinge', (.81, -2.365, .18), root)
door['closed_angle_degrees'] = 0.0
door['open_angle_degrees'] = -100.0
door['clear_opening_metres'] = 1.18
door['lock'] = 'Recessed mortise lock with real keyhole cavity'

def noise(w, h, gx, gy, seed):
    randoms = np.random.default_rng(seed).random((gy + 1, gx + 1)).astype(np.float32)
    x = np.linspace(0, gx, w, endpoint=False)
    y = np.linspace(0, gy, h, endpoint=False)
    xi, yi = x.astype(int), y.astype(int)
    xf, yf = x - xi, y - yi
    xf, yf = xf * xf * (3 - 2 * xf), yf * yf * (3 - 2 * yf)
    a = randoms[yi[:, None], xi] * (1 - xf) + randoms[yi[:, None], xi + 1] * xf
    b = randoms[yi[:, None] + 1, xi] * (1 - xf) + randoms[yi[:, None] + 1, xi + 1] * xf
    return a * (1 - yf[:, None]) + b * yf[:, None]

def save_image(name, rgb, noncolor=False):
    h, w = rgb.shape[:2]
    image = bpy.data.images.new(name, width=w, height=h, alpha=True)
    if noncolor:
        image.colorspace_settings.name = 'Non-Color'
    rgba = np.ones((h, w, 4), dtype=np.float32)
    rgba[:, :, :3] = np.clip(rgb, 0, 1)
    image.pixels.foreach_set(rgba.ravel())
    image.filepath_raw = str(TEXTURES / (name + '.png'))
    image.file_format = 'PNG'
    image.save()
    image.pack()
    return image

def textured(name, rgb, height, rough=.88, metal=0, normal_strength=2):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metal
    image = save_image(name + '_BaseColor', rgb)
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = image
    links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    dy, dx = np.gradient(height)
    n = np.stack((-dx * normal_strength, -dy * normal_strength, np.ones_like(dx)), axis=2)
    n /= np.linalg.norm(n, axis=2)[:, :, None]
    normal_image = save_image(name + '_Normal', n * .5 + .5, True)
    nt = nodes.new('ShaderNodeTexImage')
    nt.image = normal_image
    nm = nodes.new('ShaderNodeNormalMap')
    links.new(nt.outputs['Color'], nm.inputs['Color'])
    links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
    mat.diffuse_color = (*np.mean(rgb, axis=(0, 1)), 1)
    return mat

def plain(name, color, rough=.8, metal=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metal
    return mat

def facade(name, width, seed, openings=()):
    w, h = 1792, 1152
    u, v = np.meshgrid(np.linspace(0, width, w), np.linspace(0, 3.4, h))
    low = noise(w, h, 9, 7, seed)
    medium = noise(w, h, 70, 48, seed + 1)
    grain = noise(w, h, 440, 270, seed + 2)
    fine = np.random.default_rng(seed).random((h, w))
    tone = (low - .5) * .13 + (medium - .5) * .065 + (grain - .5) * .045 + (fine - .5) * .028
    plaster = np.array([.64, .60, .50])[None, None, :] + tone[:, :, None]
    damp = np.exp(-v / .38) * (.25 + .35 * low)
    plaster *= (1 - damp[:, :, None])
    row = np.floor(v / .085)
    bx = ((u + (row % 2) * .135) % .27) / .27
    bz = (v % .085) / .085
    mortar = (bx < .035) | (bx > .965) | (bz < .065) | (bz > .935)
    variation = np.sin(np.floor((u + (row % 2) * .135) / .27) * 61.7 + row * 15.23) * .045
    bricks = np.array([.49, .335, .225])[None, None, :] + variation[:, :, None] + (grain[:, :, None] - .5) * .08
    bricks[mortar] = [.37, .35, .30]
    erosion = .20 + .23 * np.exp(-v / .48)
    erosion += .14 * np.exp(-((u - width * .08) / .45) ** 2 - ((v - .9) / .7) ** 2)
    erosion += .11 * np.exp(-((u - width * .83) / .55) ** 2 - ((v - .45) / .6) ** 2)
    field = low * .72 + medium * .23 + grain * .05
    exposed = field < erosion
    rim = (~exposed) & (field < erosion + .018)
    plaster[rim] = [.40, .37, .31]
    plaster[exposed] = bricks[exposed]
    for a, b, sill in openings:
        streak = noise(w, h, 210, 3, seed + 8)
        below = (u > a) & (u < b) & (v < sill) & (v > sill - .78)
        drips = np.clip((streak - .4) * .4, 0, .23) * np.exp((v - sill) / .4)
        plaster *= (1 - np.where(below, drips, 0))[:, :, None]
    plaster *= (1 - .13 * np.exp(-((v - 3.07) / .12) ** 2))[:, :, None]
    height = .4 + medium * .03 + grain * .04 + fine * .015
    height[exposed] = .24 + grain[exposed] * .035
    height[exposed & mortar] = .08
    return textured(name, plaster, height, normal_strength=4)

front = facade('GH_Front_WeatheredLime', 5.6, 73, ((.55, 2.55, 1.15),))
back = facade('GH_Back_WeatheredLime', 5.6, 117)
side = facade('GH_Sides_WeatheredLime', 4.7, 221, ((1.25, 3.45, 1.15),))
w, h = 1024, 1536
u, v = np.meshgrid(np.linspace(0, 1, w), np.linspace(0, 1, h))
n = noise(w, h, 30, 42, 31)
grain = noise(w, h, 290, 9, 74)
fine = noise(w, h, 210, 155, 91)
wear = (n * .65 + fine * .35 < .29) | ((v < .10) & (n < .58))
wood = np.array([.32, .23, .14]) + (grain[:, :, None] - .5) * .18
paint = np.array([.27, .36, .355]) + (n[:, :, None] - .5) * .12 + (grain[:, :, None] - .5) * .055
paint[wear] = wood[wear]
door_mat = textured('GH_BlueGrey_PaintedWood', paint, grain * .18 + fine * .04 + (~wear) * .10, .84, normal_strength=3)
inside_rgb = np.ones((768, 1024, 3)) * [.70, .67, .56]
inside_noise = noise(1024, 768, 50, 40, 213)
inside_rgb += (inside_noise[:, :, None] - .5) * .07
inside_rgb[:290] *= [.55, .64, .59]
interior = textured('GH_Interior_LimeAndGreenDado', inside_rgb, inside_noise * .08)
iron = plain('GH_OxidizedIron', (.095, .085, .062), .68, .72)
brass = plain('GH_TarnishedLock', (.24, .175, .075), .54, .75)
cavity = plain('GH_KeyholeDepth', (.009, .007, .005), .95)
enamel = plain('GH_IvoryEnamel', (.58, .56, .43), .38)
ink = plain('GH_PlaqueLettering', (.065, .105, .10), .65)
rubber = plain('GH_RoofBitumen', (.055, .052, .043), .95)
glass = plain('GH_DustyWindowGlass', (.22, .285, .27), .24)
gb = next(n for n in glass.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
gb.inputs['Transmission Weight'].default_value = .82
gb.inputs['IOR'].default_value = 1.45
concrete = bpy.data.materials.get('Concrete')
if concrete is None:
    with bpy.data.libraries.load(str(ROOT / 'ArtSource/FactoryGate/FactoryGate_Rebuilt.blend'), link=False) as (src, dst):
        dst.materials = ['Concrete']
    concrete = dst.materials[0]

def box(name, center, size, mat, parent=shell, bevel=.008, uv_axes=None, uv_bounds=None):
    x, y, z = center
    a, b, c = (s / 2 for s in size)
    verts = [(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
             (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(2,3,7,6),(3,0,4,7),(1,2,6,5)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(mat)
    mesh.update()
    uv = mesh.uv_layers.new(name='UVMap')
    for poly in mesh.polygons:
        axes = uv_axes or ((0,1) if abs(poly.normal.z) > .5 else ((0,2) if abs(poly.normal.y) > .5 else (1,2)))
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            if uv_bounds:
                u0, v0, uw, vh = uv_bounds
                uv.data[li].uv = ((co[axes[0]] - u0) / uw, (co[axes[1]] - v0) / vh)
            else:
                uv.data[li].uv = (co[axes[0]] * .55, co[axes[1]] * .55)
    obj = bpy.data.objects.new(name, mesh)
    asset.objects.link(obj)
    obj.parent = parent
    if bevel:
        mod = obj.modifiers.new('Small worn edges', 'BEVEL')
        mod.width, mod.segments = bevel, 2
    return obj

def cylinder(name, center, radius, depth, mat, parent=shell, axis='Z', vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=center)
    obj = bpy.context.object
    obj.name = name
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    asset.objects.link(obj)
    obj.data.materials.append(mat)
    if axis == 'Y':
        obj.rotation_euler.x = math.pi / 2
    if axis == 'X':
        obj.rotation_euler.y = math.pi / 2
    obj.parent = parent
    if parent == door:
        obj.matrix_parent_inverse = door.matrix_world.inverted()
    mod = obj.modifiers.new('Machined edges', 'BEVEL')
    mod.width, mod.segments = min(.003, radius / 6), 2
    return obj

def cut(target, cutter):
    bpy.context.view_layer.objects.active = target
    mod = target.modifiers.new('Recess geometry', 'BOOLEAN')
    mod.operation = 'DIFFERENCE'
    mod.object = cutter
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter, do_unlink=True)

def wall(axis, fixed, start, end, openings, mat, name):
    # Rectangle decomposition leaves genuine door and window voids.
    points = sorted({start, end, *[v for hole in openings for v in hole[:2]]})
    for i, (a, b) in enumerate(zip(points, points[1:])):
        holes = [hole for hole in openings if hole[0] <= (a+b)/2 <= hole[1]]
        spans = [(.11, 3.18)] if not holes else [(.11, holes[0][2]), (holes[0][3], 3.18)]
        for j, (lo, hi) in enumerate(spans):
            if hi <= lo:
                continue
            center = ((a+b)/2, fixed, (lo+hi)/2) if axis == 0 else (fixed, (a+b)/2, (lo+hi)/2)
            dims = (b-a, .25, hi-lo) if axis == 0 else (.25, b-a, hi-lo)
            obj = box(f'{name}_{i}_{j}', center, dims, mat, uv_axes=(axis,2), uv_bounds=(start,0,end-start,3.4), bevel=0)
            obj.data.materials.append(interior)
            for poly in obj.data.polygons:
                if poly.normal[1-axis] * fixed < -.01:
                    poly.material_index = 1

box('Foundation', (0,0,.025), (5.69,4.79,.17), concrete, bevel=.018)
box('InteriorFloor', (0,0,.135), (5.32,4.42,.09), concrete, bevel=.008)
wall(0,-2.225,-2.8,2.8,[(-2.2,-.2,1.15,2.5),(.75,2.05,.18,2.48)],front,'FrontWall')
wall(0,2.225,-2.8,2.8,[],back,'RearWall')
wall(1,-2.675,-2.1,2.1,[(-1.1,1.1,1.15,2.5)],side,'LeftWall')
wall(1,2.675,-2.1,2.1,[(-.8,.8,1.45,2.5)],side,'RightWall')
for x in (-2.68,2.68):
    box('SideCementPlinth', (x,0,.285), (.27,4.45,.22), concrete)
box('RearCementPlinth',(0,2.235,.285),(5.6,.27,.22),concrete)
box('FrontPlinth_Left',(-1.025,-2.235,.285),(3.55,.27,.22),concrete)
box('FrontPlinth_Right',(2.425,-2.235,.285),(.75,.27,.22),concrete)
box('Threshold',(1.4,-2.3,.185),(1.35,.46,.065),concrete)
box('EntranceStep',(1.4,-2.64,.065),(1.62,.55,.13),concrete,bevel=.025)
box('RoofSlab',(0,0,3.24),(5.99,5.09,.16),concrete,roof,.018)
box('RoofFelt',(0,0,3.335),(5.72,4.82,.045),rubber,roof,.004)
for y in (-2.44,2.44):
    box('RoofEdge',(0,y,3.40),(5.98,.18,.17),concrete,roof,.01)
for x in (-2.9,2.9):
    box('RoofEdge',(x,0,3.40),(.18,4.7,.17),concrete,roof,.01)
for x in (-1.9,-.6,.7,2.0):
    box('FeltOverlap',(x,0,3.361),(.025,4.7,.009),rubber,roof,.002)

def window(name, center, width, height, sideways=False):
    group = empty(name, parent=shell)
    group.location = center
    if sideways:
        group.rotation_euler.z = math.pi / 2
    for x in (-width/2,width/2):
        box(name+'_Jamb',(x,0,0),(.065,.075,height+.1),door_mat,group,.005)
    for z in (-height/2,height/2):
        box(name+'_Rail',(0,0,z),(width+.10,.075,.065),door_mat,group,.005)
    for x in (-width/6,width/6):
        box(name+'_Mullion',(x,-.008,0),(.035,.055,height),door_mat,group,.003)
    box(name+'_Crossbar',(0,-.015,.05),(width,.055,.032),door_mat,group,.003)
    for col in range(3):
        for row in range(2):
            px = -width/2+(col+.5)*width/3
            pz = (-height/4+.025) if row==0 else (height/4+.025)
            ph = height/2+.05 if row==0 else height/2-.05
            pane = box(name+f'_Glass_{col}_{row}',(px,.012,pz),(width/3-.04,.012,ph-.045),glass,group,.001)
            # Dirt collects in the glazing putty, leaving the middle readable.
            box(name+'_Putty',(px,-.008,pz-ph/2+.012),(width/3-.035,.018,.018),enamel,group,.001)
    box(name+'_Sill',(0,-.06,-height/2-.075),(width+.25,.38,.10),concrete,group,.012)
    box(name+'_Lintel',(0,.055,height/2+.075),(width+.28,.24,.10),concrete,group,.009)
    for x in (-width/6,width/6):
        cylinder(name+'_Latch',(x,-.055,-.025),.012,.09,iron,group)
    return group

window('Front_ServiceWindow',(-1.2,-2.365,1.825),2.0,1.35)
window('Left_ObservationWindow',(-2.81,0,1.825),2.2,1.35,True)
right = window('Right_Window',(2.81,0,1.975),1.6,1.05,True)
right.rotation_euler.z = -math.pi/2

# Fixed timber frame and transom above the door leaf.
for x in (.785,2.015):
    box('DoorFrame_Jamb',(x,-2.32,1.31),(.075,.18,2.26),door_mat)
box('DoorFrame_Header',(1.4,-2.32,2.445),(1.30,.18,.07),door_mat)
box('TransomRail',(1.4,-2.34,2.285),(1.205,.10,.045),door_mat)
box('TransomGlass',(1.4,-2.30,2.365),(1.205,.016,.115),glass)
box('DoorLintel',(1.4,-2.23,2.565),(1.52,.31,.12),concrete)

def doorbox(name, center, size, mat=door_mat, bevel=.006):
    obj = box(name,center,size,mat,shell,bevel,uv_axes=(0,2),uv_bounds=(.81,.18,1.18,2.08))
    world = obj.matrix_world.copy()
    bpy.context.view_layer.update()
    obj.parent = door
    obj.matrix_parent_inverse = door.matrix_world.inverted()
    obj.matrix_world = world
    return obj

left_stile = doorbox('Door_Stile_Hinge',(.875,-2.365,1.225),(.12,.065,2.07))
right_stile = doorbox('Door_Stile_Lock',(1.925,-2.365,1.225),(.13,.065,2.07))
for z, thick in ((.25,.12),(.93,.13),(1.48,.13),(2.19,.13)):
    doorbox('Door_SolidRail',(1.4,-2.365,z),(.94,.065,thick))
for a,b in ((.31,.865),(.995,1.415),(1.545,2.125)):
    doorbox('Door_RecessedPanel',(1.4,-2.356,(a+b)/2),(.94,.036,b-a),bevel=.008)
    for x in (.96,1.84):
        doorbox('Door_PanelBeading',(x,-2.393,(a+b)/2),(.022,.018,b-a),bevel=.004)
    for z in (a+.012,b-.012):
        doorbox('Door_PanelBeading',(1.4,-2.393,z),(.90,.018,.025),bevel=.004)

# The iron faceplate sits in a milled pocket. The keyway is a cut void.
pocket = box('cut_lock_pocket',(1.925,-2.402,1.23),(.088,.032,.285),iron,bevel=0)
cut(right_stile,pocket)
plate = doorbox('MortiseLock_RecessedFaceplate',(1.925,-2.390,1.23),(.076,.008,.27),iron,.004)
lock_center = (1.925,-2.389,1.165)
plug = cylinder('MortiseLock_InsetCylinder',lock_center,.026,.011,brass,door,'Y',48)
bpy.context.view_layer.update()
for obj in (plate,plug,right_stile):
    round_cut = cylinder('cut_keyway_round',(1.925,-2.391,1.170),.008,.11,cavity,shell,'Y',32)
    for mod in list(round_cut.modifiers):
        round_cut.modifiers.remove(mod)
    cut(obj,round_cut)
    slot = box('cut_keyway_stem',(1.925,-2.391,1.156),(.008,.11,.025),cavity,bevel=0)
    cut(obj,slot)
doorbox('MortiseLock_DarkInterior',(1.925,-2.352,1.162),(.041,.006,.060),cavity,.002)
for z in (1.12,1.34):
    screw = cylinder('Lock_SlottedScrew',(1.925,-2.397,z),.006,.004,brass,door,'Y',24)
    slot = box('cut_screw_slot',(1.925,-2.400,z),(.009,.004,.0015),cavity,bevel=0)
    cut(screw,slot)
cylinder('Door_HandleSpindle',(1.925,-2.425,1.285),.014,.068,iron,door,'Y')
cylinder('Door_LeverHandle',(1.86,-2.458,1.285),.011,.145,iron,door,'X')
cylinder('Door_InnerSpindle',(1.925,-2.306,1.285),.014,.065,iron,door,'Y')
cylinder('Door_InnerLever',(1.86,-2.273,1.285),.011,.145,iron,door,'X')
for z in (.45,1.19,2.0):
    cylinder('Door_HingeBarrel',(.814,-2.403,z),.018,.14,iron)
    doorbox('Door_HingeLeaf',(.87,-2.402,z),(.105,.008,.10),iron,.002)

# Shallow concrete rain hood and a modest enamel identification plate.
hood = box('EntranceRainHood',(1.4,-2.60,2.79),(1.65,.91,.10),concrete,bevel=.014)
for x in (.90,1.90):
    box('HoodWallBracket',(x,-2.38,2.67),(.06,.30,.19),iron,bevel=.006)
box('Gatehouse_EnamelPlaque',(1.4,-2.357,3.015),(.75,.026,.245),ink,bevel=.009)
box('Gatehouse_PlaqueFace',(1.4,-2.374,3.015),(.716,.012,.211),enamel,bevel=.006)
font = bpy.data.fonts.load(r'C:\Windows\Fonts\simhei.ttf')
curve = bpy.data.curves.new('GatehouseSignText','FONT')
curve.body = '门 卫 室'
curve.font = font
curve.size = .16
curve.align_x = 'CENTER'
curve.align_y = 'CENTER'
curve.extrude = .0003
label = bpy.data.objects.new('GatehouseSign_Lettering',curve)
asset.objects.link(label)
label.parent = shell
label.location = (1.4,-2.382,3.015)
label.rotation_euler.x = math.pi/2
curve.materials.append(ink)
bpy.context.view_layer.objects.active = label
label.select_set(True)
bpy.ops.object.convert(target='MESH')
bm = bmesh.new()
bm.from_mesh(label.data)
bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000001)
bm.to_mesh(label.data)
bm.free()
label.select_set(False)
for x in (1.075,1.725):
    cylinder('Plaque_Fixing',(x,-2.388,3.015),.006,.004,iron,axis='Y')

# Galvanized drain, pipe collars and a vintage external wall lamp.
cylinder('Downpipe',(2.78,1.72,1.65),.044,3.1,iron)
for z in (.48,1.55,2.60):
    cylinder('DownpipeCollar',(2.78,1.72,z),.049,.045,iron)
    box('PipeWallBracket',(2.72,1.72,z),(.18,.035,.035),iron)
cylinder('RoofDrainSpout',(2.84,1.72,3.26),.05,.38,iron,roof,'X')
box('LampWallBase',(.35,-2.365,2.62),(.10,.035,.17),iron)
cylinder('LampArm',(.35,-2.49,2.67),.015,.25,iron,axis='Y')
cylinder('LampShadeRim',(.35,-2.61,2.62),.14,.025,ink)
bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,radius=1,location=(.35,-2.61,2.65))
shade = bpy.context.object
shade.name = 'Lamp_EnamelShade'
shade.scale = (.14,.14,.06)
for col in list(shade.users_collection):
    col.objects.unlink(shade)
asset.objects.link(shade)
shade.parent = shell
shade.data.materials.append(ink)
cylinder('LampFrostedBulb',(.35,-2.61,2.57),.035,.075,enamel)
cylinder('InteriorConduit',(-2.53,2.06,1.50),.009,2.55,iron)
box('OldBakeliteSwitch',(-2.53,2.06,1.2),(.085,.045,.12),iron)

# Door clearance and the offset of a surface-mounted butt hinge.
door.location.z = .22
door.scale.z = 2.02 / 2.07
door['clear_opening_metres'] = 1.205
for obj in list(scene.objects):
    if obj.name.startswith('DoorFrame_Jamb'):
        average = sum(v.co.x for v in obj.data.vertices) / len(obj.data.vertices)
        for vertex in obj.data.vertices:
            vertex.co.x += -.025 if average < 1.4 else .025
    if obj.name.startswith('Door_HingeBarrel'):
        obj.location.z = .22 + (obj.location.z - .18) * door.scale.z
        obj.location.y = -2.425
    if obj.type == 'MESH' and obj.name.startswith(('Door_Lever', 'Door_InnerLever', 'Door_HandleSpindle', 'Door_InnerSpindle', 'MortiseLock_InsetCylinder', 'Downpipe')):
        for polygon in obj.data.polygons:
            if len(polygon.vertices) == 4:
                polygon.use_smooth = True
bpy.context.view_layer.update()
closed_worlds = {obj: obj.matrix_world.copy() for obj in door.children}
door.location.y = -2.425
bpy.context.view_layer.update()
for obj, matrix in closed_worlds.items():
    obj.matrix_world = matrix
for material, base in ((iron, (.13,.115,.086)), (brass, (.29,.225,.12))):
    grain = np.random.default_rng(32).random((512,512))
    rgb = np.array(base)[None,None,:] + (grain[:,:,None]-.5)*.07
    tex = material.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image = save_image(material.name + '_BaseColor', rgb)
    bsdf = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    material.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])

# Presentation uses its own collection, never exported with the building.
def stage_object(obj):
    for col in list(obj.users_collection):
        col.objects.unlink(obj)
    stage.objects.link(obj)

groundmat = plain('Preview_Sand',(.19,.165,.125),.94)
ground = box('Preview_Ground',(0,0,-.095),(200,200,.10),groundmat,parent=None,bevel=0)
stage_object(ground)
world = bpy.data.worlds.new('Guardhouse_PreviewWorld')
world.use_nodes = True
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.40,.46,.52,1)
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.45
scene.world = world
for name, loc, power, size, color in (
    ('WarmSky',(-4,-6,9),1700,7,(1,.88,.72)),
    ('CoolFill',(5,-2,6),1100,6,(.78,.87,1)),
    ('RoofRim',(1,5,8),1400,5,(1,.92,.82))):
    data = bpy.data.lights.new(name,'AREA')
    data.energy, data.shape, data.size, data.color = power,'DISK',size,color
    obj=bpy.data.objects.new(name,data)
    stage.objects.link(obj)
    obj.location=loc
    obj.rotation_euler=(Vector((0,0,1.3))-obj.location).to_track_quat('-Z','Y').to_euler()
camera_data=bpy.data.cameras.new('Guardhouse_PreviewCamera')
camera=bpy.data.objects.new('Guardhouse_PreviewCamera',camera_data)
stage.objects.link(camera)
scene.camera=camera
camera.location=(9,-12,7.0)
camera.rotation_euler=(Vector((0,-.1,1.45))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'
camera.data.ortho_scale=9.2
scene.render.engine='CYCLES'
scene.cycles.samples=48
scene.cycles.use_denoising=True
scene.render.resolution_x=1600
scene.render.resolution_y=1300
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(SOURCE/'Previews/Guardhouse_Hero.png')
scene.view_settings.view_transform='AgX'
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_distance=10
        area.spaces.active.region_3d.view_location=(0,0,1.5)
        area.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
        area.spaces.active.shading.type='MATERIAL'
        area.spaces.active.clip_end=500
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Guardhouse_Rebuilt.blend'))
print('BUILT',len(asset.objects),'objects')
