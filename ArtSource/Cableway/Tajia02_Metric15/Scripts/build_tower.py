"""Build the cableway tower in real metres (1 Blender unit = 1 m) through MCP.

Overall height, band levels and taper are parameters. Every fabricated part
(angles, plates, bolts, ladder, cage, railings) uses fixed real-world sizes, so
changing the height adds or stretches bays without scaling any hardware.
Silhouette follows Tajia01 (tapered four-leg shaft, side outrigger platform,
wide top crosshead, ladder on the -Y face).
"""
import bpy
import math
from mathutils import Matrix, Vector

ROOT = r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia02_Metric15'

# ---------------------------------------------------------------- parameters
# Defaults build the 15 m line tower. Set TOWER_PARAMS before exec to override any of them,
# e.g. the 8 m station entry tower (see entry_tower_params.py).
_P = globals().get('TOWER_PARAMS', {})
SCENE_NAME = _P.get('SCENE_NAME', 'Tajia02_Tower_Metric15')
TOTAL_HEIGHT = _P.get('TOTAL_HEIGHT', 15.0)        # pier top (z = 0) to top of the sheaves
SHAFT_BASE_Z = .025                                # top of sole plate; piers are a separate model
SHAFT_TOP_Z = _P.get('SHAFT_TOP_Z', 13.2)
HALF_BASE = _P.get('HALF_BASE', (1.60, 1.25))      # leg centre offsets at the base (x, y)
HALF_TOP = _P.get('HALF_TOP', (.70, .55))
BANDS = [SHAFT_BASE_Z] + _P.get('BANDS', [2.45, 4.60, 6.75, 8.90, 11.05]) + [SHAFT_TOP_Z]
PLAN_BRACED_BANDS = _P.get('PLAN_BRACED_BANDS', (2, 4))
OUTRIGGER_Z = _P.get('OUTRIGGER_Z', 6.75)          # None for no side outrigger platform
TOP_DECK_Z = _P.get('TOP_DECK_Z', 11.05)
CROSSHEAD_HALF = _P.get('CROSSHEAD_HALF', 2.0)     # sheave centre line, so the rope gauge is 4 m
SHEAVE_R = .22
CROSSHEAD_TOP = TOTAL_HEIGHT - 2 * SHEAVE_R - .03 - .04   # top face of the upper flange

scene = bpy.data.scenes.new(SCENE_NAME)
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
parts = bpy.data.collections.new('Tower_Fabricated_Parts')
scene.collection.children.link(parts)


def enum_set(owner, prop, value):
    choices = {v.identifier for v in owner.bl_rna.properties[prop].enum_items}
    if value not in choices:
        raise ValueError((prop, value, choices))
    setattr(owner, prop, value)


def material(name, variant=0):
    mat = bpy.data.materials.new(name)
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    bs = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
    pos = nodes.new('ShaderNodeNewGeometry')
    coarse = nodes.new('ShaderNodeTexNoise')
    coarse.inputs['Scale'].default_value = 3.1 if variant == 0 else (4.5 if variant == 1 else 7.4)
    coarse.inputs['Detail'].default_value = 4.0
    coarse.inputs['Roughness'].default_value = .72
    links.new(pos.outputs['Position'], coarse.inputs['Vector'])
    fine = nodes.new('ShaderNodeTexNoise')
    fine.inputs['Scale'].default_value = 120.0
    fine.inputs['Detail'].default_value = 3.0
    links.new(pos.outputs['Position'], fine.inputs['Vector'])
    ramp = nodes.new('ShaderNodeValToRGB')
    if variant == 0:
        colors = [(.18, (.022, .027, .030, 1)), (.37, (.048, .054, .056, 1)), (.52, (.079, .079, .069, 1)), (.58, (.053, .050, .040, 1)), (.615, (.100, .043, .021, 1)), (.68, (.123, .033, .020, 1)), (.81, (.185, .076, .033, 1))]
    elif variant == 1:
        colors = [(.2, (.025, .029, .032, 1)), (.47, (.061, .069, .068, 1)), (.55, (.093, .060, .031, 1)), (.64, (.053, .047, .038, 1)), (.8, (.12, .13, .125, 1))]
    else:
        colors = [(.2, (.12, .116, .103, 1)), (.48, (.22, .212, .187, 1)), (.65, (.30, .283, .247, 1)), (.85, (.17, .162, .14, 1))]
    cr = ramp.color_ramp
    cr.elements.remove(cr.elements[1])
    cr.elements[0].position, cr.elements[0].color = colors[0]
    for p, c in colors[1:]:
        cr.elements.new(p).color = c
    links.new(coarse.outputs['Fac'], ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'], bs.inputs['Base Color'])
    rough = nodes.new('ShaderNodeMapRange')
    rough.inputs['From Min'].default_value = .2
    rough.inputs['From Max'].default_value = .8
    rough.inputs['To Min'].default_value = .61 if variant != 2 else .85
    rough.inputs['To Max'].default_value = .91
    links.new(fine.outputs['Fac'], rough.inputs['Value'])
    links.new(rough.outputs['Result'], bs.inputs['Roughness'])
    bs.inputs['Metallic'].default_value = 0.0 if variant == 2 else .7
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = .22
    bump.inputs['Distance'].default_value = .004 if variant != 2 else .008
    links.new(fine.outputs['Fac'], bump.inputs['Height'])
    links.new(bump.outputs['Normal'], bs.inputs['Normal'])
    mat.diffuse_color = colors[2][1]
    return mat


paint = material('Tower_Weathered_RedOxide_Steel', 0)
bare = material('Tower_DarkSteel_Hardware', 1)


def mesh_obj(name, verts, faces, mat=paint, bevel=0.0):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    parts.objects.link(ob)
    mesh.materials.append(mat)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    if bevel:
        mod = ob.modifiers.new('Fabricated edge radius', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        enum_set(mod, 'limit_method', 'ANGLE')
    normal_mod = ob.modifiers.new('Weighted fabrication normals', 'WEIGHTED_NORMAL')
    normal_mod.keep_sharp = True
    return ob


def signed_area(profile):
    return sum(x0 * y1 - x1 * y0 for (x0, y0), (x1, y1) in zip(profile, profile[1:] + profile[:1])) / 2


def prism(name, profile, a, b, mat=paint, bevel=0.0, up=(0, 1, 0)):
    if signed_area(profile) < 0:
        profile = list(reversed(profile))
    a, b = Vector(a), Vector(b)
    axis = (b - a).normalized()
    ref = Vector(up)
    if abs(axis.dot(ref)) > .98:
        ref = Vector((1, 0, 0))
    u = ref.cross(axis).normalized()
    v = axis.cross(u).normalized()
    verts = [tuple(end + u * x + v * y) for end in (a, b) for x, y in profile]
    n = len(profile)
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    return mesh_obj(name, verts, faces, mat, bevel)


def box(name, center, size, mat=paint, bevel=0.0):
    x, y, z = center
    w, d, h = size
    return prism(name, [(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2)], (x, y, z - h / 2), (x, y, z + h / 2), mat, bevel)


def angle(name, a, b, w, t, flip=(1, 1), mat=paint, up=(0, 1, 0)):
    """Equal-leg angle. flip mirrors the heel so it can face outward."""
    fx, fy = flip
    p = [(-w / 2, -w / 2), (w / 2, -w / 2), (w / 2, -w / 2 + t), (-w / 2 + t, -w / 2 + t), (-w / 2 + t, w / 2), (-w / 2, w / 2)]
    p = [(x * fx, y * fy) for x, y in p]
    return prism(name, p, a, b, mat, min(.003, t * .25), up)


def channel(name, a, b, w, d, t, mat=paint, up=(0, 1, 0)):
    p = [(-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, -d / 2 + t), (-w / 2 + t, -d / 2 + t), (-w / 2 + t, d / 2 - t), (w / 2, d / 2 - t), (w / 2, d / 2), (-w / 2, d / 2)]
    return prism(name, p, a, b, mat, min(.003, t * .25), up)


def rod(name, a, b, r, segments=8, mat=bare):
    profile = [(r * math.cos(i * 2 * math.pi / segments), r * math.sin(i * 2 * math.pi / segments)) for i in range(segments)]
    return prism(name, profile, a, b, mat)


def cylinder(name, center, radius, depth, normal=(0, 0, 1), segments=12, mat=bare):
    c = Vector(center)
    n = Vector(normal).normalized()
    return rod(name, c - n * depth / 2, c + n * depth / 2, radius, segments, mat)


def bolt(center, normal, size=.020):
    """Hex bolt head with washer. size is the nominal diameter (M20 = .020)."""
    c = Vector(center)
    n = Vector(normal).normalized()
    cylinder('Bolt washer', c + n * .002, size * 1.1, .004, n, 8, bare)
    cylinder('Hex bolt', c + n * (.004 + size * .32), size * .87, size * .64, n, 6, bare)


def half(z):
    t = (z - SHAFT_BASE_Z) / (SHAFT_TOP_Z - SHAFT_BASE_Z)
    return (HALF_BASE[0] + (HALF_TOP[0] - HALF_BASE[0]) * t, HALF_BASE[1] + (HALF_TOP[1] - HALF_BASE[1]) * t)


def face_plate(x, y, z, sx, sy, face, w=.34, h=.30, t=.010, bolts=4):
    """Gusset plate on the outside of a face at a leg/band joint. face is 'x' or 'y'."""
    if face == 'y':
        box('Gusset plate', (x - sx * .06, y + sy * .005, z), (w, t, h), paint, .002)
        offsets = [(-.07, .07), (.07, .07), (-.07, -.07), (.07, -.07)][:bolts]
        for dx, dz in offsets:
            bolt((x - sx * .06 + dx, y + sy * (.005 + t / 2), z + dz), (0, sy, 0))
    else:
        box('Gusset plate', (x + sx * .005, y - sy * .06, z), (t, w, h), paint, .002)
        offsets = [(-.07, .07), (.07, .07), (-.07, -.07), (.07, -.07)][:bolts]
        for dy, dz in offsets:
            bolt((x + sx * (.005 + t / 2), y - sy * .06 + dy, z + dz), (sx, 0, 0))


# ---------------------------------------------------------------- legs
LEG_W, LEG_T = .160, .014          # L160x14
for sx in (-1, 1):
    for sy in (-1, 1):
        x0, y0 = sx * HALF_BASE[0], sy * HALF_BASE[1]
        x1, y1 = sx * HALF_TOP[0], sy * HALF_TOP[1]
        # Sole plates and anchor bolts belong to build_leg_extensions.py (they sit on the piers).
        angle('Main tapered angle leg', (x0, y0, SHAFT_BASE_Z), (x1, y1, SHAFT_TOP_Z), LEG_W, LEG_T, (-sx, -sy))

# ---------------------------------------------------------------- horizontal bands, gussets, plan bracing
for i, z in enumerate(BANDS):
    hx, hy = half(z)
    for sy in (-1, 1):
        angle('Face horizontal', (-hx, sy * (hy - .02), z), (hx, sy * (hy - .02), z), .100, .008, (1, -sy), paint, (0, 0, 1))
        for sx in (-1, 1):
            face_plate(sx * hx, sy * hy, z, sx, sy, 'y')
    for sx in (-1, 1):
        angle('Side horizontal', (sx * (hx - .02), -hy, z), (sx * (hx - .02), hy, z), .100, .008, (1, -sx), paint, (0, 0, 1))
        for sy in (-1, 1):
            face_plate(sx * hx, sy * hy, z, sx, sy, 'x')
    if i in PLAN_BRACED_BANDS:
        for d in (-1, 1):
            angle('Plan diagonal', (-hx + .08, d * (-hy + .08), z + .05), (hx - .08, d * (hy - .08), z + .05), .075, .006, (1, 1), bare, (0, 0, 1))

# ---------------------------------------------------------------- X bracing on all four faces
for z0, z1 in zip(BANDS[:-1], BANDS[1:]):
    h0, h1 = half(z0), half(z1)
    zm = (z0 + z1) / 2
    hm = half(zm)
    for sy in (-1, 1):
        for d in (-1, 1):
            off = .012 if d == 1 else -.012
            angle('Face diagonal brace', (-d * (h0[0] - .08), sy * (h0[1] + off), z0 + .10), (d * (h1[0] - .08), sy * (h1[1] + off), z1 - .10), .075, .006, (1, 1), bare if d == 1 else paint)
        box('Crossing fish plate', (0, sy * (hm[1] + .03), zm), (.18, .008, .16), bare, .002)
        bolt((0, sy * (hm[1] + .034), zm), (0, sy, 0))
    for sx in (-1, 1):
        for d in (-1, 1):
            off = .012 if d == 1 else -.012
            angle('Side diagonal brace', (sx * (h0[0] + off), -d * (h0[1] - .08), z0 + .10), (sx * (h1[0] + off), d * (h1[1] - .08), z1 - .10), .075, .006)
        box('Crossing fish plate', (sx * (hm[0] + .03), 0, zm), (.008, .18, .16), bare, .002)
        bolt((sx * (hm[0] + .034), 0, zm), (sx, 0, 0))


# ---------------------------------------------------------------- railings
def railing(points, z, closed=False):
    """Handrail at 1.05 m, knee rail at .52 m, toe board .10 m, posts at <= 1.2 m."""
    segs = list(zip(points, points[1:] + (points[:1] if closed else [])))
    posts = []
    for a, b in segs:
        a, b = Vector((*a, z)), Vector((*b, z))
        n = max(1, math.ceil((b - a).length / 1.2))
        posts += [a.lerp(b, k / n) for k in range(n)]
        rod('Handrail tube', a + Vector((0, 0, 1.05)), b + Vector((0, 0, 1.05)), .021, 8, paint)
        rod('Knee rail', a + Vector((0, 0, .52)), b + Vector((0, 0, .52)), .014, 6, paint)
        d = (b - a).normalized()
        n2 = Vector((-d.y, d.x, 0))
        mesh_obj('Toe board', [tuple(a + n2 * .003), tuple(b + n2 * .003), tuple(b + n2 * .003 + Vector((0, 0, .10))), tuple(a + n2 * .003 + Vector((0, 0, .10))),
                                   tuple(a - n2 * .003), tuple(b - n2 * .003), tuple(b - n2 * .003 + Vector((0, 0, .10))), tuple(a - n2 * .003 + Vector((0, 0, .10)))],
                     [(0, 1, 2, 3), (5, 4, 7, 6), (4, 5, 1, 0), (3, 2, 6, 7), (4, 0, 3, 7), (1, 5, 6, 2)], paint)
    if not closed:
        posts.append(Vector((*points[-1], z)))
    for p in posts:
        angle('Railing post', p, p + Vector((0, 0, 1.08)), .050, .005, (1, 1), paint)


def grating(x0, x1, y0, y1, z, hole=None, along='y'):
    """Open steel grating: 30x3 bearing bars at 50 mm with 6 mm cross rods at 300 mm."""
    if along == 'y':
        n = int((x1 - x0) / .05)
        for k in range(n + 1):
            x = x0 + k * (x1 - x0) / n
            spans = [(y0, y1)]
            if hole and hole[0] <= x <= hole[1]:
                spans = [(y0, hole[2]), (hole[3], y1)]
            for a, b in spans:
                if b - a > .02:
                    box('Grating bearing bar', (x, (a + b) / 2, z - .015), (.003, b - a, .030), bare)
        m = int((y1 - y0) / .30)
        for k in range(m + 1):
            y = y0 + k * (y1 - y0) / m
            rod('Grating cross rod', (x0, y, z - .004), (x1, y, z - .004), .003, 4, bare)
    else:
        n = int((y1 - y0) / .05)
        for k in range(n + 1):
            y = y0 + k * (y1 - y0) / n
            box('Grating bearing bar', ((x0 + x1) / 2, y, z - .015), (x1 - x0, .003, .030), bare)
        m = int((x1 - x0) / .30)
        for k in range(m + 1):
            x = x0 + k * (x1 - x0) / m
            rod('Grating cross rod', (x, y0, z - .004), (x, y1, z - .004), .003, 4, bare)
    # edge frame
    for a, b in [((x0, y0), (x1, y0)), ((x1, y0), (x1, y1)), ((x1, y1), (x0, y1)), ((x0, y1), (x0, y0))]:
        angle('Grating edge angle', (*a, z - .03), (*b, z - .03), .050, .005, (1, 1), paint, (0, 0, 1))


# ---------------------------------------------------------------- side outrigger platform
OUT_X = 2.80
OUT_Y = .75
if OUTRIGGER_Z is not None:
    hx6, hy6 = half(OUTRIGGER_Z)
    hx_knee = half(BANDS[2])
    for y in (-OUT_Y + .04, OUT_Y - .04):
        channel('Outrigger longitudinal channel', (-OUT_X, y, OUTRIGGER_Z - .08), (OUT_X, y, OUTRIGGER_Z - .08), .140, .058, .006, paint, (0, 0, 1))
        for sx in (-1, 1):
            angle('Under platform knee brace', (sx * hx_knee[0], y, BANDS[2] + .1), (sx * (OUT_X - .15), y, OUTRIGGER_Z - .16), .090, .007)
            box('Knee joint plate', (sx * (OUT_X - .15), y, OUTRIGGER_Z - .20), (.22, .008, .18), paint, .002)
            bolt((sx * (OUT_X - .15), y - .006, OUTRIGGER_Z - .20), (0, -1, 0))
    for sx in (-1, 1):
        for k in range(4):
            x = sx * (hx6 + .15 + k * (OUT_X - hx6 - .15) / 3)
            angle('Platform cross member', (x, -OUT_Y, OUTRIGGER_Z - .045), (x, OUT_Y, OUTRIGGER_Z - .045), .063, .006, (1, 1), bare)
        xa, xb = sorted((sx * (hx6 + .10), sx * OUT_X))
        grating(xa, xb, -OUT_Y, OUT_Y, OUTRIGGER_Z, along='y')
        ends = [(sx * (hx6 + .10), -OUT_Y), (sx * OUT_X, -OUT_Y), (sx * OUT_X, OUT_Y), (sx * (hx6 + .10), OUT_Y)]
        railing(ends, OUTRIGGER_Z)

# ---------------------------------------------------------------- top service deck around the shaft
hxt, hyt = half(TOP_DECK_Z)
LADDER_Y = -(HALF_BASE[1] + .20)
# Side-step landing: the deck's front edge stops just inside the ladder, so the climber
# leaves the cage through a 0.6 m opening in the front railing instead of a hatch.
DECK = (-1.40, 1.40, LADDER_Y + .05, hyt + .65)   # x kept inside the hanging bucket path
LANDING_HALF = .30
for x in (-hxt - .12, hxt + .12):
    channel('Deck support channel', (x, DECK[2], TOP_DECK_Z - .08), (x, DECK[3], TOP_DECK_Z - .08), .140, .058, .006, paint, (0, 0, 1))
for y in (DECK[2] + .04, DECK[3] - .04):
    angle('Deck edge beam', (DECK[0], y, TOP_DECK_Z - .07), (DECK[1], y, TOP_DECK_Z - .07), .075, .006, (1, 1), paint, (0, 0, 1))
grating(DECK[0], DECK[1], DECK[2], DECK[3], TOP_DECK_Z, along='y')
railing([(-LANDING_HALF, DECK[2]), (DECK[0], DECK[2]), (DECK[0], DECK[3]), (DECK[1], DECK[3]), (DECK[1], DECK[2]), (LANDING_HALF, DECK[2])], TOP_DECK_Z)

# ---------------------------------------------------------------- vertical ladder with safety cage (GB 4053.1 dimensions)
RAIL_X = .25                          # 450 mm clear between 60x10 flat stiles
RUNG_TOP = TOP_DECK_Z
rung_count = round((RUNG_TOP - .35) / .30)
pitch = (RUNG_TOP - .35) / rung_count  # about 0.297 m
for x in (-RAIL_X, RAIL_X):
    box('Ladder stile', (x + math.copysign(.005, x), LADDER_Y, (TOP_DECK_Z + 1.10) / 2), (.010, .060, TOP_DECK_Z + 1.10), bare)
for i in range(rung_count + 1):
    z = .35 + i * pitch
    rod('Ladder rung', (-RAIL_X, LADDER_Y, z), (RAIL_X, LADDER_Y, z), .011, 8, bare)
for z in BANDS[1:-1]:
    if z > TOP_DECK_Z:
        continue
    hy = half(z)[1]
    for x in (-RAIL_X, RAIL_X):
        angle('Ladder standoff bracket', (x, LADDER_Y + .03, z + .15), (x, -hy + .02, z + .15), .050, .005, (1, 1), bare, (0, 0, 1))
        bolt((x, LADDER_Y - .03, z + .15), (0, -1, 0), .016)
# Cage hoops from 2.4 m up to 1 m above the deck, straps connecting them.
CAGE_R = .36
CAGE_CY = LADDER_Y - .35
CAGE_TOP = TOP_DECK_Z + 1.0
hoop_pts = [(CAGE_R * math.cos(a), CAGE_CY - CAGE_R * math.sin(a) * 1.0) for a in [math.pi * k / 10 for k in range(11)]]
hoop_pts = [(RAIL_X, LADDER_Y)] + hoop_pts + [(-RAIL_X, LADDER_Y)]
hoops = [2.40 + k * (CAGE_TOP - 2.40) / max(1, round((CAGE_TOP - 2.40) / .90)) for k in range(max(1, round((CAGE_TOP - 2.40) / .90)) + 1)]
for zh in hoops:
    for (xa, ya), (xb, yb) in zip(hoop_pts, hoop_pts[1:]):
        prism('Cage hoop', [(-.025, -.003), (.025, -.003), (.025, .003), (-.025, .003)], (xa, ya, zh), (xb, yb, zh), bare, 0, (0, 0, 1))
for k in (1, 3, 5, 7, 9):
    a = math.pi * k / 10
    x, y = CAGE_R * math.cos(a) * .985, CAGE_CY - CAGE_R * math.sin(a) * .985
    size = (.040, .006) if abs(math.cos(a)) < .3 else (.006, .040)
    box('Cage strap', (x, y, (hoops[0] + CAGE_TOP) / 2), (*size, CAGE_TOP - hoops[0]), bare)

# ---------------------------------------------------------------- top crosshead
CH_Y = .42
ends_x = CROSSHEAD_HALF + .12
for y in (-CH_Y, CH_Y):
    channel('Crosshead upper flange', (-ends_x, y, CROSSHEAD_TOP - .08), (ends_x, y, CROSSHEAD_TOP - .08), .160, .065, .0085, paint)
    web = lambda pts, nm: mesh_obj(nm, [(x, y + dy, z) for dy in (-.005, .005) for x, z in pts],
                                   [tuple(range(len(pts))), tuple(reversed(range(len(pts), 2 * len(pts))))] +
                                   [(i, i + len(pts), (i + 1) % len(pts) + len(pts), (i + 1) % len(pts)) for i in range(len(pts))], paint, .002)
    web([(-CROSSHEAD_HALF, CROSSHEAD_TOP - .16), (-.75, CROSSHEAD_TOP - .16), (-.75, SHAFT_TOP_Z + .05)], 'Crosshead tapered web')
    web([(.75, SHAFT_TOP_Z + .05), (.75, CROSSHEAD_TOP - .16), (CROSSHEAD_HALF, CROSSHEAD_TOP - .16)], 'Crosshead tapered web')
    for sx in (-1, 1):
        angle('Crosshead lower chord', (sx * CROSSHEAD_HALF, y, CROSSHEAD_TOP - .20), (sx * .75, y, SHAFT_TOP_Z + .02), .100, .008)
        for k in range(4):
            x = .75 + k * (CROSSHEAD_HALF - .75) / 3
            lower = SHAFT_TOP_Z + .05 + (CROSSHEAD_TOP - .16 - SHAFT_TOP_Z - .05) * (x - .75) / (CROSSHEAD_HALF - .75)
            angle('Crosshead web stiffener', (sx * x, y - .012, lower), (sx * x, y - .012, CROSSHEAD_TOP - .16), .063, .006, (1, 1), bare)
            for zb in (CROSSHEAD_TOP - .24, lower + .10):
                bolt((sx * x, y + math.copysign(.022, y), zb), (0, math.copysign(1, y), 0), .016)
    channel('Crosshead bearing beam', (-.85, y, SHAFT_TOP_Z + .04), (.85, y, SHAFT_TOP_Z + .04), .140, .058, .006, paint)
for x in (-ends_x + .05, -.75, 0, .75, ends_x - .05):
    channel('Crosshead transverse tie', (x, -CH_Y - .05, CROSSHEAD_TOP - .05), (x, CH_Y + .05, CROSSHEAD_TOP - .05), .100, .048, .0053, bare, (0, 0, 1))

# Sheave batteries at both ends: rocker beam with two rope sheaves each.
sheave_z = TOTAL_HEIGHT - SHEAVE_R
for sx in (-1, 1):
    cx = sx * CROSSHEAD_HALF
    existing_parts = set(parts.objects)
    for y in (-.09, .09):
        mesh_obj('Sheave bracket plate', [(cx + dx, y + dy, zz) for dy in (-.006, .006) for dx, zz in
                                          [(-.20, CROSSHEAD_TOP), (.20, CROSSHEAD_TOP), (.12, sheave_z - .12), (-.12, sheave_z - .12)]],
                 [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)], bare, .002)
        box('Rocker beam side', (cx, y, sheave_z - .13), (.72, .010, .10), bare, .002)
    cylinder('Rocker pivot pin', (cx, 0, sheave_z - .13), .030, .26, (0, 1, 0), 12, bare)
    for dx in (-.28, .28):
        cylinder('Rope sheave', (cx + dx, 0, sheave_z), SHEAVE_R, .070, (0, 1, 0), 24, bare)
        cylinder('Sheave rubber lining', (cx + dx, 0, sheave_z), SHEAVE_R - .012, .075, (0, 1, 0), 24, bare)
        cylinder('Sheave hub', (cx + dx, 0, sheave_z), .055, .16, (0, 1, 0), 12, bare)
        for y in (-.09, .09):
            box('Sheave axle block', (cx + dx, y, sheave_z), (.10, .02, .09), bare, .002)
    bolt((cx, -.10, CROSSHEAD_TOP + .06), (0, -1, 0))
    bolt((cx, .10, CROSSHEAD_TOP + .06), (0, 1, 0))
    # The rope runs along Y. Rotate each complete bogie so its wheel axles run along X.
    bogie_turn = Matrix.Translation((cx, 0, 0)) @ Matrix.Rotation(math.pi / 2, 4, 'Z') @ Matrix.Translation((-cx, 0, 0))
    for part in set(parts.objects) - existing_parts:
        part.matrix_world = bogie_turn @ part.matrix_world

# Tajia01's outer vertical tie on +X is omitted: it sat in the path of the hanging buckets.

scene['units'] = 'metres, 1 Blender unit = 1 m, export at scale 1'
scene['total_height_m'] = TOTAL_HEIGHT
scene['rope_gauge_m'] = CROSSHEAD_HALF * 2
scene['rope_head_points'] = [(-CROSSHEAD_HALF, 0.0, TOTAL_HEIGHT), (CROSSHEAD_HALF, 0.0, TOTAL_HEIGHT)]
scene['ladder'] = 'rung pitch %.3f m, 450 mm clear width, 22 mm rungs, cage from 2.4 m' % pitch
tris = 0
dg = bpy.context.evaluated_depsgraph_get()
for o in parts.objects:
    me = o.evaluated_get(dg).to_mesh()
    me.calc_loop_triangles()
    tris += len(me.loop_triangles)
    o.evaluated_get(dg).to_mesh_clear()
print('BUILT', len(parts.objects), 'objects', tris, 'triangles', 'rung pitch', round(pitch, 4))
