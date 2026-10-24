"""Build the level one guardhouse bolt cutter (30 inch, for the 10 mm gate chain) in an isolated background Blender."""
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
scene.name = 'BoltCutter_Authoring'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
asset = bpy.data.collections.new('BoltCutter')
studio = bpy.data.collections.new('PreviewStudio')
scene.collection.children.link(asset)
scene.collection.children.link(studio)
MM = .001

def noise(nx, ny, seed, n):
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

def full_material(name, prefix, rgb, rough, metal, normal_height, strength):
    m,_=material(name,tuple(rgb.reshape(-1,3).mean(0)),float(rough.mean()),float(metal.mean()))
    texture(m,image_file(prefix+'_BaseColor',rgb,True),'Base Color')
    texture(m,image_file(prefix+'_Roughness',rough),'Roughness')
    texture(m,image_file(prefix+'_Metallic',metal),'Metallic')
    attach_normal(m,image_file(prefix+'_Normal',normal_map(normal_height,strength)),.5)
    image_file(prefix+'_HDRP_Mask',np.dstack((metal,np.ones_like(rough),np.ones_like(rough),1-rough)))
    return m

def blend(rgb, color, weight):
    return rgb*(1-weight[:,:,None])+np.array(color,np.float32)*weight[:,:,None]

# Forged head: black mill scale, brown rust where damp collected, and freshly
# ground bright steel along both cutting edges. Planar top-view UVs over the whole tool.
S=.78
N=2048
cloud=noise(9,9,101,N); fine=noise(300,300,102,N); pits=noise(140,140,103,N)
rust=np.clip((cloud*.6+noise(40,40,104,N)*.4-.55)*3.2,0,1)
pitting=np.clip((pits-.72)*5,0,1)*rust
u=(np.arange(N,dtype=np.float32)[None,:]+.5)/N*S
v=(np.arange(N,dtype=np.float32)[:,None]+.5)/N*S-S/2
blade=np.clip(1-(np.abs(v)-3.5*MM)/(2.5*MM),0,1)*np.clip((u-700*MM)/(6*MM),0,1)
rgb=np.array([.15,.145,.14],np.float32)[None,None,:]*(.8+.3*fine+.15*cloud)[:,:,None]
rgb=blend(rgb,(.33,.18,.09),rust*.85)
rgb=blend(rgb,(.20,.11,.06),pitting)
rgb=blend(rgb,(.56,.55,.52),blade*(.85+.15*fine))
rough=np.clip(.62+.1*fine+.25*rust-.4*blade,0,1)
metal=np.clip(.85-.75*rust+.15*blade,0,1)
forged=full_material('Forged_BlackSteel','Forged',rgb,rough,metal,fine*.3+rust*.5+pitting*.6,6)

# Handles: sun-faded red enamel, chipped down to dark steel and rust.
N=1024
cloud=noise(5,5,201,N); fine=noise(180,180,202,N)
chips=np.clip((noise(10,10,203,N)*.6+noise(45,45,204,N)*.4-.64)*7,0,1)
under=np.clip((noise(12,12,205,N)-.45)*3,0,1)
rgb=np.array([.44,.09,.06],np.float32)[None,None,:]*(.85+.2*fine)[:,:,None]
rgb=blend(rgb,(.50,.28,.21),np.clip(cloud-.35,0,1)*.6)
bare=blend(np.ones((N,N,3),np.float32)*np.array([.16,.15,.14],np.float32),(.34,.19,.10),under)
rgb=rgb*(1-chips[:,:,None])+bare*chips[:,:,None]
rough=np.clip(.55+.15*cloud+.1*fine+chips*(.1+.25*under),0,1)
metal=chips*(.8-.6*under)
paint=full_material('Handle_FadedRedPaint','Paint',rgb,rough,metal,chips*.6+fine*.1,5)

# Ribbed rubber grips, dried out and cracked, with dust in the grooves.
cracks=np.clip((noise(160,18,301,N)-.7)*6,0,1)
ribs=.5+.5*np.cos(np.arange(N,dtype=np.float32)[None,:]/N*math.tau*20)*np.ones((N,1),np.float32)
dust=np.clip(1-ribs*1.6,0,1)
rgb=np.array([.07,.068,.064],np.float32)[None,None,:]*(.85+.3*fine)[:,:,None]
rgb=blend(rgb,(.33,.29,.24),dust*(.4+.4*cloud))
rgb=blend(rgb,(.18,.17,.15),cracks*.7)
rough=np.clip(.8+.1*fine+.1*dust-.15*(1-cloud),0,1)
rubber=full_material('Grip_DriedRubber','Rubber',rgb,rough,np.zeros_like(rough),cracks*.6+fine*.15,4)

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
        d=min(r/math.tan(angle/2),(a-p).length*.45,(b-p).length*.45)
        r=d*math.tan(angle/2)
        p0=p+da*d;p1=p+db*d
        center=p+(da+db).normalized()*(r/math.sin(angle/2))
        a0=math.atan2(p0.y-center.y,p0.x-center.x);a1=math.atan2(p1.y-center.y,p1.x-center.x)
        sweep=(a1-a0+math.pi)%math.tau-math.pi
        for k in range(7):
            t=a0+sweep*k/6
            out.append((center.x+r*math.cos(t),center.y+r*math.sin(t)))
    return out

def plate(name, outline, z0, thickness, mat, bevel_width):
    """Flat forged part: extrude a closed 2D outline (mm) and soften its edges."""
    outline=[(x*MM,y*MM) for x,y in outline]
    z0*=MM;thickness*=MM
    bm=bmesh.new()
    bottom=[bm.verts.new((x,y,z0)) for x,y in outline]
    top=[bm.verts.new((x,y,z0+thickness)) for x,y in outline]
    bm.faces.new(list(reversed(bottom)));bm.faces.new(top)
    n=len(outline)
    for i in range(n):
        j=(i+1)%n
        bm.faces.new((bottom[i],bottom[j],top[j],top[i]))
    rim=[e for e in bm.edges if all(abs(v.co.z-z0)<1e-9 for v in e.verts) or all(abs(v.co.z-z0-thickness)<1e-9 for v in e.verts)]
    bmesh.ops.bevel(bm,geom=rim,offset=bevel_width*MM,segments=2,profile=.5,affect='EDGES',clamp_overlap=True)
    bmesh.ops.dissolve_degenerate(bm,dist=1e-6,edges=list(bm.edges))
    me=bpy.data.meshes.new(name+'_Mesh');bm.to_mesh(me);bm.free()
    ob=bpy.data.objects.new(name,me);asset.objects.link(ob);me.materials.append(mat)
    return ob

def mirror(points):
    return [(x,-y) for x,y in reversed(points)]

def tube(name, samples, mat, ring=24, u_length=.0754, end_cap=0):
    """Loft a round section along (centre, radius) samples in the XY plane, closed at both ends.
    end_cap adds a hemispherical end of that radius after the last sample."""
    pts=[c for c,_ in samples];radii=[r for _,r in samples]
    tangents=[(pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]).normalized() for i in range(len(pts))]
    tip_end=pts[-1]
    if end_cap:
        t=tangents[-1];last=pts[-1];tip_end=last+t*end_cap
        for k in range(1,6):
            a=math.pi/2*k/6
            pts.append(last+t*end_cap*math.sin(a));radii.append(end_cap*math.cos(a));tangents.append(t)
    verts=[];faces=[];face_uv=[]
    dist=[0]
    for a,b in zip(pts,pts[1:]):dist.append(dist[-1]+(b-a).length)
    for c,r,t in zip(pts,radii,tangents):
        n=Vector((-t.y,t.x,0))
        for j in range(ring):
            a=math.tau*j/ring
            verts.append(tuple(c+n*r*math.cos(a)+Vector((0,0,r*math.sin(a)))))
    for i in range(len(pts)-1):
        for j in range(ring):
            jn=(j+1)%ring
            faces.append((i*ring+j,i*ring+jn,(i+1)*ring+jn,(i+1)*ring+j))
            u0=dist[i]/u_length;u1=dist[i+1]/u_length
            face_uv.append([(u0,j/ring),(u0,(j+1)/ring),(u1,(j+1)/ring),(u1,j/ring)])
    tips=[(0,pts[0]),(len(pts)-1,tip_end)]
    for end,tip in tips:
        verts.append(tuple(tip));c=len(verts)-1
        for j in range(ring):
            jn=(j+1)%ring
            faces.append((c,end*ring+jn,end*ring+j) if end==0 else (c,end*ring+j,end*ring+jn))
            uend=dist[end]/u_length
            face_uv.append([(uend,.5),(uend,jn/ring),(uend,j/ring)])
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(verts,[],faces);me.update()
    uv=me.uv_layers.new(name='UVMap')
    for poly,uvs in zip(me.polygons,face_uv):
        for li,coord in zip(poly.loop_indices,uvs):uv.data[li].uv=coord
    ob=bpy.data.objects.new(name,me);asset.objects.link(ob);me.materials.append(mat)
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    return ob

# Layout in millimetres. Tool lies flat, jaws toward +X, handles toward -X,
# mechanism plane parallel to the ground. ZC is the mid plane of every part.
ZC=16
CORE=14;PLATE=4
upper_jaw=[(604,10),(640,6),(700,4),(752,1),(760,5),(758,13),(745,23),(718,32),(690,37),(660,38),(630,36),(612,32),(604,24)]
jaw_u=plate('Jaw_Upper',round_polygon(upper_jaw,[2,6,0,0,1.5,3,6,8,8,8,6,4,2]),ZC-CORE/2,CORE,forged,1.2)
jaw_l=plate('Jaw_Lower',round_polygon(mirror(upper_jaw),[2,4,6,8,8,8,6,3,1.5,0,0,6,2]),ZC-CORE/2,CORE,forged,1.2)
upper_head=[(522,8),(548,3),(566,2),(598,12),(602,24),(600,36),(588,38),(560,30),(530,27),(522,24)]
head_u=plate('HandleHead_Upper',round_polygon(upper_head,3),ZC-CORE/2,CORE,forged,1)
head_l=plate('HandleHead_Lower',round_polygon(mirror(upper_head),3),ZC-CORE/2,CORE,forged,1)
cheek=[(548,0),(556,-16),(590,-30),(630,-38),(676,-38),(698,-28),(706,-12),(706,12),(698,28),(676,38),(630,38),(590,30),(556,16)]
cheek=round_polygon(cheek,[4,10,12,12,10,6,4,4,6,10,12,12,10])
plate_top=plate('HeadPlate_Top',cheek,ZC+CORE/2+.2,PLATE,forged,.6)
plate_bottom=plate('HeadPlate_Bottom',cheek,ZC-CORE/2-.2-PLATE,PLATE,forged,.6)

# Jaw pivots, jaw-to-handle joints and the centre pivot: hex nuts on top, low round heads underneath.
bm=bmesh.new()
for x,y in [(684,20),(684,-20),(603,27),(603,-27),(562,0)]:
    top_z=(ZC+CORE/2+.2+PLATE)*MM;bottom_z=(ZC-CORE/2-.2-PLATE)*MM
    for segments,radius,height,z,rot in [(6,8,5,top_z+2.5*MM,0),(24,4.5,3,top_z+6.5*MM,0),(24,6.5,2,bottom_z-1*MM,0)]:
        bmesh.ops.create_cone(bm,cap_ends=True,cap_tris=False,segments=segments,radius1=radius*MM,radius2=radius*MM,depth=height*MM,
                                   matrix=Matrix.Translation((x*MM,y*MM,z))@Matrix.Rotation(math.radians(15),4,'Z'))
me=bpy.data.meshes.new('Bolts_Mesh');bm.to_mesh(me);bm.free()
bolts=bpy.data.objects.new('Bolts',me);asset.objects.link(bolts);me.materials.append(forged)

# Handle centre lines: leave the head at y=16, flare out, then run parallel through the grips.
def handle_y(x):
    t=(530-x)/530
    return 16+28*(1-(1-t)**1.6)
handles=[];grips=[]
for side,label in [(1,'Upper'),(-1,'Lower')]:
    c=lambda x:Vector((x*MM,side*handle_y(x)*MM,ZC*MM))
    tube_samples=[(c(x),12*MM) for x in range(525,25,-10)]
    handles.append(tube('HandleTube_'+label,tube_samples,paint))
    handles.append(tube('Ferrule_'+label,[(c(x),13.5*MM) for x in range(542,512,-3)],paint))
    grip_samples=[]
    for x in np.arange(232,6,-1.5):
        r=15.6 if x>222 else 15.2+.35*math.cos((232-x)/(232-6)*math.tau*20)
        if x>228:r=13.2+2.4*(232-x)/4
        grip_samples.append((c(float(x)),r*MM))
    grips.append(tube('Grip_'+label,grip_samples,rubber,ring=32,u_length=.226,end_cap=15*MM))

# Planar top-view UVs for every forged part, matching the blade strip in the texture.
for ob in [jaw_u,jaw_l,head_u,head_l,plate_top,plate_bottom,bolts]:
    uv=ob.data.uv_layers.new(name='UVMap')
    for loop in ob.data.loops:
        co=ob.data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=(co.x/S,(co.y+S/2)/S)

# Settle on the ground: it rests on the grip ends and the underside of the head.
head_parts=[jaw_u,jaw_l,head_u,head_l,plate_top,plate_bottom,bolts]
def lowest(objects, matrix):
    return min((matrix@v.co).z for ob in objects for v in ob.data.vertices)
lo,hi=-.05,.05
for _ in range(50):
    mid=(lo+hi)/2;m=Matrix.Rotation(mid,4,'Y')
    if lowest(head_parts,m)<lowest(grips,m):hi=mid
    else:lo=mid
settle=Matrix.Rotation(lo,4,'Y')
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

def link(ob, col):
    for c in list(ob.users_collection):c.objects.unlink(ob)
    col.objects.link(ob)
board,_=material('Studio_DustyBoard',(.16,.13,.10),.88)
bpy.ops.mesh.primitive_plane_add(size=8,location=(0,0,0))
floor=bpy.context.object;floor.name='StudioSurface';link(floor,studio);floor.data.materials.append(board)
world=bpy.data.worlds.new('SoftStudio');world.use_nodes=True;scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs['Color'].default_value=(.55,.6,.66,1);bg.inputs['Strength'].default_value=.25
for name,loc,energy,size in [('Key',(-.8,-1.0,1.3),80,.9),('Fill',(1.0,-.3,.6),15,.7),('Rim',(.2,1.1,.8),35,.5)]:
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
report={'asset':'BoltCutter','blender_version':bpy.app.version_string,'source':inspect(objects),'settle_angle_degrees':math.degrees(lo),'jaw_opening_capacity_note':'Sized as a 30 inch cutter for the 10 mm gate chain wire','unity_verified':False,'method':'Independent background Blender process'}
assert all(o['nonmanifold_edges']==0 and o['zero_area_faces']==0 and o['uv_layers'] for o in report['source']['objects']),report['source']
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:ob.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Exports/BoltCutter.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_tspace=True,mesh_smooth_type='FACE',bake_anim=False,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Exports/BoltCutter.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_normals=True,export_tangents=True)
for im in bpy.data.images:
    if im.source=='FILE':im.pack()
scene['asset_notes']='30 inch bolt cutter: forged black jaws with ground edges, faded red enamel handles, cracked rubber grips. Unity material setup pending.'
aim((.55,-.85,.75),(0,0,.01),.9)
scene.render.filepath=str(ROOT/'Previews/BoltCutter_Overall.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blender/BoltCutter.blend'),compress=True)
(ROOT/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ASSET_SAVED',json.dumps(report),flush=True)
bpy.ops.render.render(write_still=True)
aim((0,0,2),(0,0,0),.84)
scene.render.filepath=str(ROOT/'Previews/BoltCutter_Top.png');bpy.ops.render.render(write_still=True)
jaw=Vector((660*MM-offset.x,-offset.y,.02))
aim(jaw+Vector((.25,-.35,.3)),jaw,.2)
scene.render.filepath=str(ROOT/'Previews/BoltCutter_JawDetail.png');bpy.ops.render.render(write_still=True)
# Square icon with transparent background for a future Backpack thumbnail.
floor.is_shadow_catcher=True;scene.render.film_transparent=True
scene.render.resolution_x=scene.render.resolution_y=512
aim((.25,-.45,1.0),(0,0,.01),.85)
scene.render.filepath=str(ROOT/'Previews/BoltCutter_Icon.png');bpy.ops.render.render(write_still=True)
print('BUILD_COMPLETE',flush=True)
