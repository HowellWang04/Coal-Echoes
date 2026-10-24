"""Build the broken army green desk lamp for the level one guardhouse desk in an isolated background Blender.

An old enamelled steel lamp: weighted round base with a toggle switch, a slightly bent stem,
a knuckle joint and a short arm. The hinge has given way, so the bell shade sags down and to one
side, dented on one flank, with a broken bulb stub in the socket and a cloth cord trailing off.
Paint chips to dark steel and rust on edges, the cream enamel inside is stained, dust on top.
Parts are joined into one mesh and the procedural surfaces are baked into one 1024 atlas.
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
scene.name = 'DeskLamp_Authoring'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
studio = bpy.data.collections.new('PreviewStudio')
scene.collection.children.link(studio)
BAKE_SIZE = 1024
KINDS = ('paint', 'enamel', 'iron', 'glass', 'cord')


# ---------------------------------------------------------------- bake materials
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

mat_sockets = {}

def bake_material(kind):
    mat = bpy.data.materials.new('Bake_' + kind)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    for n in list(nodes):
        nodes.remove(n)
    out = nodes.new('ShaderNodeOutputMaterial')
    coord = nodes.new('ShaderNodeTexCoord').outputs['Object']
    geo = nodes.new('ShaderNodeNewGeometry')
    bevel = nodes.new('ShaderNodeBevel'); bevel.inputs['Radius'].default_value = .0025; bevel.samples = 16
    dot = nodes.new('ShaderNodeVectorMath'); dot.operation = 'DOT_PRODUCT'
    links.new(bevel.outputs['Normal'], dot.inputs[0]); links.new(geo.outputs['Normal'], dot.inputs[1])
    edge = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', 1, dot.outputs['Value'], True), 10, True)
    ao = nodes.new('ShaderNodeAmbientOcclusion'); ao.inputs['Distance'].default_value = .03; ao.samples = 16; ao.only_local = True
    sep = nodes.new('ShaderNodeSeparateXYZ'); links.new(geo.outputs['Normal'], sep.inputs[0])
    upward = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', sep.outputs['Z'], .4, True), 1.8, True)
    cloud = noise(nodes, links, coord, 14, 5)
    speck = noise(nodes, links, coord, 160, 3)
    rust_n = noise(nodes, links, coord, 45, 8, .75)

    if kind == 'paint':
        # Army green enamel, chipped on edges to dark steel, rust blooming in the chips.
        chips = math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', edge, 1.4), math_node(nodes, links, 'MULTIPLY', rust_n, 1.0))
        chips = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', chips, .86), 6, True)
        rust = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', cloud, .35), 2.5, True)
        paint = mix_color(nodes, links, math_node(nodes, links, 'MULTIPLY', cloud, .8), (.04, .055, .016), (.085, .10, .032))
        bare = mix_color(nodes, links, rust, (.035, .032, .028), (.16, .065, .025))
        base = mix_color(nodes, links, chips, paint, bare)
        rough_v = math_node(nodes, links, 'ADD', .5, math_node(nodes, links, 'MULTIPLY', chips, math_node(nodes, links, 'ADD', .1, math_node(nodes, links, 'MULTIPLY', rust, .3))))
        metal_v = math_node(nodes, links, 'MULTIPLY', chips, math_node(nodes, links, 'SUBTRACT', .8, math_node(nodes, links, 'MULTIPLY', rust, .7)))
        height = math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', chips, -.6), math_node(nodes, links, 'MULTIPLY', speck, .08))
    elif kind == 'enamel':
        stain = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', math_node(nodes, links, 'ADD', cloud, math_node(nodes, links, 'MULTIPLY', speck, .4)), .45), 2.2, True)
        base = mix_color(nodes, links, stain, (.26, .235, .165), (.10, .07, .035))
        rough_v = math_node(nodes, links, 'ADD', .35, math_node(nodes, links, 'MULTIPLY', stain, .4))
        metal_v = 0.0
        height = math_node(nodes, links, 'MULTIPLY', stain, .2)
    elif kind == 'iron':
        rust = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', cloud, .7), math_node(nodes, links, 'MULTIPLY', rust_n, .5)), .6), 3, True)
        base = mix_color(nodes, links, rust, (.09, .085, .075), (.15, .062, .024))
        rough_v = math_node(nodes, links, 'ADD', .45, math_node(nodes, links, 'MULTIPLY', rust, .45))
        metal_v = math_node(nodes, links, 'SUBTRACT', .9, math_node(nodes, links, 'MULTIPLY', rust, .75))
        height = math_node(nodes, links, 'MULTIPLY', rust, .4)
    elif kind == 'glass':
        base = mix_color(nodes, links, cloud, (.10, .10, .09), (.22, .21, .18))
        rough_v = math_node(nodes, links, 'ADD', .08, math_node(nodes, links, 'MULTIPLY', cloud, .3))
        metal_v = 0.0
        height = 0.0
    else:  # braided cloth cord
        weave = noise(nodes, links, coord, 900, 2)
        base = mix_color(nodes, links, weave, (.025, .02, .016), (.07, .055, .04))
        rough_v = .9
        metal_v = 0.0
        height = math_node(nodes, links, 'MULTIPLY', weave, .3)

    dust_amount = .5 if kind in ('paint', 'iron') else .3
    drift = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', math_node(nodes, links, 'ADD', math_node(nodes, links, 'MULTIPLY', cloud, .6), math_node(nodes, links, 'MULTIPLY', speck, .5)), .35), 1.8, True)
    dust = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'MULTIPLY', upward, drift), dust_amount, True)
    base = mix_color(nodes, links, dust, base, (.25, .21, .16))
    grime = math_node(nodes, links, 'MULTIPLY', math_node(nodes, links, 'SUBTRACT', 1, ao.outputs['AO']), .7, True)
    base = mix_color(nodes, links, grime, base, (.015, .012, .009))
    rough_v = math_node(nodes, links, 'ADD', rough_v, math_node(nodes, links, 'MULTIPLY', dust, .4), True)
    metal_v = math_node(nodes, links, 'MULTIPLY', metal_v, math_node(nodes, links, 'SUBTRACT', 1, dust), True)

    bsdf = nodes.new('ShaderNodeBsdfPrincipled')
    links.new(base, bsdf.inputs['Base Color'])
    links.new(rough_v, bsdf.inputs['Roughness']); links.new(metal_v, bsdf.inputs['Metallic'])
    bump = nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = .3; bump.inputs['Distance'].default_value = .0006
    if isinstance(height, float):
        bump.inputs['Height'].default_value = height
    else:
        links.new(height, bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    emit = nodes.new('ShaderNodeEmission')
    target = nodes.new('ShaderNodeTexImage')
    mat_sockets[mat.name] = {'base': base, 'rough': rough_v, 'metal': metal_v, 'bsdf': bsdf, 'emit': emit, 'out': out, 'target': target}
    return mat

SOURCE = {k: bake_material(k) for k in KINDS}


# ---------------------------------------------------------------- modelling
bm = bmesh.new()

def tag(faces, kind):
    for f in faces:
        f.material_index = KINDS.index(kind)

def lathe(profile, kinds, matrix, segments=40):
    """Revolve a closed (radius, height) outline about the local Z axis.
    kinds[i] is the material of the band from point i to i+1; radius 0 points close to a pole."""
    rings = []
    for r, z in profile:
        if r < 1e-6:
            rings.append([bm.verts.new(matrix @ Vector((0, 0, z)))])
        else:
            rings.append([bm.verts.new(matrix @ Vector((r * math.cos(a), r * math.sin(a), z)))
                          for a in (math.tau * i / segments for i in range(segments))])
    faces = []
    n = len(profile)
    for i in range(n - 1):
        a, b = rings[i], rings[i + 1]
        band = []
        for j in range(segments):
            jn = (j + 1) % segments
            if len(a) == 1:
                band.append(bm.faces.new((a[0], b[j], b[jn])))
            elif len(b) == 1:
                band.append(bm.faces.new((a[j], b[0], a[jn])))
            else:
                band.append(bm.faces.new((a[j], b[j], b[jn], a[jn])))
        tag(band, kinds[i])
        faces += band
    return faces, rings

def tube(points, radius, kind, ring=12, caps=True):
    pts = [Vector(p) for p in points]
    verts = []
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        side = t.cross(Vector((0, 0, 1)) if abs(t.z) < .9 else Vector((1, 0, 0))).normalized()
        up = side.cross(t)
        r = radius(i / (len(pts) - 1)) if callable(radius) else radius
        verts.append([bm.verts.new(p + (side * math.cos(a) + up * math.sin(a)) * r) for a in (math.tau * k / ring for k in range(ring))])
    faces = []
    for a, b in zip(verts, verts[1:]):
        for k in range(ring):
            kn = (k + 1) % ring
            faces.append(bm.faces.new((a[k], a[kn], b[kn], b[k])))
    if caps:
        faces.append(bm.faces.new(list(reversed(verts[0]))))
        faces.append(bm.faces.new(verts[-1]))
    tag(faces, kind)
    return faces

def block(size, matrix, kind, bevel=.0015):
    before = set(bm.faces)
    bmesh.ops.create_cube(bm, size=1, matrix=matrix @ Matrix.Diagonal((*size, 1)))
    if bevel:
        faces = [f for f in bm.faces if f not in before]
        bmesh.ops.bevel(bm, geom=list({e for f in faces for e in f.edges}), offset=bevel, segments=2, affect='EDGES', clamp_overlap=True)
    tag([f for f in bm.faces if f not in before], kind)

# Weighted base, drawn as a solid of revolution.
base_profile = [(0, 0), (.088, 0), (.0905, .004), (.0905, .009), (.087, .015), (.074, .028), (.05, .034), (.022, .038), (.015, .041), (0, .041)]
lathe(base_profile, ['paint'] * 9, Matrix.Identity(4), 56)
# Felt-less underside ring and the toggle switch at the front.
lathe([(0, -.001), (.06, -.001), (.06, .0005), (0, .0005)], ['iron'] * 3, Matrix.Identity(4), 40)
block((.018, .026, .010), Matrix.Translation((0, -.06, .027)) @ Matrix.Rotation(math.radians(-16), 4, 'X'), 'iron')
block((.006, .010, .016), Matrix.Translation((0, -.062, .038)) @ Matrix.Rotation(math.radians(-30), 4, 'X'), 'iron')
# Stem, bent slightly where someone once knocked the lamp over.
JOINT = Vector((.028, .026, .295))
tube([(0, .012, .035), (0, .012, .09), (.002, .014, .15), (.010, .019, .21), (.020, .024, .26), JOINT], .0075, 'paint', 14)
lathe([(0, 0), (.016, 0), (.016, .006), (.0085, .012), (0, .012)], ['paint'] * 4, Matrix.Translation((0, .012, .034)), 28)  # stem collar
# Knuckle joint with a wing nut, then the short arm running forward.
knuckle = Matrix.Translation(JOINT) @ Matrix.Rotation(math.pi / 2, 4, 'Y')
lathe([(0, -.017), (.012, -.017), (.013, -.012), (.013, .012), (.012, .017), (0, .017)], ['iron'] * 5, knuckle, 24)
block((.006, .03, .02), Matrix.Translation(JOINT + Vector((.022, 0, 0))), 'iron', .002)
ARM_END = JOINT + Vector((.0, -.13, .035))
tube([JOINT, JOINT + Vector((0, -.06, .022)), ARM_END], .0065, 'paint', 14)

# The shade hinge has snapped: the bare socket hangs from the arm end with the jagged stub of a
# broken bulb, and the shade lies on its side on the desk beside the base, dented on its upper flank.
socket_m = Matrix.Translation(ARM_END) @ Vector((.05, -.12, -1)).normalized().to_track_quat('Z', 'Y').to_matrix().to_4x4()
lathe([(0, -.012), (.011, -.012), (.012, -.004), (.011, .004), (0, .004)], ['iron'] * 4, socket_m, 20)
lathe([(0, .0), (.015, .0), (.016, .03), (.013, .036), (0, .036)], ['iron'] * 4, socket_m, 24)
rng = np.random.default_rng(7)
faces, rings = lathe([(0, .034), (.019, .034), (.021, .042), (.0185, .046), (0, .046)], ['glass'] * 4, socket_m, 24)
for v in rings[3]:
    v.co += socket_m.to_3x3() @ Vector((0, 0, float(rng.uniform(-.004, .012))))
# Local +Z of the shade runs from the hinge end to the mouth; lying down, the axis rises
# toward the mouth so the outer flank rests on the desk.
heading, rise = math.radians(-35), math.radians(24)
axis = Vector((math.cos(heading) * math.cos(rise), math.sin(heading) * math.cos(rise), math.sin(rise)))
shade_m = Matrix.Translation((.10, -.20, 0)) @ axis.to_track_quat('Z', 'Y').to_matrix().to_4x4()
outer = [(0, -.004), (.021, -.004), (.026, .016), (.034, .046), (.052, .084), (.074, .114), (.085, .124), (.087, .129)]
inner = [(.083, .129), (.080, .124), (.070, .114), (.049, .085), (.031, .048), (.022, .018), (.017, .0), (0, .0)]
shade_kinds = ['paint'] * 7 + ['paint'] + ['enamel'] * 7
shade_faces, shade_rings = lathe(outer + inner, shade_kinds, shade_m, 48)
lathe([(0, -.012), (.011, -.012), (.012, -.004), (.011, .004), (0, .004)], ['iron'] * 4, shade_m, 20)  # torn-off hinge collar
dent_centre = shade_m @ Vector((-.015, .055, .085))
dent_normal = (shade_m.to_3x3() @ Vector((-.25, 1, 0))).normalized()
shade_verts = {v for ring in shade_rings for v in ring}
for v in shade_verts:
    d = (v.co - dent_centre).length
    v.co -= dent_normal * .018 * math.exp(-(d / .03) ** 2)
collar_verts = {v for f in bm.faces for v in f.verts if (v.co - shade_m.translation).length < .02} - shade_verts
lift = -min(v.co.z for v in shade_verts)
for v in shade_verts | collar_verts:
    v.co.z += lift
# Cloth cord leaving the back of the base, slack across the desk top, end frayed.
def catmull(points, steps=8):
    p = [Vector(x) for x in points]
    p = [p[0]] + p + [p[-1]]
    out = []
    for i in range(1, len(p) - 2):
        for k in range(steps):
            t = k / steps
            a, b, c, d = p[i - 1], p[i], p[i + 1], p[i + 2]
            out.append(.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - a - 3 * c + d) * t ** 3))
    return out + [p[-2]]
cord = [(0, .086, .012), (0, .10, .005), (.01, .13, .0028), (.045, .175, .0028), (.10, .205, .0028), (.17, .215, .0028), (.24, .195, .0028), (.31, .225, .0028)]
tube(catmull(cord), lambda t: .0028 if t < .97 else .0034, 'cord', 10)

bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
me = bpy.data.meshes.new('DeskLamp_Mesh')
bm.normal_update(); bm.to_mesh(me); bm.free()
for mat in SOURCE.values():
    me.materials.append(mat)
lamp = bpy.data.objects.new('DeskLamp', me)
col = bpy.data.collections.new('DeskLamp'); scene.collection.children.link(col); col.objects.link(lamp)


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
scene.render.bake.margin = 8
scene.render.bake.use_clear = True

uv = me.uv_layers.new(name='UVMap'); me.uv_layers.active = uv
bpy.context.view_layer.objects.active = lamp; lamp.select_set(True)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=.004, scale_to_bounds=False)
bpy.ops.uv.pack_islands(margin=.004, rotate=True)
bpy.ops.object.mode_set(mode='OBJECT')

def bake_pass(pass_name, colorspace):
    image = bpy.data.images.new(f'DeskLamp_{pass_name}', BAKE_SIZE, BAKE_SIZE, alpha=False)
    image.colorspace_settings.name = colorspace
    for mat in SOURCE.values():
        s = mat_sockets[mat.name]
        links = mat.node_tree.links
        s['target'].image = image
        mat.node_tree.nodes.active = s['target']
        if pass_name == 'Normal':
            links.new(s['bsdf'].outputs['BSDF'], s['out'].inputs['Surface'])
        else:
            value = {'BaseColor': s['base'], 'Roughness': s['rough'], 'Metallic': s['metal']}[pass_name]
            links.new(value, s['emit'].inputs['Color'])
            links.new(s['emit'].outputs['Emission'], s['out'].inputs['Surface'])
    if pass_name == 'Normal':
        scene.cycles.samples = 16
        bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', uv_layer='UVMap')
    else:
        scene.cycles.samples = 32 if pass_name == 'BaseColor' else 8
        bpy.ops.object.bake(type='EMIT', uv_layer='UVMap')
    image.filepath_raw = str(ROOT / 'Textures' / f'DeskLamp_{pass_name}.png'); image.file_format = 'PNG'; image.save()
    return image

maps = {p: bake_pass(p, 'sRGB' if p == 'BaseColor' else 'Non-Color') for p in ('BaseColor', 'Roughness', 'Metallic', 'Normal')}
rough = np.empty(BAKE_SIZE * BAKE_SIZE * 4, np.float32); maps['Roughness'].pixels.foreach_get(rough)
metal = np.empty_like(rough); maps['Metallic'].pixels.foreach_get(metal)
mask = bpy.data.images.new('DeskLamp_HDRP_Mask', BAKE_SIZE, BAKE_SIZE, alpha=True)
mask.colorspace_settings.name = 'Non-Color'
px = np.ones_like(rough); px[0::4] = metal[0::4]; px[3::4] = 1 - rough[0::4]
mask.pixels.foreach_set(px)
mask.filepath_raw = str(ROOT / 'Textures' / 'DeskLamp_HDRP_Mask.png'); mask.file_format = 'PNG'; mask.save()

final = bpy.data.materials.new('DeskLamp_ArmyGreen')
final.use_nodes = True
nodes, links = final.node_tree.nodes, final.node_tree.links
bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
for p, socket in (('BaseColor', 'Base Color'), ('Roughness', 'Roughness'), ('Metallic', 'Metallic')):
    t = nodes.new('ShaderNodeTexImage'); t.image = maps[p]; links.new(t.outputs['Color'], bsdf.inputs[socket])
t = nodes.new('ShaderNodeTexImage'); t.image = maps['Normal']
nm = nodes.new('ShaderNodeNormalMap'); links.new(t.outputs['Color'], nm.inputs['Color']); links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
me.materials.clear(); me.materials.append(final)
for poly in me.polygons:
    poly.material_index = 0
for mat in SOURCE.values():
    bpy.data.materials.remove(mat)
b2 = bmesh.new(); b2.from_mesh(me); bmesh.ops.triangulate(b2, faces=list(b2.faces)); b2.to_mesh(me); b2.free()
bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))


# ---------------------------------------------------------------- checks and export
bpy.context.view_layer.update()
bounds = [lamp.matrix_world @ Vector(c) for c in lamp.bound_box]
me.calc_loop_triangles()
chk = bmesh.new(); chk.from_mesh(me)
report = {'asset': 'DeskLamp', 'blender_version': bpy.app.version_string, 'texture_size': BAKE_SIZE,
          'dimensions_xyz_m': [round(max(v[i] for v in bounds) - min(v[i] for v in bounds), 4) for i in range(3)],
          'min_z_m': round(min(v.z for v in bounds), 5), 'max_z_m': round(max(v.z for v in bounds), 4),
          'triangles': len(me.loop_triangles), 'zero_area_faces': sum(f.calc_area() < 1e-12 for f in chk.faces),
          'uv_layers': [u.name for u in me.uv_layers], 'unity_verified': False,
          'method': 'Independent background Blender process, procedural surfaces baked in Cycles'}
chk.free()
assert report['zero_area_faces'] == 0 and report['min_z_m'] > -.002, report
bpy.ops.export_scene.fbx(filepath=str(ROOT / 'Exports/DeskLamp.fbx'), use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                         use_tspace=True, mesh_smooth_type='FACE', bake_anim=False, path_mode='COPY', embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(ROOT / 'Exports/DeskLamp.glb'), export_format='GLB', use_selection=True, export_apply=True,
                          export_yup=True, export_normals=True, export_tangents=True)
for im in bpy.data.images:
    if im.filepath_raw:
        im.pack()
(ROOT / 'verification.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('ASSET_SAVED', json.dumps(report), flush=True)


# ---------------------------------------------------------------- previews
def link(ob, c):
    for x in list(ob.users_collection):
        x.objects.unlink(ob)
    c.objects.link(ob)
desk_mat = bpy.data.materials.new('Studio_DustyDesk'); desk_mat.use_nodes = True
next(n for n in desk_mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED').inputs['Base Color'].default_value = (.14, .07, .045, 1)
bpy.ops.mesh.primitive_plane_add(size=3, location=(0, 0, 0))
floor = bpy.context.object; floor.name = 'StudioSurface'; link(floor, studio); floor.data.materials.append(desk_mat)
world = bpy.data.worlds.new('SoftStudio'); world.use_nodes = True; scene.world = world
bg = next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND')
bg.inputs['Color'].default_value = (.55, .6, .66, 1); bg.inputs['Strength'].default_value = .3
for name, loc, energy, size in [('Key', (-.6, -.8, 1.0), 60, .6), ('Fill', (.8, -.3, .5), 12, .5), ('Rim', (.3, .7, .7), 25, .4)]:
    d = bpy.data.lights.new(name, 'AREA'); d.energy = energy; d.shape = 'DISK'; d.size = size
    lo = bpy.data.objects.new(name, d); studio.objects.link(lo); lo.location = loc
    lo.rotation_euler = (Vector((0, 0, .15)) - lo.location).to_track_quat('-Z', 'Y').to_euler()
camdata = bpy.data.cameras.new('PreviewCamera'); cam = bpy.data.objects.new('PreviewCamera', camdata); studio.objects.link(cam); scene.camera = cam
camdata.lens = 50; camdata.clip_start = .01
def aim(location, target):
    cam.location = location; cam.rotation_euler = (Vector(target) - Vector(location)).to_track_quat('-Z', 'Y').to_euler()
scene.cycles.samples = 128; scene.cycles.use_denoising = True
scene.render.resolution_x = 1200; scene.render.resolution_y = 1000
scene.render.image_settings.file_format = 'PNG'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Blender/DeskLamp.blend'), compress=True)
for name, loc, target in [('Overall', (.55, -.75, .45), (.06, .02, .15)), ('Side', (-.75, -.25, .3), (.03, .0, .17)), ('ShadeInside', (.25, -.45, .02), (.05, -.08, .2))]:
    aim(loc, target)
    scene.render.filepath = str(ROOT / f'Previews/DeskLamp_{name}.png')
    bpy.ops.render.render(write_still=True)
print('BUILD_COMPLETE', flush=True)
