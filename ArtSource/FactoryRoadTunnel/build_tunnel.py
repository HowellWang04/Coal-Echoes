import json
import math
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
OUTPUT = ROOT / 'Assets/Art Resources/Environment/FactoryRoadTunnel'
OUTPUT.mkdir(parents=True, exist_ok=True)
class Obj:
    def __init__(self):
        self.lines = []
        self.index = 1

    def group(self, name):
        self.lines.append('g ' + name)

    def face(self, points, normal=None, uv=None):
        # OBJ uses the mirrored X convention expected by Unity's model importer.
        points = np.asarray(points, dtype=float)
        if normal is None:
            normal = np.cross(points[1] - points[0], points[2] - points[0])
            normal /= np.linalg.norm(normal)
        if uv is None:
            axis = np.argmax(np.abs(normal))
            axes = [i for i in range(3) if i != axis]
            uv = points[:, axes] / 3.0
        normals = np.tile(normal, (len(points), 1)) if np.asarray(normal).ndim == 1 else normal
        for p, n, t in zip(points, normals, uv):
            self.lines.append(f'v {-p[0]:.5f} {p[1]:.5f} {p[2]:.5f}')
            self.lines.append(f'vt {t[0]:.5f} {t[1]:.5f}')
            self.lines.append(f'vn {-n[0]:.6f} {n[1]:.6f} {n[2]:.6f}')
        indices = list(range(self.index, self.index + len(points)))[::-1]
        self.lines.append('f ' + ' '.join(f'{i}/{i}/{i}' for i in indices))
        self.index += len(points)

    def box(self, minimum, maximum):
        x0, y0, z0 = minimum
        x1, y1, z1 = maximum
        self.face([(x0,y0,z0),(x0,y1,z0),(x1,y1,z0),(x1,y0,z0)])
        self.face([(x1,y0,z1),(x1,y1,z1),(x0,y1,z1),(x0,y0,z1)])
        self.face([(x0,y0,z1),(x0,y1,z1),(x0,y1,z0),(x0,y0,z0)])
        self.face([(x1,y0,z0),(x1,y1,z0),(x1,y1,z1),(x1,y0,z1)])
        self.face([(x0,y1,z0),(x0,y1,z1),(x1,y1,z1),(x1,y1,z0)])
        self.face([(x0,y0,z1),(x0,y0,z0),(x1,y0,z0),(x1,y0,z1)])

    def save(self, name):
        (OUTPUT / name).write_text('\n'.join(self.lines) + '\n')

portal = Obj()
portal.group('Concrete_Portal')
inner, outer, spring = 4.6, 5.5, 3.0
segments = 24
for i in range(segments):
    a0 = math.pi * i / segments + .001
    a1 = math.pi * (i+1) / segments - .001
    def p(radius, angle, z):
        return (radius * math.cos(angle), spring + radius * math.sin(angle), z)
    portal.face([p(inner,a0,-.9),p(inner,a1,-.9),p(outer,a1,-.9),p(outer,a0,-.9)])
    portal.face([p(inner,a1,4.8),p(inner,a0,4.8),p(outer,a0,4.8),p(outer,a1,4.8)])
    portal.face([p(inner,a0,1.4),p(inner,a1,1.4),p(inner,a1,-.9),p(inner,a0,-.9)])
    portal.face([p(outer,a0,-.9),p(outer,a1,-.9),p(outer,a1,4.8),p(outer,a0,4.8)])
    portal.face([p(inner,a0,-.9),p(outer,a0,-.9),p(outer,a0,1.4),p(inner,a0,1.4)])
    portal.face([p(outer,a1,-.9),p(inner,a1,-.9),p(inner,a1,1.4),p(outer,a1,1.4)])
portal.box((-5.5,-.65,-.9),(-4.6,3.03,1.4))
portal.box((4.6,-.65,-.9),(5.5,3.03,1.4))
for side in [-1,1]:
    # Sloped wing walls merge into the excavated shoulders.
    a = np.array([side*5.15,-.65,.35])
    b = np.array([side*8.8,-.65,-8.5])
    c = b + np.array([0,2.45,0])
    d = a + np.array([0,6.45,0])
    offset = np.array([side*.65,0,0])
    vertices = [a,b,c,d]
    if side < 0:
        vertices = vertices[::-1]
    portal.face(vertices)
    portal.face([v+offset for v in vertices[::-1]])
    portal.face([d,c,c+offset,d+offset])
    portal.face([b,a,a+offset,b+offset])
    portal.face([a,d,d+offset,a+offset])
    portal.face([c,b,b+offset,c+offset])
portal.save('ConcreteTunnelPortal.obj')

# A short apron covers the cell edges at the native Terrain hole.
apron = Obj()
apron.group('Entrance_Apron')
apron.box((-5.6,-.18,-2.6),(5.6,-.035,.15))
apron.save('TunnelEntranceApron.obj')

# Only the overhang around the Terrain opening needs a separate rock mesh.
collar = Obj()
collar.group('Rock_Collar')
collar_heights = json.loads((SOURCE / 'collar-heights.json').read_text())
def arch(x):
    return 3 + math.sqrt(max(0, 5.5**2 - x*x)) if abs(x) < 5.5 else -.2
def crown(x, z):
    native = collar_heights[round((z+.1)/.5)][round((x+8)/.25)]
    cover = 3 + math.sqrt(max(0,6.1**2-x*x)) if abs(x)<6.1 else -.2
    return max(native-.04, cover+.05)
for i in range(64):
    x0, x1 = -8+i*.25, -8+(i+1)*.25
    collar.face([(x0,arch(x0),-.1),(x0,crown(x0,0),-.1),(x1,crown(x1,0),-.1),(x1,arch(x1),-.1)],
                uv=np.array([(x0/20,arch(x0)/20),(x0/20,crown(x0,0)/20),(x1/20,crown(x1,0)/20),(x1/20,arch(x1)/20)]))
    for j in range(12):
        z0, z1 = -.1+j*.5, -.1+(j+1)*.5
        collar.face([(x0,crown(x0,z0),z0),(x0,crown(x0,z1),z1),(x1,crown(x1,z1),z1),(x1,crown(x1,z0),z0)],
                    uv=np.array([(x0/20,z0/20),(x0/20,z1/20),(x1/20,z1/20),(x1/20,z0/20)]))
collar.save('TunnelRockCollar.obj')

interior = Obj()
profile = [(-4.6,0),(-4.6,3)] + [(4.6*math.cos(a),3+4.6*math.sin(a)) for a in np.linspace(math.pi,0,33)[1:]] + [(4.6,0)]
for section in range(12):
    interior.group(f'Interior_{section:02}')
    z0, z1 = section * 1.5, (section+1)*1.5
    for (x0,y0),(x1,y1) in zip(profile[:-1],profile[1:]):
        interior.face([(x0,y0,z0),(x1,y1,z0),(x1,y1,z1),(x0,y0,z1)], uv=np.array([(0,z0/3),(1,z0/3),(1,z1/3),(0,z1/3)]))
    interior.face([(-4.6,0,z0),(-4.6,0,z1),(4.6,0,z1),(4.6,0,z0)])
interior.group('Black_End')
interior.face([(-4.62,-.05,18),(-4.62,7.65,18),(4.62,7.65,18),(4.62,-.05,18)])
interior.save('TunnelInterior.obj')
print(json.dumps({'output': str(OUTPUT)}))
