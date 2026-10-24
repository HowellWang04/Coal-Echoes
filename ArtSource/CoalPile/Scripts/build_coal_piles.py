"""Build three mined coal piles: heightfield bodies at coal's angle of repose plus angular lumps."""
import bpy
import bmesh
import math
import random
from mathutils import Vector, Euler, noise
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\CoalPile')
REPOSE = math.tan(math.radians(36))  # loose coal settles at roughly 35 to 38 degrees
CELL = 0.1
SINK = 0.06  # mesh edges end below the ground so the terrain hides them

# peaks: (x, y, height); scoop: (direction, offset, bucket radius) for a loader cut face.
PILES = {
    'CoalPile_A': dict(seed=11, peaks=[(0.0, 0.0, 1.6)], stretch=(1.0, 0.85), lumps=260, preview_x=-10.0),
    'CoalPile_B': dict(seed=23, peaks=[(-1.3, 0.2, 2.4), (1.4, -0.3, 2.0)], stretch=(1.0, 1.0), lumps=480, preview_x=0.0),
    'CoalPile_C': dict(seed=37, peaks=[(0.0, 0.4, 3.2), (-1.8, -0.6, 2.2)], stretch=(1.0, 1.0), lumps=620,
                       scoop=((1.0, 0.3), 2.4, 3.5), preview_x=13.0),
}


def smin(a, b, k):
    h = max(k - abs(a - b), 0.0) / k
    return min(a, b) - h * h * k * 0.25


def smoothstep(e0, e1, x):
    t = min(max((x - e0) / (e1 - e0), 0.0), 1.0)
    return t * t * (3 - 2 * t)


def surface(x, y, cfg):
    """Returns height above ground plus dust and fresh-cut weights."""
    seed = cfg['seed']
    sx, sy = cfg['stretch']
    height = None
    for cx, cy, h in cfg['peaks']:
        dx, dy = (x - cx) / sx, (y - cy) / sy
        r = math.hypot(dx, dy)
        th = math.atan2(dy, dx)
        wobble = 1 + 0.12 * noise.noise(Vector((math.cos(th) * 1.3, math.sin(th) * 1.3, seed + cx)))
        rr = r / wobble
        cone = smin(h - REPOSE * rr, h - 0.12, 0.5)  # rounded crest
        # Slump gullies run roughly downslope, strongest mid-slope; the warp keeps them from reading as spokes.
        t = min(max(rr * REPOSE / h, 0.0), 1.0)
        mid = math.sin(math.pi * t) ** 0.7
        warp = 0.8 * noise.noise(Vector((x * 0.6, y * 0.6, seed + 2)))
        g = noise.noise(Vector((math.cos(th) * 3.0 + warp, math.sin(th) * 3.0 - warp, rr * 0.6 + seed * 0.1)))
        cone += 0.06 * mid * (abs(g) * 2 - 0.6)
        height = cone if height is None else -smin(-height, -cone, 0.8)
    height += 0.05 * noise.noise(Vector((x * 0.9, y * 0.9, seed + 3)))
    height += 0.03 * noise.noise(Vector((x * 2.5, y * 2.5, seed)))

    fresh = 0.0
    scoop = cfg.get('scoop')
    if scoop:
        (ux, uy), offset, radius = scoop
        n = math.hypot(ux, uy)
        ux, uy = ux / n, uy / n
        bx, by = ux * (offset + radius), uy * (offset + radius)
        cut = (math.hypot(x - bx, y - by) - radius) * math.tan(math.radians(60))
        cut += 0.12 * noise.noise(Vector((x * 1.5, y * 1.5, seed + 5)))
        if cut < height:
            fresh = smoothstep(0.02, 0.15, height - cut)
            height = cut

    final = 0.1 * math.log1p(math.exp(height / 0.1)) - SINK  # soft toe instead of a crease
    dust = 1 - smoothstep(0.0, 0.6, final)
    return final, dust, fresh


def build_body(name, cfg):
    xs = [p[0] for p in cfg['peaks']]
    ys = [p[1] for p in cfg['peaks']]
    hmax = max(p[2] for p in cfg['peaks'])
    reach = hmax / REPOSE * 1.25 + 0.8
    sx, sy = cfg['stretch']
    x0, x1 = min(xs) - reach * sx, max(xs) + reach * sx
    y0, y1 = min(ys) - reach * sy, max(ys) + reach * sy
    nx, ny = int((x1 - x0) / CELL) + 1, int((y1 - y0) / CELL) + 1

    bm = bmesh.new()
    # R dust weight, G fresh cut face, B lump; read by the material and baked into the textures.
    mask = bm.loops.layers.float_color.new('CoalMask')
    grid, data = {}, {}
    for j in range(ny):
        for i in range(nx):
            x, y = x0 + i * CELL, y0 + j * CELL
            data[i, j] = surface(x, y, cfg)
    for (i, j), (z, dust, fresh) in data.items():
        grid[i, j] = bm.verts.new((x0 + i * CELL, y0 + j * CELL, z))
    for j in range(ny - 1):
        for i in range(nx - 1):
            quad = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
            if all(data[k][0] <= -SINK + 0.004 for k in quad):
                continue
            face = bm.faces.new([grid[k] for k in quad])
            face.smooth = True
            for loop, k in zip(face.loops, quad):
                loop[mask] = (data[k][1], data[k][2], 0.0, 1.0)
    for v in [v for v in bm.verts if not v.link_faces]:
        bm.verts.remove(v)
    return bm, mask


def add_lumps(bm, mask, cfg):
    rnd = random.Random(cfg['seed'])
    xs = [v.co.x for v in bm.verts]
    ys = [v.co.y for v in bm.verts]
    placed = 0
    while placed < cfg['lumps']:
        x, y = rnd.uniform(min(xs), max(xs)), rnd.uniform(min(ys), max(ys))
        z, dust, fresh = surface(x, y, cfg)
        if z < -0.02:
            continue
        # Lumps roll down: the toe and the foot of the cut face collect the most and the biggest.
        toe = dust
        if rnd.random() > 0.25 + 0.75 * toe:
            continue
        size = (0.05 + 0.3 * rnd.random() ** 3) * (1 + 0.6 * toe)
        scale = Vector((size * rnd.uniform(0.7, 1.3), size * rnd.uniform(0.7, 1.3), size * rnd.uniform(0.5, 0.9)))
        rot = Euler((rnd.uniform(0, 6.28), rnd.uniform(0, 6.28), rnd.uniform(0, 6.28))).to_matrix()
        centre = Vector((x, y, z - 0.35 * scale.z))
        # The hull of a few random points gives the blocky, flat-faceted break of coal.
        points = []
        for _ in range(10):
            p = Vector((rnd.gauss(0, 1), rnd.gauss(0, 1), rnd.gauss(0, 1))).normalized() * rnd.uniform(0.7, 1.0)
            points.append(bm.verts.new(rot @ Vector((p.x * scale.x, p.y * scale.y, p.z * scale.z)) + centre))
        hull = bmesh.ops.convex_hull(bm, input=points)
        leftover = list({g for g in hull['geom_interior'] + hull['geom_unused'] if isinstance(g, bmesh.types.BMVert)})
        bmesh.ops.delete(bm, geom=leftover, context='VERTS')
        faces = [g for g in hull['geom'] if isinstance(g, bmesh.types.BMFace)]
        for f in faces:
            f.smooth = False
            for loop in f.loops:
                loop[mask] = (0.35 * toe, 0.0, 1.0, 1.0)
        placed += 1


coal = bpy.data.collections.get('CoalPiles') or bpy.data.collections.new('CoalPiles')
if coal.name not in bpy.context.scene.collection.children:
    bpy.context.scene.collection.children.link(coal)
for name, cfg in PILES.items():
    old = bpy.data.objects.get(name)
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    bm, mask = build_body(name, cfg)
    add_lumps(bm, mask, cfg)
    # The open heightfield has no inside, so recalc can pick either side; face it up explicitly.
    for f in bm.faces:
        f.normal_update()
        if f.smooth and f.normal.z < 0:
            f.normal_flip()
    bmesh.ops.recalc_face_normals(bm, faces=[f for f in bm.faces if not f.smooth])
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    ob.location.x = cfg['preview_x']
    coal.objects.link(ob)
    print(name, 'verts', len(me.vertices), 'tris', sum(len(p.vertices) - 2 for p in me.polygons),
          'size', tuple(round(d, 2) for d in ob.dimensions))
