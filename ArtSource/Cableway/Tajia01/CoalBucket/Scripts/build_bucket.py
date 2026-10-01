"""Build the approved coal bucket silhouette with the tower's accepted palette."""
import bpy
import bmesh
import math
from mathutils import Vector
from pathlib import Path

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia01\CoalBucket')
scene=bpy.data.scenes.new('Tajia01_CoalBucket_Rebuilt')
bpy.context.window.scene=scene
parts=bpy.data.collections.new('CoalBucket_Fabricated_Parts');scene.collection.children.link(parts)
paint=bpy.data.materials['Tower_Weathered_RedOxide_Steel'].copy();paint.name='Bucket_WarmGray_OldPaint'
bare=bpy.data.materials['Tower_DarkSteel_Hardware'].copy();bare.name='Bucket_DarkSteel_Hardware'
inside=bpy.data.materials.new('Bucket_CoalDust_Interior')
bs=next(n for n in inside.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs['Base Color'].default_value=(.012,.011,.010,1)
bs.inputs['Roughness'].default_value=.91
bs.inputs['Metallic'].default_value=.06
noise=inside.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=55;noise.inputs['Detail'].default_value=3
geometry=inside.node_tree.nodes.new('ShaderNodeNewGeometry');inside.node_tree.links.new(geometry.outputs['Position'],noise.inputs['Vector'])
ramp=inside.node_tree.nodes.new('ShaderNodeValToRGB')
ramp.color_ramp.elements[0].color=(.008,.007,.006,1);ramp.color_ramp.elements[1].color=(.023,.021,.018,1)
inside.node_tree.links.new(noise.outputs['Fac'],ramp.inputs['Fac']);inside.node_tree.links.new(ramp.outputs[0],bs.inputs['Base Color'])
bump=inside.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.13;bump.inputs['Distance'].default_value=.007
inside.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);inside.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])

def enum_set(owner,prop,value):
    choices={v.identifier for v in owner.bl_rna.properties[prop].enum_items}
    assert value in choices,(prop,value,choices)
    setattr(owner,prop,value)

def mesh_obj(name,vertices,faces,mat=paint,bevel=.007,indices=None):
    me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    ob=bpy.data.objects.new(name,me);parts.objects.link(ob)
    for m in (mat if isinstance(mat,list) else [mat]):me.materials.append(m)
    for i,p in enumerate(me.polygons):
        p.use_smooth=True
        if indices:p.material_index=indices[i]
    if bevel:
        mod=ob.modifiers.new('Steel edge radius','BEVEL');mod.width=bevel;mod.segments=2;enum_set(mod,'limit_method','ANGLE')
    normal=ob.modifiers.new('Fabricated normals','WEIGHTED_NORMAL');normal.keep_sharp=True
    return ob

def prism(name,profile,a,b,mat=paint,bevel=.006,up=(0,1,0)):
    a,b=Vector(a),Vector(b);axis=(b-a).normalized();ref=Vector(up)
    if abs(axis.dot(ref))>.98:ref=Vector((1,0,0))
    u=ref.cross(axis).normalized();v=axis.cross(u).normalized();n=len(profile)
    vertices=[tuple(end+u*x+v*y) for end in (a,b) for x,y in profile]
    faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh_obj(name,vertices,faces,mat,bevel)

def box(name,c,size,mat=paint,bevel=.006):
    x,y,z=c;w,d,h=size
    return prism(name,[(-w/2,-d/2),(w/2,-d/2),(w/2,d/2),(-w/2,d/2)],(x,y,z-h/2),(x,y,z+h/2),mat,bevel)

def rod(name,c,r,depth,normal=(0,1,0),sides=20,mat=bare,bevel=.004):
    c=Vector(c);n=Vector(normal).normalized()
    profile=[(r*math.cos(2*math.pi*i/sides),r*math.sin(2*math.pi*i/sides)) for i in range(sides)]
    return prism(name,profile,c-n*depth/2,c+n*depth/2,mat,bevel)

def bolt(c,normal=(0,-1,0),r=.035):
    c=Vector(c);n=Vector(normal)
    rod('Bolt washer',c,r*1.35,.014,n,12,bare,0)
    rod('Hex bolt',c+n*.022,r,.040,n,6,bare,.003)

def band(name,a,b,width,depth,mat=paint):
    return prism(name,[(-width/2,-depth/2),(width/2,-depth/2),(width/2,depth/2),(-width/2,depth/2)],a,b,mat,.006)

def plate(name,outline,y,thickness,mat=paint):
    n=len(outline);vertices=[(x,y+d,z) for d in (-thickness/2,thickness/2) for x,z in outline]
    faces=[tuple(range(n)),tuple(reversed(range(n,2*n)))]+[(i,i+n,(i+1)%n+n,(i+1)%n) for i in range(n)]
    return mesh_obj(name,vertices,faces,mat,.009)

def rounded_rect(hx,hy,r):
    points=[]
    for cx,cy,start in [(hx-r,hy-r,0),(-hx+r,hy-r,90),(-hx+r,-hy+r,180),(hx-r,-hy+r,270)]:
        for j in range(4):
            a=math.radians(start+j*30);points.append((cx+r*math.cos(a),cy+r*math.sin(a)))
    return points

# A closed steel shell with an open mouth, wall thickness and a curved lower body.
outer=[(.37,.31,.15,.10),(.48,.38,.21,.11),(.60,.46,.35,.12),(.76,.58,.80,.09),(1.00,.76,1.78,.065)]
inner=[(.34,.28,.205,.08),(.445,.345,.265,.085),(.56,.42,.40,.09),(.718,.538,.825,.07),(.958,.718,1.78,.047)]
vertices=[]
for rings in (outer,inner):
    for hx,hy,z,r in rings:vertices.extend((x,y,z) for x,y in rounded_rect(hx,hy,r))
n=16;layers=len(outer);offset=n*layers;faces=[];indices=[]
for j in range(layers-1):
    for i in range(n):
        nxt=(i+1)%n
        faces.append((j*n+i,j*n+nxt,(j+1)*n+nxt,(j+1)*n+i));indices.append(0)
        faces.append((offset+j*n+nxt,offset+j*n+i,offset+(j+1)*n+i,offset+(j+1)*n+nxt));indices.append(1)
faces.append(tuple(reversed(range(n))));indices.append(0)
faces.append(tuple(range(offset,offset+n)));indices.append(1)
for i in range(n):
    nxt=(i+1)%n;a=(layers-1)*n
    faces.append((a+i,a+nxt,offset+a+nxt,offset+a+i));indices.append(2)
shell=mesh_obj('Open thick steel hopper',vertices,faces,[paint,inside,bare],.009,indices)

# Folded rectangular mouth, with actual joined corner pieces.
for sy in (-1,1):
    box('Long rolled rim',(0,sy*.764,1.786),(1.99,.074,.092),bare,.016)
for sx in (-1,1):
    box('Side rolled rim',(sx*.998,0,1.786),(.074,1.49,.092),bare,.016)
for sx in (-1,1):
    for sy in (-1,1):
        box('Rim corner splice',(sx*.938,sy*.703,1.82),(.17,.17,.030),paint,.008)
        bolt((sx*.942,sy*.70,1.843),(0,0,1),.024)

# Narrow seams and rivets follow the sloped front and rear plates.
for sy in (-1,1):
    normal=Vector((0,sy,-.34)).normalized()
    for sx in (-1,1):
        band('Hopper sheet seam',(sx*.45,sy*.493,.43),(sx*.67,sy*.786,1.71),.055,.018,bare)
        for j in range(7):
            z=.49+j*.184;t=(z-.43)/1.28
            bolt((sx*(.45+.22*t),sy*(.493+.293*t+.012),z),normal,.022)
    for x in (-.67,-.34,0,.34,.67):bolt((x,sy*.81,1.725),(0,sy,0),.023)

# Trunnion reinforcements and the two broad bent suspension arms.
pivot_z=.98
for sx in (-1,1):
    prism('Side trunnion reinforcing strip',[(-.15,-.022),(.15,-.022),(.15,.022),(-.15,.022)],(sx*.75,0,.70),(sx*.985,0,1.62),paint,.006,up=(1,0,0))
    rod('Trunnion backing disc',(sx*.867,0,pivot_z),.225,.072,(sx,0,0),32,paint,.008)
    rod('Bucket side pivot axle',(sx*.99,0,pivot_z),.108,.27,(sx,0,0),24,bare,.009)
    outline=[(sx*x,z) for x,z in [(.96,.87),(1.155,.91),(1.235,1.58),(.78,2.67),(.30,3.37),(.235,3.64),(.04,3.64),(.12,3.31),(.58,2.58),(1.015,1.54)]]
    plate('Bent suspension yoke',outline,0,.145,paint)
    rod('Trunnion outside collar',(sx*1.15,0,pivot_z),.165,.063,(sx,0,0),28,bare,.008)
    rod('Large pivot hex nut',(sx*1.205,0,pivot_z),.116,.07,(sx,0,0),6,bare,.006)
    for z,x in [(1.34,1.09),(1.55,1.105),(2.08,.90),(2.31,.80),(3.36,.19)]:
        bolt((sx*x,-.09,z),(0,-1,0),.032)
    # A small pivot grease nipple, placed on the axle end.
    rod('Pivot grease nipple',(sx*1.25,0,pivot_z),.028,.035,(sx,0,0),8,bare,.002)

# Low front mechanism visible on the approved silhouette.
rod('Lower front boss flange',(0,-.44,.34),.19,.075,(0,-1,-.17),32,paint,.008)
rod('Lower front boss body',(0,-.51,.328),.132,.15,(0,-1,-.17),24,bare,.011)
rod('Lower front retaining cap',(0,-.602,.308),.143,.052,(0,-1,-.17),12,bare,.012)
for a in (35,145,215,325):
    angle=math.radians(a)
    bolt((.153*math.cos(angle),-.493,.34+.153*math.sin(angle)),(0,-1,0),.025)
band('Lower boss retaining latch',(.185,-.49,.44),(.18,-.58,.20),.046,.045,bare)

# Trolley, a central hanging pin and two properly grooved running wheels.
rod('Main suspension pin',(0,0,3.55),.105,.50,(0,1,0),28,bare,.008)
for sy in (-1,1):
    rod('Suspension pin collar',(0,sy*.25,3.55),.144,.052,(0,1,0),24,bare,.006)
    bolt((0,sy*.292,3.55),(0,sy,0),.087)
    plate('Trolley cheek plate',[(-.13,3.51),(.13,3.51),(.17,3.89),(.11,4.01),(-.11,4.01),(-.17,3.89)],sy*.19,.065,paint)
    box('Trolley axle bridge',(0,sy*.25,3.91),(1.05,.085,.18),paint,.015)

def wheel(name,c,profile,segments=48):
    cx,cy,cz=c;verts=[]
    for r,y in profile:
        for i in range(segments):
            a=i*2*math.pi/segments;verts.append((cx+r*math.cos(a),cy+y,cz+r*math.sin(a)))
    faces=[]
    for j in range(len(profile)):
        k=(j+1)%len(profile)
        for i in range(segments):faces.append((j*segments+i,j*segments+(i+1)%segments,k*segments+(i+1)%segments,k*segments+i))
    return mesh_obj(name,verts,faces,bare,.002)

profile=[(.070,-.092),(.208,-.092),(.25,-.075),(.25,-.043),(.213,-.032),(.196,-.018),(.196,.018),(.213,.032),(.25,.043),(.25,.075),(.208,.092),(.070,.092)]
for x in (-.38,.38):
    wheel('Grooved trolley wheel',(x,0,3.94),profile)
    rod('Trolley through axle',(x,0,3.94),.06,.65,(0,1,0),20,bare,.005)
    for sy in (-1,1):
        rod('Wheel hub',(x,sy*.103,3.94),.105,.045,(0,1,0),24,bare,.005)
        bolt((x,sy*.32,3.94),(0,sy,0),.053)
box('Central cable keeper',(0,0,3.72),(.16,.20,.067),bare,.009)
scene['attachment_pivot_authoring']=[0,0,3.72]
scene['authoring_to_export_scale']=bpy.data.scenes['Tajia01_Tower_Rebuilt']['authoring_to_export_scale']
scene['reference_image']='../Concepts/CoalBucket_Concept_v01.png'
scene['palette']='Matches approved muted warm gray tower materials'
scene['scope']='Independent coal hopper, yoke and trolley; no tower or cable geometry'

# Neutral studio presentation, separate from the exported assembly.
stage=bpy.data.collections.new('CoalBucket_Preview_Stage');scene.collection.children.link(stage)
floor_mat=bpy.data.materials['Preview warm gray floor']
floor=box('Bucket preview floor',(0,0,-.025),(80,80,.05),floor_mat,0)
parts.objects.unlink(floor);stage.objects.link(floor)
scene.world=bpy.data.worlds['Tower neutral studio world']
for name,loc,energy,size in [('Bucket key',(-4,-5,7),480,4),('Bucket fill',(5,-2,5),320,3),('Bucket rim',(1,4,6),520,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.size=size
    ob=bpy.data.objects.new(name,data);stage.objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector((0,0,2))-ob.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('CoalBucket Camera');enum_set(data,'type','ORTHO');data.ortho_scale=4.95
camera=bpy.data.objects.new('CoalBucket Camera',data);stage.objects.link(camera)
camera.location=(6,-9,5.6);camera.rotation_euler=(Vector((0,0,2.03))-camera.location).to_track_quat('-Z','Y').to_euler();scene.camera=camera
scene.render.engine='CYCLES';scene.cycles.device=bpy.data.scenes['Tajia01_Tower_Rebuilt'].cycles.device
scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
enum_set(scene.render.image_settings,'file_format','PNG')
scene.render.filepath=str(ROOT/'Previews'/'CoalBucket_v01_Overview.png')
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        enum_set(area.spaces.active.shading,'type','MATERIAL');area.spaces.active.overlay.show_overlays=False
        area.spaces.active.region_3d.view_location=(0,0,2.1)
        area.spaces.active.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
        area.spaces.active.region_3d.view_distance=8
        enum_set(area.spaces.active.region_3d,'view_perspective','ORTHO')
print('Bucket built',len(parts.objects),'editable mesh parts')
