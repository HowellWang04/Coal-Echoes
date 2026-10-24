"""Build the sand dunes that close the main road and the side gaps inside the factory after the opening time-lapse.

Each dune is a heightfield in factory coordinates: u runs from the FactoryGate pivot into the factory along the
main road, v points to the right of it, both in metres. Blender X is v and Y is u, relative to the dune's origin,
so in Unity the prefab sits at that point with the gate's rotation. Dunes that spill over the factory walls follow
the ground outside from Ground/<name>.json, written by export_ground.cs; the others sit on the flat factory floor.
"""
import bpy
import bmesh
import json
import math
from mathutils import Vector, noise
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\SandDune')
CELL = 0.2
SINK = 0.05  # mesh edges end below the ground so the terrain hides them
SKIRT = 0.15  # how far the wall skirts reach below the sunken edge
SLIP = math.tan(math.radians(32))  # lee face toward the gate, dry sand's angle of repose; a dune may set its own 'slip'
CREST_ROUND = 2.5  # how softly the two slopes meet at the crest, a dune may set its own 'crest_round'
STOSS = math.tan(math.radians(19))  # windward back slope, a dune may set its own 'stoss'
TOE = 0.8  # soft blend, in metres of height, where the slopes run into the ground
DRIFT = 0.9  # extra height where sand banks up against a wall
DRIFT_FALL = 1.8

# solids: building footprints (u0, u1, v0, v1) cut out of the sand. Their faces get a skirt, so windows show sand.
# ends: (side, v, slope) where the sand runs out past the buildings or over a wall into open ground.
# The factory walls are not solids: the side dunes bury them (top at 3.68 m) and spill down the outside.
DUNES = {
    'SandDune_MainRoad': dict(
        seed=5, origin=(30.0, -3.6), domain=((17.0, 50.0), (-18.0, 13.0)),
        crest=29.6, bow=1.6, bow_span=(-11.65, 4.5), height=4.9,
        slip=math.tan(math.radians(42)), crest_round=1.2,  # steeper than repose and a sharper crest, so it reads as unclimbable
        solids=[(24.43, 34.53, 4.5, 99.0),  # WorkersDormitory, road-side gable at v 4.45
                (-99.0, 24.5, -99.0, -10.9),  # SM_ConcreteHouse_Pack_2_C_4
                (26.8, 43.8, -99.0, -11.65)],  # SM_ConcreteHouse_Pack_2_C_2
        ends=[(-1, -11.0, 0.9), (1, 4.5, 0.6)],
    ),
    # Between the concrete houses and the left factory wall (inner face v -44.69, outer -45.19).
    'SandDune_Left': dict(
        seed=11, origin=(30.0, -43.0), domain=((14.0, 54.0), (-60.0, -34.0)),
        crest=30.0, bow=0.6, bow_span=(-52.0, -40.0), height=5.0, stoss=math.tan(math.radians(25)),
        solids=[(-99.0, 24.5, -40.12, 99.0),  # SM_ConcreteHouse_Pack_2_C_4, outer face at v -40.09
                (26.8, 43.8, -40.98, 99.0)],  # SM_ConcreteHouse_Pack_2_C_2, outer face at v -40.95
        ends=[(1, -40.5, 0.9), (-1, -45.2, 0.6)],
    ),
    # Between the dormitory's far gable and the right factory wall (inner face v 39.81, outer 40.31).
    'SandDune_Right': dict(
        seed=17, origin=(29.6, 38.3), domain=((14.0, 54.0), (28.0, 58.0)),
        crest=29.6, bow=0.6, bow_span=(36.7, 48.0), height=5.0, stoss=math.tan(math.radians(25)),
        solids=[(24.43, 34.53, -99.0, 36.62)],  # WorkersDormitory, gable leans from v 36.85 at the foot to 36.61
        ends=[(-1, 36.7, 0.6), (1, 40.3, 0.6)],
    ),
}


def smin(a, b, k):
    h = max(k - abs(a - b), 0.0) / k
    return min(a, b) - h * h * k * 0.25


def smax(a, b, k):
    return -smin(-a, -b, k)


def smoothstep(e0, e1, x):
    t = min(max((x - e0) / (e1 - e0), 0.0), 1.0)
    return t * t * (3 - 2 * t)


def rect_distance(u, v, r):
    du = max(r[0] - u, 0.0, u - r[1])
    dv = max(r[2] - v, 0.0, v - r[3])
    return math.hypot(du, dv)


def inside(u, v, r):
    return r[0] < u < r[1] and r[2] < v < r[3]


def surface(u, v, cfg):
    """Sand height above the ground, zero outside the dune."""
    seed = cfg['seed']
    lo, hi = cfg['bow_span']
    t = min(max((v - lo) / (hi - lo), 0.0), 1.0)
    crest = cfg['crest'] - cfg['bow'] * math.sin(math.pi * t) + 1.0 * noise.noise(Vector((v * 0.1, 2.1, seed)))
    height = cfg['height'] + 0.45 * noise.noise(Vector((v * 0.15, 7.3, seed)))
    height += DRIFT * max(math.exp(-rect_distance(u, v, r) / DRIFT_FALL) for r in cfg['solids'])
    d = u - crest
    h = smin(height + d * cfg.get('slip', SLIP), height - d * cfg.get('stoss', STOSS), cfg.get('crest_round', CREST_ROUND))
    for side, edge, slope in cfg['ends']:
        h -= max((v - edge) * side, 0.0) * slope
    h = smax(h, 0.0, TOE)
    if h <= 0.0:
        return 0.0

    body = smoothstep(0.0, 0.8, h)
    h += body * (0.12 * noise.noise(Vector((u * 0.22, v * 0.22, seed + 1.0)))
                 + 0.04 * noise.noise(Vector((u * 0.9, v * 0.9, seed + 2.0))))
    # Wind ripples on the back slope, crests running across the wind.
    phase = u / 0.7 + 1.6 * noise.noise(Vector((u * 0.08, v * 0.08, seed + 3.0)))
    h += smoothstep(0.3, 2.0, d) * body * 0.025 * math.sin(2 * math.pi * phase)
    # Shallow avalanche chutes down the slip face.
    h += smoothstep(-0.5, -2.0, d) * body * 0.06 * noise.noise(Vector((v * 1.1, u * 0.12, seed + 4.0)))
    return max(h, 0.0)


def load_ground(name):
    """Bilinear ground height relative to the factory floor; flat when the dune has no ground file."""
    path = ROOT / 'Ground' / f'{name}.json'
    if not path.exists():
        return lambda u, v: 0.0
    data = json.loads(path.read_text(encoding='utf-8'))
    gu, gv, cell, rows = data['u0'], data['v0'], data['cell'], data['heights']

    def ground(u, v):
        x = min(max((u - gu) / cell, 0.0), len(rows) - 1.001)
        y = min(max((v - gv) / cell, 0.0), len(rows[0]) - 1.001)
        i, j = int(x), int(y)
        fx, fy = x - i, y - j
        near = rows[i][j] * (1 - fy) + rows[i][j + 1] * fy
        far = rows[i + 1][j] * (1 - fy) + rows[i + 1][j + 1] * fy
        return near * (1 - fx) + far * fx
    return ground


def build(name, cfg):
    (ua, ub), (va, vb) = cfg['domain']
    u0, v0 = cfg['origin']
    solids = cfg['solids']
    ground = load_ground(name)

    def factory(co):
        return co.y + u0, co.x + v0

    nx, ny = round((vb - va) / CELL), round((ub - ua) / CELL)
    bm = bmesh.new()
    grid = [[bm.verts.new((va + i * CELL - v0, ua + j * CELL - u0, 0.0)) for i in range(nx + 1)] for j in range(ny + 1)]
    for j in range(ny):
        for i in range(nx):
            bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))

    # Split the grid exactly on every building face, then drop what lies inside the buildings.
    for r in solids:
        for value, normal in ((r[0], (0, 1, 0)), (r[1], (0, 1, 0)), (r[2], (1, 0, 0)), (r[3], (1, 0, 0))):
            if abs(value) >= 90:
                continue
            co = Vector((0, value - u0, 0)) if normal[1] else Vector((value - v0, 0, 0))
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-4,
                                   plane_co=co, plane_no=Vector(normal))
    dead = [f for f in bm.faces if any(inside(*factory(f.calc_center_median()), r) for r in solids)]
    bmesh.ops.delete(bm, geom=dead, context='FACES')

    depth = {vert: surface(*factory(vert.co), cfg) for vert in bm.verts}
    empty = [f for f in bm.faces if all(depth[v] <= 0.0 for v in f.verts)]
    bmesh.ops.delete(bm, geom=empty, context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    for vert in bm.verts:
        vert.co.z = ground(*factory(vert.co)) + depth[vert] - SINK
    for f in bm.faces:
        f.smooth = True
        if f.normal.z < 0:
            f.normal_flip()

    # Skirts close the sand against the walls, so a buried window looks onto sand instead of through it.
    def on_wall(co):
        return any(rect_distance(*factory(co), r) < 1e-3 for r in solids)

    bottoms = {}
    for e in [e for e in bm.edges if e.is_boundary]:
        a, b = e.verts
        if not (on_wall(a.co) and on_wall(b.co)) or max(depth[a], depth[b]) <= 1e-3:
            continue
        for v in (a, b):
            if v not in bottoms:
                bottoms[v] = bm.verts.new((v.co.x, v.co.y, ground(*factory(v.co)) - SINK - SKIRT))
        sand = e.link_faces[0].calc_center_median()
        f = bm.faces.new((a, b, bottoms[b], bottoms[a]))
        f.normal_update()
        outward = (a.co + b.co) * 0.5 - sand
        if f.normal.x * outward.x + f.normal.y * outward.y < 0:
            f.normal_flip()
        f.smooth = False
        e.smooth = False

    uv = bm.loops.layers.uv.new('UVMap')
    for f in bm.faces:
        for loop in f.loops:
            co = loop.vert.co
            if f.smooth:
                loop[uv].uv = (co.x, co.y)
            else:
                loop[uv].uv = (co.y if abs(f.normal.x) > abs(f.normal.y) else co.x, co.z)

    mesh = bpy.data.meshes.get(name) or bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.clear()
    mesh.materials.append(bpy.data.materials.get('SandDune') or bpy.data.materials.new('SandDune'))

    coll = bpy.data.collections.get('SandDunes')
    if coll is None:
        coll = bpy.data.collections.new('SandDunes')
        bpy.context.scene.collection.children.link(coll)
    ob = bpy.data.objects.get(name) or bpy.data.objects.new(name, mesh)
    ob.data = mesh
    if ob.name not in coll.objects:
        coll.objects.link(ob)
    zs = [v.co.z for v in mesh.vertices]
    print(name, 'verts', len(mesh.vertices), 'faces', len(mesh.polygons), 'height', round(max(zs), 2))


for dune_name, dune_cfg in DUNES.items():
    build(dune_name, dune_cfg)
