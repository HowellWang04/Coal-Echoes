"""Build the approved coal preparation plant gate in Blender 5.2.

blender --background --factory-startup --python ArtSource/FactoryGate/build_gate.py
Optional script arguments after -- are --skip-textures and --skip-render.
Outputs a source blend, game exports, PBR maps, renders, and a geometry report.
"""

from pathlib import Path
import argparse
import hashlib
import json
import math
import random
import sys

import bpy
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parent.parent
ASSET = ROOT / 'Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt'
TEXTURES = ASSET / 'Textures'
PREVIEWS = SOURCE / 'Previews'
sys.path.insert(0, str(SOURCE))
import texture_maps

rng = random.Random(1973)
groups = {}
materials = {}


class Mesh:
    def __init__(self):
        self.vertices, self.faces, self.uv = [], [], []

    def box(self, center, dimensions, uv_scale=1.0, jitter=0.0, offset=None):
        x, y, z = center
        a, b, c = (d / 2 for d in dimensions)
        v = [(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
             (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
        if jitter:
            v = [tuple(value + rng.uniform(-jitter, jitter) for value in p) for p in v]
        base = len(self.vertices)
        self.vertices.extend(v)
        faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(2,3,7,6),(3,0,4,7),(1,2,6,5)]
        off = offset or (rng.random(), rng.random())
        for k, face in enumerate(faces):
            self.faces.append(tuple(base + i for i in face))
            axes = (0,1) if k < 2 else ((0,2) if k < 4 else (1,2))
            self.uv.append([(v[i][axes[0]] * uv_scale + off[0], v[i][axes[1]] * uv_scale + off[1]) for i in face])

    def brick_face(self, center, width, height, axis):
        # Only the exposed face and its chamfer are needed over the solid mortar.
        u,v=Vector(axis),Vector((0,0,1))
        normal=u.cross(v)
        b=rng.uniform(.003,.009)
        w,h=width/2,height/2
        corners=[(-w+b,-h+b),(w-b,-h+b),(w-b,h-b),(-w+b,h-b),
                 (-w+b,-h),(w-b,-h),(w,-h+b),(w,h-b),(w-b,h),(-w+b,h),(-w,h-b),(-w,-h+b)]
        start=len(self.vertices)
        for i,(x,z) in enumerate(corners):
            p=Vector(center)+u*x+v*z-normal*(rng.uniform(0,.002) if i<4 else b)
            if i>=4:
                p+=u*rng.uniform(-.002,.002)+v*rng.uniform(-.002,.002)
            self.vertices.append(tuple(p))
        faces=[(0,1,2,3),(0,4,5,1),(1,6,7,2),(2,8,9,3),(3,10,11,0),
               (0,11,4),(1,5,6),(2,7,8),(3,9,10)]
        for face in faces:
            self.faces.append(tuple(start+i for i in face))
            self.uv.append([((self.vertices[start+i][0]+self.vertices[start+i][1])/9+.5,self.vertices[start+i][2]/4.5) for i in face])

    def object(self, name, mat, parent, bevel=0.0, segments=1, smooth=False):
        mesh = bpy.data.meshes.new(name)
        used=sorted({i for face in self.faces for i in face})
        remap={old:new for new,old in enumerate(used)}
        mesh.from_pydata([self.vertices[i] for i in used], [], [tuple(remap[i] for i in face) for face in self.faces])
        mesh.update()
        uv = mesh.uv_layers.new(name='UVMap')
        for poly, coords in zip(mesh.polygons, self.uv):
            for loop, co in zip(poly.loop_indices, coords):
                uv.data[loop].uv = co
        obj = bpy.data.objects.new(name, mesh)
        asset_collection.objects.link(obj)
        obj.parent = parent
        mesh.materials.append(materials[mat])
        if bevel:
            mod = obj.modifiers.new('Small physical edge bevel', 'BEVEL')
            mod.width, mod.segments = bevel, segments
            mod.affect = 'EDGES'
        if smooth:
            for poly in mesh.polygons:
                poly.use_smooth = True
            mod = obj.modifiers.new('Face weighted normals', 'WEIGHTED_NORMAL')
            mod.keep_sharp, mod.weight = True, 50
        return obj


def builder(group, mat):
    key = (group.name, mat)
    if key not in groups:
        groups[key] = (Mesh(), group)
    return groups[key][0]


def box(group, name, center, dims, mat='FramePaint', bevel=0.004):
    m = Mesh()
    m.box(center, dims)
    return m.object(name, mat, group, bevel, 2, True)


def empty(name, parent=None):
    o = bpy.data.objects.new(name, None)
    asset_collection.objects.link(o)
    o.parent = parent
    return o


def material(name):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.use_backface_culling = True
    n, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = n.get('Principled BSDF')
    bsdf.inputs['IOR'].default_value = 1.5
    for suffix, colorspace in (('BaseColor','sRGB'),('Normal','Non-Color'),('ORM','Non-Color')):
        source='Brick' if name=='BrickWeathered' and suffix!='BaseColor' else name
        image = bpy.data.images.load(str(TEXTURES / f'{source}_{suffix}.png'), check_existing=True)
        image.colorspace_settings.name = colorspace
        node = n.new('ShaderNodeTexImage')
        node.image, node.label = image, suffix
        node.location = (-700, {'BaseColor':300,'Normal':-200,'ORM':30}[suffix])
        node.interpolation = 'Linear'
        if suffix == 'BaseColor':
            links.new(node.outputs['Color'], bsdf.inputs['Base Color'])
        elif suffix == 'Normal':
            normal = n.new('ShaderNodeNormalMap')
            normal.location = (-280,-180)
            links.new(node.outputs['Color'], normal.inputs['Color'])
            links.new(normal.outputs['Normal'], bsdf.inputs['Normal'])
        else:
            split = n.new('ShaderNodeSeparateColor')
            split.location = (-280,40)
            links.new(node.outputs['Color'], split.inputs['Color'])
            links.new(split.outputs['Green'], bsdf.inputs['Roughness'])
            links.new(split.outputs['Blue'], bsdf.inputs['Metallic'])
    return mat


def tube(group, name, points, radius, mat='Iron', sides=8):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions, curve.resolution_u = '3D', 2
    curve.bevel_depth, curve.bevel_resolution = radius, 2 if sides > 8 else 1
    spline = curve.splines.new('POLY')
    spline.points.add(len(points)-1)
    for p, co in zip(spline.points, points):
        p.co = (*co, 1)
    curve.use_fill_caps = True
    obj = bpy.data.objects.new(name, curve)
    asset_collection.objects.link(obj)
    obj.parent = group
    curve.materials.append(materials[mat])
    return obj


def cylinder(group, name, p, radius, depth, mat='Iron', axis='Z', vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=p)
    o = bpy.context.object
    o.name, o.parent = name, group
    for col in list(o.users_collection):
        col.objects.unlink(o)
    asset_collection.objects.link(o)
    if axis == 'Y':
        o.rotation_euler.x = math.pi/2
    elif axis == 'X':
        o.rotation_euler.y = math.pi/2
    o.data.materials.append(materials[mat])
    mod = o.modifiers.new('Machined edge', 'BEVEL')
    mod.width, mod.segments = 0.0015, 1
    for face in o.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    return o


def brickwork(group, xmin, xmax, ymin, ymax, zmin, zmax, pillar):
    mortar = builder(group, 'Mortar')
    mortar.box(((xmin+xmax)/2,(ymin+ymax)/2,(zmin+zmax)/2), (xmax-xmin-0.012,ymax-ymin-0.012,zmax-zmin))
    rows = round((zmax-zmin) / 0.157)
    course = (zmax-zmin) / rows
    gap = 0.010
    # A staggered bond around all four exposed sides of each pier / wall.
    for row in range(rows):
        z = zmin + (row+0.5)*course
        dirt_chance=.85 if z<.85 or row>=rows-2 else (.40 if z<1.5 else .10)
        shift = 0.17 if row % 2 else 0
        x = xmin - shift
        while x < xmax - 0.005:
            end = min(x+0.34, xmax)
            start = max(x, xmin)
            if end-start > gap:
                for y,axis in ((ymin-.001,(1,0,0)),(ymax+.001,(-1,0,0))):
                    bricks=builder(group,'BrickWeathered' if rng.random()<dirt_chance else 'Brick')
                    bricks.brick_face(((start+end)/2,y,z),end-start-gap,course-gap,axis)
            x += 0.34
        y = ymin
        while y < ymax-.005:
            end = min(y+0.34, ymax)
            for x,axis in ((xmin-.001,(0,-1,0)),(xmax+.001,(0,1,0))):
                bricks=builder(group,'BrickWeathered' if rng.random()<dirt_chance else 'Brick')
                bricks.brick_face((x,(y+end)/2,z),end-y-gap,course-gap,axis)
            y += 0.34
    if not pillar:
        cap = builder(group,'Brick')
        for level in range(2):
            x = xmin
            while x < xmax:
                end = min(x+.32,xmax)
                cap.box(((x+end)/2,(ymin+ymax)/2,zmax+.045+level*.09), (end-x-.01,ymax-ymin+.07,.08),1/4.5,.003,offset=(.5,0))
                x += .32
        builder(group,'Concrete').box(((xmin+xmax)/2,(ymin+ymax)/2,zmax+.195),(xmax-xmin+.025,ymax-ymin+.085,.045),1/4.5,.005,offset=(.5,0))


def wall_whitewash(group, x0, x1, front):
    m = Mesh()
    # Eroded plaster bridges mortar seams with a shallow indentation over them.
    course=(3.46-.28)/round((3.46-.28)/.157)
    center=1.38 if x0<0 else 1.17
    local=random.Random(57 if x0<0 else 194)
    nx,nz=52,64
    for j in range(nz+1):
        for i in range(nx+1):
            x=x0+(x1-x0)*i/nx
            z=center-.48+j/nz*.96
            if 0<i<nx:
                x+=local.uniform(-.002,.002)
            z+=local.uniform(-.002,.002)
            seam=abs(((z-.28)/course+.5)%1-.5)*course
            relief=.003*math.exp(-(seam/.012)**2)
            y=(-1 if front else 1)*(.258-relief)
            m.vertices.append((x,y,z))
    boundary_cache={}
    def plaster_field(index):
        x,y,z=m.vertices[index]
        u=(x-x0)/(x1-x0)
        v=z-center
        seed=38 if x0<0 else 179
        patches=[(.12,.09,.57,.26),(.62,-.08,.43,.26),(.86,.28,.08,.043),(.37,-.38,.10,.038)] if x0<0 else [(.82,.10,.52,.25),(.27,-.08,.40,.22),(.10,.22,.085,.055),(.58,-.35,.09,.045)]
        shape=max(.10*(1-((u-cx)/rx)**2-((v-cz)/rz)**2) for cx,cz,rx,rz in patches)
        shape+=(float(texture_maps.surface_noise(u,v,17,seed))-.5)*.055
        shape+=(float(texture_maps.surface_noise(u,v,47,seed+1))-.5)*.024
        # Broad missing pieces and smaller exposed-brick islands break the band.
        notch=(.72,.12,.17,.12) if x0<0 else (.36,.12,.16,.13)
        cx,cz,rx,rz=notch
        shape=min(shape,.06*(((u-cx)/rx)**2+((v-cz)/rz)**2-1))
        pitting=float(texture_maps.surface_noise(u,v,33,seed+8))
        return min(shape,(.80-pitting)*.10)
    values=[plaster_field(i) for i in range(len(m.vertices))]
    for j in range(nz):
        for i in range(nx):
            a=j*(nx+1)+i
            for face in ((a,a+1,a+nx+2),(a,a+nx+2,a+nx+1)):
                clipped=[]
                for k,l in zip(face,face[1:]+face[:1]):
                    if values[k]>=0:
                        clipped.append(k)
                    if (values[k]>=0)!=(values[l]>=0):
                        key=tuple(sorted((k,l)))
                        if key not in boundary_cache:
                            t=values[k]/(values[k]-values[l])
                            p=Vector(m.vertices[k]).lerp(Vector(m.vertices[l]),t)
                            boundary_cache[key]=len(m.vertices)
                            m.vertices.append(tuple(p))
                        clipped.append(boundary_cache[key])
                if len(clipped)<3:
                    continue
                face=tuple(clipped)
                if not front:
                    face=tuple(reversed(face))
                m.faces.append(face)
                m.uv.append([(m.vertices[k][0]/1.1,m.vertices[k][2]/1.1) for k in face])
    return m.object('Old whitewash front' if front else 'Old whitewash rear','Whitewash',group)


def steel_sheet(group, x0, x1):
    m = Mesh()
    nx, nz = 40, 44
    atlas_offset=0 if x0<0 else .5
    z0,z1 = .23,3.045
    for j in range(nz+1):
        for i in range(nx+1):
            u,v = i/nx,j/nz
            x,z = x0+(x1-x0)*u,z0+(z1-z0)*v
            if j == 0:
                z += rng.uniform(-.022,.003)
            # A very slight sheet imperfection, never a swollen or soft panel.
            y = -.008 + .0025*math.sin(u*14)*math.sin(v*11)
            m.vertices.append((x,y,z))
    for j in range(nz):
        for i in range(nx):
            a=j*(nx+1)+i
            face=(a,a+1,a+nx+2,a+nx+1)
            m.faces.append(face)
            m.uv.append([(atlas_offset+i/nx*.5,j/nz),(atlas_offset+(i+1)/nx*.5,j/nz),
                         (atlas_offset+(i+1)/nx*.5,(j+1)/nz),(atlas_offset+i/nx*.5,(j+1)/nz)])
    o=m.object('Sheet steel panels','GatePaint',group)
    solid=o.modifiers.new('Steel sheet thickness 6 mm','SOLIDIFY')
    solid.thickness=.006
    bpy.context.view_layer.objects.active=o
    o.select_set(True)
    bpy.ops.object.modifier_apply(modifier=solid.name)
    seed=104 if x0<0 else 682
    # One boolean with closed polygonal cutters makes actual through holes.
    holes=texture_maps.corrosion_holes(seed)
    cutter=Mesh()
    for u,v,ru,rv,hseed in holes:
        local=random.Random(hseed)
        ring=[]
        for i in range(13):
            angle=i*2*math.pi/13
            r=local.uniform(.73,1.26)
            ring.append((x0+(u+ru*math.cos(angle)*r)*(x1-x0),z0+(v+rv*math.sin(angle)*r)*(z1-z0)))
        base=len(cutter.vertices)
        cutter.vertices.extend([(x,y,z) for y in (-.12,.12) for x,z in ring])
        cutter.faces.extend([tuple(base+i for i in range(13)),tuple(base+13+i for i in reversed(range(13)))])
        for i in range(13):
            j=(i+1)%13
            cutter.faces.append((base+i,base+13+i,base+13+j,base+j))
        cutter.uv.extend([[(0,0)]*len(f) for f in cutter.faces[len(cutter.uv):]])
    cut=cutter.object('Temporary corrosion cutters','GatePaint',group)
    mod=o.modifiers.new('Through corrosion openings','BOOLEAN')
    mod.operation,mod.solver,mod.object='DIFFERENCE','EXACT',cut
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cut,do_unlink=True)
    o['through_holes']=len(holes)
    for vert in o.data.vertices:
        u,v=(vert.co.x-x0)/(x1-x0),(vert.co.z-z0)/(z1-z0)
        for hu,hv,ru,rv,_ in holes:
            distance=((u-hu)/ru)**2+((v-hv)/rv)**2
            if .35<distance<1.8:
                vert.co.y+=.002*math.sin(math.atan2(v-hv,u-hu)*3+seed)
                break
    # Reset all faces to the same projected atlas including fresh cut walls.
    for poly in o.data.polygons:
        for loop in poly.loop_indices:
            co=o.data.vertices[o.data.loops[loop].vertex_index].co
            o.data.uv_layers.active.data[loop].uv=(atlas_offset+(co.x-x0)/(x1-x0)*.5,(co.z-z0)/(z1-z0))
    lifted_paint(group,x0,x1,seed)
    return o


def lifted_paint(group,x0,x1,seed):
    local=random.Random(seed+128)
    m=Mesh()
    count=0
    for _ in range(14000):
        u,v=local.uniform(.025,.975),local.uniform(.025,.975)
        field=float(texture_maps.damage_field(u,v,seed,False))
        if not -.013<field<.006 or abs(v-.414)<.03:
            continue
        dx=float(texture_maps.damage_field(u+.001,v,seed,False)-texture_maps.damage_field(u-.001,v,seed,False))
        dz=float(texture_maps.damage_field(u,v+.001,seed,False)-texture_maps.damage_field(u,v-.001,seed,False))
        direction=Vector((dx,0,dz)).normalized()
        across=Vector((-direction.z,0,direction.x))
        surface_y=-.008+.0025*math.sin(u*14)*math.sin(v*11)
        center=Vector((x0+u*(x1-x0),surface_y-.0002,.23+v*2.815))
        length=local.uniform(.008,.034)
        width=length*local.uniform(.5,1.2)
        lift=local.uniform(.0007,.0035)
        # Two anchored corners and three lifted irregular tips, with real thickness.
        points=[center-across*width*.5-direction*length*.35,
                center+across*width*.5-direction*length*.35,
                center+across*width*.43+direction*length*.38+Vector((0,-lift*.65,0)),
                center+direction*length*.67+Vector((0,-lift,0)),
                center-across*width*.47+direction*length*.29+Vector((0,-lift*.6,0))]
        if direction.x<0:
            points.reverse()
        # Orient every front polygon towards the viewer regardless of curl direction.
        if (points[1]-points[0]).cross(points[2]-points[0]).y>0:
            points.reverse()
        base=len(m.vertices)
        m.vertices.extend(tuple(p) for p in points)
        m.faces.append(tuple(range(base,base+5)))
        # Intact paint sample avoids unrelated atlas rust cutting across a flake.
        pu,pv=.55,.62
        m.uv.append([((0 if seed==104 else .5)+pu*.5+(p.x-center.x)*.008,pv+(p.z-center.z)*.008) for p in points])
        count+=1
        if count>=300:
            break
    obj=m.object('Lifted paint flakes','GatePaint',group)
    solid=obj.modifiers.new('Thin paint shell','SOLIDIFY')
    solid.thickness=.0003
    obj['raised_paint_flakes']=count


def chain_link(group, pos, direction, twist, index):
    # Oval closed welded links, alternating planes to interlock.
    tangent=Vector(direction).normalized()
    side=tangent.cross(Vector((0,1,0))).normalized()
    normal=tangent.cross(side).normalized()
    across=side*math.cos(twist)+normal*math.sin(twist)
    points=[]
    for k in range(33):
        t=2*math.pi*k/32
        p=Vector(pos)+tangent*(.040*math.cos(t))+across*(.026*math.sin(t))
        points.append(tuple(p))
    tube(group,f'Chain link {index:02}',points,.005,'Iron')
    return points


def locking_chain(group):
    # A single open chain threads both handle apertures. The shackle joins its ends.
    anchors=[(-.05,-.30,1.49),(-.14,-.29,1.50),(-.23,-.245,1.55),
             (-.215,-.175,1.61),(-.19,-.17,1.69),(-.14,-.18,1.755),
             (0,-.185,1.76),(.14,-.18,1.755),(.19,-.17,1.69),
             (.215,-.175,1.61),(.23,-.245,1.55),(.14,-.29,1.50),(.05,-.30,1.49)]
    controls=[Vector(p) for p in anchors]
    extended=[controls[0]*2-controls[1]]+controls+[controls[-1]*2-controls[-2]]
    path=[]
    for i in range(1,len(extended)-2):
        a,b,c,d=extended[i-1:i+3]
        for j in range(24):
            t=j/24
            path.append((2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t)*.5)
    path.append(controls[-1])
    cumulative=[0.0]
    for a,b in zip(path,path[1:]):
        cumulative.append(cumulative[-1]+(b-a).length)
    count=round(cumulative[-1]/.061)+1
    if count%2==0:
        count+=1
    def sample(distance):
        import bisect
        i=min(len(path)-2,max(0,bisect.bisect_right(cumulative,distance)-1))
        t=(distance-cumulative[i])/(cumulative[i+1]-cumulative[i])
        return path[i].lerp(path[i+1],t)
    rings=[]
    # Fitted rotations clear neighboring links around this fixed locking route.
    rotations=[0,30,60,90,15,60,90,-45,-30,-60,-75,-60,-30,-30,15,30,15,90,-75,-45,0]
    assert count==len(rotations)
    for i in range(count):
        distance=cumulative[-1]*i/(count-1)
        center=sample(distance)
        tangent=sample(min(cumulative[-1],distance+.003))-sample(max(0,distance-.003))
        if i in (0,count-1):
            tangent=Vector((1,0,0))
        rings.append(chain_link(group,center,tangent,math.pi/2+i*math.pi/2+math.radians(rotations[i]),i))
    shackle=[(-.05,-.30,1.37),(-.05,-.30,1.472),(-.038,-.30,1.495),
             (-.022,-.30,1.505),(.022,-.30,1.505),(.038,-.30,1.495),
             (.05,-.30,1.472),(.05,-.30,1.37)]
    tube(group,'Padlock shackle',shackle,.008,'Iron')
    # Seat the loop against both handle bars; keep every link and the lock together.
    offset=Vector((0,-.017,-.040))
    group.location=offset
    review={'chain_links':count,'center_spacing_m':cumulative[-1]/(count-1),
            'chain_rings':[[tuple(Vector(p)+offset) for p in ring] for ring in rings],
            'wire_radius_m':.005,'closed_lock_shackle':[tuple(Vector(p)+offset) for p in shackle+[shackle[0]]],
            'route':[tuple(p+offset) for p in path],
            'end_link_centers':[tuple(Vector(p)+offset) for p in (anchors[0],anchors[-1])],
            'assembly_offset':tuple(offset)}
    (SOURCE/'chain_geometry.json').write_text(json.dumps(review,indent=2),encoding='utf-8')
    group['chain_links']=count
    group['construction']='One chain threaded through both handles; both terminal links secured by the padlock shackle'


def chip_concrete(obj,parent):
    import bmesh
    local=random.Random(290+sum(ord(c) for c in parent.name))
    bm=bmesh.new()
    bm.from_mesh(obj.data)
    # Clip selected exposed corners into irregular triangular broken surfaces.
    corners=[v for v in bm.verts if v.co.y<-.2 and local.random()<.65]
    for v in corners:
        if v.is_valid:
            bmesh.ops.bevel(bm,geom=[v],offset=local.uniform(.009,.028),segments=1,affect='VERTICES')
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    for face in obj.data.polygons:
        axes=(0,1) if abs(face.normal.z)>.7 else ((1,2) if abs(face.normal.x)>.7 else (0,2))
        for loop in face.loop_indices:
            co=obj.data.vertices[obj.data.loops[loop].vertex_index].co
            obj.data.uv_layers.active.data[loop].uv=(co[axes[0]]/4.5+.5,co[axes[1]]/4.5)

def gate_leaf(group, side):
    x0,x1=(-2.70,-.018) if side<0 else (.018,2.70)
    center=(x0+x1)/2
    frames=builder(group,'FramePaint')
    for x in (x0+.045,x1-.045):
        frames.box((x,0,2.07),(.09,.105,3.86),1.0)
    for z in (.185,1.39,3.05,3.955):
        frames.box((center,0,z),(x1-x0,.115,.085 if z!=1.39 else .065),1.0)
    for i in range(1,11):
        x=x0+(x1-x0)*i/11
        frames.box((x,0,3.51),(.027,.035,.81),1.0)
        # Restrained visible weld beads at the ends of each vertical bar.
        for z in (3.092,3.912):
            tube(group,'Rail weld',[(x-.017,-.023,z),(x+.017,-.023,z)],.003,'Iron')
    steel_sheet(group,x0+.05,x1-.05)
    # Back face diagonal flat-steel braces, kept behind the sheet.
    for za,zb in ((.24,1.34),(1.43,3.0)):
        if side>0:
            za,zb=zb,za
        p1,p2=Vector((x0+.12,.069,za)),Vector((x1-.12,.069,zb))
        obj=box(group,'Rear diagonal brace',(p1+p2)/2,(.055,.025,(p2-p1).length),'FramePaint',.002)
        obj.rotation_euler=(p2-p1).to_track_quat('Z','Y').to_euler()
        # Box was built in world coordinates; rotate around its own center.
        for v in obj.data.vertices:
            v.co -= (p1+p2)/2
        obj.location=(p1+p2)/2
    hinge_x = x0-.025 if side<0 else x1+.025
    for z in (.68,2.05,3.52):
        cylinder(group,'Hinge barrel',(hinge_x,.0,z),.043,.22)
        box(group,'Hinge strap',(hinge_x-side*.135,.014,z),(.29,.016,.085),'Iron',.003)
        cylinder(group,'Hinge pin',(hinge_x,.0,z+.12),.023,.03)
    # Two handle plates with real fastening bolts and a raised steel handle.
    hx=side*.19
    box(group,'Handle mounting plate',(hx,-.079,1.64),(.27,.035,.32),'FramePaint',.004)
    for dx in (-.097,.097):
        for dz in (-.125,.125):
            cylinder(group,'Handle bolt',(hx+dx,-.104,1.64+dz),.012,.017,'Iron','Y',6)
    tube(group,'Pull handle',[(hx-.075,-.11,1.66),(hx-.075,-.20,1.66),(hx-.065,-.225,1.66),
         (hx+.065,-.225,1.66),(hx+.075,-.20,1.66),(hx+.075,-.11,1.66)],.014,'Iron')


def build_geometry():
    root=empty('FactoryGate_Rebuilt')
    root['setting']='Fictional Ningxia coal preparation plant; 1970s working history'
    root['units']='metres; Blender Z up; game export Y up'
    root['reference']='Approved straight-top industrial gate concept, 2026-09-23'
    for side,label in ((-1,'Left'),(1,'Right')):
        pier=empty(f'{label}_Pier',root)
        px=side*3.17
        concrete=builder(pier,'Concrete')
        concrete.box((px,0,.19),(.96,.95,.38),1/4.5,.009,offset=(.5,0))
        brickwork(pier,px-.39,px+.39,-.385,.385,.38,4.11,True)
        concrete.box((px,0,4.155),(.86,.86,.09),1/4.5,.004,offset=(.5,0))
        concrete.box((px,0,4.29),(1.05,.99,.18),1/4.5,.010,offset=(.5,0))
        wing=empty(f'{label}_WingWall',root)
        x0,x1=(-4.245,-3.56) if side<0 else (3.56,4.245)
        builder(wing,'Concrete').box(((x0+x1)/2,0,.14),(x1-x0,.62,.28),1/4.5,.008,offset=(.5,0))
        brickwork(wing,x0,x1,-.25,.25,.28,3.46,False)
        wall_whitewash(wing,x0+.003,x1-.003,True)
        wall_whitewash(wing,x0+.003,x1-.003,False)
        leaf=empty(f'{label}_GateLeaf',root)
        gate_leaf(leaf,side)
        # Embedded hinge-anchor plates on the inner side of the masonry.
        anchor=builder(pier,'Iron')
        for z in (.68,2.05,3.52):
            anchor.box((side*2.77,0,z),(.07,.28,.20))
    lock=empty('Chain_And_Padlock',root)
    locking_chain(lock)
    box(lock,'Padlock body',(0,-.30,1.33),(.18,.085,.18),'Iron',.009)
    box(lock,'Padlock face',(0,-.346,1.33),(.13,.008,.13),'FramePaint',.003)
    cylinder(lock,'Keyhole surround',(0,-.355,1.34),.019,.007,'Iron','Y')
    # Dark recessed key slit is real geometry in the presentation mesh.
    box(lock,'Key slit',(0,-.360,1.33),(.006,.004,.025),'Iron',.0007)
    for (name,mat),(mesh,parent) in groups.items():
        bevel={'Brick':0,'BrickWeathered':0,'Concrete':.012,'Mortar':.001,'FramePaint':.003,'Iron':.003}[mat]
        obj=mesh.object(f'{name}_{mat}',mat,parent,bevel,1 if mat in ('Brick','Concrete') else 2,mat in ('FramePaint','Iron'))
        if mat=='Concrete':
            chip_concrete(obj,parent)
    root['through_corrosion_openings']=sum(o.get('through_holes',0) for o in asset_collection.objects)
    root['raised_paint_flakes']=sum(o.get('raised_paint_flakes',0) for o in asset_collection.objects)
    # Apply construction modifiers and merge each component by its material.
    # Leaves remain separate, with origins positioned at their hinge axes.
    for obj in list(asset_collection.objects):
        if obj.type not in ('MESH','CURVE'):
            continue
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        if obj.type=='CURVE':
            bpy.ops.object.convert(target='MESH')
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    for parent in [o for o in asset_collection.objects if o.type=='EMPTY' and o!=root]:
        for mat in materials.values():
            children=[o for o in list(asset_collection.objects) if o.parent==parent and o.type=='MESH']
            subset=[o for o in children if o.data.materials and o.data.materials[0]==mat]
            if not subset:
                continue
            bpy.ops.object.select_all(action='DESELECT')
            for o in subset:
                o.select_set(True)
            bpy.context.view_layer.objects.active=subset[0]
            if len(subset)>1:
                bpy.ops.object.join()
            bpy.context.object.name=f'{parent.name}_{mat.name}'
        if 'GateLeaf' in parent.name:
            pivot=Vector((-2.70,0,0) if parent.name.startswith('Left') else (2.70,0,0))
            for child in parent.children:
                child.location -= pivot
            parent.location=pivot
    for obj in [o for o in asset_collection.objects if o.type=='MESH']:
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        mod=obj.modifiers.new('Stable export triangulation','TRIANGULATE')
        mod.quad_method='FIXED'
        bpy.ops.object.modifier_apply(modifier=mod.name)
        if obj.data.materials[0].name in ('Concrete','Whitewash'):
            # Clipped boundaries can leave microscopic slivers.
            # Remove those before glTF splits flat-shaded vertices for export.
            import bmesh
            bm=bmesh.new()
            bm.from_mesh(obj.data)
            bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_area()<1e-7],context='FACES')
            bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
            bm.normal_update()
            bm.to_mesh(obj.data)
            bm.free()
            obj.data.update()
    # Only the guard-post side wing belongs to the modular gate asset.
    outer_wing=bpy.data.objects.get('Right_WingWall')
    if outer_wing:
        for obj in list(outer_wing.children_recursive)+[outer_wing]:
            bpy.data.objects.remove(obj,do_unlink=True)
    return root


def export_asset(root):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in asset_collection.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.gltf(filepath=str(ASSET/'FactoryGate_Rebuilt.glb'),export_format='GLB',
        use_selection=True,export_yup=True,export_animations=False,export_extras=True,export_tangents=True,
        export_vertex_color='NAME',export_vertex_color_name='BrickTint',export_all_vertex_colors=False)
    bpy.ops.export_scene.fbx(filepath=str(ASSET/'FactoryGate_Rebuilt.fbx'),use_selection=True,
        object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
        bake_space_transform=False,add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
    mesh_objects=[o for o in asset_collection.objects if o.type=='MESH']
    triangles=0
    verts=0
    bounds=[]
    parts=[]
    for o in mesh_objects:
        mesh=o.data
        mesh.calc_loop_triangles()
        n=len(mesh.loop_triangles)
        triangles+=n
        verts+=len(mesh.vertices)
        parts.append({'name':o.name,'triangles':n,'vertices':len(mesh.vertices),'materials':[m.name for m in mesh.materials]})
        bounds.extend(o.matrix_world @ Vector(c) for c in o.bound_box)
    dims=[max(v[i] for v in bounds)-min(v[i] for v in bounds) for i in range(3)]
    report={'blender':bpy.app.version_string,'units':'metres','size_xyz_blender':dims,
            'triangles':triangles,'vertices':verts,'mesh_objects':len(mesh_objects),
            'materials':list(materials),'parts':parts,'unity_runtime_verified':False,
            'through_corrosion_openings':root['through_corrosion_openings'],
            'raised_paint_flakes':root['raised_paint_flakes'],
            'source_model_unchanged_sha256':hashlib.sha256((ASSET.parent/'FactoryGate.glb').read_bytes()).hexdigest()}
    (SOURCE/'asset_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('ASSET_REPORT',json.dumps({k:v for k,v in report.items() if k!='parts'}),flush=True)


def camera(name,position,target,ortho):
    data=bpy.data.cameras.new(name)
    obj=bpy.data.objects.new(name,data)
    stage_collection.objects.link(obj)
    obj.location=position
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    data.type,data.ortho_scale='ORTHO',ortho
    data.lens=55
    return obj


def setup_stage():
    scene=bpy.context.scene
    scene.render.engine='CYCLES'
    scene.cycles.samples=96
    scene.cycles.use_denoising=True
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX'
    prefs.get_devices()
    for device in prefs.devices:
        device.use=device.type=='OPTIX'
    scene.cycles.device='GPU'
    scene.view_settings.view_transform='AgX'
    scene.view_settings.look='AgX - Medium High Contrast'
    scene.render.image_settings.file_format='PNG'
    scene.render.resolution_percentage=100
    scene.world.use_nodes=True
    bg=scene.world.node_tree.nodes.get('Background')
    bg.inputs['Color'].default_value=(.38,.43,.51,1)
    bg.inputs['Strength'].default_value=.45
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.025))
    floor=bpy.context.object
    floor.name='Preview ground only'
    for c in list(floor.users_collection):
        c.objects.unlink(floor)
    stage_collection.objects.link(floor)
    mat=bpy.data.materials.new('Preview ground')
    mat.diffuse_color=(.135,.15,.165,1)
    mat.use_nodes=True
    bsdf=mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value=(.135,.15,.165,1)
    bsdf.inputs['Roughness'].default_value=.92
    floor.data.materials.append(mat)
    for name,pos,energy,size,color in [('Key',(-4,-6,10),2300,6,(1,.92,.81)),('Fill',(6,-2,7),1300,5,(.8,.87,1)),('Rim',(0,4,8),2100,4,(1,.96,.9))]:
        data=bpy.data.lights.new(name,'AREA')
        data.energy,data.shape,data.size=energy,'DISK',size
        data.color=color
        obj=bpy.data.objects.new(name,data)
        stage_collection.objects.link(obj)
        obj.location=pos
        obj.rotation_euler=(Vector((0,0,2))-obj.location).to_track_quat('-Z','Y').to_euler()
    return [
        (camera('Hero',(2.2,-26,7.0),(0,0,2.1),9.7),'Gate_Hero.png',(1800,1200)),
        (camera('Front',(0,-25,4.5),(0,0,2.1),9.5),'Gate_Front.png',(1800,1150)),
        (camera('Lock Detail',(1.1,-4,2.4),(0,-.1,1.58),1.60),'Gate_Lock_Detail.png',(1400,1200)),
        (camera('Lock Top',(.75,-1.2,2.75),(0,-.19,1.59),1.0),'Gate_Lock_Top.png',(1400,1400)),
        (camera('Masonry Detail',(-5,-6,5.8),(-3.15,0,3.1),2.9),'Gate_Masonry_Detail.png',(1300,1400)),
        (camera('Rear',(-7,24,8.5),(0,0,2.1),10.3),'Gate_Rear.png',(1800,1200)),
        (camera('Paint Detail',(-2.9,-3.7,3.0),(-1.98,0,2.60),1.15),'Gate_Paint_Detail.png',(1600,1200)),
        (camera('Corrosion Detail',(1.8,-4,1.0),(1.45,0,.42),1.60),'Gate_Corrosion_Detail.png',(1600,1050)),
        (camera('Whitewash Detail',(-5,-5,2.8),(-3.75,0,1.35),2.0),'Gate_Whitewash_Detail.png',(1300,1400)),
    ]


def prepare_source_view():
    bpy.ops.file.pack_all()
    for screen in bpy.data.screens:
        if screen.name not in ('Layout','Modeling','Shading'):
            continue
        for area in screen.areas:
            if area.type!='VIEW_3D':
                continue
            space=area.spaces.active
            space.shading.type='MATERIAL'
            space.shading.use_scene_world=False
            space.shading.use_scene_lights=False
            space.region_3d.view_location=Vector((0,0,2.1))
            space.region_3d.view_rotation=bpy.context.scene.camera.rotation_euler.to_quaternion()
            space.region_3d.view_distance=12
            space.region_3d.view_perspective='ORTHO'
    if bpy.context.window:
        bpy.context.window.workspace=bpy.data.workspaces['Layout']


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--skip-textures',action='store_true')
    parser.add_argument('--skip-render',action='store_true')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
    ASSET.mkdir(parents=True,exist_ok=True)
    PREVIEWS.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    asset_collection=bpy.data.collections.new('Factory Gate Asset')
    bpy.context.scene.collection.children.link(asset_collection)
    stage_collection=bpy.data.collections.new('Preview Only')
    bpy.context.scene.collection.children.link(stage_collection)
    bpy.context.scene.unit_settings.system='METRIC'
    if not args.skip_textures:
        texture_maps.build(TEXTURES)
    for name in ('GatePaint','FramePaint','Iron','Brick','BrickWeathered','Concrete','Mortar','Whitewash'):
        materials[name]=material(name)
    root=build_geometry()
    texture_maps.tint_individual_bricks(asset_collection)
    export_asset(root)
    views=setup_stage()
    bpy.context.scene.camera=views[0][0]
    bpy.context.scene.render.resolution_x,bpy.context.scene.render.resolution_y=views[0][2]
    bpy.context.scene.render.filepath=str(PREVIEWS/views[0][1])
    # Pack while texture paths are still absolute in a new unsaved scene.
    prepare_source_view()
    # Relative texture paths keep unpacking within the project.
    import os
    for image in bpy.data.images:
        if image.source=='FILE' and image.filepath:
            image.filepath='//'+Path(os.path.relpath(bpy.path.abspath(image.filepath),SOURCE)).as_posix()
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'FactoryGate_Rebuilt.blend'))
    if not args.skip_render:
        for cam,filename,resolution in views:
            scene=bpy.context.scene
            scene.camera=cam
            scene.render.resolution_x,scene.render.resolution_y=resolution
            scene.render.filepath=str(PREVIEWS/filename)
            bpy.ops.render.render(write_still=True)
            print('RENDER_COMPLETE',filename,flush=True)
    print('FACTORY_GATE_BUILD_COMPLETE',flush=True)
