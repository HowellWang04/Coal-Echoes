"""Build the level one guardhouse desk and chair in an isolated background Blender.

1970s factory office pieces: a single-pedestal desk with one centre drawer, a pedestal
drawer and a cupboard door, and a slat-back wooden chair. Both are painted reddish brown,
worn to bare wood on the edges and dusty on top. Parts are modelled as separate bevelled
blocks, joined per piece, and the procedural paint is baked into one texture atlas each.
"""
import bpy
import bmesh
import json
import math
from pathlib import Path
import numpy as np
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
for folder in ['Blender', 'Exports', 'Textures', 'Previews']:
    (ROOT / folder).mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.name = 'GuardhouseFurniture_Authoring'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
studio = bpy.data.collections.new('PreviewStudio')
scene.collection.children.link(studio)
BAKE_SIZE = 2048


# ---------------------------------------------------------------- bake materials
def nodes_of(mat):
    return mat.node_tree.nodes, mat.node_tree.links

def math_node(nodes, links, op, a, b=None, clamp=False):
    n = nodes.new('ShaderNodeMath'); n.operation = op; n.use_clamp = clamp
    for i, v in enumerate((a, b)):
        if v is None:
            continue
        if isinstance(v, (int, float)):
            n.inputs[i].default_value = v
        else:
            links.new(v, n.inputs[i])
    return n.outputs[0]

def mix_color(nodes, links, fac, a, b):
    n = nodes.new('ShaderNodeMix'); n.data_type = 'RGBA'; n.clamp_factor = True
    color_a, color_b = [s for s in n.inputs if s.type == 'RGBA']
    for sock, v in ((n.inputs['Factor'], fac), (color_a, a), (color_b, b)):
        if isinstance(v, (int, float)):
            sock.default_value = v
        elif isinstance(v, tuple):
            sock.default_value = (*v, 1)
        else:
            links.new(v, sock)
    return next(s for s in n.outputs if s.type == 'RGBA')

def noise(nodes, links, vector, scale, detail=4, rough=.55):
    n = nodes.new('ShaderNodeTexNoise')
    n.inputs['Scale'].default_value = scale
    n.inputs['Detail'].default_value = detail
    n.inputs['Roughness'].default_value = rough
    links.new(vector, n.inputs['Vector'])
    return n.outputs['Fac']

def bake_material(name, kind):
    """Procedural source material. Exposes color, roughness, metal and height sockets
    through named reroutes so each bake pass can route one of them into the output."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = nodes_of(mat)
    for n in list(nodes):
        nodes.remove(n)
    out = nodes.new('ShaderNodeOutputMaterial')
    coord = nodes.new('ShaderNodeTexCoord').outputs['Object']
    grain_uv = nodes.new('ShaderNodeUVMap'); grain_uv.uv_map = 'Grain'
    geo = nodes.new('ShaderNodeNewGeometry')
    bevel = nodes.new('ShaderNodeBevel'); bevel.inputs['Radius'].default_value = .014; bevel.samples = 16
    dot = nodes.new('ShaderNodeVectorMath'); dot.operation = 'DOT_PRODUCT'
    links.new(bevel.outputs['Normal'], dot.inputs[0]); links.new(geo.outputs['Normal'], dot.inputs[1])
    edge = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', 1, dot.outputs['Value'], True), 9, True)
    ao = nodes.new('ShaderNodeAmbientOcclusion'); ao.inputs['Distance'].default_value = .06; ao.samples = 16
    ao.only_local = True  # desk and chair are both built at the origin
    sep = nodes.new('ShaderNodeSeparateXYZ'); links.new(geo.outputs['Normal'], sep.inputs[0])
    upward = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', sep.outputs['Z'], .55, True), 2.2, True)
    cloud = noise(nodes, links, coord, 6, 5)
    speck = noise(nodes, links, coord, 70, 3)

    if kind == 'wood':
        # Grain runs along U of the Grain map, which every part lays along its length.
        stretch = nodes.new('ShaderNodeVectorMath'); stretch.operation = 'MULTIPLY'
        stretch.inputs[1].default_value = (3, 90, 1)
        links.new(grain_uv.outputs['UV'], stretch.inputs[0])
        grain = noise(nodes, links, stretch.outputs[0], 1.2, 6, .62)
        rings = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SINE', math_node(nodes, links, 'MULTIPLY', grain, 38)), .5)
        rings = math_node(nodes, links, 'ADD', rings, .5)
        # Paint chips cluster on edges and corners; a few scuffs reach flat faces.
        chip_noise = noise(nodes, links, coord, 22, 8, .78)
        wear = math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', edge, 1.45), math_node(nodes, links, 'MULTIPLY', chip_noise, .8))
        wear = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', wear, .92), 6, True)
        # Hands and sleeves rubbed the varnish thin on the desk top and chair seat.
        rub = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', noise(nodes, links, coord, 2.5, 3), .52), 2.5, True)
        rub = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'MULTIPLY', rub, upward), .55)
        wear = math_node(nodes, links, 'MAXIMUM', wear, rub)
        fade = math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', cloud, .6), math_node(nodes, links, 'MULTIPLY', upward, .3))
        paint = mix_color(nodes, links, fade, (.068, .03, .019), (.13, .072, .048))
        paint = mix_color(nodes, links, math_node(nodes, links, 'MULTIPLY', rings, .12), paint, (.07, .022, .012))
        bare = mix_color(nodes, links, rings, (.30, .19, .10), (.17, .10, .05))
        base = mix_color(nodes, links, wear, paint, bare)
        rough_v = math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', wear, .25), math_node(nodes, links, 'ADD', .62, math_node(nodes, links, 'MULTIPLY', cloud, .14)))
        metal_v = 0.0
        height = math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', rings, .25), math_node(nodes, links, 'MULTIPLY', wear, -.6))
    elif kind == 'iron':
        rust = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', cloud, .7), math_node(nodes, links, 'MULTIPLY', speck, .5)), .62), 3, True)
        base = mix_color(nodes, links, rust, (.03, .027, .024), (.13, .055, .022))
        rough_v = math_node(nodes, links, 'ADD', .55, math_node(nodes, links, 'MULTIPLY', rust, .35))
        metal_v = math_node(nodes, links, 'SUBTRACT', .85, math_node(nodes, links, 'MULTIPLY', rust, .7))
        height = math_node(nodes, links, 'MULTIPLY', rust, .4)
    elif kind == 'brass':
        tarnish = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', math_node(nodes, links, 'ADD', cloud, edge), .1), 1.4, True)
        base = mix_color(nodes, links, tarnish, (.30, .21, .085), (.06, .055, .032))
        rough_v = math_node(nodes, links, 'ADD', .35, math_node(nodes, links, 'MULTIPLY', tarnish, .35))
        metal_v = .9
        height = math_node(nodes, links, 'MULTIPLY', speck, .1)
    else:  # keyhole and door gaps
        base = (.008, .007, .006)
        rough_v, metal_v, height = .9, 0.0, 0.0

    # Dust settles on upward faces, grime gathers in corners. Hardware stays cleaner.
    dust_amount = .8 if kind == 'wood' else .4
    settle = math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', upward, .85), .12)
    drift = math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', noise(nodes, links, coord, 3.5, 6, .65), .65), math_node(nodes, links, 'MULTIPLY', speck, .5))
    drift = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', drift, .38), 1.8, True)
    dust = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'MULTIPLY', settle, drift), dust_amount, True)
    base = mix_color(nodes, links, dust, base, (.25, .21, .16))
    grime = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', 1, ao.outputs['AO']), .75, True)
    base = mix_color(nodes, links, grime, base, (.018, .014, .01))
    rough_v = math_node(nodes, links, 'ADD', rough_v, math_node(nodes, links, 'MULTIPLY', dust, .35), True)
    metal_v = math_node(nodes, links, 'MULTIPLY', metal_v, math_node(nodes, links, 'SUBTRACT', 1, dust), True)

    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    links.new(base, bsdf.inputs['Base Color'])
    links.new(rough_v, bsdf.inputs['Roughness']); links.new(metal_v, bsdf.inputs['Metallic'])
    bump = nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = .35; bump.inputs['Distance'].default_value = .0015
    if isinstance(height, float):
        bump.inputs['Height'].default_value = height
    else:
        links.new(height, bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    emit = nodes.new('ShaderNodeEmission'); emit.inputs['Strength'].default_value = 1
    target = nodes.new('ShaderNodeTexImage'); target.name = 'BakeTarget'
    mat_sockets[name] = {'base': base, 'rough': rough_v, 'metal': metal_v, 'bsdf': bsdf, 'emit': emit, 'out': out, 'target': target}
    links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return mat

mat_sockets = {}
SOURCE = {k: bake_material('Bake_' + k, k) for k in ('wood', 'iron', 'brass', 'dark')}


# ---------------------------------------------------------------- modelling
class Piece:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.grain = self.bm.loops.layers.uv.new('Grain')

    def _finish(self, before, matrix, kind, bevel):
        faces = [f for f in self.bm.faces if f not in before]
        if bevel:
            edges = list({e for f in faces for e in f.edges})
            bmesh.ops.bevel(self.bm, geom=edges, offset=bevel, segments=2, profile=.5, affect='EDGES', clamp_overlap=True)
            faces = [f for f in self.bm.faces if f not in before]
        bmesh.ops.transform(self.bm, matrix=matrix, verts=list({v for f in faces for v in f.verts}))
        index = list(SOURCE).index(kind)
        for f in faces:
            f.material_index = index

    def box(self, size, center, kind='wood', rot_x=0.0, bevel=.003):
        """Axis box built at the origin. Grain U runs along the longest side."""
        sx, sy, sz = size
        before = set(self.bm.faces)
        res = bmesh.ops.create_cube(self.bm, size=1)
        verts = res['verts']
        for v in verts:
            v.co = Vector((v.co.x * sx, v.co.y * sy, v.co.z * sz))
        faces = list({f for v in verts for f in v.link_faces})
        long_axis = max(range(3), key=lambda i: size[i])
        for f in faces:
            n = f.normal
            drop = max(range(3), key=lambda i: abs(n[i]))
            axes = [i for i in range(3) if i != drop]
            if long_axis in axes:
                axes.sort(key=lambda i: i != long_axis)
            for loop in f.loops:
                loop[self.grain].uv = (loop.vert.co[axes[0]], loop.vert.co[axes[1]])
        self._finish(before, Matrix.Translation(center) @ Matrix.Rotation(rot_x, 4, 'X'), kind, bevel)

    def profile_x(self, outline_yz, x0, width, kind='wood', bevel=.003):
        """Prism from a closed outline in the YZ plane, extruded along +X. Grain U follows Z."""
        before = set(self.bm.faces)
        bottom = [self.bm.verts.new((x0, y, z)) for y, z in outline_yz]
        top = [self.bm.verts.new((x0 + width, y, z)) for y, z in outline_yz]
        faces = [self.bm.faces.new(list(reversed(bottom))), self.bm.faces.new(top)]
        n = len(outline_yz)
        for i in range(n):
            j = (i + 1) % n
            faces.append(self.bm.faces.new((bottom[i], bottom[j], top[j], top[i])))
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        for f in faces:
            side = abs(f.normal.x) > .5
            for loop in f.loops:
                co = loop.vert.co
                loop[self.grain].uv = (co.z, co.y) if side else (co.z, co.x + co.y)
        self._finish(before, Matrix.Identity(4), kind, bevel)

    def cylinder_y(self, radius, depth, center, kind, segments=20):
        before = set(self.bm.faces)
        res = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=segments, radius1=radius, radius2=radius, depth=depth,
                                    matrix=Matrix.Translation(center) @ Matrix.Rotation(math.pi / 2, 4, 'X'))
        faces = list({f for v in res['verts'] for f in v.link_faces})
        for f in faces:
            for loop in f.loops:
                loop[self.grain].uv = (loop.vert.co.x, loop.vert.co.z)
        self._finish(before, Matrix.Identity(4), kind, 0)

    def build(self):
        me = bpy.data.meshes.new(self.name + '_Mesh')
        self.bm.normal_update()
        self.bm.to_mesh(me)
        self.bm.free()
        for mat in SOURCE.values():
            me.materials.append(mat)
        ob = bpy.data.objects.new(self.name, me)
        col = bpy.data.collections.new(self.name)
        scene.collection.children.link(col)
        col.objects.link(ob)
        return ob


def pull(p, x, z, y_face):
    """Iron bar pull: two standoffs and a grip bar."""
    for dx in (-.035, .035):
        p.box((.012, .02, .012), (x + dx, y_face - .01, z), 'iron', bevel=.002)
    p.box((.09, .01, .012), (x, y_face - .024, z), 'iron', bevel=.003)

def keyhole(p, x, z, y_face):
    p.box((.026, .003, .036), (x, y_face - .0015, z), 'brass', bevel=.001)
    p.cylinder_y(.0035, .002, (x, y_face - .0035, z + .004), 'dark', 12)
    p.box((.003, .002, .009), (x, y_face - .0035, z - .002), 'dark', bevel=0)


# Desk: 1.20 x 0.60 x 0.78 m. Front (drawer side) faces -Y, pedestal on +X, origin floor centre.
desk = Piece('GuardDesk')
W, D, H, TOP = 1.20, .60, .78, .025
LEG = .045
desk.box((W, D, TOP), (0, 0, H - TOP / 2), bevel=.006)
LX, LY = W / 2 - .035, D / 2 - .035
PX = .19  # inner pedestal post
for x in (-LX, PX, LX):
    for y in (-LY, LY):
        desk.box((LEG, LEG, H - TOP), (x, y, (H - TOP) / 2))
# Aprons and stretchers on the open side.
desk.box((.02, 2 * LY - LEG, .135), (-LX, 0, .6875))
desk.box((.025, 2 * LY - LEG, .035), (-LX, 0, .10))
desk.box((W - .07 - LEG, .02, .205), (0, LY + .005, .6525))
# Pedestal carcass.
for x in (PX, LX):
    desk.box((.016, 2 * LY - LEG, H - TOP - .05), (x, 0, .05 + (H - TOP - .05) / 2), bevel=.002)
desk.box((LX - PX - LEG, .014, H - TOP - .05), ((LX + PX) / 2, LY + .008, .05 + (H - TOP - .05) / 2), bevel=.002)
desk.box((LX - PX - LEG, 2 * LY - LEG, .02), ((LX + PX) / 2, 0, .07), bevel=.002)
FRONT = -LY - LEG / 2  # front plane of the legs
inner_w = LX - PX - LEG
px_mid = (LX + PX) / 2
desk.box((inner_w, .02, .055), (px_mid, FRONT + .012, .0675), bevel=.002)            # kick rail
desk.box((inner_w, .02, .02), (px_mid, FRONT + .012, .595), bevel=.002)              # divider rail
desk.box((inner_w, .02, .02), (px_mid, FRONT + .012, H - TOP - .01), bevel=.002)     # top rail
# Centre opening frame.
cx0, cx1 = -LX + LEG / 2, PX - LEG / 2
desk.box((cx1 - cx0, .02, .02), ((cx0 + cx1) / 2, FRONT + .012, H - TOP - .01), bevel=.002)
desk.box((cx1 - cx0, .02, .022), ((cx0 + cx1) / 2, FRONT + .012, .634), bevel=.002)
# Dark backing so the 3 mm reveals around drawers and the door read as gaps.
desk.box((cx1 - cx0, .004, .09), ((cx0 + cx1) / 2, FRONT + .014, .69), 'dark', bevel=0)
desk.box((inner_w, .004, .64), (px_mid, FRONT + .014, .415), 'dark', bevel=0)
# Drawer fronts and the cupboard door, slightly proud of the frame.
SLAB = .02
yf = FRONT - .003 + SLAB / 2
desk.box((cx1 - cx0 - .006, SLAB, .085), ((cx0 + cx1) / 2, yf, .6895), bevel=.004)
desk.box((inner_w - .006, SLAB, .125), (px_mid, yf, .6675), bevel=.004)
desk.box((inner_w - .006, SLAB, .485), (px_mid, yf, .3425), bevel=.004)
# Raised moulding on the door.
door_face = yf - SLAB / 2
for z in (.13, .555):
    desk.box((inner_w - .07, .006, .014), (px_mid, door_face - .003, z), bevel=.002)
for x in (PX + LEG / 2 + .035, LX - LEG / 2 - .035):
    desk.box((.014, .006, .411), (x, door_face - .003, .3425), bevel=.002)
pull(desk, (cx0 + cx1) / 2 - .12, .6895, door_face)
keyhole(desk, (cx0 + cx1) / 2 + .06, .6895, door_face)
pull(desk, px_mid, .6675, door_face)
desk.box((.018, .018, .03), (LX - LEG / 2 - .055, door_face - .009, .40), 'iron', bevel=.004)   # door knob
keyhole(desk, LX - LEG / 2 - .055, .46, door_face)
desk_ob = desk.build()

# Chair: seat 0.42 x 0.40 at 0.45 m, back legs lean back 70 mm at the top rail. Front faces -Y.
chair = Piece('GuardChair')
SW, SD, SH, ST = .42, .40, .45, .022
CL = .035
fx, fy = SW / 2 - .0275, -SD / 2 + .0275
by = SD / 2 - .0275
LEAN, TOPZ, KNEE = .07, .90, SH - ST
for x in (-fx, fx):
    chair.box((CL, CL, KNEE), (x, fy, KNEE / 2))
    y0 = by - CL / 2
    chair.profile_x([(y0, 0), (y0 + CL, 0), (y0 + CL, KNEE), (y0 + CL + LEAN, TOPZ), (y0 + LEAN, TOPZ), (y0, KNEE)], x - CL / 2, CL)
chair.box((SW, SD, ST), (0, 0, SH - ST / 2), bevel=.005)
for y in (fy, by):
    chair.box((2 * fx - CL, .018, .065), (0, y, KNEE - .0325))
for x in (-fx, fx):
    chair.box((.018, by - fy - CL, .065), (x, (fy + by) / 2, KNEE - .0325))
    chair.box((.02, by - fy - CL, .022), (x, (fy + by) / 2, .16))
chair.box((2 * fx - CL, .02, .022), (0, (fy + by) / 2, .16))
chair.box((2 * fx - CL, .02, .022), (0, fy, .21))
tilt = math.atan2(LEAN, TOPZ - KNEE)
for z, h in ((.64, .045), (.835, .075)):
    y = by + (z - KNEE) / (TOPZ - KNEE) * LEAN
    chair.box((2 * fx - CL, .02, h), (0, y, z), rot_x=-tilt, bevel=.004)
chair_ob = chair.build()


# ---------------------------------------------------------------- UVs and bake
scene.render.engine = 'CYCLES'
try:
    prefs = bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type = 'OPTIX'; prefs.get_devices()
    for d in prefs.devices:
        d.use = d.type == 'OPTIX'
    scene.cycles.device = 'GPU'
except TypeError:
    scene.cycles.device = 'CPU'
scene.render.bake.margin = 12
scene.render.bake.use_clear = True

def atlas(ob):
    uv = ob.data.uv_layers.new(name='UVMap')
    ob.data.uv_layers.active = uv
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=.004, scale_to_bounds=False)
    bpy.ops.uv.pack_islands(margin=.004, rotate=True)
    bpy.ops.object.mode_set(mode='OBJECT')

def bake_pass(ob, prefix, pass_name, colorspace):
    image = bpy.data.images.new(f'{prefix}_{pass_name}', BAKE_SIZE, BAKE_SIZE, alpha=False)
    image.colorspace_settings.name = colorspace
    for mat in SOURCE.values():
        s = mat_sockets[mat.name]
        nodes, links = nodes_of(mat)
        s['target'].image = image
        nodes.active = s['target']
        if pass_name == 'Normal':
            links.new(s['bsdf'].outputs['BSDF'], s['out'].inputs['Surface'])
        else:
            value = {'BaseColor': s['base'], 'Roughness': s['rough'], 'Metallic': s['metal']}[pass_name]
            links.new(value, s['emit'].inputs['Color'])
            links.new(s['emit'].outputs['Emission'], s['out'].inputs['Surface'])
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    if pass_name == 'Normal':
        scene.cycles.samples = 16
        bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', uv_layer='UVMap')
    else:
        scene.cycles.samples = 32 if pass_name == 'BaseColor' else 8
        bpy.ops.object.bake(type='EMIT', uv_layer='UVMap')
    image.filepath_raw = str(ROOT / 'Textures' / f'{prefix}_{pass_name}.png')
    image.file_format = 'PNG'
    image.save()
    return image

def finish(ob):
    prefix = ob.name
    atlas(ob)
    maps = {p: bake_pass(ob, prefix, p, 'sRGB' if p == 'BaseColor' else 'Non-Color') for p in ('BaseColor', 'Roughness', 'Metallic', 'Normal')}
    # HDRP mask: R metallic, G AO (white), B detail (white), A smoothness.
    rough = np.empty(BAKE_SIZE * BAKE_SIZE * 4, np.float32); maps['Roughness'].pixels.foreach_get(rough)
    metal = np.empty_like(rough); maps['Metallic'].pixels.foreach_get(metal)
    mask = bpy.data.images.new(prefix + '_HDRP_Mask', BAKE_SIZE, BAKE_SIZE, alpha=True)
    mask.colorspace_settings.name = 'Non-Color'
    px = np.ones_like(rough)
    px[0::4] = metal[0::4]; px[3::4] = 1 - rough[0::4]
    mask.pixels.foreach_set(px)
    mask.filepath_raw = str(ROOT / 'Textures' / f'{prefix}_HDRP_Mask.png'); mask.file_format = 'PNG'; mask.save()
    # Final single material from the baked maps; the bake sources are dropped.
    final = bpy.data.materials.new(prefix + '_PaintedWood')
    final.use_nodes = True
    nodes, links = nodes_of(final)
    bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
    for p, socket in (('BaseColor', 'Base Color'), ('Roughness', 'Roughness'), ('Metallic', 'Metallic')):
        t = nodes.new('ShaderNodeTexImage'); t.image = maps[p]; links.new(t.outputs['Color'], bsdf.inputs[socket])
    t = nodes.new('ShaderNodeTexImage'); t.image = maps['Normal']
    nm = nodes.new('ShaderNodeNormalMap'); links.new(t.outputs['Color'], nm.inputs['Color']); links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
    ob.data.materials.clear(); ob.data.materials.append(final)
    for poly in ob.data.polygons:
        poly.material_index = 0
    ob.data.uv_layers.remove(ob.data.uv_layers['Grain'])
    bm = bmesh.new(); bm.from_mesh(ob.data); bmesh.ops.triangulate(bm, faces=list(bm.faces)); bm.to_mesh(ob.data); bm.free()
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))

for ob in (desk_ob, chair_ob):
    finish(ob)
for mat in SOURCE.values():
    bpy.data.materials.remove(mat)


# ---------------------------------------------------------------- checks and export
def inspect(ob):
    bpy.context.view_layer.update()
    bounds = [ob.matrix_world @ Vector(c) for c in ob.bound_box]
    ob.data.calc_loop_triangles()
    bm = bmesh.new(); bm.from_mesh(ob.data)
    info = {'name': ob.name, 'dimensions_xyz_m': [round(max(v[i] for v in bounds) - min(v[i] for v in bounds), 4) for i in range(3)],
            'min_z_m': round(min(v.z for v in bounds), 5), 'triangles': len(ob.data.loop_triangles),
            'nonmanifold_edges': sum(not e.is_manifold for e in bm.edges), 'zero_area_faces': sum(f.calc_area() < 1e-12 for f in bm.faces),
            'uv_layers': [u.name for u in ob.data.uv_layers]}
    bm.free()
    return info

report = {'assets': [inspect(desk_ob), inspect(chair_ob)], 'blender_version': bpy.app.version_string, 'texture_size': BAKE_SIZE,
          'method': 'Independent background Blender process, procedural paint baked in Cycles', 'unity_verified': False}
for a in report['assets']:
    assert a['zero_area_faces'] == 0 and a['uv_layers'] == ['UVMap'] and abs(a['min_z_m']) < 1e-4, a
for ob in (desk_ob, chair_ob):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.fbx(filepath=str(ROOT / f'Exports/{ob.name}.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             use_tspace=True, mesh_smooth_type='FACE', bake_anim=False, path_mode='COPY', embed_textures=True)
    bpy.ops.export_scene.gltf(filepath=str(ROOT / f'Exports/{ob.name}.glb'), export_format='GLB', use_selection=True, export_apply=True,
                              export_yup=True, export_normals=True, export_tangents=True)
for im in bpy.data.images:
    if im.source in {'FILE', 'GENERATED'} and im.filepath_raw:
        im.pack()
(ROOT / 'verification.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('ASSET_SAVED', json.dumps(report), flush=True)


# ---------------------------------------------------------------- previews
def link(ob, col):
    for c in list(ob.users_collection):
        c.objects.unlink(ob)
    col.objects.link(ob)
floor_mat = bpy.data.materials.new('Studio_Concrete'); floor_mat.use_nodes = True
next(n for n in floor_mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Base Color'].default_value = (.22, .21, .19, 1)
bpy.ops.mesh.primitive_plane_add(size=10, location=(0, 0, 0))
floor = bpy.context.object; floor.name = 'StudioFloor'; link(floor, studio); floor.data.materials.append(floor_mat)
bpy.ops.mesh.primitive_plane_add(size=10, location=(0, .34, 2), rotation=(math.pi / 2, 0, 0))
wall = bpy.context.object; wall.name = 'StudioWall'; link(wall, studio); wall.data.materials.append(floor_mat)
world = bpy.data.worlds.new('SoftStudio'); world.use_nodes = True; scene.world = world
bg = next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND')
bg.inputs['Color'].default_value = (.55, .6, .66, 1); bg.inputs['Strength'].default_value = .3
for name, loc, energy, size in [('Key', (-1.6, -2.0, 2.4), 260, 1.4), ('Fill', (2.0, -1.2, 1.2), 60, 1.2), ('Rim', (1.2, .2, 2.4), 80, .8)]:
    d = bpy.data.lights.new(name, 'AREA'); d.energy = energy; d.shape = 'DISK'; d.size = size
    lo = bpy.data.objects.new(name, d); studio.objects.link(lo); lo.location = loc
    lo.rotation_euler = (Vector((0, 0, .4)) - lo.location).to_track_quat('-Z', 'Y').to_euler()
camdata = bpy.data.cameras.new('PreviewCamera'); cam = bpy.data.objects.new('PreviewCamera', camdata); studio.objects.link(cam); scene.camera = cam
camdata.lens = 40
def aim(location, target):
    cam.location = location; cam.rotation_euler = (Vector(target) - Vector(location)).to_track_quat('-Z', 'Y').to_euler()
scene.cycles.samples = 128; scene.cycles.use_denoising = True
scene.render.resolution_x = 1400; scene.render.resolution_y = 1000
scene.render.image_settings.file_format = 'PNG'
# Lay out both pieces as they will sit in the guardhouse: chair pulled out and turned.
chair_ob.location = (-.15, -.62, 0); chair_ob.rotation_euler = (0, 0, math.radians(-16))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Blender/GuardhouseFurniture.blend'), compress=True)
for name, loc, target in [('Overall', (-1.7, -2.6, 1.55), (0, -.25, .45)), ('DeskFront', (.35, -1.45, .9), (.3, 0, .45)), ('Chair', (-1.0, -1.7, .85), (-.15, -.62, .45))]:
    aim(loc, target)
    scene.render.filepath = str(ROOT / f'Previews/GuardhouseFurniture_{name}.png')
    bpy.ops.render.render(write_still=True)
chair_ob.location = (0, 0, 0); chair_ob.rotation_euler = (0, 0, 0)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Blender/GuardhouseFurniture.blend'), compress=True)
print('BUILD_COMPLETE', flush=True)
