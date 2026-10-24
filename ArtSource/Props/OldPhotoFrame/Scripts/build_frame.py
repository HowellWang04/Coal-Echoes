"""Build the requested old wooden photo frame in an isolated background Blender."""
import bpy
import bmesh
import json
import math
import random
import shutil
import hashlib
from pathlib import Path
import numpy as np
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT.parents[2]
SOURCE = PROJECT / 'ArtSource/Props/OldPhotoFrame/References/StreetMarket_Original.png'
for folder in ['Blender', 'Exports', 'Textures', 'Previews']:
    (ROOT / folder).mkdir(parents=True, exist_ok=True)
random.seed(928)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.name = 'OldPhotoFrame_Authoring'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
asset = bpy.data.collections.new('OldPhotoFrame')
studio = bpy.data.collections.new('PreviewStudio')
scene.collection.children.link(asset)
scene.collection.children.link(studio)
N = 2048

def noise(nx, ny, seed):
    r = np.random.default_rng(seed)
    g = r.random((ny, nx), dtype=np.float32)
    x = np.arange(N, dtype=np.float32) * nx / N
    y = np.arange(N, dtype=np.float32) * ny / N
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

u=np.arange(N,dtype=np.float32)[None,:]/N
v=np.arange(N,dtype=np.float32)[:,None]/N
cloud=noise(8,13,1); fine=noise(270,600,2)
warp=noise(5,18,3)*.032+noise(20,50,4)*.002
grain=np.power(.5+.5*np.sin((v+warp)*math.tau*156),18)
fibres=noise(25,800,5)
pores=np.clip((fibres-.63)*4,0,1)
worn=np.clip((noise(65,140,6)*.52+cloud*.48-.58)*4,0,1)
tone=.77+.30*cloud+.09*fine-.16*grain-.19*pores
rgb=np.array([.205,.133,.083],dtype=np.float32)[None,None,:]*tone[:,:,None]
rubbed=np.array([.42,.31,.20],dtype=np.float32)
rgb=rgb*(1-worn[:,:,None]*.4)+rubbed*worn[:,:,None]*.4
# Uneven old varnish, small exposed fibres and scattered scratches.
scuffs=np.zeros((N,N),np.float32)
scratch_rng=np.random.default_rng(294)
for i in range(1600):
    x=int(scratch_rng.integers(0,N));y=int(scratch_rng.integers(0,N))
    length=int(scratch_rng.integers(3,62));width=int(scratch_rng.integers(1,4))
    xa=max(0,x-length);xb=min(N,x+length);ya=max(0,y-width);yb=min(N,y+width+1)
    xx=np.arange(xa,xb)[None,:];yy=np.arange(ya,yb)[:,None]
    stroke=np.clip(1-((xx-x)/length)**2,0,1)*np.clip(1-abs(yy-y)/(width+1),0,1)
    scuffs[ya:yb,xa:xb]=np.maximum(scuffs[ya:yb,xa:xb],stroke*scratch_rng.uniform(.25,.85))
scuffs*=np.clip((cloud-.25)*2,0,1)
rgb=rgb*(1-scuffs[:,:,None]*.8)+rubbed*scuffs[:,:,None]*.8
rgb*= (.70+.30*noise(11,31,293))[:,:,None]
rough=np.clip(.47+.20*cloud+.19*pores+.15*worn,0,1)
wood,bs=material('Frame_AgedWalnut',(.065,.035,.015))
texture(wood,image_file('Wood_BaseColor',rgb,True),'Base Color')
texture(wood,image_file('Wood_Roughness',rough),'Roughness')
attach_normal(wood,image_file('Wood_Normal',normal_map(grain*.12+pores*.4+fine*.1,2)),.45)
image_file('Wood_HDRP_Mask',np.dstack((np.zeros_like(rough),np.ones_like(rough),np.ones_like(rough),1-rough)))
bs.inputs['Coat Weight'].default_value=.07
bs.inputs['Coat Roughness'].default_value=.48
edge,_=material('WornWood_Edges',(.11,.069,.037),.76)
edgewear=np.clip((noise(41,89,531)-.53)*3,0,1)
edgecolor=rgb*(1-edgewear[:,:,None]*.7)+rubbed*edgewear[:,:,None]*.7
texture(edge,image_file('Wood_EdgeColor',edgecolor,True),'Base Color')
backmat,bbs=material('Backboard_Fiberboard',(.08,.052,.028),.94)
texture(backmat,image_file('Backboard_BaseColor',np.array([.30,.24,.17])[None,None,:]*(.72+.23*noise(18,24,23)+.12*fine)[:,:,None],True),'Base Color')
iron,_=material('Hardware_OxidizedIron',(.043,.036,.027),.74,.58)

# The supplied photo is copied byte for byte. Its aspect ratio determines the opening.
dst=ROOT/'Textures/StreetMarket_Original.png'; shutil.copy2(SOURCE,dst)
photoim=bpy.data.images.load(str(dst))
pw,ph=photoim.size
W=.320; IW=.264; IH=IW*ph/pw; H=IH+.056
photo,pbs=material('Photo_OriginalBlackAndWhite',(.8,.8,.8),.83)
texture(photo,photoim,'Base Color')

# Fine local cracks, concentrated around a small upper-right impact point.
# Coordinates are photo UVs, so the source photograph stays untouched.
paths=[
    [(.81,.83),(.85,.88),(.875,.933),(.918,1.0)],
    [(.81,.83),(.87,.812),(.94,.838),(1.0,.852)],
    [(.81,.83),(.793,.76),(.805,.681),(.775,.601),(.784,.544)],
    [(.81,.83),(.735,.842),(.683,.811),(.63,.82)],
    [(.81,.83),(.765,.884),(.743,.952),(.72,1.0)],
    [(.81,.83),(.842,.781),(.898,.748),(.951,.691)],
    [(.805,.681),(.839,.652),(.85,.61)],
    [(.875,.933),(.943,.95),(.972,.974)],
    [(.735,.842),(.708,.894),(.664,.922)],
    [(.87,.812),(.886,.859),(.85,.88)],
]
crack=np.zeros((N,N),np.float32)
def draw_segment(a,b,width):
    x0,y0=a; x1,y1=b
    xmin=max(0,int((min(x0,x1)-width*3)*N)); xmax=min(N,int((max(x0,x1)+width*3)*N)+1)
    ymin=max(0,int((min(y0,y1)-width*3)*N)); ymax=min(N,int((max(y0,y1)+width*3)*N)+1)
    xx=np.arange(xmin,xmax,dtype=np.float32)[None,:]/(N-1)
    yy=np.arange(ymin,ymax,dtype=np.float32)[:,None]/(N-1)
    dx=x1-x0;dy=y1-y0
    t=np.clip(((xx-x0)*dx+(yy-y0)*dy)/(dx*dx+dy*dy),0,1)
    distance=np.sqrt((xx-x0-t*dx)**2+(yy-y0-t*dy)**2)
    m=np.exp(-(distance/width)**2*2)
    crack[ymin:ymax,xmin:xmax]=np.maximum(crack[ymin:ymax,xmin:xmax],m)

jagged=[]
for pi,path in enumerate(paths):
    pts=[path[0]]
    for a,b in zip(path,path[1:]):
        for k in range(1,5):
            t=k/4
            jitter=random.uniform(-.0017,.0017) if k<4 else 0
            pts.append((a[0]+(b[0]-a[0])*t+jitter,a[1]+(b[1]-a[1])*t+jitter*.45))
    jagged.append(pts)
    for a,b in zip(pts,pts[1:]):draw_segment(a,b,.00065 if pi<6 else .00042)
# A few short splinters at the impact, not a missing piece.
for i in range(11):
    angle=i*math.tau/11+random.uniform(-.12,.12)
    a=(.81,.83); b=(.81+math.cos(angle)*random.uniform(.004,.011),.83+math.sin(angle)*random.uniform(.004,.014))
    draw_segment(a,b,.00048)
border=np.exp(-np.minimum(np.minimum(u,1-u),np.minimum(v,1-v))*85)
grime=border*(.25+.75*noise(25,22,19))
grough=np.clip(.035+noise(12,12,22)*.025+grime*.27+crack*.63,0,1)
glass,gbs=material('Glass_LocalCracks',(.96,.985,.973),.045)
gbs.inputs['IOR'].default_value=1.47
trans=np.clip(1-crack*.6-grime*.13,0,1)
texture(glass,image_file('Glass_Transmission',trans),'Transmission Weight')
texture(glass,image_file('Glass_Roughness',grough),'Roughness')
attach_normal(glass,image_file('Glass_Normal',normal_map(crack,1.2)),.34)
image_file('Glass_CrackMask',crack)
image_file('Glass_HDRP_Mask',np.dstack((np.zeros_like(grough),np.ones_like(grough),np.ones_like(grough),1-grough)))
image_file('Glass_BaseColorOpacity',np.dstack((np.ones_like(crack)*.88,np.ones_like(crack)*.9,np.ones_like(crack)*.87,np.clip(.025+crack*.66+grime*.11,0,1))),True)

def mesh(name,verts,faces,mat):
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);asset.objects.link(ob);me.materials.append(mat)
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    return ob

def bevel(ob,width,segments=2,edge_mat=None):
    bpy.context.view_layer.objects.active=ob
    mod=ob.modifiers.new('SoftWornEdges','BEVEL');mod.width=width;mod.segments=segments
    if edge_mat:
        ob.data.materials.append(edge_mat);mod.material=len(ob.data.materials)-1
    bpy.ops.object.modifier_apply(modifier=mod.name)

def planar_uv(ob,axes=(0,1),scale=(IW,IH),offset=(.5,.5)):
    uv=ob.data.uv_layers.new(name='UVMap')
    for loop in ob.data.loops:
        co=ob.data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=(co[axes[0]]/scale[0]+offset[0],co[axes[1]]/scale[1]+offset[1])

def box(name,loc,size,mat,radius=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=bpy.context.object;ob.name=name;ob.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for col in list(ob.users_collection):col.objects.unlink(ob)
    asset.objects.link(ob);ob.data.materials.append(mat)
    if radius:bevel(ob,radius)
    return ob

# Closed four-piece, mitered wooden moulding. Cross section has a shallow cove.
profile=[(0,-.012),(0,.007),(.0015,.010),(.0045,.0115),(.007,.011),(.009,.009),(.012,.005),(.018,.0035),(.021,.0045),(.023,.006),(.025,.006),(.028,.0035),(.028,-.012)]
corners=[(-1,-1),(1,-1),(1,1),(-1,1)]
for side in range(4):
    a=corners[side];b=corners[(side+1)%4]
    verts=[]
    for sx,sy in [a,b]:
        verts.extend([(sx*(W/2-t),sy*(H/2-t),z) for t,z in profile])
    n=len(profile)
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]
    faces.extend([(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)])
    ob=mesh(['Frame_Bottom','Frame_Right','Frame_Top','Frame_Left'][side],verts,faces,wood)
    bevel(ob,.00035,2,edge)
    uv=ob.data.uv_layers.new(name='UVMap')
    horizontal=side%2==0
    for loop in ob.data.loops:
        co=ob.data.vertices[loop.vertex_index].co
        length=co.x if horizontal else co.y
        cross=(H/2-abs(co.y)) if horizontal else (W/2-abs(co.x))
        uv.data[loop.index].uv=(length/.40+.51+side*.137,cross/.08+side*.21)

box('Backboard', (0,0,-.0104), (W-.01,H-.01,.004),backmat,.00065)
photoplate=box('Photograph',(0,0,-.002),(IW+.001,IH+.001,.0004),photo,.0001)
# Override default cube UVs with the same full photograph on the front.
photoplate.data.uv_layers.remove(photoplate.data.uv_layers.active)
planar_uv(photoplate)
glassob=box('GlassPane',(0,0,.0002),(IW+.001,IH+.001,.0013),glass,.00012)
glassob.data.uv_layers.remove(glassob.data.uv_layers.active)
planar_uv(glassob)

# Closed narrow fracture prisms catch a little light and survive FBX export.
# They are subordinate to the texture mask, never thick white wires.
fracture,fbs=material('Glass_FractureEdges',(.24,.27,.26),.32)
fbs.inputs['Transmission Weight'].default_value=.32
fbs.inputs['IOR'].default_value=1.47
vs=[];fs=[]
for pi,pts in enumerate(jagged):
    for a,b in zip(pts,pts[1:]):
        ax=(a[0]-.5)*IW;ay=(a[1]-.5)*IH
        bx=(b[0]-.5)*IW;by=(b[1]-.5)*IH
        dx=bx-ax;dy=by-ay;length=math.hypot(dx,dy)
        width=.000035 if pi<6 else .000019
        nx=-dy/length*width;ny=dx/length*width
        base=len(vs);z=.00087
        vs.extend([(ax+nx,ay+ny,z),(ax-nx,ay-ny,z),(ax,ay,z+.000045),
                   (bx+nx,by+ny,z),(bx-nx,by-ny,z),(bx,by,z+.000045)])
        fs.extend([tuple(base+i for i in f) for f in [(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]])
ob=mesh('FineFractureEdges',vs,fs,fracture);planar_uv(ob)

for i,(x,y) in enumerate([(-W*.36,-H*.44),(W*.36,-H*.44),(-W*.36,H*.44),(W*.36,H*.44)]):
    box('BackingTab_'+str(i+1),(x,y,-.014),( .015,.005,.001),iron,.0002)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=(x,y,-.0148))
    ob=bpy.context.object;ob.name='TabRivet_'+str(i+1);ob.scale=(.0017,.0017,.0006)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for col in list(ob.users_collection):col.objects.unlink(ob)
    asset.objects.link(ob);ob.data.materials.append(iron)

# Hanging loop on the back, within the outer frame silhouette.
bpy.ops.mesh.primitive_torus_add(major_segments=24,minor_segments=6,location=(0,H*.37,-.0155),major_radius=.010,minor_radius=.0012)
ob=bpy.context.object;ob.name='HangingLoop';ob.scale.x=.65
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
for col in list(ob.users_collection):col.objects.unlink(ob)
asset.objects.link(ob);ob.data.materials.append(iron)

# Turn the asset upright. Blender Z is up, its front faces -Y, bottom origin is zero.
transform=Matrix.Translation((0,0,H/2)) @ Matrix.Rotation(math.pi/2,4,'X')
for ob in list(asset.objects):
    ob.matrix_world=transform @ ob.matrix_world
bpy.context.view_layer.update()
for ob in list(asset.objects):
    ob.data.transform(ob.matrix_world);ob.matrix_world=Matrix.Identity(4)
    bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
    ob.data.update()

def studio_link(ob):
    for col in list(ob.users_collection):col.objects.unlink(ob)
    studio.objects.link(ob)

wallmat,_=material('Studio_ClayPlaster',(.18,.17,.145),.93)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,.033,H/2),rotation=(math.pi/2,0,0))
wall=bpy.context.object;wall.name='StudioWall';studio_link(wall);wall.data.materials.append(wallmat)
world=bpy.data.worlds.new('SoftStudio');world.use_nodes=True;scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs['Color'].default_value=(.55,.6,.66,1);bg.inputs['Strength'].default_value=.18
for name,loc,energy,size in [('Key',(-.38,-.48,.63),18,.45),('Fill',(.38,-.18,.24),3,.30),('Top',(.05,.02,.55),6,.25)]:
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size
    ob=bpy.data.objects.new(name,d);studio.objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector((0,0,H*.5))-ob.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('PreviewCamera');cam=bpy.data.objects.new('PreviewCamera',camdata);studio.objects.link(cam);scene.camera=cam
camdata.type='ORTHO';camdata.ortho_scale=.43
cam.location=(.29,-.9,.35);cam.rotation_euler=(Vector((0,0,H*.5))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES'
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
scene.cycles.device='GPU';scene.cycles.samples=64;scene.cycles.use_denoising=True
scene.cycles.max_bounces=12;scene.cycles.transmission_bounces=8
scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'

def inspect(objects):
    bpy.context.view_layer.update()
    bounds=[ob.matrix_world@Vector(co) for ob in objects for co in ob.bound_box]
    result={'mesh_count':len(objects),'dimensions_xyz_m':[max(v[i] for v in bounds)-min(v[i] for v in bounds) for i in range(3)],'objects':[]}
    for ob in objects:
        ob.data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(ob.data)
        result['objects'].append({'name':ob.name,'triangles':len(ob.data.loop_triangles),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'zero_area_faces':sum(f.calc_area()<1e-14 for f in bm.faces),'uv_layers':len(ob.data.uv_layers)})
        bm.free()
    result['triangles']=sum(o['triangles'] for o in result['objects'])
    return result

objects=list(asset.objects)
report={'asset':'OldPhotoFrame','blender_version':bpy.app.version_string,'source':inspect(objects),'photo_size_px':[pw,ph],'photo_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'photo_copy_identical':SOURCE.read_bytes()==dst.read_bytes(),'photo_opening_m':[IW,IH],'unity_verified':False,'damage':'Localized upper-right cracks with intact pane; photo remains legible','method':'Independent background Blender process'}
assert report['photo_copy_identical']
assert all(o['nonmanifold_edges']==0 and o['zero_area_faces']==0 and o['uv_layers'] for o in report['source']['objects']),report['source']
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:ob.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Exports/OldPhotoFrame.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_tspace=True,mesh_smooth_type='FACE',bake_anim=False,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Exports/OldPhotoFrame.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_normals=True,export_tangents=True)
for im in bpy.data.images:
    if im.source=='FILE':im.pack()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            region=area.spaces.active.region_3d;region.view_distance=.55;region.view_location=Vector((0,0,H/2));region.view_rotation=cam.rotation_euler.to_quaternion()
scene['asset_notes']='Aged walnut frame, original archived photograph, localized upper-right glass cracks. Unity material setup pending.'
scene.render.filepath=str(ROOT/'Previews/OldPhotoFrame_Overall.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blender/OldPhotoFrame.blend'),compress=True)
(ROOT/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ASSET_SAVED',json.dumps(report),flush=True)
bpy.ops.render.render(write_still=True)
cam.location=(0,-1,H/2);cam.rotation_euler=(Vector((0,0,H/2))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.ortho_scale=.385
scene.render.filepath=str(ROOT/'Previews/OldPhotoFrame_Front.png');bpy.ops.render.render(write_still=True)
cam.location=(.18,-.7,.33);target=Vector((.067,0,.195));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camdata.ortho_scale=.177
scene.render.filepath=str(ROOT/'Previews/OldPhotoFrame_GlassDetail.png');bpy.ops.render.render(write_still=True)
print('BUILD_COMPLETE',flush=True)
