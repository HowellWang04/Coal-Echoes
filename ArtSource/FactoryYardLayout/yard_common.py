"""Shared grid and geometry helpers for the factory yard layout (factory frame: u along the main road, v across)."""
import json, math, os
import numpy as np
from matplotlib.path import Path
from scipy import ndimage as ndi

HERE = os.path.dirname(os.path.abspath(__file__))
SURVEY = os.path.join(HERE, 'Survey')

RES = 0.2
U0, U1, V0, V1 = -150.0, 136.0, -58.0, 52.0
NU, NV = int(round((U1 - U0) / RES)), int(round((V1 - V0) / RES))
GU, GV = np.meshgrid(U0 + (np.arange(NU) + .5) * RES, V0 + (np.arange(NV) + .5) * RES, indexing='ij')

# world XZ of the factory frame origin and axes (same as export.cs)
ORIGIN = (-2320.0, -1830.0); AXIS_U = (0.7314, 0.682); AXIS_V = (0.682, -0.7314)

CATS = {'气瓶': 'cyl', '金属桶与桶堆': 'drum', '塑料桶与油壶': 'drum', '方形储液桶': 'drum',
        '托盘': 'pallet', '水泥袋与袋装托盘': 'pallet', '木箱与木制摆件': 'box', '纸箱': 'box',
        '垃圾容器': 'bin', '搬运车': 'cart', '梯子与作业平台': 'ladder'}


def survey(name):
    return json.load(open(os.path.join(SURVEY, name), encoding='utf-8'))


def to_world(u, v):
    return (ORIGIN[0] + AXIS_U[0] * u + AXIS_V[0] * v, ORIGIN[1] + AXIS_U[1] * u + AXIS_V[1] * v)


def cell(u, v):
    return int((u - U0) / RES), int((v - V0) / RES)


def poly_mask(hull, pad=0.0):
    h = np.asarray(hull, float)
    if pad:
        c = h.mean(0); d = h - c; n = np.linalg.norm(d, axis=1, keepdims=True) + 1e-9
        h = h + d / n * pad
    a0 = max(int((h[:, 0].min() - U0) / RES) - 1, 0); a1 = min(int((h[:, 0].max() - U0) / RES) + 2, NU)
    b0 = max(int((h[:, 1].min() - V0) / RES) - 1, 0); b1 = min(int((h[:, 1].max() - V0) / RES) + 2, NV)
    if a1 <= a0 or b1 <= b0:
        return None
    sub = Path(h).contains_points(np.c_[GU[a0:a1, b0:b1].ravel(), GV[a0:a1, b0:b1].ravel()], radius=1e-6)
    return (slice(a0, a1), slice(b0, b1)), sub.reshape(a1 - a0, b1 - b0)


def raster(hulls, pad=0.0):
    m = np.zeros((NU, NV), bool)
    for h in hulls:
        if len(h) < 3: continue
        r = poly_mask(h, pad)
        if r is None: continue
        (sa, sb), sub = r
        m[sa, sb] |= sub
    return m


def rect_mask(u0, u1, v0, v1):
    m = np.zeros((NU, NV), bool)
    a0, b0 = cell(max(u0, U0), max(v0, V0)); a1, b1 = cell(min(u1, U1 - 1e-3), min(v1, V1 - 1e-3))
    m[a0:a1 + 1, b0:b1 + 1] = True
    return m


def ground_grids(gr):
    nu, nv, R = gr['nu'], gr['nv'], gr['R']
    t = np.array([np.nan if x is None else x for x in gr['terr']], float).reshape(nu, nv)
    s = np.array([np.nan if x is None else x for x in gr['surf']], float).reshape(nu, nv)
    iu = np.clip(((GU - gr['U0']) / R).astype(int), 0, nu - 1); iv = np.clip(((GV - gr['V0']) / R).astype(int), 0, nv - 1)
    return t[iu, iv], s[iu, iv]


def hull_of(points):
    p = sorted(set(map(tuple, np.round(np.asarray(points, float), 4))))
    if len(p) < 3: return [list(x) for x in p]
    def cr(o, a, b): return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lo, up = [], []
    for q in p:
        while len(lo) >= 2 and cr(lo[-2], lo[-1], q) <= 0: lo.pop()
        lo.append(q)
    for q in reversed(p):
        while len(up) >= 2 and cr(up[-2], up[-1], q) <= 0: up.pop()
        up.append(q)
    return [list(x) for x in lo[:-1] + up[:-1]]


def area(h):
    h = np.asarray(h)
    return 0.5 * abs(np.dot(h[:, 0], np.roll(h[:, 1], 1)) - np.dot(h[:, 1], np.roll(h[:, 0], 1))) if len(h) > 2 else 0


def min_rect(h):
    """minimum-area rectangle: (area, angle, w, d, centre)"""
    h = np.asarray(h, float); best = None
    for i in range(len(h)):
        e = h[(i + 1) % len(h)] - h[i]
        if np.hypot(*e) < 1e-6: continue
        th = math.atan2(e[1], e[0]); c, s = math.cos(-th), math.sin(-th)
        r = h @ np.array([[c, s], [-s, c]])
        a = np.ptp(r[:, 0]) * np.ptp(r[:, 1])
        if best is None or a < best[0]:
            cen = np.array([(r[:, 0].max() + r[:, 0].min()) / 2, (r[:, 1].max() + r[:, 1].min()) / 2])
            cen = cen @ np.array([[c, -s], [s, c]])
            best = (a, th, np.ptp(r[:, 0]), np.ptp(r[:, 1]), cen)
    return best


def rot(pts, ang, c):
    ca, sa = math.cos(ang), math.sin(ang)
    p = np.asarray(pts, float) - c
    return np.c_[p[:, 0] * ca - p[:, 1] * sa, p[:, 0] * sa + p[:, 1] * ca]
