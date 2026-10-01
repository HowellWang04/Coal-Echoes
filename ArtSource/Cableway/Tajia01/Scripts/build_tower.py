"""Build the approved standalone tower in a new Blender scene through MCP."""
import bpy
import math
import random
from mathutils import Vector

random.seed(19)
ROOT = r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia01'
scene = bpy.data.scenes.new('Tajia01_Tower_Rebuilt')
bpy.context.window.scene = scene
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
    coarse.inputs['Scale'].default_value = 3.8 if variant == 0 else (5.5 if variant == 1 else 9.0)
    coarse.inputs['Detail'].default_value = 4.0
    coarse.inputs['Roughness'].default_value = .72
    links.new(pos.outputs['Position'], coarse.inputs['Vector'])
    fine = nodes.new('ShaderNodeTexNoise')
    fine.inputs['Scale'].default_value = 145.0
    fine.inputs['Detail'].default_value = 3.0
    links.new(pos.outputs['Position'], fine.inputs['Vector'])
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.label = 'Charcoal steel, oxide, surviving red primer'
    if variant == 0:
        colors = [(.18,(.022,.027,.030,1)),(.37,(.048,.054,.056,1)),(.52,(.079,.079,.069,1)),(.58,(.053,.050,.040,1)),(.615,(.100,.043,.021,1)),(.68,(.123,.033,.020,1)),(.81,(.185,.076,.033,1))]
    elif variant == 1:
        colors = [(.2,(.025,.029,.032,1)),(.47,(.061,.069,.068,1)),(.55,(.093,.060,.031,1)),(.64,(.053,.047,.038,1)),(.8,(.12,.13,.125,1))]
    else:
        colors = [(.2,(.12,.116,.103,1)),(.48,(.22,.212,.187,1)),(.65,(.30,.283,.247,1)),(.85,(.17,.162,.14,1))]
    cr=ramp.color_ramp
    cr.elements.remove(cr.elements[1])
    cr.elements[0].position,cr.elements[0].color=colors[0]
    for p,c in colors[1:]:
        cr.elements.new(p).color=c
    links.new(coarse.outputs['Fac'],ramp.inputs['Fac'])
    links.new(ramp.outputs['Color'],bs.inputs['Base Color'])
    rough=nodes.new('ShaderNodeMapRange')
    rough.inputs['From Min'].default_value=.2
    rough.inputs['From Max'].default_value=.8
    rough.inputs['To Min'].default_value=.61 if variant!=2 else .85
    rough.inputs['To Max'].default_value=.91
    links.new(fine.outputs['Fac'],rough.inputs['Value'])
    links.new(rough.outputs['Result'],bs.inputs['Roughness'])
    metal=nodes.new('ShaderNodeValToRGB')
    metal.color_ramp.elements[0].position=.55 if variant == 0 else .42
    metal.color_ramp.elements[0].color=(.76,.76,.76,1) if variant!=2 else (0,0,0,1)
    metal.color_ramp.elements[1].position=.64 if variant == 0 else .55
    metal.color_ramp.elements[1].color=(.08,.08,.08,1) if variant!=2 else (0,0,0,1)
    links.new(coarse.outputs['Fac'],metal.inputs['Fac'])
    links.new(metal.outputs['Color'],bs.inputs['Metallic'])
    bump=nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value=.22
    bump.inputs['Distance'].default_value=.014 if variant!=2 else .024
    links.new(fine.outputs['Fac'],bump.inputs['Height'])
    links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    mat.diffuse_color=colors[2][1]
    return mat

paint=material('Tower_Weathered_RedOxide_Steel',0)
bare=material('Tower_DarkSteel_Hardware',1)
concrete=material('Tower_Footing_Concrete',2)

def mesh_obj(name,verts,faces,mat=paint,bevel=.012):
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    ob=bpy.data.objects.new(name,mesh)
    parts.objects.link(ob)
    mesh.materials.append(mat)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    if bevel:
        mod=ob.modifiers.new('Fabricated edge radius','BEVEL')
        mod.width=bevel
        mod.segments=1
        enum_set(mod,'limit_method','ANGLE')
    if name.startswith(('Bolt washer','Ladder rung','Walkway grating')):
        for mod in list(ob.modifiers):
            if mod.type=='BEVEL':
                ob.modifiers.remove(mod)
    normal_mod=ob.modifiers.new('Weighted fabrication normals','WEIGHTED_NORMAL')
    normal_mod.keep_sharp=True
    return ob

def prism(name,profile,a,b,mat=paint,bevel=.009,up=(0,1,0)):
    a,b=Vector(a),Vector(b)
    axis=(b-a).normalized()
    ref=Vector(up)
    if abs(axis.dot(ref))>.98:
        ref=Vector((1,0,0))
    u=ref.cross(axis).normalized()
    v=axis.cross(u).normalized()
    verts=[tuple(end+u*x+v*y) for end in (a,b) for x,y in profile]
    n=len(profile)
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh_obj(name,verts,faces,mat,bevel)

def box(name,center,size,mat=paint,bevel=.008):
    x,y,z=center
    w,d,h=size
    return prism(name,[(-w/2,-d/2),(w/2,-d/2),(w/2,d/2),(-w/2,d/2)],(x,y,z-h/2),(x,y,z+h/2),mat,bevel)

def angle(name,a,b,w=.14,t=.035,mat=paint):
    p=[(-w/2,-w/2),(w/2,-w/2),(w/2,-w/2+t),(-w/2+t,-w/2+t),(-w/2+t,w/2),(-w/2,w/2)]
    return prism(name,p,a,b,mat,.008)

def channel(name,a,b,w=.24,d=.13,t=.035,mat=paint):
    p=[(-w/2,-d/2),(w/2,-d/2),(w/2,-d/2+t),(-w/2+t,-d/2+t),(-w/2+t,d/2-t),(w/2,d/2-t),(w/2,d/2),(-w/2,d/2)]
    return prism(name,p,a,b,mat,.008)

def cylinder(name,center,radius,depth,normal=(0,0,1),segments=12,mat=bare,bevel=.003):
    c=Vector(center); n=Vector(normal).normalized()
    profile=[(radius*math.cos(i*2*math.pi/segments),radius*math.sin(i*2*math.pi/segments)) for i in range(segments)]
    return prism(name,profile,c-n*depth/2,c+n*depth/2,mat,bevel)

def bolt(center,normal=(0,-1,0),r=.044):
    c=Vector(center); n=Vector(normal)
    cylinder('Bolt washer',c,r*1.40,.020,n,12,bare,.002)
    cylinder('Hex bolt',c+n*.035,r,.053,n,6,bare,.003)

def plate(name,outline,y,thickness=.035,mat=paint):
    verts=[(x,y+dy,z) for dy in (-thickness/2,thickness/2) for x,z in outline]
    n=len(outline)
    faces=[tuple(range(n)),tuple(reversed(range(n,2*n)))]
    faces += [(i,i+n,(i+1)%n+n,(i+1)%n) for i in range(n)]
    return mesh_obj(name,verts,faces,mat,.008)

def half(z):
    return (1.32-.070*z,1.00-.055*z)

# Four tapered main columns, with real L profiles rather than solid rods.
levels=[.30,2.34,4.12,5.90,7.94,9.88]
for sx in (-1,1):
    for sy in (-1,1):
        a=half(levels[0]); b=half(levels[-1])
        angle('Main tapered angle column',(sx*a[0],sy*a[1],levels[0]),(sx*b[0],sy*b[1],levels[-1]),.27,.055)
        box('Concrete foundation pad',(sx*a[0],sy*a[1],.12),(.74,.62,.24),concrete,.035)
        box('Column sole plate',(sx*a[0],sy*a[1],.29),(.64,.54,.09),bare,.014)
        for ox in (-.23,.23):
            for oy in (-.18,.18):
                bolt((sx*a[0]+ox,sy*a[1]+oy,.355),(0,0,1),.05)

# Collar channels and gussets at every floor band.
for z in levels:
    hx,hy=half(z)
    for sy in (-1,1):
        channel('Front rear collar',(-hx-.12,sy*(hy+.10),z),(hx+.12,sy*(hy+.10),z),.19,.15)
        for sx in (-1,1):
            x=sx*hx
            outline=[(x-.24,z+.20),(x+.24,z+.20),(x+.24,z-.15),(x+.13,z-.27),(x-.24,z-.15)]
            plate('Column gusset',outline,sy*(hy+.20),.038)
            for dx,dz in [(-.14,.10),(.14,.10),(-.13,-.09),(.13,-.09)]:
                bolt((x+dx,sy*(hy+.225),z+dz),(0,sy,0),.041)
    for sx in (-1,1):
        channel('Side collar',(sx*(hx+.10),-hy-.13,z),(sx*(hx+.10),hy+.13,z),.19,.15)
        for sy in (-1,1):
            box('Side splice plate',(sx*(hx+.20),sy*hy,z),(.04,.40,.38),paint,.012)
            for oy in (-.12,.12):
                bolt((sx*(hx+.225),sy*hy+oy,z+.09),(sx,0,0),.04)
                bolt((sx*(hx+.225),sy*hy+oy,z-.09),(sx,0,0),.04)

# Cross braces on all four faces. Offset the crossing members slightly.
for z0,z1 in zip(levels[:-1],levels[1:]):
    h0=half(z0); h1=half(z1)
    for sy in (-1,1):
        for direction in (-1,1):
            off=.055 if direction==1 else -.015
            angle('Face diagonal brace',(-direction*h0[0],sy*(h0[1]+off),z0+.11),(direction*h1[0],sy*(h1[1]+off),z1-.12),.125,.026,bare if direction==1 else paint)
        zm=(z0+z1)/2
        hm=half(zm)
        plate('Crossing fish plate',[(-.16,zm-.13),(.16,zm-.13),(.16,zm+.13),(-.16,zm+.13)],sy*(hm[1]+.12),.028,bare)
        bolt((0,sy*(hm[1]+.143),zm),(0,sy,0),.045)
    for sx in (-1,1):
        for direction in (-1,1):
            angle('Side diagonal brace',(sx*(h0[0]-.04),-direction*h0[1],z0+.10),(sx*(h1[0]+.02),direction*h1[1],z1-.11),.12,.027)

# Wide maintenance outrigger at the original lower-middle level.
deck_z=4.18
for y in (-.84,.72):
    channel('Outrigger longitudinal channel',(-3.15,y,deck_z),(3.15,y,deck_z),.25,.17,.04)
    for sx in (-1,1):
        angle('Under platform knee',(sx*half(2.34)[0],y,2.34),(sx*3.05,y,deck_z-.05),.22,.043)
        angle('Upper outrigger stay',(sx*1.08,y,5.46),(sx*3.02,y,4.29),.14,.030)
        angle('Outer raised bracket',(sx*3.02,y,4.28),(sx*2.62,y,5.31),.14,.030)
        angle('Inner raised bracket',(sx*1.13,y,4.31),(sx*2.62,y,5.31),.13,.028)
        for x,z in [(sx*3.02,4.23),(sx*2.62,5.31),(sx*1.1,4.23)]:
            plate('Outrigger joint plate',[(x-.16,z-.12),(x+.16,z-.12),(x+.16,z+.12),(x-.16,z+.12)],y-.10,.034)
            bolt((x-.07,y-.124,z),(0,-1,0),.041)
            bolt((x+.07,y-.124,z),(0,-1,0),.041)
for x in (-3.08,-2.1,-1.15,0,1.15,2.1,3.08):
    angle('Platform cross member',(x,-.91,deck_z+.06),(x,.79,deck_z+.06),.11,.027,bare)
# Narrow open steel grating around the tower, leaving the shaft unobstructed.
for sx in (-1,1):
    for k in range(14):
        x=sx*(1.38+k*.122)
        box('Walkway grating bearing bar',(x,-.05,4.32),(.025,1.47,.05),bare,.002)
    for y in (-.59,-.17,.25,.62):
        box('Walkway grating cross rod',(sx*2.18,y,4.345),(1.79,.018,.02),bare,.002)

# Wide shallow top crosshead with tapered web panels and narrow flanges.
for y in (-.44,.44):
    channel('Top crosshead upper flange',(-3.04,y,10.13),(3.04,y,10.13),.23,.20,.038)
    plate('Crosshead left tapered web',[(-3.00,10.06),(-.65,10.06),(-.65,9.10)],y,.035)
    plate('Crosshead right tapered web',[(.65,9.10),(.65,10.06),(3.00,10.06)],y,.035)
    angle('Crosshead lower diagonal flange',(-3.02,y,10.05),(-.64,y,9.06),.14,.032)
    angle('Crosshead lower diagonal flange',(.64,y,9.06),(3.02,y,10.05),.14,.032)
    channel('Crosshead bearing collar',(-.78,y,9.09),(.78,y,9.09),.18,.14,.03)
    for x in (-2.94,-1.90,-.70,.70,1.90,2.94):
        lower=9.11 if abs(x)<.8 else (9.60 if abs(x)<2 else 9.96)
        angle('Crosshead web stiffener',(x,y-.03,lower),(x,y-.03,10.20),.105,.025,bare)
        for z in (10.10,lower+.08):
            bolt((x,y-.105,z),(0,-1,0),.041)
for x in (-3.0,-.64,.64,3.0):
    channel('Crosshead transverse tie',(x,-.56,10.15),(x,.56,10.15),.13,.14,.03,bare)

# Thin right-hand outer vertical tie, characteristic of the original silhouette.
for y in (-.47,.47):
    angle('Outer vertical hanger',(3.0,y,4.28),(3.0,y,10.14),.09,.024)
    for z in (4.36,10.06):
        box('Outer tie splice',(3.0,y-.06,z),(.19,.025,.28),bare,.005)
        bolt((3.0,y-.082,z-.07),(0,-1,0),.035)
        bolt((3.0,y-.082,z+.07),(0,-1,0),.035)

# Full height access ladder, tilted gently to follow the taper of the front face.
def ladder_y(z):
    return -1.34+.067*z
for x in (-.30,.30):
    channel('Ladder side rail',(x,ladder_y(.15),.15),(x,ladder_y(10.30),10.30),.082,.073,.020,bare)
for i in range(35):
    z=.33+i*.286
    cylinder('Ladder rung',(0,ladder_y(z)-.022,z),.024,.66,(1,0,0),10,bare,.002)
for z in (1.1,2.34,4.12,5.90,7.94,9.88):
    for x in (-.30,.30):
        angle('Ladder wall bracket',(x,ladder_y(z)+.02,z),(x,-half(z)[1],z),.068,.02,bare)
        bolt((x,ladder_y(z)-.056,z),(0,-1,0),.032)

# Compact top end saddle and axle details, without the suspended bucket.
for x in (-3.02,3.02):
    for y in (-.37,.37):
        box('Crosshead end bearing plate',(x,y,10.13),(.23,.075,.37),bare,.016)
        cylinder('Top crosshead axle',(x,y-.05,10.14),.09,.15,(0,1,0),16,bare,.006)
        bolt((x,y-.14,10.14),(0,-1,0),.055)

scene['reference_image']='Concepts/Tower_Concept_v01.png'
scene['scope']='Standalone tower rebuilt from approved concept; coal bucket is excluded.'
scene['authoring_scale']='Ten times original FBX size for material authoring. Export scale is 0.1.'
print('BUILT',len(parts.objects),'objects',sum(len(o.data.polygons) for o in parts.objects),'base polygons')

# Preview stage is separate from the export collection.
stage=bpy.data.collections.new('Preview_Stage')
scene.collection.children.link(stage)
floor_mat=bpy.data.materials.new('Preview warm gray floor')
floor_bs=next(n for n in floor_mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
floor_bs.inputs['Base Color'].default_value=(.36,.345,.32,1)
floor_bs.inputs['Roughness'].default_value=.9
floor=box('Preview floor',(0,0,-.09),(200,200,.10),floor_mat,0)
parts.objects.unlink(floor);stage.objects.link(floor)
world=bpy.data.worlds.new('Tower neutral studio world')
scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs['Color'].default_value=(.37,.39,.42,1)
bg.inputs['Strength'].default_value=.5
for name,loc,energy,size in [('Key',(-5,-7,14),1900,7),('Fill',(7,-4,9),1400,6),('Rim',(1,5,12),2200,5)]:
    data=bpy.data.lights.new(name,'AREA')
    data.energy=energy;data.shape='DISK' if 'DISK' in {v.identifier for v in data.bl_rna.properties['shape'].enum_items} else data.shape
    data.size=size
    ob=bpy.data.objects.new(name,data);stage.objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector((0,0,5))-ob.location).to_track_quat('-Z','Y').to_euler()
cam_data=bpy.data.cameras.new('Tower Preview Camera')
enum_set(cam_data,'type','ORTHO')
cam_data.ortho_scale=12.25
cam=bpy.data.objects.new('Tower Preview Camera',cam_data);stage.objects.link(cam)
cam.location=(12,-22,12.5)
cam.rotation_euler=(Vector((0,0,5.10))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.camera=cam
scene.render.resolution_x=1200;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
enum_set(scene.render.image_settings,'file_format','PNG')
scene.render.filepath=ROOT+'\\Previews\\Tower_v01_Overview.png'
scene.render.engine='BLENDER_EEVEE'
scene.render.film_transparent=False
print('VIEW',scene.view_settings.view_transform,scene.view_settings.look)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        enum_set(area.spaces.active.shading,'type','MATERIAL')
        area.spaces.active.overlay.show_overlays=False
        area.spaces.active.region_3d.view_location=(0,0,5.1)
        area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
        area.spaces.active.region_3d.view_distance=16
        enum_set(area.spaces.active.region_3d,'view_perspective','ORTHO')
print('Tower build and preview stage complete')
