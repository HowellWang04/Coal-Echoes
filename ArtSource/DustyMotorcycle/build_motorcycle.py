"""Build an original weathered commuter motorcycle, in metres, without source assets."""
from pathlib import Path
import json
import math
import random
import sys

import bpy
import numpy as np
from mathutils import Vector

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parent.parent
ASSET = PROJECT / 'Assets/Art Resources/Environment/Gate Area/DustyMotorcycle'
TEX = ASSET / 'Textures'
PREVIEW = HERE / 'Preview'
for folder in (ASSET, TEX, PREVIEW):
    folder.mkdir(parents=True, exist_ok=True)
random.seed(175)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
parts = []
materials = {}
shader_channels = {}


def weathered(name, color, metal=0.0, rough=.7, dust=.5, bump=.002):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*color, 1)
    ns, links = mat.node_tree.nodes, mat.node_tree.links
    ns.clear()
    out = ns.new('ShaderNodeOutputMaterial')
    bs = ns.new('ShaderNodeBsdfPrincipled')
    links.new(bs.outputs['BSDF'], out.inputs['Surface'])
    coord = ns.new('ShaderNodeTexCoord')
    geo = ns.new('ShaderNodeNewGeometry')

    def noise(scale, detail=3):
        n = ns.new('ShaderNodeTexNoise')
        n.inputs['Scale'].default_value = scale
        n.inputs['Detail'].default_value = detail
        links.new(coord.outputs['Object'], n.inputs['Vector'])
        return n.outputs['Fac']

    def op(kind, a, b=0):
        n = ns.new('ShaderNodeMath')
        n.operation = kind
        for i, value in enumerate((a, b)):
            if isinstance(value, (float, int)):
                n.inputs[i].default_value = value
            else:
                links.new(value, n.inputs[i])
        return n.outputs[0]

    def mix(a, b, f):
        n = ns.new('ShaderNodeMixRGB')
        for index, value in ((1, a), (2, b)):
            if isinstance(value, tuple):
                n.inputs[index].default_value = (*value, 1)
            else:
                links.new(value, n.inputs[index])
        if isinstance(f, (int, float)):
            n.inputs[0].default_value = f
        else:
            links.new(f, n.inputs[0])
        return n.outputs[0]

    normal = ns.new('ShaderNodeSeparateXYZ')
    links.new(geo.outputs['Normal'], normal.inputs[0])
    height = ns.new('ShaderNodeSeparateXYZ')
    links.new(geo.outputs['Position'], height.inputs[0])
    macro = noise(8)
    grain = noise(190, 2)
    speck = noise(68)
    coverage = op('MULTIPLY', op('SUBTRACT', macro, .43), 3.8)
    coverage = op('ADD', coverage, .13)
    coverage = op('ADD', coverage, op('MULTIPLY', op('MAXIMUM', normal.outputs['Z'], 0), .25))
    coverage = op('MULTIPLY', coverage, dust)
    coverage = op('MINIMUM', op('MAXIMUM', coverage, 0), .87)
    worn = op('MULTIPLY', op('MAXIMUM', op('SUBTRACT', speck, .51), 0), 4.5)
    rust_color = (.075, .027, .012) if metal > .25 else (.038, .026, .016)
    base = mix(color, rust_color, worn)
    if name == 'Faded_Red_Enamel':
        chips = op('GREATER_THAN', noise(95), .61)
        patches = op('MINIMUM', op('MAXIMUM', op('MULTIPLY', op('SUBTRACT', noise(24), .47), 6), 0), 1)
        base = mix(base, (.063, .027, .014), op('MULTIPLY', patches, chips))
        scale = ns.new('ShaderNodeVectorMath'); scale.operation='MULTIPLY'
        scale.inputs[1].default_value=(110,110,5)
        links.new(coord.outputs['Object'],scale.inputs[0])
        streak = ns.new('ShaderNodeTexNoise'); streak.inputs['Scale'].default_value=1
        links.new(scale.outputs[0],streak.inputs['Vector'])
        runs = op('MULTIPLY', op('GREATER_THAN', streak.outputs['Fac'], .64), op('GREATER_THAN', noise(19), .50))
        base = mix(base, (.34,.28,.19), op('MULTIPLY',runs,.80))
    if name == 'Worn_Brown_Seat':
        cracks=ns.new('ShaderNodeTexVoronoi');cracks.feature='DISTANCE_TO_EDGE'
        cracks.inputs['Scale'].default_value=88
        links.new(coord.outputs['Object'],cracks.inputs['Vector'])
        base=mix(base,(.005,.003,.002),op('LESS_THAN',cracks.outputs['Distance'],.025))
    dirt = mix((.12, .085, .046), (.38, .285, .17), grain)
    base = mix(base, dirt, coverage)
    mud = op('MULTIPLY', op('MAXIMUM', op('SUBTRACT', .48, height.outputs['Z']), 0), 2.0)
    mud = op('MULTIPLY', mud, op('MULTIPLY', speck, .9))
    base = mix(base, (.11, .072, .034), mud)
    grime = op('MULTIPLY', op('GREATER_THAN', noise(32), .58), .48)
    base = mix(base,(.022,.016,.011),grime)
    ao=ns.new('ShaderNodeAmbientOcclusion');ao.inputs['Distance'].default_value=.08
    ao.samples=8
    darken=ns.new('ShaderNodeMixRGB');darken.blend_type='MULTIPLY';darken.inputs[0].default_value=.47
    links.new(base,darken.inputs[1]);links.new(ao.outputs['Color'],darken.inputs[2])
    base=darken.outputs[0]
    metallic = op('MULTIPLY', metal, op('SUBTRACT', 1, op('MULTIPLY', coverage, .95)))
    roughness = op('MINIMUM', op('ADD', rough, op('MULTIPLY', coverage, .36)), .98)
    links.new(base, bs.inputs['Base Color'])
    links.new(metallic, bs.inputs['Metallic'])
    links.new(roughness, bs.inputs['Roughness'])
    surface = ns.new('ShaderNodeBump')
    surface.inputs['Strength'].default_value = .42
    surface.inputs['Distance'].default_value = bump
    links.new(grain, surface.inputs['Height'])
    links.new(surface.outputs['Normal'], bs.inputs['Normal'])
    shader_channels[name] = (base, metallic, roughness, bs, out)
    materials[name] = mat
    return mat


weathered('Faded_Red_Enamel', (.33, .028, .016), .38, .49, .93)
weathered('Oxidized_Steel', (.24, .265, .28), .88, .35, .68, .0015)
weathered('Engine_Alloy', (.18, .19, .18), .76, .53, .78, .002)
weathered('Black_Frame', (.016, .021, .019), .60, .50, .65)
weathered('Tire_Rubber', (.014, .012, .010), .0, .86, .75, .006)
weathered('Worn_Brown_Seat', (.043, .028, .018), .0, .76, .90, .003)
weathered('Warm_Ivory', (.62, .49, .29), .12, .57, .68)
weathered('Lamp_Glass', (.48, .43, .28), .35, .26, .32, .0004)
weathered('Tail_Red', (.36, .015, .007), .06, .30, .32)
weathered('Amber_Lens', (.60, .17, .009), .0, .32, .45)
weathered('Old_Olive_Storage', (.072, .086, .043), .42, .62, .96, .003)


def finish(obj, name, mat, smooth=True):
    obj.name = name
    obj.data.materials.append(materials[mat])
    if smooth:
        for p in obj.data.polygons:
            p.use_smooth = True
    parts.append(obj)
    return obj


def box(name, pos, size, mat, bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Rounded metal edges', 'BEVEL')
        mod.width, mod.segments = bevel, 3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, name, mat)


def ellipsoid(name, pos, scale, mat, segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=pos)
    obj = bpy.context.object
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat)


def rod(name, a, b, radius, mat, vertices=12, radius2=None):
    delta = Vector(b) - Vector(a)
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius if radius2 is None else radius2,
                                   depth=delta.length, location=(Vector(a)+Vector(b))*.5)
    obj = bpy.context.object
    obj.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, mat)


def tube(name, points, radius, mat, resolution=3):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 12
    curve.bevel_depth, curve.bevel_resolution = radius, resolution
    spline = curve.splines.new('BEZIER')
    spline.bezier_points.add(len(points)-1)
    for p, co in zip(spline.bezier_points, points):
        p.co = co
        p.handle_left_type = p.handle_right_type = 'AUTO'
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target='MESH')
    obj.select_set(False)
    return finish(obj, name, mat)


def torus(name, pos, major, minor, mat, rotation=(math.pi/2, 0, 0), segments=64, minor_segments=10):
    bpy.ops.mesh.primitive_torus_add(major_segments=segments, minor_segments=minor_segments,
                                    location=pos, major_radius=major, minor_radius=minor, rotation=rotation)
    return finish(bpy.context.object, name, mat)


def extruded_profile(name, profile, y0, y1, mat, bevel=.01):
    n = len(profile)
    verts = [(x, y, z) for y in (y0, y1) for x, z in profile]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    if bevel:
        mod = obj.modifiers.new('Soft stamped edges', 'BEVEL')
        mod.width, mod.segments = bevel, 3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.select_set(False)
    return finish(obj, name, mat)


def fender(name, cx, cz, radius, width, start, end, mat):
    verts, faces = [], []
    steps, cross = 48, 8
    for i in range(steps+1):
        a = math.radians(start+(end-start)*i/steps)
        for j in range(cross+1):
            y = width*(j/cross-.5)
            r = radius + .019*(1-(2*j/cross-1)**2)
            verts.append((cx+r*math.cos(a), y, cz+r*math.sin(a)))
    for i in range(steps):
        for j in range(cross):
            k = i*(cross+1)+j
            faces.append((k, k+1, k+cross+2, k+cross+1))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    mod = obj.modifiers.new('Stamped sheet thickness', 'SOLIDIFY')
    mod.thickness = .003
    bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.select_set(False)
    finish(obj, name, mat)
    for side in (-1, 1):
        points = [(cx+radius*math.cos(math.radians(a)), side*width*.5, cz+radius*math.sin(math.radians(a)))
                  for a in np.linspace(start, end, 14)]
        tube(name+'_RolledEdge', points, .0032, 'Oxidized_Steel', 1)


# Wheelbase 1.43 m. A small, practical 175 cc commuter, not a replica of a source asset.
for label, cx in (('Front', .75), ('Rear', -.68)):
    cz = .34
    torus(label+'_Tire', (cx, 0, cz), .278, .060, 'Tire_Rubber', minor_segments=16)
    for side in (-1, 1):
        torus(label+'_Rim', (cx, side*.038, cz), .250, .014, 'Oxidized_Steel')
        torus(label+'_Tire_Bead', (cx, side*.050, cz), .275, .0045, 'Tire_Rubber')
        torus(label+'_Sidewall_Rib', (cx, side*.058, cz), .292, .0027, 'Tire_Rubber')
        for i in range(28):
            a = 2*math.pi*i/28
            end = a + side*.27
            rod(label+'_CrossedSpoke', (cx+.076*math.cos(a), side*.044, cz+.076*math.sin(a)),
                (cx+.243*math.cos(end), side*.026, cz+.243*math.sin(end)), .0018, 'Oxidized_Steel', 6)
    rod(label+'_DrumHub', (cx,-.065,cz), (cx,.065,cz), .084, 'Engine_Alloy', 40)
    rod(label+'_Axle', (cx,-.11,cz), (cx,.11,cz), .014, 'Oxidized_Steel', 12)
    for i in range(56):
        a = 2*math.pi*i/56
        for row in (-1, 0, 1):
            aa = a+abs(row)*.032
            r = .333 if row == 0 else .318
            obj = box(label+'_Tread', (cx+r*math.cos(aa), row*.041, cz+r*math.sin(aa)),
                      (.023, .037, .009), 'Tire_Rubber', .0015)
            obj.rotation_euler.y = math.pi/2-aa
            obj.rotation_euler.z = row*.23

# Double-cradle frame and a braced swingarm.
for side in (-1, 1):
    y = side*.09
    tube('Cradle', [(.39,y,.99), (.30,y,.73), (.21,y,.34), (-.19,y,.29), (-.37,y,.46), (-.41,y,.83)], .019, 'Black_Frame')
    tube('TopRail', [(.39,y,.99), (-.18,y,.83), (-.76,y,.82), (-.91,y,.79)], .018, 'Black_Frame')
    rod('FrameBrace', (-.15,y,.76), (-.35,y,.38), .016, 'Black_Frame')
    rod('SwingArm', (-.28,y,.39), (-.68,side*.09,.34), .019, 'Black_Frame')
    rod('SwingArmUpper', (-.28,y,.43), (-.67,side*.11,.36), .011, 'Black_Frame')
    # Twin rear shocks with actual helical springs.
    a, b = Vector((-.68, side*.122, .37)), Vector((-.53, side*.122, .78))
    rod('ShockPiston', a, b, .013, 'Oxidized_Steel')
    direction = (b-a).normalized()
    u, v = direction.cross(Vector((0,1,0))).normalized(), Vector((0,1,0))
    helix = []
    for i in range(145):
        t = i/144
        helix.append(a+(b-a)*(.12+.72*t)+.025*(u*math.cos(t*math.tau*12)+v*math.sin(t*math.tau*12)))
    tube('ShockSpring', helix, .0045, 'Oxidized_Steel', 1)
    for p in (a, b):
        rod('ShockBolt', (p.x, side*.10, p.z), (p.x, side*.153, p.z), .014, 'Oxidized_Steel', 6)

# Forks, gaiters, triple clamps, brake linkage and front mudguard.
for side in (-1,1):
    rod('LowerFork', (.75,side*.092,.34), (.585,side*.092,.72), .026, 'Engine_Alloy', 20)
    rod('ForkStanchion', (.585,side*.092,.72), (.415,side*.092,1.105), .016, 'Oxidized_Steel', 20)
    rod('ForkGaiterCore', (.58,side*.092,.73), (.47,side*.092,.98), .026, 'Tire_Rubber', 20)
    for t in np.linspace(0, 1, 12):
        p = Vector((.58,side*.092,.73)).lerp(Vector((.47,side*.092,.98)), float(t))
        d = Vector((-.11,0,.25)).normalized()
        rod('GaiterRib', p-d*.005, p+d*.005, .031, 'Tire_Rubber', 20)
    rod('FenderStay', (.75,side*.095,.36), (.90,side*.078,.60), .005, 'Oxidized_Steel', 8)
    rod('FenderStay', (.75,side*.095,.36), (.49,side*.078,.59), .005, 'Oxidized_Steel', 8)
for x,z in ((.42,1.10),(.48,.96)):
    box('TripleClamp', (x,0,z), (.065,.27,.027), 'Black_Frame', .010)
fender('Red_Front_Mudguard', .75,.34,.368,.151,20,161,'Faded_Red_Enamel')
fender('Rear_Mudguard', -.68,.34,.37,.17,55,205,'Black_Frame')
box('Rear_MudFlap', (-1.03,0,.29), (.012,.16,.15), 'Tire_Rubber', .008)

# Sculpted six-section tank with a low horizontal shoulder and tapered rear.
sections = [(-.235,.018,.865,.925),(-.185,.125,.800,1.007),(-.07,.176,.800,1.059),
            (.16,.171,.835,1.070),(.315,.108,.865,1.025),(.35,.015,.911,.963)]
verts, faces = [], []
for x, width, bottom, top in sections:
    for j in range(24):
        t=math.tau*j/24
        verts.append((x, width*math.cos(t), (top+bottom)/2+(top-bottom)/2*math.sin(t)))
for k in range(len(sections)-1):
    for j in range(24):
        faces.append((k*24+j,k*24+(j+1)%24,(k+1)*24+(j+1)%24,(k+1)*24+j))
faces += [tuple(reversed(range(24))), tuple(range((len(sections)-1)*24,len(sections)*24))]
mesh = bpy.data.meshes.new('HandShaped_Tank')
mesh.from_pydata(verts, [], faces)
obj = bpy.data.objects.new('RedFuelTank', mesh)
bpy.context.collection.objects.link(obj)
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
sub = obj.modifiers.new('Pressed tank curves', 'SUBSURF')
sub.levels = 2
bpy.ops.object.modifier_apply(modifier=sub.name)
obj.select_set(False)
finish(obj,'RedFuelTank','Faded_Red_Enamel')
rod('FuelCap', (.18,0,1.051),(.18,0,1.078), .035,'Oxidized_Steel',32)
box('FuelCap_Latch',(.18,0,1.082),(.046,.012,.006),'Engine_Alloy',.002)
for side in (-1,1):
    tube('Ivory_Tank_Pinstripe', [(-.15,side*.121,.934),(-.035,side*.171,.956),(.14,side*.165,.967),(.27,side*.120,.958)], .006,'Warm_Ivory',2)
    box('Rubber_KneePad',(-.14,side*.132,.867),(.13,.016,.074),'Tire_Rubber',.018)
    for i in range(6):
        rod('KneePad_Ridge',(-.19+i*.019,side*.144,.845),(-.18+i*.019,side*.144,.891),.0028,'Tire_Rubber',6)

# Long brown vinyl saddle with piping, strap and rear luggage rack.
seat = extruded_profile('BenchSeat',[(-.89,.825),(-.89,.888),(-.77,.920),(-.48,.919),(-.33,.903),(-.20,.892),(-.20,.828)],-.152,.152,'Worn_Brown_Seat',.035)
for side in (-1,1):
    tube('Seat_Piping',[(-.85,side*.147,.898),(-.70,side*.153,.913),(-.48,side*.153,.908),(-.33,side*.144,.895),(-.23,side*.126,.883)],.0034,'Warm_Ivory',1)
    box('Seat_Strap_Buckle',(-.59,side*.162,.865),(.035,.009,.037),'Oxidized_Steel',.002)
box('SeatStrap',(-.59,0,.925),(.039,.300,.006),'Worn_Brown_Seat',.002)
for i in range(10):
    x=-.82+i*.058
    tube('Seat_Stitch',[ (x,-.127,.918),(x,0,.926),(x,.127,.918)],.0010,'Black_Frame',1)
for side in (-1,1):
    tube('RearRack_Rail',[(-.79,side*.133,.80),(-.92,side*.154,.89),(-1.12,side*.154,.88)],.008,'Oxidized_Steel',2)
for x in (-.93,-1.00,-1.07,-1.12):
    rod('RackCrossbar',(x,-.15,.886),(x,.15,.886),.006,'Oxidized_Steel',10)
box('TailLampHousing',(-.955,0,.782),(.065,.12,.075),'Black_Frame',.01)
box('Red_TailLens',(-.994,0,.785),(.018,.102,.052),'Tail_Red',.008)
box('RearPlate',(-1.014,0,.675),(.008,.168,.087),'Warm_Ivory',.004)

# Small luggage box on the rear rack, with a separate lid seam, latch and straps.
box('StorageBox',(-1.055,0,.990),(.29,.34,.192),'Old_Olive_Storage',.018)
box('StorageLidSeal',(-1.055,0,1.088),(.296,.346,.009),'Black_Frame',.009)
box('StorageBoxLid',(-1.055,0,1.110),(.31,.358,.042),'Old_Olive_Storage',.016)
for side in (-1,1):
    y=side*.105
    box('LuggageStrap_Top',(-1.055,y,1.134),(.304,.022,.006),'Worn_Brown_Seat',.002)
    for x in (-1.205,-.905):
        box('LuggageStrap_Vertical',(x,y,1.003),(.005,.022,.232),'Worn_Brown_Seat',.002)
    box('LuggageStrap_Buckle',(-1.210,y,1.019),(.009,.039,.044),'Oxidized_Steel',.003)
    for z in (.934,1.054):
        rod('BoxCornerRivet',(-1.205,side*.139,z),(-1.211,side*.139,z),.0045,'Oxidized_Steel',8)
box('StorageLatchMount',(-1.216,0,1.081),(.012,.033,.058),'Oxidized_Steel',.004)
box('StorageLatchCatch',(-1.226,0,1.074),(.010,.019,.033),'Black_Frame',.003)
tube('StorageHandle',[(-1.10,-.052,1.137),(-1.10,-.040,1.161),(-1.10,.040,1.161),(-1.10,.052,1.137)],.005,'Black_Frame',2)
for y in (-.116,.116):
    rod('StorageLidHinge',(-.901,y-.025,1.091),(-.901,y+.025,1.091),.008,'Oxidized_Steel',12)

# Original angled side covers, inset vents and external fasteners.
for side in (-1,1):
    profile=[(-.60,.765),(-.30,.765),(-.29,.622),(-.51,.538),(-.59,.576)]
    extruded_profile('Red_SideCover',profile,side*.12,side*.175,'Faded_Red_Enamel',.016)
    for i in range(5):
        box('SideCover_Vent',(-.54+i*.034,side*.179,.687),(.019,.004,.045),'Black_Frame',.002)
    rod('SideCover_Bolt',(-.44,side*.18,.738),(-.44,side*.187,.738),.009,'Oxidized_Steel',6)

# Single-cylinder engine with cooling fins, cylinder head, carburetor and spark cable.
ellipsoid('Crankcase',(-.06,0,.435),(.225,.133,.137),'Engine_Alloy')
for side in (-1,1):
    rod('Round_EngineCover',(-.015,side*.108,.437),(-.015,side*.159,.437),.102,'Engine_Alloy',40)
    torus('Cover_Rim',(-.015,side*.160,.437),.088,.004,'Oxidized_Steel',minor_segments=6)
    for i in range(7):
        a=math.tau*i/7
        x,z=-.015+.081*math.cos(a),.437+.081*math.sin(a)
        rod('Cover_HexBolt',(x,side*.158,z),(x,side*.165,z),.006,'Oxidized_Steel',6)
box('CylinderCore',(.045,0,.627),(.15,.155,.198),'Black_Frame',.018)
for i in range(10):
    box('CoolingFin',(.05,0,.541+i*.018),(.213,.206,.007),'Engine_Alloy',.023)
box('CylinderHead',(.06,0,.744),(.19,.19,.038),'Engine_Alloy',.021)
rod('SparkPlug',(.082,0,.760),(.091,0,.799),.010,'Warm_Ivory',10)
tube('IgnitionCable',[(.09,0,.800),(.0,.03,.833),(-.19,.06,.780),(-.28,.03,.680)],.004,'Tire_Rubber',2)
rod('IntakeNeck',(-.10,0,.660),(-.20,0,.675),.030,'Engine_Alloy',16)
box('Carburetor',(-.205,0,.653),(.062,.088,.091),'Engine_Alloy',.018)
rod('FuelLine',(-.12,.09,.804),(-.20,.07,.683),.004,'Tire_Rubber',8)
for side in (-1,1):
    rod('FootPegStem',(-.20,side*.11,.357),(-.20,side*.245,.357),.011,'Oxidized_Steel',10)
    rod('Ribbed_FootPeg',(-.20,side*.17,.357),(-.20,side*.255,.357),.022,'Tire_Rubber',16)
    for y in np.linspace(.178,.249,7):
        rod('PegRib',(-.20,side*y-.002,.357),(-.20,side*y+.002,.357),.024,'Tire_Rubber',12)
    rod('PassengerPeg',(-.62,side*.12,.44),(-.62,side*.225,.44),.016,'Tire_Rubber',12)
tube('Exhaust_Header',[(.15,-.033,.656),(.25,-.063,.64),(.29,-.15,.43),(.20,-.195,.272),(-.24,-.195,.254)],.022,'Oxidized_Steel',3)
rod('Long_Muffler',(-.24,-.195,.254),(-.89,-.195,.30),.047,'Oxidized_Steel',32,radius2=.036)
rod('Muffler_Outlet',(-.896,-.195,.30),(-.906,-.195,.301),.025,'Black_Frame',24)
rod('GearLever',(-.10,.17,.346),(.055,.19,.326),.008,'Oxidized_Steel',8)
rod('GearLeverRubber',(.055,.185,.326),(.055,.23,.326),.010,'Tire_Rubber',12)
rod('Rear_Sprocket',(-.68,.069,.34),(-.68,.080,.34),.126,'Black_Frame',40)
for z in (.228,.457):
    tube('Chain', [(-.68,.084,z),(-.49,.084,z+.008),(-.25,.084,.39 if z>.3 else .27)],.009,'Black_Frame',1)
box('ChainGuard',(-.48,.095,.477),(.48,.067,.034),'Black_Frame',.01)
for side in (-1,1):
    tube('CenterStand',[(-.22,side*.07,.32),(-.37,side*.145,.08),(-.39,side*.17,.02)],.012,'Black_Frame',2)
rod('StandFoot',(-.42,-.17,.018),(-.42,.17,.018),.010,'Oxidized_Steel',10)

# Round lamp, instrument, swept handlebar and one small utilitarian mirror.
rod('LampBucket',(.405,0,1.017),(.57,0,1.017),.102,'Black_Frame',48,radius2=.11)
torus('LampRim',(.577,0,1.017),.102,.007,'Oxidized_Steel',(0,math.pi/2,0),64,10)
rod('HeadlampLens',(.574,0,1.017),(.583,0,1.017),.095,'Lamp_Glass',48)
for i in range(-6,7):
    y=i*.012
    h=math.sqrt(max(.089**2-y*y,0))
    rod('FlutedGlass',(.584,y,1.017-h),(.584,y,1.017+h),.0013,'Lamp_Glass',6)
for side in (-1,1):
    rod('LampBracket',(.43,side*.09,1.00),(.48,side*.11,1.028),.008,'Black_Frame',10)
    rod('SignalStalk',(.47,side*.11,.956),(.47,side*.203,.956),.006,'Black_Frame',10)
    rod('AmberIndicator',(.455,side*.205,.956),(.483,side*.205,.956),.025,'Amber_Lens',24)
tube('Handlebar',[ (.35,-.33,1.135),(.40,-.23,1.158),(.425,-.10,1.102),(.425,.10,1.102),(.40,.23,1.158),(.35,.33,1.135)],.010,'Oxidized_Steel',3)
for side in (-1,1):
    rod('HandleGrip',(.35,side*.28,1.14),(.324,side*.39,1.129),.017,'Tire_Rubber',20)
    box('SwitchHousing',(.37,side*.256,1.142),(.042,.04,.041),'Black_Frame',.009)
    tube('BrakeLever',[(.405,side*.251,1.139),(.43,side*.31,1.13),(.401,side*.388,1.111)],.005,'Oxidized_Steel',2)
tube('FrontBrakeCable',[(.398,-.255,1.136),(.47,-.13,.98),(.51,-.12,.72),(.76,-.11,.42),(.77,-.084,.35)],.003,'Tire_Rubber',2)
tube('ThrottleCable',[(.37,.255,1.14),(.36,.12,1.02),(.29,.14,.85),(-.20,.09,.66)],.0028,'Tire_Rubber',2)
tube('MirrorStem',[(.37,-.25,1.155),(.355,-.25,1.30),(.335,-.30,1.36)],.005,'Oxidized_Steel',2)
ellipsoid('MirrorBack',(.335,-.31,1.365),(.015,.067,.05),'Black_Frame',24,12)
ellipsoid('MirrorFace',(.318,-.31,1.365),(.003,.060,.043),'Oxidized_Steel',24,12)
rod('Speedometer',(.383,0,1.083),(.365,0,1.163),.050,'Black_Frame',32)
rod('SpeedometerFace',(.365,0,1.161),(.364,0,1.166),.044,'Black_Frame',32)
for i in range(11):
    a=math.radians(-130+i*26)
    x,y=.364+.035*math.sin(a),.035*math.cos(a)
    box('GaugeTick',(x,y,1.167),(.004,.0017,.0008),'Warm_Ivory',0)
rod('GaugeNeedle',(.364,0,1.169),(.343,-.019,1.169),.0012,'Tail_Red',6)

print('Built original geometry',len(parts),'parts',flush=True)
# Apply geometry and put one material atlas on the static preview asset.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
bike = bpy.context.object
bike.name = 'Dusty_Red_Motorcycle'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.context.scene.cursor.location=(0,0,0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False) if hasattr(bpy.ops.mesh,'normals_make_consistent') else None
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.0025, area_weight=.25)
bpy.ops.object.mode_set(mode='OBJECT')
triangulate=bike.modifiers.new('Stable triangles and tangents','TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=triangulate.name)

scene = bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.samples=16
scene.cycles.use_denoising=True
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX'
    prefs.get_devices()
    for device in prefs.devices:
        device.use=device.type!='CPU'
    if any(d.use for d in prefs.devices):
        scene.cycles.device='GPU'
except Exception as error:
    print('Using CPU for baking',str(error),flush=True)
scene.render.bake.margin=12
scene.render.bake.use_clear=True
SIZE=4096


def bake_channels(name, channel):
    image=bpy.data.images.new(name,width=SIZE,height=SIZE,alpha=True)
    image.colorspace_settings.name='sRGB' if channel=='color' else 'Non-Color'
    for mat in bike.data.materials:
        ns,links=mat.node_tree.nodes,mat.node_tree.links
        color,metal,rough,bs,out=shader_channels[mat.name]
        target=ns.new('ShaderNodeTexImage')
        target.image=image
        ns.active=target
        if channel=='normal':
            links.new(bs.outputs['BSDF'],out.inputs['Surface'])
        else:
            emission=ns.new('ShaderNodeEmission')
            if channel=='color':
                links.new(color,emission.inputs['Color'])
            else:
                combine=ns.new('ShaderNodeCombineColor')
                combine.mode='RGB'
                links.new(metal,combine.inputs[0])
                links.new(rough,combine.inputs[1])
                combine.inputs[2].default_value=1
                links.new(combine.outputs[0],emission.inputs['Color'])
            links.new(emission.outputs[0],out.inputs['Surface'])
    print('Baking',name,flush=True)
    bpy.ops.object.bake(type='NORMAL' if channel=='normal' else 'EMIT')
    image.filepath_raw=str(TEX/(name+'.png'))
    image.file_format='PNG'
    image.save()
    return image


base=bake_channels('Motorcycle_BaseColor','color')
normal=bake_channels('Motorcycle_Normal','normal')
packed=bake_channels('Motorcycle_Packed','mask')
pixels=np.empty(SIZE*SIZE*4,dtype=np.float32)
packed.pixels.foreach_get(pixels)
pixels=pixels.reshape((-1,4))
metal=pixels[:,0].copy()
rough=pixels[:,1].copy()


def save_data_map(name,data):
    image=bpy.data.images.new(name,width=SIZE,height=SIZE,alpha=True)
    image.colorspace_settings.name='Non-Color'
    image.pixels.foreach_set(data.reshape(-1))
    image.filepath_raw=str(TEX/(name+'.png'))
    image.file_format='PNG'
    image.save()
    return image


mask=np.ones_like(pixels)
mask[:,0]=metal
mask[:,2]=0
mask[:,3]=1-rough
hdrp=save_data_map('Motorcycle_HDRPMask',mask)
orm=np.ones_like(pixels)
orm[:,1]=rough
orm[:,2]=metal
orm_image=save_data_map('Motorcycle_ORM',orm)
# Packed is a temporary bake, kept outside Assets after conversion.
(TEX/'Motorcycle_Packed.png').replace(HERE/'Motorcycle_Packed.bake.png')

atlas=bpy.data.materials.new('Motorcycle_Weathered_Atlas')
atlas.use_nodes=True
nodes,links=atlas.node_tree.nodes,atlas.node_tree.links
principled=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
for image,kind in ((base,'color'),(normal,'normal'),(orm_image,'orm')):
    n=nodes.new('ShaderNodeTexImage'); n.image=image
    if kind=='color':
        links.new(n.outputs['Color'],principled.inputs['Base Color'])
    elif kind=='normal':
        nm=nodes.new('ShaderNodeNormalMap')
        links.new(n.outputs['Color'],nm.inputs['Color'])
        links.new(nm.outputs[0],principled.inputs['Normal'])
    else:
        separate=nodes.new('ShaderNodeSeparateColor'); separate.mode='RGB'
        links.new(n.outputs['Color'],separate.inputs[0])
        links.new(separate.outputs[1],principled.inputs['Roughness'])
        links.new(separate.outputs[2],principled.inputs['Metallic'])
for p in bike.data.polygons:
    p.material_index=0
bike.data.materials.clear()
bike.data.materials.append(atlas)
bike.data.calc_loop_triangles()
report={'source':'Original procedural geometry and materials; no third-party model or image used.',
        'triangles':len(bike.data.loop_triangles),'vertices':len(bike.data.vertices),
        'size_xyz_metres':list(bike.dimensions),'texture_resolution':SIZE,'blender':bpy.app.version_string}
(HERE/'asset_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.export_scene.fbx(filepath=str(ASSET/'Dusty_Red_Motorcycle.fbx'),use_selection=True,
                        object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
                        add_leaf_bones=False,bake_anim=False,path_mode='STRIP')
bpy.ops.export_scene.gltf(filepath=str(HERE/'Dusty_Red_Motorcycle.glb'),export_format='GLB',use_selection=True,
                         export_yup=True,export_animations=False,export_tangents=True)

# Neutral studio preview. Stage geometry is not included in either model export.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.009))
floor=bpy.context.object
mat=bpy.data.materials.new('Preview_Floor');mat.diffuse_color=(.105,.118,.123,1)
mat.use_nodes=True
bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs['Base Color'].default_value=(.105,.118,.123,1)
bs.inputs['Roughness'].default_value=.84
floor.data.materials.append(mat)
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.38,.43,.48,1)
scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.45
for name,pos,power,size in [('Key',(2.4,-3.8,5),650,4),('Fill',(-2,-1,2.5),260,3),('Rim',(-1,3,4),850,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=pos
    obj.rotation_euler=(Vector((0,0,.65))-obj.location).to_track_quat('-Z','Y').to_euler()
camera_data=bpy.data.cameras.new('PreviewCamera')
camera=bpy.data.objects.new('PreviewCamera',camera_data)
scene.collection.objects.link(camera);scene.camera=camera
camera.location=(2.9,-4.2,2.35)
camera.rotation_euler=(Vector((-.04,0,.64))-camera.location).to_track_quat('-Z','Y').to_euler()
camera_data.type='ORTHO';camera_data.ortho_scale=2.72
scene.render.resolution_x=1600;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.cycles.samples=64
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(PREVIEW/'motorcycle_three_quarter.png')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'Dusty_Red_Motorcycle.blend'))
bpy.ops.render.render(write_still=True)
camera.location=(.2,-4.5,1.25)
camera.rotation_euler=(Vector((-.04,0,.66))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(PREVIEW/'motorcycle_side.png')
bpy.ops.render.render(write_still=True)
print('COMPLETE',json.dumps(report),flush=True)
