"""Build the approved cableway shelter in an isolated Blender process."""
import bpy, math, random, json, numpy as np
from pathlib import Path
from mathutils import Vector, Quaternion
random.seed(927)
ROOT=Path(__file__).resolve().parents[1]
SCENE=bpy.data.scenes.new('CablewayStationShell_Authoring')
bpy.context.window.scene=SCENE

def enum(owner,key,value):
    valid={x.identifier for x in owner.bl_rna.properties[key].enum_items}
    assert value in valid,(key,value,valid)
    setattr(owner,key,value)

# Portable image based PBR surfaces, with broad worn paint and restrained oxidation.
N=2048
rng=np.random.default_rng(927)
def noise(nx,ny):
    grid=rng.random((ny,nx)).astype(np.float32)
    x=np.linspace(0,nx-1,N);y=np.linspace(0,ny-1,N)
    rows=np.array([np.interp(x,np.arange(nx),row) for row in grid],np.float32)
    return np.array([np.interp(y,np.arange(ny),rows[:,i]) for i in range(N)],np.float32).T

def image(name,data,color=True):
    im=bpy.data.images.new(name,width=N,height=N,alpha=name.endswith('HDRP_Mask'))
    if not color:im.colorspace_settings.name='Non-Color'
    rgba=np.ones((N,N,4),np.float32)
    rgba[:,:,:3]=data[:,:,None] if data.ndim==2 else data
    im.pixels.foreach_set(rgba.ravel())
    im.filepath_raw=str(ROOT/'Textures'/(name+'.png'));enum(im,'file_format','PNG');im.save();im.pack()
    return im

materials={}
for kind in ['Steel','Cladding','Concrete']:
    broad=noise(11,14);medium=noise(49,61);fine=noise(237,249)
    yy=np.linspace(0,1,N)[:,None]
    if kind=='Concrete':
        value=.47+.15*(broad-.5)+.16*(medium-.5)+.12*(fine-.5)
        dark=np.clip((.33-medium)*2.1,0,.3)
        rgb=np.stack([value*.99-dark,value*.93-dark,value*.81-dark],axis=2)
        rough=np.clip(.87+fine*.1,0,1);metal=np.zeros_like(broad);height=medium*.35+fine*.55
    else:
        streak=noise(27,3)
        weather=noise(7,9)
        oxide=np.clip((weather*.60+streak*.30+medium*.10-.60)*5,0,1)
        if kind=='Cladding':oxide=np.maximum(oxide*.80,np.clip((.105-yy)*4.8,0,.48))
        oxide=np.clip(oxide,0,.88)
        gray=np.array([.36,.345,.31]) if kind=='Cladding' else np.array([.31,.295,.26])
        rust=np.array([.32,.205,.105])
        rgb=gray[None,None,:]*(1-oxide[:,:,None])+rust[None,None,:]*oxide[:,:,None]
        rgb+=(weather[:,:,None]-.5)*.10+(streak[:,:,None]-.5)*.035+(fine[:,:,None]-.5)*.025
        if kind=='Cladding':rgb*=1-np.clip((.12-yy)*1.4,0,.15)[:,:,None]
        rough=np.clip(.73+oxide*.19+(fine-.5)*.07,0,1)
        metal=.32*(1-oxide);height=(medium*.25+fine*.18+oxide*.28)
    rgb=np.clip(rgb,.025,.85)
    dx=np.roll(height,1,axis=1)-np.roll(height,-1,axis=1);dy=np.roll(height,1,axis=0)-np.roll(height,-1,axis=0)
    normal=np.stack([dx*2,dy*2,np.ones_like(dx)],axis=2);normal/=np.linalg.norm(normal,axis=2)[:,:,None];normal=normal*.5+.5
    imgs={'BaseColor':image('Station_'+kind+'_BaseColor',rgb),'Roughness':image('Station_'+kind+'_Roughness',rough,False),'Metallic':image('Station_'+kind+'_Metallic',metal,False),'Normal':image('Station_'+kind+'_Normal',normal,False)}
    image('Station_'+kind+'_ORM',np.stack([np.ones_like(metal),rough,metal],axis=2),False)
    image('Station_'+kind+'_HDRP_Mask',np.stack([metal,np.ones_like(metal),np.ones_like(metal)],axis=2),False)
    # HDRP alpha contains smoothness.
    mask=bpy.data.images['Station_'+kind+'_HDRP_Mask'];arr=np.empty(N*N*4,np.float32);mask.pixels.foreach_get(arr);arr.reshape(N,N,4)[:,:,3]=1-rough
    mask.pixels.foreach_set(arr);mask.save();mask.pack()
    mat=bpy.data.materials.new('Station_'+kind);mat.use_nodes=True
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
    for key,socket in [('BaseColor','Base Color'),('Roughness','Roughness'),('Metallic','Metallic')]:
        n=nodes.new('ShaderNodeTexImage');n.image=imgs[key];links.new(n.outputs['Color'],bs.inputs[socket])
    tex=nodes.new('ShaderNodeTexImage');tex.image=imgs['Normal'];n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.4;links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs[0],bs.inputs['Normal'])
    materials[kind]=mat
print('TEXTURES_READY',flush=True)

# Fabricated subassemblies retain their own editable meshes and material slots.
collection=bpy.data.collections.new('CablewayStationShell');SCENE.collection.children.link(collection)
parts={}
def part(name,kind):
    d={'v':[],'f':[],'uv':[],'kind':kind};parts[name]=d;return d
steel=part('StructuralSteel','Steel');fast=part('PlatesAndFasteners','Steel');wall=part('CorrugatedWalls','Cladding');roof=part('BrokenRoof','Cladding');floor=part('WalkwayGrating','Steel');rail=part('OverheadGuide','Steel');base=part('ConcretePlinth','Concrete')

def poly(d,verts,faces,uvs=None):
    off=len(d['v']);d['v'].extend(verts)
    for i,f in enumerate(faces):
        d['f'].append(tuple(off+j for j in f))
        if uvs:d['uv'].append(uvs[i])
        else:
            coords=[Vector(verts[j]) for j in f];a=coords[1]-coords[0];b=coords[-1]-coords[0]
            phase=random.random()*.8
            if len(f)==4:d['uv'].append([(phase,0),(phase+a.length/.7,0),(phase+a.length/.7,b.length/3),(phase,b.length/3)])
            else:d['uv'].append([(phase+c.x*.3,c.z*.3+c.y*.15) for c in coords])

def box(d,center,size,rot=None):
    c=Vector(center);x,y,z=[v/2 for v in size]
    v=[Vector((a*x,b*y,k*z)) for a,b,k in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    v=[tuple(c+(rot@p if rot else p)) for p in v]
    poly(d,v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])

def beam(d,a,b,w=.18,depth=None,section=True):
    a,b=Vector(a),Vector(b);length=(b-a).length;rot=(b-a).to_track_quat('Z','Y');depth=depth or w
    if not section:box(d,(a+b)/2,(w,depth,length),rot);return
    thick=min(w,depth)*.14
    box(d,(a+b)/2,(thick,depth,length),rot)
    for s in [-1,1]:box(d,(a+b)/2+rot@Vector((0,s*(depth-thick)/2,0)),(w,thick,length),rot)

def cylinder(d,center,radius,length,axis=(0,0,1),sides=8):
    q=Vector(axis).to_track_quat('Z','Y');c=Vector(center);v=[]
    for z in [-length/2,length/2]:
        for i in range(sides):v.append(tuple(c+q@Vector((radius*math.cos(i*2*math.pi/sides),radius*math.sin(i*2*math.pi/sides),z))))
    f=[tuple(range(sides-1,-1,-1)),tuple(range(sides,sides*2))]+[(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
    poly(d,v,f)

def bolts(c,plane='Y',cols=2,rows=3):
    for i in range(cols):
        for j in range(rows):
            x=(i-(cols-1)/2)*.16;z=(j-(rows-1)/2)*.19
            center=Vector(c)+Vector((x,0,z) if plane=='Y' else (0,x,z))
            axis=(0,1,0) if plane=='Y' else (1,0,0)
            cylinder(fast,center,.055,.016,axis,12);cylinder(fast,center+Vector(axis)*.023,.039,.037,axis,6)

# Station shell extents near the existing station footprint.
W=10.76;HALF=5.1;EAVE=6.85;RIDGE=8.35;FLOOR=.38
bays=[-13.55,-8.15,-2.75,2.65,8.05,13.15]
# Slightly irregular individual footing slabs keep the base from reading as one perfect block.
for side in [-1,1]:
    for j in range(11):
        y=-12.73+j*2.515
        box(base,(side*4.92,y,.11),(.98,2.495,.5))
for y in [-13.55,13.15]:box(base,(0,y,.06),(10.4,1.0,.4))
# Thin strip floor and traversable grating panels.
for y in np.arange(-12.8,12.0,1.45):
    for x in [-3.7,-1.25,1.25,3.7]:
        for xx in np.arange(x-1.18,x+1.18,.145):box(floor,(xx,y,.39),(.032,1.37,.05))
        for yy in [-.67,0,.67]:box(floor,(x,y+yy,.365),(2.38,.028,.075))
    beam(steel,(-5,y-.72,.28),(5,y-.72,.28),.16,.24)
for side in [-1,1]:beam(steel,(side*5.08,-13.8,.35),(side*5.08,12.4,.35),.22,.3)
for index,y in enumerate(bays):
    for side in [-1,1]:
        x=side*HALF
        beam(steel,(x,y,.4),(x,y,EAVE),.48,.54)
        box(fast,(x,y,.46),(.72,.70,.09))
        for dx in [-.25,.25]:
            for dy in [-.24,.24]:cylinder(fast,(x+dx,y+dy,.53),.053,.10,sides=6)
        # Knee braces and visibly bolted beam joints.
        beam(steel,(x,y,EAVE-1.45),(x-side*1.5,y,EAVE),.18,.22)
        box(fast,(x,y-.295,EAVE-.38),(.55,.045,.95));bolts((x,y-.33,EAVE-.38))
        for z in [1.25,3.4,5.75]:
            if index<len(bays)-1:beam(steel,(x,y,z),(x,bays[index+1],z),.105,.12)
    beam(steel,(-HALF,y,EAVE),(HALF,y,EAVE),.36,.49)
    beam(steel,(-HALF-.3,y,EAVE-.09),(0,y,RIDGE),.22,.29)
    beam(steel,(0,y,RIDGE),(HALF+.3,y,EAVE-.09),.22,.29)
    beam(steel,(0,y,EAVE),(0,y,RIDGE),.13,.14)
    for side in [-1,1]:beam(steel,(side*3.2,y,EAVE),(0,y,RIDGE),.105,.13)
for x in [-5.25,-3.6,-1.8,0,1.8,3.6,5.25]:
    z=RIDGE-abs(x)/HALF*(RIDGE-EAVE)
    beam(steel,(x,-13.85,z),(x,13.45,z),.115,.14)
for side in [-1,1]:
    for j in [1,3]:
        a,b=bays[j],bays[j+1]
        beam(steel,(side*5.04,a,.65),(side*5.04,b,5.85),.095,.11)
        if j==3:beam(steel,(side*5.04,a,5.85),(side*5.04,b,.65),.095,.11)
# Central hanging guide support, with open space for an independently placed bucket.
beam(rail,(0,-14.0,6.28),(0,12.9,6.28),.23,.36)
for y in bays:
    beam(rail,(0,y,6.25),(0,y,EAVE),.10,.12)
    box(fast,(0,y,6.34),(.48,.4,.05))
# Worn side handrails, kept inside the wall shell.
for side in [-1,1]:
    for y in np.arange(-13.2,11.2,2.7):
        beam(steel,(side*4.7,y,.45),(side*4.7,y,1.58),.07,.07,False)
    for z in [.98,1.58]:beam(steel,(side*4.7,-13.2,z),(side*4.7,11.1,z),.065,.065,False)

# Exposed exterior steel straps visibly tie the corrugated side together.
for y in bays:
    box(fast,(5.34,y,3.5),(.09,.20,6.2))
    box(fast,(5.38,y,6.35),(.06,.58,.93))
    bolts((5.43,y,6.35),plane='X')
for j in range(len(bays)-1):
    for z in [1.15,4.85,6.43]:
        box(steel,(5.35,(bays[j]+bays[j+1])/2,z),(.09,bays[j+1]-bays[j],.21))
        for y in [bays[j]+.2,bays[j+1]-.2]:
            cylinder(fast,(5.415,y,z),.046,.035,(1,0,0),6)
# Last grating bay reaches the open rear portal.
for x in [-3.7,-1.25,1.25,3.7]:
    for xx in np.arange(x-1.18,x+1.18,.145):box(floor,(xx,13.02,.39),(.032,.91,.05))
    for yy in [-.44,0,.44]:box(floor,(x,13.02+yy,.365),(2.38,.028,.075))
beam(steel,(-5,13.48,.28),(5,13.48,.28),.16,.24)

# Double sided corrugated panels are fabricated geometry, not flat planes with normal maps.
def sheet(d,origin,u,v,width,height,damage=0,phase=0):
    origin,u,v=Vector(origin),Vector(u),Vector(v);normal=u.cross(v).normalized()
    nx=max(8,int(width/.044));ny=4
    coords=[]
    heights=[]
    for i in range(nx+1):
        s=i/nx
        ragged=damage*(.12+random.random()*.19)
        heights.append(height-ragged)
    for side in [-1,1]:
        for j in range(ny+1):
            t=j/ny
            for i in range(nx+1):
                s=i/nx;h=heights[i]
                bend=damage*.11*math.sin(s*9)*t**5
                p=origin+u*(s*width)+v*(t*h)+normal*(.035*math.sin(s*width*2*math.pi/.177)+bend+side*.007)
                coords.append(tuple(p))
    count=(nx+1)*(ny+1);faces=[];uvs=[]
    for side in range(2):
        for j in range(ny):
            for i in range(nx):
                a=side*count+j*(nx+1)+i
                f=(a,a+1,a+1+nx+1,a+nx+1)
                if side==0:f=f[::-1]
                faces.append(f)
                uvs.append([(phase+(((k-side*count)%(nx+1))/nx)*.85,((k-side*count)//(nx+1))/ny) for k in f])
    boundary=list(range(nx+1))+[j*(nx+1)+nx for j in range(1,ny+1)]+[ny*(nx+1)+i for i in range(nx-1,-1,-1)]+[j*(nx+1) for j in range(ny-1,0,-1)]
    for i,a in enumerate(boundary):
        b=boundary[(i+1)%len(boundary)];faces.append((a,b,b+count,a+count));uvs.append([(phase,0),(phase+1,0),(phase+1,.03),(phase,.03)])
    poly(d,coords,faces,uvs)
for side in [1]:
    for i in range(26):
        y=-13.4+i*1.03
        # Broad intact masses, localized missing sheets, no uniform shredded silhouette.
        if (side==-1 and i in [2,13]) or (side==1 and i in [0,1,11,12]):continue
        h=5.75+random.uniform(-.10,.15)
        if i in [3,14]:h-=1.0
        sheet(wall,(side*5.22,y,.7),(0,1,0),(0,0,1),1.075,h,1 if i in [3,14,20] else .12,(i*.371)%1)
        for z in [1.25,3.4,5.75]:
            for dy in [.17,.8]:cylinder(fast,(side*5.29,y+dy,z),.028,.026,(side,0,0),6)
# User approved a single long clad side; opposite long side and both ends stay open.
# Roof sheets run down the slope. Missing patches expose trusses in coherent groups.
for side in [-1,1]:
    u=Vector((0,1,0));v=Vector((side*5.5,0,-1.57));length=v.length;v.normalize()
    for i in range(27):
        if (side==-1 and i in [3,4,14,15,16]) or (side==1 and i in [8,9,18]):continue
        y=-13.8+i*1.025
        sheet(roof,(0,y,RIDGE+.09),u,v,1.065,length if i!=12 else length*.73,.9 if i in [2,5,12,13,17] else .1,i*.213)
# Ridge flashing and end trims with a few missing stretches.
for j in range(10):
    if j==5:continue
    for side in [-1,1]:beam(steel,(side*.16,-13.7+j*2.7,RIDGE+.13),(side*.16,-11.15+j*2.7,RIDGE+.13),.032,.25,False)

objects=[]
for name,d in parts.items():
    mesh=bpy.data.meshes.new('Station_'+name+'_Mesh');mesh.from_pydata(d['v'],[],d['f']);mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for polygon,values in zip(mesh.polygons,d['uv']):
        for li,val in zip(polygon.loop_indices,values):uv.data[li].uv=val
    ob=bpy.data.objects.new(name,mesh);collection.objects.link(ob);mesh.materials.append(materials[d['kind']]);objects.append(ob)
    if name in ['StructuralSteel','ConcretePlinth','OverheadGuide']:
        bevel=ob.modifiers.new('Small fabricated edge bevels','BEVEL');bevel.width=.003 if name!='ConcretePlinth' else .035;bevel.segments=1
        # Bake geometry modifiers into the exported source mesh; retain logical components.
        bpy.context.view_layer.objects.active=ob;ob.select_set(True);bpy.ops.object.modifier_apply(modifier=bevel.name);ob.select_set(False)
# Triangulate before tangent calculation and remove collapsed bevel edges.
import bmesh
for ob in objects:
    bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.dissolve_degenerate(bm,dist=.00001,edges=list(bm.edges));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free();ob.data.update()
print('GEOMETRY_READY',sum(len(o.data.polygons) for o in objects),flush=True)

# Studio presentation. Export selection excludes camera, lamps and studio floor.
world=SCENE.world or bpy.data.worlds.new('Station_Studio');SCENE.world=world;world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.62,.60,.56,1);bg.inputs[1].default_value=.45
floorMat=bpy.data.materials.new('StudioGround');floorMat.use_nodes=True
bs=next(n for n in floorMat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.37,.355,.325,1);bs.inputs['Roughness'].default_value=.95
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.17));ground=bpy.context.object;ground.name='StudioGround';ground.data.materials.append(floorMat)
def area(name,loc,power,size):
    light=bpy.data.lights.new(name,'AREA');light.energy=power;enum(light,'shape','DISK');light.size=size
    o=bpy.data.objects.new(name,light);SCENE.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,3))-o.location).to_track_quat('-Z','Y').to_euler()
area('Key',(-10,-16,23),6000,14);area('Fill',(15,-6,15),4200,12);area('Rim',(3,18,22),7000,13)
cam=bpy.data.cameras.new('StationCamera');camera=bpy.data.objects.new('StationCamera',cam);SCENE.collection.objects.link(camera);SCENE.camera=camera
enum(cam,'type','ORTHO');cam.ortho_scale=34
camera.location=(26,-38,14);camera.rotation_euler=(Vector((0,-1,3.8))-camera.location).to_track_quat('-Z','Y').to_euler()
try:SCENE.render.engine='CYCLES'
except TypeError:raise
prefs=bpy.context.preferences.addons['cycles'].preferences
try:
    enum(prefs,'compute_device_type','OPTIX');prefs.get_devices()
    for d in prefs.devices:d.use=d.type!='CPU'
    if any(d.type!='CPU' for d in prefs.devices):enum(SCENE.cycles,'device','GPU')
except Exception as e:print('GPU_SETUP',str(e),flush=True)
SCENE.cycles.samples=32;SCENE.cycles.use_denoising=True
SCENE.render.resolution_x=1500;SCENE.render.resolution_y=1100;SCENE.render.resolution_percentage=100
enum(SCENE.render.image_settings,'file_format','PNG')
# Save material images packed into the editable file.
SCENE['approved_reference']='References/Approved_Concept.png';SCENE['approved_enclosure']='One long side only, opposite side and both ends open. See References/Approved_SingleSide.png.';SCENE['model_units']='metres';SCENE['scope']='Station shell and fixed guide only. Existing independent coal bucket is not duplicated.'
SCENE['target_original_world_dimensions']='10.76 m width, 28.03 m length, 8.55 m height'
bpy.data.libraries.write(str(ROOT/'Blender/CablewayStationShell_v01.blend'),{SCENE},fake_user=True,compress=True)
SCENE.render.filepath=str(ROOT/'Previews/Station_Overview.png');bpy.ops.render.render(write_still=True)
camera.location=(-25,32,19);camera.rotation_euler=(Vector((0,1,3.3))-camera.location).to_track_quat('-Z','Y').to_euler();SCENE.render.filepath=str(ROOT/'Previews/Station_Rear.png');bpy.ops.render.render(write_still=True)
camera.location=(2,-19,4.6);camera.rotation_euler=(Vector((0,7,4.0))-camera.location).to_track_quat('-Z','Y').to_euler();enum(cam,'type','PERSP');cam.lens=23
SCENE.render.filepath=str(ROOT/'Previews/Station_Interior.png');bpy.ops.render.render(write_still=True)
# Return to overview for the editable source.
enum(cam,'type','ORTHO');cam.ortho_scale=34;camera.location=(26,-38,14);camera.rotation_euler=(Vector((0,-1,3.8))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Exports/CablewayStationShell_v01.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,use_triangles=True,mesh_smooth_type='FACE',use_tspace=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,path_mode='COPY',embed_textures=True,bake_anim=False)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Exports/CablewayStationShell_v01.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_normals=True,export_tangents=True,export_materials='EXPORT')
report={'units':'metres','parts':[],'unity_imported':False,'scene_replaced':False}
import bmesh
for o in objects:
    o.data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(o.data)
    report['parts'].append({'name':o.name,'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles),'uv_layers':[u.name for u in o.data.uv_layers],'boundary_edges':sum(e.is_boundary for e in bm.edges),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'zero_area_faces':sum(f.calc_area()<1e-10 for f in bm.faces)})
    bm.free()
points=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
report['dimensions']=[max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
report['triangles']=sum(p['triangles'] for p in report['parts'])
(ROOT/'verification.json').write_text(json.dumps(report,indent=2))
bpy.data.libraries.write(str(ROOT/'Blender/CablewayStationShell_v01.blend'),{SCENE},fake_user=True,compress=True)
print('COMPLETE',json.dumps(report),flush=True)
