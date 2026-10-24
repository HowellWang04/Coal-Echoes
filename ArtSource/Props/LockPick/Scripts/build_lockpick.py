"""Build the level one lock pick set (hook pick and tension wrench) in an isolated background Blender."""
import bpy
import bmesh
import json
import math
from pathlib import Path
import numpy as np
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
for folder in ['Blender', 'Exports', 'Textures', 'Previews']:
    (ROOT / folder).mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.name = 'LockPick_Authoring'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
asset = bpy.data.collections.new('LockPick')
studio = bpy.data.collections.new('PreviewStudio')
scene.collection.children.link(asset)
scene.collection.children.link(studio)
MM = .001
N = 1024

def noise(nx, ny, seed, n=N):
    r = np.random.default_rng(seed)
    g = r.random((ny, nx), dtype=np.float32)
    x = np.arange(n, dtype=np.float32) * nx / n
    y = np.arange(n, dtype=np.float32) * ny / n
    ix = x.astype(int); iy = y.astype(int)
    fx = x - ix; fy = y - iy
    fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
    return ((1-fy[:,None])*((1-fx)*g[iy[:,None]%ny,ix%nx]+fx*g[iy[:,None]%ny,(ix+1)%nx])
            +fy[:,None]*((1-fx)*g[(iy[:,None]+1)%ny,ix%nx]+fx*g[(iy[:,None]+1)%ny,(ix+1)%nx]))

def image_file(name, data, color=False):
    h, w = data.shape[:2]
    rgba = np.ones((h,w,4), dtype=np.float32)
    if data.ndim == 2:
        rgba[:,:,:3] = data[:,:,None]
    else:
        rgba[:,:,:data.shape[2]] = data
    im = bpy.data.images.new(name, width=w, height=h, alpha=True)
    # Save supplied display RGB values without a second color transform.
    im.colorspace_settings.name = 'Non-Color'
    im.pixels.foreach_set(np.clip(rgba,0,1).ravel())
    im.filepath_raw = str(ROOT / 'Textures' / (name+'.png'))
    im.file_format = 'PNG'; im.save()
    bpy.data.images.remove(im)
    im = bpy.data.images.load(str(ROOT / 'Textures' / (name+'.png')))
    im.colorspace_settings.name = 'sRGB' if color else 'Non-Color'
    return im

def normal_map(height, strength):
    dy, dx = np.gradient(height)
    norm = np.dstack((-dx*strength,-dy*strength,np.ones_like(height)))
    norm /= np.linalg.norm(norm,axis=2)[:,:,None]
    return norm*.5+.5

def material(name, color, roughness=.6, metal=0):
    m=bpy.data.materials.new(name); m.use_nodes=True
    bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=roughness
    bs.inputs['Metallic'].default_value=metal
    m.diffuse_color=(*color,1)
    return m,bs

def texture(mat, im, socket=None):
    n=mat.node_tree.nodes.new('ShaderNodeTexImage'); n.image=im; n.label=im.name
    if socket:
        bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        mat.node_tree.links.new(n.outputs['Color'],bs.inputs[socket])
    return n

def attach_normal(mat, im, strength=.5):
    tex=texture(mat,im)
    normal=mat.node_tree.nodes.new('ShaderNodeNormalMap')
    normal.inputs['Strength'].default_value=strength
    mat.node_tree.links.new(tex.outputs['Color'],normal.inputs['Color'])
    bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    mat.node_tree.links.new(normal.outputs['Normal'],bs.inputs['Normal'])

def mask_map(name, metal, rough):
    image_file(name,np.dstack((metal,np.ones_like(rough),np.ones_like(rough),1-rough)))

# Worn spring steel. Planar top-view UVs, brushed along the tool length,
# dull tarnish clouds and a few rust specks on edges that saw sweat and damp.
cloud=noise(6,6,11); fine=noise(160,160,12)
brushed=noise(6,620,13)*.7+noise(3,240,14)*.3
tarnish=np.clip((cloud*.75+noise(23,23,15)*.25-.48)*2.6,0,1)
rust=np.clip((noise(90,90,16)*.6+noise(9,9,17)*.4-.74)*6,0,1)*.8
steel_rgb=np.array([.63,.63,.61],np.float32)[None,None,:]*(.9+.08*brushed+.04*fine)[:,:,None]
steel_rgb=steel_rgb*(1-tarnish[:,:,None]*.45)+np.array([.36,.32,.27])*tarnish[:,:,None]*.45
steel_rgb=steel_rgb*(1-rust[:,:,None])+np.array([.30,.19,.12])*(.75+.35*fine)[:,:,None]*rust[:,:,None]
steel_rough=np.clip(.30+.12*brushed+.22*tarnish+.45*rust,0,1)
steel_metal=np.clip(1-rust*.75,0,1)
steel,_=material('Steel_WornLockPick',(.55,.55,.53),.36,1)
texture(steel,image_file('Steel_BaseColor',steel_rgb,True),'Base Color')
texture(steel,image_file('Steel_Roughness',steel_rough),'Roughness')
texture(steel,image_file('Steel_Metallic',steel_metal),'Metallic')
attach_normal(steel,image_file('Steel_Normal',normal_map(brushed*.6+rust*.8,3)),.25)
mask_map('Steel_HDRP_Mask',steel_metal,steel_rough)

# Black cotton friction tape, wound helically and rubbed grey where the thumb sits.
u=np.arange(N,dtype=np.float32)[None,:]/N
v=np.arange(N,dtype=np.float32)[:,None]/N
weave=(.5+.5*np.sin(u*math.tau*170))*(.5+.5*np.sin(v*math.tau*170))
phase=(2*u+v)%1
lap=np.clip(1-phase/.035,0,1)
ridge=np.clip(phase/.02,0,1)
rubbed=np.clip((noise(5,4,21)*.7+noise(40,40,22)*.3-.5)*2.4,0,1)
fray=np.clip((noise(300,30,23)-.7)*5,0,1)*lap
tape_rgb=np.array([.055,.052,.048],np.float32)[None,None,:]*(.8+.35*weave+.15*fine)[:,:,None]
tape_rgb=tape_rgb*(1-rubbed[:,:,None]*.5)+np.array([.20,.19,.17])*rubbed[:,:,None]*.5
tape_rgb=tape_rgb*(1-lap[:,:,None]*.6)+np.array([.24,.21,.17])*lap[:,:,None]*.6
tape_rgb=tape_rgb*(1-fray[:,:,None]*.5)+np.array([.30,.28,.24])*fray[:,:,None]*.5
tape_rough=np.clip(.86-.22*rubbed+.06*weave,0,1)
tape,_=material('Grip_FrictionTape',(.05,.048,.044),.82)
texture(tape,image_file('Tape_BaseColor',tape_rgb,True),'Base Color')
texture(tape,image_file('Tape_Roughness',tape_rough),'Roughness')
attach_normal(tape,image_file('Tape_Normal',normal_map(weave*.15+ridge*.9+fray*.2,4)),.6)
mask_map('Tape_HDRP_Mask',np.zeros_like(tape_rough),tape_rough)

# A dried, slightly cracked rubber band holding the two tools together.
n2=512
cracks=np.clip((noise(90,8,31,n2)-.68)*6,0,1)
rubber_rgb=np.array([.36,.18,.13],np.float32)[None,None,:]*(.78+.3*noise(14,4,32,n2))[:,:,None]
rubber_rgb=rubber_rgb*(1-cracks[:,:,None]*.6)+np.array([.47,.37,.29])*cracks[:,:,None]*.6
rubber,_=material('RubberBand_Aged',(.36,.18,.13),.78)
texture(rubber,image_file('Rubber_BaseColor',rubber_rgb,True),'Base Color')

def link(ob, col=asset):
    for c in list(ob.users_collection):c.objects.unlink(ob)
    col.objects.link(ob)

def round_polygon(points, radius):
    """Replace each corner of a closed polygon by a short arc of the given radius."""
    out=[]
    n=len(points)
    for i in range(n):
        p=Vector(points[i]);a=Vector(points[i-1]);b=Vector(points[(i+1)%n])
        r=radius[i] if isinstance(radius,(list,tuple)) else radius
        da=(a-p).normalized();db=(b-p).normalized()
        angle=math.acos(max(-1,min(1,da.dot(db))))
        if r<=0 or angle>math.pi-1e-3:
            out.append(tuple(p));continue
        d=r/math.tan(angle/2)
        p0=p+da*d;p1=p+db*d
        center=p+(da+db).normalized()*(r/math.sin(angle/2))
        a0=math.atan2(p0.y-center.y,p0.x-center.x);a1=math.atan2(p1.y-center.y,p1.x-center.x)
        sweep=(a1-a0+math.pi)%math.tau-math.pi
        for k in range(7):
            t=a0+sweep*k/6
            out.append((center.x+r*math.cos(t),center.y+r*math.sin(t)))
    return out

def plate(name, outline, z0, thickness, mat, bevel_width):
    """Flat steel part cut from sheet: extrude a closed 2D outline and soften its edges."""
    bm=bmesh.new()
    bottom=[bm.verts.new((x,y,z0)) for x,y in outline]
    top=[bm.verts.new((x,y,z0+thickness)) for x,y in outline]
    bm.faces.new(list(reversed(bottom)));bm.faces.new(top)
    n=len(outline)
    for i in range(n):
        j=(i+1)%n
        bm.faces.new((bottom[i],bottom[j],top[j],top[i]))
    rim=[e for e in bm.edges if all(abs(v.co.z-z0)<1e-9 for v in e.verts) or all(abs(v.co.z-z0-thickness)<1e-9 for v in e.verts)]
    bmesh.ops.bevel(bm,geom=rim,offset=bevel_width,segments=2,profile=.5,affect='EDGES',clamp_overlap=True)
    # Clamped bevel collapses to zero width at the tight inner hook curve.
    bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges))
    me=bpy.data.meshes.new(name+'_Mesh');bm.to_mesh(me);bm.free()
    ob=bpy.data.objects.new(name,me);asset.objects.link(ob);me.materials.append(mat)
    return ob

def dedupe(points, tolerance):
    out=[points[0]]
    for p in points[1:]:
        if (Vector(p)-Vector(out[-1])).length>tolerance:out.append(p)
    if (Vector(out[0])-Vector(out[-1])).length<=tolerance:out.pop()
    # Drop points on straight runs; they only make sliver triangles in the caps.
    i=0
    while i<len(out):
        a=Vector(out[i-1]);b=Vector(out[i]);c=Vector(out[(i+1)%len(out)])
        if abs((b-a).normalized().cross((c-b).normalized()))<.002:out.pop(i)
        else:i+=1
    return out

# Hook pick, 0.7 mm sheet. Wide flat handle, narrow shaft, short hook rising toward +Y.
HANDLE_END=66*MM; SHAFT_W=1.1*MM; HANDLE_W=4.5*MM
center=[];half=[]
x=HANDLE_W
while x<=112*MM+1e-9:
    t=min(1,max(0,(x-HANDLE_END)/(10*MM)));t=t*t*(3-2*t)
    center.append(Vector((x,0)));half.append(HANDLE_W*(1-t)+SHAFT_W*t);x+=.5*MM
R=2*MM
for k in range(1,13):
    a=math.pi/2*k/12
    center.append(Vector((112*MM+R*math.sin(a),R-R*math.cos(a))));half.append(SHAFT_W-(SHAFT_W-.75*MM)*k/12)
for k in range(1,4):
    center.append(Vector((112*MM+R,R+.3*MM*k)));half.append(.75*MM-.05*MM*k)
normals=[]
for i in range(len(center)):
    tangent=(center[min(i+1,len(center)-1)]-center[max(i-1,0)]).normalized()
    normals.append(Vector((-tangent.y,tangent.x)))
right=[tuple(c-n*w) for c,n,w in zip(center,normals,half)]
left=[tuple(c+n*w) for c,n,w in zip(center,normals,half)]
tip=center[-1];tn=normals[-1];tt=Vector((tn.y,-tn.x));tw=half[-1]
tip_cap=[tuple(tip-tn*tw*math.cos(a)+tt*tw*math.sin(a)) for a in [math.pi*k/8 for k in range(1,8)]]
start_cap=[(HANDLE_W-HANDLE_W*math.sin(a),HANDLE_W*math.cos(a)) for a in [math.pi*k/16 for k in range(1,16)]]
pick_outline=dedupe(right+tip_cap+list(reversed(left))+start_cap,.04*MM)
PICK_Z=.4*MM;PICK_T=.7*MM
pick=plate('HookPick',pick_outline,PICK_Z,PICK_T,steel,.08*MM)

# Tension wrench, 1.0 mm sheet, cut as a flat L. Rests on the taped handle.
L=62*MM;W=3.2*MM;LEG=12*MM
wrench_outline=round_polygon([(0,-W/2),(L-W,-W/2),(L-W,-LEG),(L,-LEG),(L,W/2),(0,W/2)],[.5*MM,.6*MM,.4*MM,.4*MM,1.6*MM,.5*MM])
WRENCH_Z=1.52*MM;WRENCH_T=1.0*MM
# Turned slightly about the band so it does not sit perfectly parallel.
turn=Matrix.Rotation(math.radians(2.5),2)
wrench_outline=[tuple(turn@Vector((x+4*MM-36*MM,y))+Vector((36*MM,1.0*MM))) for x,y in wrench_outline]
wrench=plate('TensionWrench',wrench_outline,WRENCH_Z,WRENCH_T,steel,.1*MM)

# Friction tape wound over the pick handle. Superellipse section hugs the sheet.
TAPE_X0=6*MM;TAPE_X1=64*MM;RING=40;TA=HANDLE_W+.4*MM;TB=PICK_T/2+.4*MM;TZ=PICK_Z+PICK_T/2
rng=np.random.default_rng(41)
xs=np.linspace(TAPE_X0,TAPE_X1,59)
wobble=1+.012*np.sin(xs/(9.6*MM)*math.tau)+rng.normal(0,.003,len(xs))
verts=[];faces=[];face_uv=[]
for i,x in enumerate(xs):
    for j in range(RING):
        a=math.tau*j/RING;c=math.cos(a);s=math.sin(a)
        y=TA*wobble[i]*math.copysign(abs(c)**.5,c);z=TZ+TB*wobble[i]*math.copysign(abs(s)**.5,s)
        verts.append((x,y,z))
for i in range(len(xs)-1):
    for j in range(RING):
        jn=(j+1)%RING
        faces.append((i*RING+j,i*RING+jn,(i+1)*RING+jn,(i+1)*RING+j))
        u0=(xs[i]-TAPE_X0)/(TAPE_X1-TAPE_X0)*3;u1=(xs[i+1]-TAPE_X0)/(TAPE_X1-TAPE_X0)*3
        face_uv.append([(u0,j/RING),(u0,(j+1)/RING),(u1,(j+1)/RING),(u1,j/RING)])
for end,x in [(0,TAPE_X0),(len(xs)-1,TAPE_X1)]:
    verts.append((x,0,TZ));c=len(verts)-1
    for j in range(RING):
        jn=(j+1)%RING
        f=(c,end*RING+jn,end*RING+j) if end==0 else (c,end*RING+j,end*RING+jn)
        faces.append(f);face_uv.append([(.5+.5*math.cos(math.tau*k/RING)*.02,.5) for k in range(3)])
me=bpy.data.meshes.new('GripTape_Mesh');me.from_pydata(verts,[],faces);me.update()
uv=me.uv_layers.new(name='UVMap')
for poly,uvs in zip(me.polygons,face_uv):
    for li,coord in zip(poly.loop_indices,uvs):uv.data[li].uv=coord
grip=bpy.data.objects.new('GripTape',me);asset.objects.link(grip);me.materials.append(tape)

# Rubber band around the stacked handles. Section path is the convex hull of
# the tape and the wrench, offset outward so the band sits on both.
BAND_X=36*MM;BAND_W=1.8*MM;BAND_T=.8*MM;GAP=.05*MM
section=[(TA*math.copysign(abs(math.cos(a))**.5,math.cos(a)),TZ+TB*math.copysign(abs(math.sin(a))**.5,math.sin(a))) for a in [math.tau*j/RING for j in range(RING)]]
section+= [(1.0*MM+sy*W/2,WRENCH_Z+sz*WRENCH_T) for sy in (-1,1) for sz in (0,1)]
pts=sorted(set(section))
def cross(o,a,b):return (a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0])
lower=[];upper=[]
for p in pts:
    while len(lower)>=2 and cross(lower[-2],lower[-1],p)<=0:lower.pop()
    lower.append(p)
for p in reversed(pts):
    while len(upper)>=2 and cross(upper[-2],upper[-1],p)<=0:upper.pop()
    upper.append(p)
hull=lower[:-1]+upper[:-1]
path=[]
for i,p in enumerate(hull):
    a=Vector(hull[i-1]);b=Vector(p);c=Vector(hull[(i+1)%len(hull)])
    n0=Vector(((b-a).y,-(b-a).x)).normalized();n1=Vector(((c-b).y,-(c-b).x)).normalized()
    t0=math.atan2(n0.y,n0.x);sweep=(math.atan2(n1.y,n1.x)-t0)%math.tau
    steps=max(1,math.ceil(sweep/(math.pi/14)))
    for k in range(steps+1):
        t=t0+sweep*k/steps
        path.append((b,Vector((math.cos(t),math.sin(t)))))
verts=[];faces=[];face_uv=[]
lengths=[0]
for i in range(1,len(path)+1):
    lengths.append(lengths[-1]+((path[i%len(path)][0]+path[i%len(path)][1]*GAP)-(path[i-1][0]+path[i-1][1]*GAP)).length)
for p,n in path:
    inner=p+n*GAP;outer=p+n*(GAP+BAND_T)
    verts.extend([(BAND_X-BAND_W/2,inner.x,inner.y),(BAND_X-BAND_W/2,outer.x,outer.y),(BAND_X+BAND_W/2,outer.x,outer.y),(BAND_X+BAND_W/2,inner.x,inner.y)])
for i in range(len(path)):
    j=(i+1)%len(path)
    u0=lengths[i]/lengths[-1];u1=lengths[i+1]/lengths[-1]
    for s in range(4):
        sn=(s+1)%4
        faces.append((i*4+s,j*4+s,j*4+sn,i*4+sn))
        face_uv.append([(u0,s/4),(u1,s/4),(u1,(s+1)/4),(u0,(s+1)/4)])
me=bpy.data.meshes.new('RubberBand_Mesh');me.from_pydata(verts,[],faces);me.update()
uv=me.uv_layers.new(name='UVMap')
for poly,uvs in zip(me.polygons,face_uv):
    for li,coord in zip(poly.loop_indices,uvs):uv.data[li].uv=coord
band=bpy.data.objects.new('RubberBand',me);asset.objects.link(band);me.materials.append(rubber)
for ob in [grip,band]:
    bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()

# Planar top-view UVs for the steel, shared by both tools.
S=.13
for ob in [pick,wrench]:
    uv=ob.data.uv_layers.new(name='UVMap')
    for loop in ob.data.loops:
        co=ob.data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=((co.x+.003)/S,(co.y+.065)/S)

# Settle the set on a surface: it rests on the band underneath and on the hook tip.
band_bottom=min(v.co.z for v in band.data.vertices)
tip_x=112*MM+R
angle=math.atan2(PICK_Z-band_bottom,tip_x-BAND_X)
settle=Matrix.Translation((BAND_X,0,band_bottom))@Matrix.Rotation(angle,4,'Y')@Matrix.Translation((-BAND_X,0,-band_bottom))
for ob in asset.objects:ob.data.transform(settle)
coords=[v.co.copy() for ob in asset.objects for v in ob.data.vertices]
offset=Vector(((max(c.x for c in coords)+min(c.x for c in coords))/2,(max(c.y for c in coords)+min(c.y for c in coords))/2,min(c.z for c in coords)))
for ob in asset.objects:
    ob.data.transform(Matrix.Translation(-offset))
    bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
    ob.data.update()
    bpy.context.view_layer.objects.active=ob
    for o in asset.objects:o.select_set(o==ob)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(50))

wood,_=material('Studio_DustyBoard',(.16,.13,.10),.88)
bpy.ops.mesh.primitive_plane_add(size=4,location=(0,0,0))
floor=bpy.context.object;floor.name='StudioSurface';link(floor,studio);floor.data.materials.append(wood)
world=bpy.data.worlds.new('SoftStudio');world.use_nodes=True;scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs['Color'].default_value=(.55,.6,.66,1);bg.inputs['Strength'].default_value=.25
for name,loc,energy,size in [('Key',(-.16,-.20,.26),3.2,.18),('Fill',(.20,-.06,.12),.6,.14),('Rim',(.04,.22,.16),1.4,.10)]:
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size
    ob=bpy.data.objects.new(name,d);studio.objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector((0,0,0))-ob.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('PreviewCamera');cam=bpy.data.objects.new('PreviewCamera',camdata);studio.objects.link(cam);scene.camera=cam
camdata.type='ORTHO';camdata.clip_start=.001
def aim(location,target,scale):
    cam.location=location;cam.rotation_euler=(Vector(target)-Vector(location)).to_track_quat('-Z','Y').to_euler();camdata.ortho_scale=scale
scene.render.engine='CYCLES'
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    for d in prefs.devices:d.use=d.type=='OPTIX'
    scene.cycles.device='GPU'
except TypeError:
    scene.cycles.device='CPU'
scene.cycles.samples=96;scene.cycles.use_denoising=True
scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'

def inspect(objects):
    bpy.context.view_layer.update()
    bounds=[ob.matrix_world@Vector(co) for ob in objects for co in ob.bound_box]
    result={'mesh_count':len(objects),'dimensions_xyz_m':[max(v[i] for v in bounds)-min(v[i] for v in bounds) for i in range(3)],'min_z_m':min(v.z for v in bounds),'objects':[]}
    for ob in objects:
        ob.data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(ob.data)
        result['objects'].append({'name':ob.name,'triangles':len(ob.data.loop_triangles),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'zero_area_faces':sum(f.calc_area()<1e-14 for f in bm.faces),'uv_layers':len(ob.data.uv_layers)})
        bm.free()
    result['triangles']=sum(o['triangles'] for o in result['objects'])
    return result

objects=list(asset.objects)
report={'asset':'LockPick','blender_version':bpy.app.version_string,'source':inspect(objects),'settle_angle_degrees':math.degrees(angle),'unity_verified':False,'method':'Independent background Blender process'}
assert all(o['nonmanifold_edges']==0 and o['zero_area_faces']==0 and o['uv_layers'] for o in report['source']['objects']),report['source']
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:ob.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Exports/LockPick.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_tspace=True,mesh_smooth_type='FACE',bake_anim=False,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Exports/LockPick.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_normals=True,export_tangents=True)
for im in bpy.data.images:
    if im.source=='FILE':im.pack()
scene['asset_notes']='Hook pick with friction-taped handle and L tension wrench, held by an aged rubber band. Unity material setup pending.'
aim((.11,-.17,.15),(0,0,.001),.15)
scene.render.filepath=str(ROOT/'Previews/LockPick_Overall.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blender/LockPick.blend'),compress=True)
(ROOT/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ASSET_SAVED',json.dumps(report),flush=True)
bpy.ops.render.render(write_still=True)
aim((0,0,.4),(0,0,0),.135)
scene.render.filepath=str(ROOT/'Previews/LockPick_Top.png');bpy.ops.render.render(write_still=True)
hook=Vector((tip_x-offset.x-.004,0,.001))
aim(hook+Vector((.03,-.05,.04)),hook,.028)
scene.render.filepath=str(ROOT/'Previews/LockPick_HookDetail.png');bpy.ops.render.render(write_still=True)
# Square icon with transparent background for a future Backpack thumbnail.
floor.is_shadow_catcher=True;scene.render.film_transparent=True
scene.render.resolution_x=scene.render.resolution_y=512
aim((.05,-.09,.20),(0,0,.001),.135)
scene.render.filepath=str(ROOT/'Previews/LockPick_Icon.png');bpy.ops.render.render(write_still=True)
print('BUILD_COMPLETE',flush=True)
