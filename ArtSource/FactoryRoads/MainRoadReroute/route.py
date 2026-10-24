"""Replaces the hairpin loop at the west end of the factory road with one gently curving main road and five branches.

Factory frame as in ArtSource/FactoryYardLayout (u along the main road, v across). Writes route.json for apply.cs
and route-plan.png. Run: python route.py (needs numpy, scipy, matplotlib).
"""
import json, math, os, sys
OUT = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(OUT, '..', '..', 'FactoryYardLayout'))
from yard_common import *

LANE_OFFSET = 3.125          # the two existing 8.5 m lanes sit 6.25 m apart
MAIN_HALF = LANE_OFFSET + 8.5 / 2
STRAIGHT_V = -10.825         # centre of the existing straight
JOIN_U = -30.0               # new knots start here; everything east of it stays as it was
BRANCH_WIDTH = 5.5

# main road centre: leaves the straight between the brick sheds, S-bends through the old loop, stops at the west shed
MAIN = [(-20, STRAIGHT_V), (JOIN_U, STRAIGHT_V), (-41, -10.2), (-50, -7.0), (-59, -2.5), (-69, 2.2), (-80, 4.6),
        (-91, 4.4), (-101, 2.6), (-110, 2.0), (-119.5, 2.6)]
# branches start on the main road centre (their start fades out under it) and stop short of the shed fronts
BRANCHES = {
    '支路 至 ConcreteHouse_Pack_2_C_9': [(-90.5, 4.4), (-92.5, 12), (-95, 19), (-96.5, 25.2)],
    '支路 至 ConcreteHouse_Pack_2_C_0': [(-66.5, 1.0), (-66, 9), (-64.5, 16), (-62.5, 21.8)],   # between the steel beam pile and the fences
    '支路 至 ConcreteHouse_Pack_1_C_2': [(-82.5, 4.6), (-83.5, -6), (-84.5, -18), (-85.5, -31.2)],
    '支路 至 BrickHouse_1_C': [(-54.5, -5.3), (-56.5, -12), (-58, -19.5)],
    '支路 至料仓': [(-100, 2.7), (-108, -1.5), (-114.5, -6.5), (-118, -10.5)],
}


def catmull(points, step=0.5):
    """centripetal Catmull-Rom through the points, sampled every ~step metres"""
    p = np.asarray(points, float)
    p = np.vstack([2 * p[0] - p[1], p, 2 * p[-1] - p[-2]])
    out = []
    for i in range(1, len(p) - 2):
        p0, p1, p2, p3 = p[i - 1], p[i], p[i + 1], p[i + 2]
        t0 = 0; t1 = t0 + np.linalg.norm(p1 - p0) ** .5; t2 = t1 + np.linalg.norm(p2 - p1) ** .5; t3 = t2 + np.linalg.norm(p3 - p2) ** .5
        n = max(2, int(np.linalg.norm(p2 - p1) / step))
        for t in np.linspace(t1, t2, n, endpoint=False):
            a1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1
            a2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2
            a3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3
            b1 = (t2 - t) / (t2 - t0) * a1 + (t - t0) / (t2 - t0) * a2
            b2 = (t3 - t) / (t3 - t1) * a2 + (t - t1) / (t3 - t1) * a3
            out.append((t2 - t) / (t2 - t1) * b1 + (t - t1) / (t2 - t1) * b2)
    out.append(p[-2])
    return np.array(out)


def tangents(c):
    t = np.gradient(c, axis=0)
    return t / np.linalg.norm(t, axis=1, keepdims=True)


def offset(c, d):
    t = tangents(c)
    n = np.c_[-t[:, 1], t[:, 0]]
    return c + n * d


def knots(poly, spacing):
    """Bezier knots every ~spacing metres with tangents along the curve (one third of the neighbouring span)"""
    seg = np.r_[0, np.cumsum(np.linalg.norm(np.diff(poly, axis=0), axis=1))]
    count = max(2, int(round(seg[-1] / spacing)) + 1)
    at = np.linspace(0, seg[-1], count)
    idx = [int(np.argmin(abs(seg - a))) for a in at]
    t = tangents(poly)
    out = []
    for k, i in enumerate(idx):
        lin = (seg[i] - seg[idx[k - 1]]) / 3 if k > 0 else 0
        lout = (seg[idx[k + 1]] - seg[i]) / 3 if k < len(idx) - 1 else 0
        out.append({'p': poly[i].tolist(), 'tin': (-t[i] * lin).tolist(), 'tout': (t[i] * lout).tolist()})
    return out


def world(k):
    """factory-frame knot -> world XZ position and XZ tangents"""
    to_xz = lambda d: [AXIS_U[0] * d[0] + AXIS_V[0] * d[1], AXIS_U[1] * d[0] + AXIS_V[1] * d[1]]
    return {'p': list(to_world(*k['p'])), 'tin': to_xz(k['tin']), 'tout': to_xz(k['tout'])}


centre = catmull(MAIN)
centre = centre[centre[:, 0] <= JOIN_U + 1e-6]       # new part only
# lane '向内' runs gate -> west on the north side; lane '向外' runs west -> gate on the south side
lane_in = offset(centre, LANE_OFFSET)    # left of the westward direction = north (smaller v)
lane_out = offset(centre, -LANE_OFFSET)[::-1]
if lane_in[:, 1].mean() > lane_out[:, 1].mean(): lane_in, lane_out = offset(centre, -LANE_OFFSET), offset(centre, LANE_OFFSET)[::-1]
branches = {name: catmull(pts) for name, pts in BRANCHES.items()}

route = {
    'laneIn': [world(k) for k in knots(lane_in, 9)],     # appended after the straight's eastern knots
    'laneOut': [world(k) for k in knots(lane_out, 9)],   # placed before the straight's eastern knots
    'branches': {n: [world(k) for k in knots(c, 6)] for n, c in branches.items()},
    'joinU': JOIN_U, 'branchWidth': BRANCH_WIDTH,
}

# ---- things standing on the new roads ----
B = survey('buildings.json'); obs = survey('obstacles.json'); EQ = survey('equip.json')
dm = ndi.distance_transform_edt(~raster([np.r_[offset(centre, MAIN_HALF), offset(centre, -MAIN_HALF)[::-1]].tolist()])) * RES
db = ndi.distance_transform_edt(~raster([np.r_[offset(c, BRANCH_WIDTH / 2), offset(c, -BRANCH_WIDTH / 2)[::-1]].tolist() for c in branches.values()])) * RES
droad = np.minimum(dm, db)
solid = raster([o['hull'] for o in obs if o['k'] in ('struct', 'wall')]) | raster([e['hull'] for e in EQ], pad=0.5)
for b in B: solid |= rect_mask(*b['r'])
dsolid = ndi.distance_transform_edt(~solid) * RES

# dead trees: move each one off the road to the nearest free spot 2.5-6 m beyond the road edge, 5 m apart
trees = {'枯树 01': (-91.6, 5.6), '枯树 02': (-69.6, 2.5), '枯树 03': (-61.3, 4.0), '枯树 04': (-80.3, 0.2),
         '枯树 05': (-98.2, 1.5), '枯树 06': (-74.8, 7.9), '枯树 07': (-84.4, 7.0)}
taken = []
moves = {}
ok = (droad >= 2.5) & (droad <= 6.0) & (dsolid >= 3.0)
for name, (u, v) in sorted(trees.items(), key=lambda kv: kv[1][0]):
    d = np.hypot(GU - u, GV - v)
    cand = ok.copy()
    for (tu, tv) in taken: cand &= np.hypot(GU - tu, GV - tv) >= 5.0
    d[~cand] = np.inf
    i, j = np.unravel_index(np.argmin(d), d.shape)
    taken.append((GU[i, j], GV[i, j]))
    moves[name] = {'from': [u, v], 'to': [float(GU[i, j]), float(GV[i, j])], 'world': list(to_world(GU[i, j], GV[i, j]))}
route['trees'] = moves

# median lamps follow the new road every ~42 m like the straight; loop lamps go to the branch ends
def on_centre(u):
    i = int(np.argmin(abs(centre[:, 0] - u)))
    t = tangents(centre)[i]
    return centre[i], t
lamp_moves = {}
for name, u in [('SM_StreetLight13', -50.0), ('SM_StreetLight15', -92.0), ('SM_StreetLight16', -113.0)]:
    p, t = on_centre(u)
    lamp_moves[name] = {'to': list(to_world(*p)), 'roadDir': [AXIS_U[0] * t[0] + AXIS_V[0] * t[1], AXIS_U[1] * t[0] + AXIS_V[1] * t[1]]}
for name, bname, side in [('SM_StreetLight17', '支路 至 ConcreteHouse_Pack_2_C_9', 1), ('SM_StreetLight20', '支路 至 ConcreteHouse_Pack_2_C_0', 1),
                          ('SM_StreetLight22', '支路 至 ConcreteHouse_Pack_1_C_2', -1)]:
    c = branches[bname]; t = tangents(c)
    i = len(c) - 1 - int(6 / 0.5)     # 6 m before the branch end
    n = np.array([-t[i, 1], t[i, 0]]) * side
    p = c[i] + n * (BRANCH_WIDTH / 2 + 1.2)
    lamp_moves[name] = {'to': list(to_world(*p)), 'roadDir': [AXIS_U[0] * t[i, 0] + AXIS_V[0] * t[i, 1], AXIS_U[1] * t[i, 0] + AXIS_V[1] * t[i, 1]]}
route['lamps'] = lamp_moves

# signs: the one on the old north lane edge moves to the new edge; the one next to the north branch faces up the branch
p, t = on_centre(-74.8)
n = np.array([-t[1], t[0]]); n = n if n[1] < 0 else -n
# signs keep facing the direction of travel, as on the straight (westwards); the branch sign faces up the branch
route['signs'] = {'SM_StreetSign21': {'to': list(to_world(*(p + n * (MAIN_HALF + 1.2)))),
                                      'faceXZ': [AXIS_U[0] * t[0] + AXIS_V[0] * t[1], AXIS_U[1] * t[0] + AXIS_V[1] * t[1]]},
                  'SM_StreetSign22': {'to': None, 'faceXZ': [-AXIS_V[0], -AXIS_V[1]]}}

json.dump(route, open(os.path.join(OUT, 'route.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

# ---- preview ----
import matplotlib; matplotlib.use('Agg')
import matplotlib.pyplot as plt
plt.rcParams['font.sans-serif'] = ['Microsoft YaHei']
ext = survey('extra.json')
fig, ax = plt.subplots(figsize=(16, 10))
img = np.ones((NU, NV, 3))
oldroad = np.zeros((NU, NV), bool)
for tris in ext['roads']:
    for tr in tris:
        r = poly_mask([[tr[0], tr[1]], [tr[2], tr[3]], [tr[4], tr[5]]])
        if r is not None: oldroad[r[0]] |= r[1]
img[oldroad] = (.88, .88, .88); img[solid] = (.45, .45, .45)
img[dm < 0.01] = (.62, .55, .45); img[db < 0.01] = (.80, .70, .50)
ax.imshow(img.transpose(1, 0, 2), extent=(U0, U1, V1, V0), origin='upper')
for d in ext['doors']:
    if d[4] <= -429: ax.add_patch(plt.Rectangle((d[0], d[2]), d[1] - d[0], d[3] - d[2], fc='lime', ec='g', lw=.5))
ax.plot(centre[:, 0], centre[:, 1], 'k--', lw=.6)
for n, m in moves.items():
    ax.annotate('', m['to'], m['from'], arrowprops=dict(arrowstyle='->', color='darkgreen'))
    ax.plot(*m['to'], 'o', color='darkgreen'); ax.plot(*m['from'], 'x', color='darkgreen')
ax.set_xlim(-145, -10); ax.set_ylim(48, -50); ax.grid(True, lw=.2)
plt.savefig(os.path.join(OUT, 'route-plan.png'), dpi=90, bbox_inches='tight')
print('lane knots', len(route['laneIn']), len(route['laneOut']), 'branches', {n: len(k) for n, k in route['branches'].items()})
print('trees', {n: [round(x, 1) for x in m['to']] for n, m in moves.items()})
