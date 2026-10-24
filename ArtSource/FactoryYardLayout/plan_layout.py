"""Plan where the loose factory yard props go: grouped by kind, against walls and equipment.

Reads Survey/*.json (export.cs), writes plan.json (factory frame) and apply_plan.json (world XZ, for apply.cs).
Run: python plan_layout.py (needs numpy, scipy, matplotlib)
"""
import collections, random
from yard_common import *

random.seed(7)
items = survey('yard_export.json')['items']
obs = survey('obstacles.json'); EQ = survey('equip.json'); B = survey('buildings.json')
ext = survey('extra.json'); gr = survey('ground.json')
byid = {it['id']: it for it in items}

# ---- 1. rigid stacks: things standing on other movable things move together ----
for it in items:
    s = {k: v for k, v in it['sup'].items() if k != 'air'}
    on_items = {int(k): v for k, v in s.items() if k.lstrip('-').isdigit() and int(k) in byid}
    other = {k: v for k, v in s.items() if not k.lstrip('-').isdigit()}
    it['on_items'] = sorted(on_items, key=on_items.get, reverse=True)
    it['parent'] = it['on_items'][0] if on_items and sum(on_items.values()) >= sum(other.values()) else None
uf = {i: i for i in byid}
def find(x):
    while uf[x] != x: uf[x] = uf[uf[x]]; x = uf[x]
    return x
for it in items:
    if it['parent'] is not None:
        for o in it['on_items'][:2]: uf[find(it['id'])] = find(o)
members = collections.defaultdict(list)
for i in byid: members[find(i)].append(i)
st = []
for ids in members.values():
    bases = [i for i in ids if byid[i]['parent'] is None] or ids
    base = max(bases, key=lambda i: area(byid[i]['hull']))
    st.append({'ids': ids, 'base': base, 'cat': CATS[byid[base]['g']], 'hull': hull_of([p for i in ids for p in byid[i]['hull']])})

# ---- 2. grids ----
S = raster([o['hull'] for o in obs if o['k'] in ('struct', 'wall')])
P = raster([o['hull'] for o in obs if o['k'] == 'prop'])
road = np.zeros((NU, NV), bool)
for tris in ext['roads']:
    for t in tris:
        r = poly_mask([[t[0], t[1]], [t[2], t[3]], [t[4], t[5]]])
        if r is not None: road[r[0]] |= r[1]
road = ndi.binary_closing(road, iterations=2)
terr, surf = ground_grids(gr)
interior = Path([(-150, -55.5), (134, -55.5), (134, 29.5), (24, 29.5), (24, 49.5), (-150, 49.5)]).contains_points(
    np.c_[GU.ravel(), GV.ravel()]).reshape(NU, NV)

Ssolid = S.copy()
for b in B: Ssolid |= rect_mask(*b['r'])
# equipment on legs or made of many pipes: fill the whole footprint, nothing goes between the legs
EQmask = raster([e['hull'] for e in EQ if '出入口' not in e['g']], pad=0.15)
EQmask = ndi.binary_closing(EQmask, structure=np.ones((3, 3)), iterations=7)   # pieces under ~3 m apart read as one machine
lab, nlab = ndi.label(EQmask)
for k in range(1, nlab + 1):   # and no props in the notches between them either
    ij = np.argwhere(lab == k)
    EQmask |= raster([hull_of(np.c_[GU[ij[:, 0], ij[:, 1]], GV[ij[:, 0], ij[:, 1]]])])
ENmask = raster([e['hull'] for e in EQ if '出入口' in e['g']], pad=0.15)
Ssolid |= EQmask | ENmask

# ---- 3. what already stands against a wall stays (unless it is in front of a door) ----
dS0 = ndi.distance_transform_edt(~S) * RES
for s in st:
    (sa, sb), sub = poly_mask(s['hull'])
    s['dmin'] = float(dS0[sa, sb][sub].min()) if sub.any() else 99
    s['anch'] = s['dmin'] <= 0.8
for _ in range(2):   # a second row right behind an anchored stack also counts
    dA = ndi.distance_transform_edt(~raster([s['hull'] for s in st if s['anch']])) * RES
    for s in st:
        if s['anch']: continue
        (sa, sb), sub = poly_mask(s['hull'])
        if sub.any() and dA[sa, sb][sub].min() <= 0.5: s['anch'] = True

# door keep-outs: 4.5 m in front of every ground-level door, 0.8 m to each side
K = np.zeros((NU, NV), bool)
for d in ext['doors']:
    u0, u1, v0, v1, y = d
    if y > -429.0: continue
    cu, cv = (u0 + u1) / 2, (v0 + v1) / 2
    host = min(B, key=lambda b: max(b['r'][0] - cu, cu - b['r'][1], 0) + max(b['r'][2] - cv, cv - b['r'][3], 0))
    bu, bv = (host['r'][0] + host['r'][1]) / 2, (host['r'][2] + host['r'][3]) / 2
    if (u1 - u0) < (v1 - v0):   # door plane spans v -> faces along u
        sg = 1 if cu > bu else -1; face = host['r'][1] if sg > 0 else host['r'][0]
        K |= rect_mask(min(face, face + sg * 4.5), max(face, face + sg * 4.5), v0 - 0.8, v1 + 0.8)
    else:
        sg = 1 if cv > bv else -1; face = host['r'][3] if sg > 0 else host['r'][2]
        K |= rect_mask(u0 - 0.8, u1 + 0.8, min(face, face + sg * 4.5), max(face, face + sg * 4.5))
K |= rect_mask(118, 136, -34, 4)   # gate forecourt

road_near = ndi.binary_dilation(road, iterations=3)
for s in st:
    r = poly_mask(s['hull'])
    s['blocks_door'] = bool(r is not None and (K[r[0]] & r[1]).sum() * RES * RES > 0.15)
    s['on_road'] = bool(r is not None and (road_near[r[0]] & r[1]).sum() * RES * RES > 0.15)
    if s['blocks_door'] or s['on_road']: s['anch'] = False
anchored = [s for s in st if s['anch']]
loose = [s for s in st if not s['anch']]
print(f'{len(items)} items, {len(st)} stacks, {len(loose)} to move ({sum(s["blocks_door"] for s in st)} in front of doors, {sum(s["on_road"] for s in st)} on roads)')

Amask = raster([s['hull'] for s in anchored])
roadK = ndi.binary_dilation(road, iterations=6)
occ = Ssolid | ndi.binary_dilation(P, iterations=2) | roadK | K | ~interior | ndi.binary_dilation(Amask, iterations=1) | np.isnan(surf)
blockers = Ssolid | P | Amask          # a walkway in front of a group must not hit these
dS, inds = ndi.distance_transform_edt(~Ssolid, return_indices=True)
dS = dS * RES

# what each solid cell is: 1 building, 2 perimeter wall, 3 equipment, 4 entrance/gate (never lean on)
kind = np.zeros((NU, NV), np.int8)
kind[raster([o['hull'] for o in obs if o['k'] == 'wall'])] = 2
kind[raster([o['hull'] for o in obs if o['k'] == 'struct' and o['g'] in ('料仓与储罐', '管道', '煤堆')])] = 3
for b in B: kind[rect_mask(*b['r'])] = 1
kind[EQmask] = 3
kind[raster([o['hull'] for o in obs if o['k'] == 'struct' and o['g'] in ('出入口', 'FactoryGate', 'Guard Post')]) | ENmask] = 4
kind[Ssolid & (kind == 0)] = 1
PREF = {  # extra cost per stack for leaning on building / wall / equipment
    'pallet': {1: 0.6, 2: 0.0, 3: 2.5}, 'box': {1: 0.4, 2: 0.0, 3: 2.5},
    'drum': {1: 1.2, 2: 1.0, 3: 0.0}, 'cyl': {1: 0.0, 2: 0.6, 3: 1.5},
    'cart': {1: 0.0, 2: 0.5, 3: 1.0}, 'ladder': {1: 0.0, 2: 0.6, 3: 1.2}, 'bin': {1: 0.0, 2: 0.4, 3: 2.0}}

# yard regions split by the main road; a stack never crosses the road
split = rect_mask(-125, -116, 1.6, 3.6)    # between the end of the main road and the west shed the two sides meet; cut them apart
region, _ = ndi.label(interior & ~roadK & ~Ssolid & ~split)
ri = ndi.distance_transform_edt(region == 0, return_distances=False, return_indices=True)
def region_at(u, v):
    i, j = cell(u, v); i = min(max(i, 0), NU - 1); j = min(max(j, 0), NV - 1)
    return region[ri[0][i, j], ri[1][i, j]]


# ---- 4. footprints of the stacks to move ----
def tilt_deg(e):
    x, z = math.radians(e[0]), math.radians(e[2])
    return math.degrees(math.acos(max(-1, min(1, math.cos(x) * math.cos(z)))))

UPRIGHT = {'SM_Ledder': (0.73, 1.07), 'SM_StepLift': (0.69, 0.91), 'SM_GasCilinder': (0.30, 0.30)}

def local_mask(poly):
    p = np.asarray(poly)
    ra = int(math.ceil(np.abs(p[:, 0]).max() / RES)) + 1; rb = int(math.ceil(np.abs(p[:, 1]).max() / RES)) + 1
    lu, lv = np.meshgrid(np.arange(-ra, ra + 1) * RES, np.arange(-rb, rb + 1) * RES, indexing='ij')
    return ra, rb, Path(p).contains_points(np.c_[lu.ravel(), lv.ravel()], radius=1e-6).reshape(lu.shape)

for s in loose:
    base = byid[s['base']]
    s['standup'] = []   # single fallen-over ladders and cylinders are stood back up
    if len(s['ids']) == 1 and s['cat'] in ('cyl', 'ladder') and 4 < tilt_deg(base['e']) < 60:
        s['standup'] = [s['base']]
    if s['standup']:
        a, b = next(v for k, v in UPRIGHT.items() if base['n'].startswith(k))
        if s['cat'] == 'ladder':
            h = np.array([[-a / 2, -b / 2], [a / 2, -b / 2], [a / 2, b / 2], [-a / 2, b / 2]])
        else:
            h = np.array([[0.2 * math.cos(k * math.pi / 6), 0.2 * math.sin(k * math.pi / 6)] for k in range(12)])
        s['rect'] = (0, 0.0, a, b, np.mean(s['hull'], 0)); s['poly0'] = h
    else:
        s['rect'] = min_rect(s['hull']); s['poly0'] = rot(s['hull'], -s['rect'][1], s['rect'][4])
    first = byid[s['ids'][0]]['n'].lower()
    round_ = s['cat'] == 'cyl' or first.startswith(('sm_barrel', 'sm_plastic_barrel', 'barrelgasbag'))
    s['jit'] = 0.0 if round_ else math.radians(random.uniform(-3, 3))
    s['variants'] = {}
    for along in ('u', 'v'):   # long side along the wall
        p = s['poly0']; w, d = np.ptp(p[:, 0]), np.ptp(p[:, 1])
        extra = 0.0 if (w >= d) == (along == 'u') else math.pi / 2
        q = rot(p, extra + s['jit'], np.zeros(2))
        ra, rb, m = local_mask(q)
        s['variants'][along] = (extra, q, ra, rb, m, np.ptp(q[:, 0]) if along == 'u' else np.ptp(q[:, 1]),
                                np.ptp(q[:, 1]) if along == 'u' else np.ptp(q[:, 0]))
    s['c0'] = np.asarray(s['rect'][4], float)
    s['region'] = region_at(*s['c0'])

MAXROWS = {'cyl': 2, 'drum': 2, 'pallet': 2, 'box': 2, 'bin': 1, 'cart': 1, 'ladder': 1}
GAP = {'cyl': (0.04, 0.10), 'drum': (0.08, 0.25), 'pallet': (0.15, 0.35), 'box': (0.10, 0.30),
       'bin': (0.3, 0.6), 'cart': (0.4, 0.8), 'ladder': (0.3, 0.6)}
WALKWAY = [3.0]
placed_mask = np.zeros((NU, NV), bool)
placed_by_cat = {c: np.zeros((NU, NV), bool) for c in PREF}
cat_mask = {c: raster([s['hull'] for s in anchored if s['cat'] == c]) for c in PREF}


def fits(s, along, ci, cj, tmp):
    extra, q, ra, rb, m, W, D = s['variants'][along]
    a0, b0 = ci - ra, cj - rb
    if a0 < 0 or b0 < 0 or a0 + m.shape[0] > NU or b0 + m.shape[1] > NV: return False
    sl = (slice(a0, a0 + m.shape[0]), slice(b0, b0 + m.shape[1]))
    if (occ[sl] & m).any() or (placed_mask[sl] & m).any() or (tmp[sl] & m).any(): return False
    hs = surf[sl][m]
    return hs.size > 0 and np.nanmax(hs) - np.nanmin(hs) <= 0.15


def backed(cu, cv, n, t, W, D):
    """front-row pieces must have a wall or machine right behind them, at both ends"""
    for off in (-W / 2 + 0.1, W / 2 - 0.1):
        ok = False
        for dist in (0.1, 0.3, 0.5, 0.7):
            i, j = cell(cu - n[0] * (D / 2 + dist) + t[0] * off, cv - n[1] * (D / 2 + dist) + t[1] * off)
            if 0 <= i < NU and 0 <= j < NV and Ssolid[i, j]: ok = True; break
        if not ok: return False
    return True


def corridor_ok(cu, cv, n, t, W, D, extra_block):
    for off in (-W / 2 + 0.15, 0.0, W / 2 - 0.15):
        for dist in np.arange(0.2, WALKWAY[0] + 0.01, 0.2):
            i, j = cell(cu + n[0] * (D / 2 + dist) + t[0] * off, cv + n[1] * (D / 2 + dist) + t[1] * off)
            if not (0 <= i < NU and 0 <= j < NV): return False
            if blockers[i, j] or placed_mask[i, j] or extra_block[i, j]: return False
    return True


def stamp(s, along, ci, cj, target):
    extra, q, ra, rb, m, W, D = s['variants'][along]
    a0, b0 = ci - ra - 1, cj - rb - 1
    md = ndi.binary_dilation(np.pad(m, 1), iterations=1)
    ia, ib = max(a0, 0), max(b0, 0); ja, jb = min(a0 + md.shape[0], NU), min(b0 + md.shape[1], NV)
    target[ia:ja, ib:jb] |= md[ia - a0:ja - a0, ib - b0:jb - b0]


# wall-side start cells with the outward normal snapped to a factory axis
starts = []
for i, j in np.argwhere((dS > 0.0) & (dS <= 0.3) & ~occ)[::2]:
    si, sj = inds[0][i, j], inds[1][i, j]
    if kind[si, sj] == 4: continue
    du, dv = i - si, j - sj
    n = (1.0 if du > 0 else -1.0, 0.0) if abs(du) >= abs(dv) else (0.0, 1.0 if dv > 0 else -1.0)
    starts.append((i, j, n, int(kind[si, sj]), region[i, j]))
starts_arr = np.array([[GU[s[0], s[1]], GV[s[0], s[1]]] for s in starts])

# ---- 5. groups: same kind, same side of the road, within 20 m; runs of at most ~16 m ----
groups = []
for cat in ['pallet', 'box', 'drum', 'cyl', 'cart', 'bin', 'ladder']:
    ss = [s for s in loose if s['cat'] == cat]
    if not ss: continue
    cs = np.array([s['c0'] for s in ss]); lab = list(range(len(ss)))
    def fl(x):
        while lab[x] != x: lab[x] = lab[lab[x]]; x = lab[x]
        return x
    for a in range(len(ss)):
        for b in range(a + 1, len(ss)):
            if ss[a]['region'] == ss[b]['region'] and np.hypot(*(cs[a] - cs[b])) < 20: lab[fl(a)] = fl(b)
    cl = collections.defaultdict(list)
    for k in range(len(ss)): cl[fl(k)].append(ss[k])
    for g in cl.values():
        pts = np.array([s['c0'] for s in g]); ax_ = np.linalg.svd(pts - pts.mean(0))[2][0] if len(g) > 1 else np.array([1, 0])
        g.sort(key=lambda s: float(np.dot(s['c0'], ax_)))
        chunk, width = [], 0
        for s in g:
            w = s['variants']['u'][5]
            if chunk and width + w > 16: groups.append((cat, chunk)); chunk, width = [], 0
            chunk.append(s); width += w + 0.3
        if chunk: groups.append((cat, chunk))
for cat, g in groups:
    g.sort(key=lambda s: -s['variants']['u'][5] * s['variants']['u'][6])
groups.sort(key=lambda g: -sum(s['variants']['u'][5] * s['variants']['u'][6] for s in g[1]))


def try_group(cat, g, start, tdir):
    i0, j0, n, akind, reg = start
    n = np.array(n); t = np.array([-n[1], n[0]]) * tdir
    along = 'u' if abs(t[0]) > 0.5 else 'v'
    p0 = np.array([GU[i0, j0], GV[i0, j0]]) - n * dS[i0, j0]
    tmp = np.zeros((NU, NV), bool)
    res, rows, skip = [], 1, 0.0
    row_off, row_depth, s_pos = 0.12, 0.0, 0.0
    limit = sum(s['variants'][along][5] for s in g) * 1.4 + 3
    lo, hi = GAP[cat]
    rng = random.Random(hash((i0, j0, tdir)) & 0xffff)
    for s in g:
        extra, q, ra, rb, m, W, D = s['variants'][along]
        while True:
            if s_pos + W > limit:
                if rows >= MAXROWS[cat] or row_depth == 0: return None
                rows += 1; row_off += row_depth + 0.25; row_depth = 0.0; s_pos = 0.0
                continue
            ci, cj = cell(*(p0 + n * (row_off + D / 2) + t * (s_pos + W / 2)))
            if 0 <= ci < NU and 0 <= cj < NV and fits(s, along, ci, cj, tmp) and \
               (rows > 1 or backed(GU[ci, cj], GV[ci, cj], n, t, W, D)) and \
               corridor_ok(GU[ci, cj], GV[ci, cj], n, t, W, D, tmp):
                res.append((s, along, ci, cj, n, t)); stamp(s, along, ci, cj, tmp)
                s_pos += W + rng.uniform(lo, hi); row_depth = max(row_depth, D)
                break
            s_pos += 0.2; skip += 0.2
    travel = sum(np.hypot(GU[ci, cj] - s['c0'][0], GV[ci, cj] - s['c0'][1]) for s, _, ci, cj, _, _ in res)
    near_same = ndi.binary_dilation(tmp, iterations=10) & (cat_mask[cat] | placed_by_cat[cat])
    cohesion = min(near_same.sum() * RES * RES, 4.0)
    return 0.12 * travel + 4 * (rows - 1) + 0.8 * skip + PREF[cat][akind] * len(g) - 1.5 * cohesion, res


plan, failed = [], []
queue = list(groups)
while queue:
    cat, g = queue.pop(0)
    gc = np.mean([s['c0'] for s in g], 0)
    retry = g[0].get('retry', False)
    WALKWAY[0] = 2.0 if retry else 3.0
    best = None
    for k in np.where(np.hypot(*(starts_arr - gc).T) < (80 if retry else 35))[0]:
        if starts[k][4] != g[0]['region']: continue
        for tdir in (1, -1):
            r = try_group(cat, g, starts[k], tdir)
            if r and (best is None or r[0] < best[0]): best = r
    if best is None:
        if len(g) > 1: queue[0:0] = [(cat, [s]) for s in g]       # place the pieces one by one
        elif not retry: g[0]['retry'] = True; queue.append((cat, g))  # search farther, 2 m walkway
        else: failed.append(byid[g[0]['base']]['n'])
        continue
    for (s, along, ci, cj, n, t) in best[1]:
        stamp(s, along, ci, cj, placed_mask); stamp(s, along, ci, cj, placed_by_cat[cat])
        extra, q, ra, rb, m, W, D = s['variants'][along]
        plan.append({'ids': s['ids'], 'base': s['base'], 'cat': cat, 'standup': s['standup'],
                     'from': [float(s['c0'][0]), float(s['c0'][1])], 'to': [float(GU[ci, cj]), float(GV[ci, cj])],
                     'dyaw': math.degrees((0.0 if s['standup'] else -s['rect'][1]) + extra + s['jit']),
                     'poly': (q + [GU[ci, cj], GV[ci, cj]]).tolist(), 'n': list(map(float, n))})

tr = [np.hypot(p['to'][0] - p['from'][0], p['to'][1] - p['from'][1]) for p in plan]
print(f'moved {len(plan)} stacks / {sum(len(p["ids"]) for p in plan)} items, travel mean {np.mean(tr):.1f} max {np.max(tr):.1f}; kept in place: {failed}')
json.dump({'plan': plan, 'kept': failed}, open(os.path.join(HERE, 'plan.json'), 'w', encoding='utf-8'), ensure_ascii=False)
np.savez_compressed(os.path.join(SURVEY, 'plan_masks.npz'), K=K, Ssolid=Ssolid, road=road, P=P)

# ---- 6. world-space instructions for apply.cs ----
rng = random.Random(11)
rows = []
for p in plan:
    fx, fz = to_world(*p['from']); tx, tz = to_world(*p['to'])
    nx = AXIS_U[0] * p['n'][0] + AXIS_V[0] * p['n'][1]; nz = AXIS_U[1] * p['n'][0] + AXIS_V[1] * p['n'][1]
    syaw = None
    if p['standup']:
        if p['cat'] == 'ladder':   # yaw 47 puts the ladder's long local Z along u
            syaw = (47.0 if abs(p['n'][1]) > 0.5 else 137.0) + rng.choice((0, 180)) + rng.uniform(-3, 3)
        else:
            syaw = rng.uniform(0, 360)
    rows.append({'base': p['base'], 'ids': p['ids'], 'dyaw': round(p['dyaw'], 3), 'from': [round(fx, 3), round(fz, 3)],
                 'to': [round(tx, 3), round(tz, 3)], 'normal': [round(nx, 4), round(nz, 4)],
                 'standup': p['standup'][0] if p['standup'] else None, 'standupYaw': None if syaw is None else round(syaw, 2)})
json.dump(rows, open(os.path.join(HERE, 'apply_plan.json'), 'w'), indent=0)
