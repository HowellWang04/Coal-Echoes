"""Build the approved standalone roller frame in a fresh background Blender."""
import bpy
import bmesh
import math
import json
import shutil
import random
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
STATION = ROOT.parent / 'CablewayStationShell'
random.seed(28)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.name = 'CablewayStationRollerFrame_Authoring'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
asset = bpy.data.collections.new('CablewayStationRollerFrame')
scene.collection.children.link(asset)
studio = bpy.data.collections.new('PreviewStudio')
scene.collection.children.link(studio)

def surface_maps(name, kind, darken):
    """Tileable fine wear over the station palette, exported as ordinary PBR maps."""
    n=2048
    rng=np.random.default_rng(728 if not darken else 729)
    def noise(cells):
        grid=rng.random((cells,cells),dtype=np.float32)
        coords=np.arange(n,dtype=np.float32)*cells/n
        lo=coords.astype(int)%cells; hi=(lo+1)%cells; f=coords%1
        rows=grid[:,lo]*(1-f)[None,:]+grid[:,hi]*f[None,:]
        return rows[lo,:]*(1-f)[:,None]+rows[hi,:]*f[:,None]
    cloud=noise(18)
    grain=noise(340)
    freckles=noise(180)
    chips=np.clip((.58*freckles+.42*cloud-.58)*8,0,1)
    pits=np.clip((grain-.64)*4,0,1)
    # Narrow irregular streaks contribute smaller weathering marks than the broad station grain.
    streak=noise(74)
    tone=(.76+.20*cloud+.08*grain)[...,None]
    base=np.array([.40,.385,.35],dtype=np.float32)*tone
    oxid=np.array([.17,.077,.027],dtype=np.float32)
    if darken:
        base*=.27
        chips*=.65
    base=base*(1-chips[...,None]*.85)+oxid*chips[...,None]*.85
    base*=1-pits[...,None]*.14
    rough=np.clip(.68+.20*chips+.09*grain,0,1)
    metal=np.clip(.62-.52*chips,0,1)
    height=.6*grain+.30*chips+.1*streak
    gy,gx=np.gradient(height)
    normal=np.dstack((-gx*.72,-gy*.72,np.ones_like(gx)))
    normal/=np.linalg.norm(normal,axis=2)[...,None]
    maps={'BaseColor':base,'Roughness':np.repeat(rough[...,None],3,axis=2),'Metallic':np.repeat(metal[...,None],3,axis=2),'Normal':normal*.5+.5,'ORM':np.dstack((np.ones_like(rough),rough,metal)),'HDRP_Mask':np.dstack((metal,np.ones_like(rough),np.ones_like(rough),1-rough))}
    for suffix,rgb in maps.items():
        rgba=np.ones((n,n,4),dtype=np.float32)
        rgba[:,:,:rgb.shape[2]]=rgb
        image=bpy.data.images.new(name+'_'+suffix,width=n,height=n,alpha=True)
        image.colorspace_settings.name='sRGB' if suffix=='BaseColor' else 'Non-Color'
        image.pixels.foreach_set(rgba.ravel())
        image.filepath_raw=str(ROOT/'Textures'/(name+'_'+suffix+'.png'))
        image.file_format='PNG';image.save()
        bpy.data.images.remove(image)

def material(name, kind, darken=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    ns = mat.node_tree.nodes
    bs = next(n for n in ns if n.type == 'BSDF_PRINCIPLED')
    if kind=='Steel':surface_maps(name,kind,darken)
    for suffix, socket in [('BaseColor','Base Color'),('Roughness','Roughness'),('Metallic','Metallic'),('Normal','Normal')]:
        src = STATION / 'Textures' / ('Station_' + kind + '_' + suffix + '.png')
        dst = ROOT / 'Textures' / (name + '_' + suffix + '.png')
        if kind!='Steel':shutil.copy2(src, dst)
        im = bpy.data.images.load(str(dst), check_existing=False)
        if suffix != 'BaseColor':
            im.colorspace_settings.name = 'Non-Color'
        im.pack()
        tex = ns.new('ShaderNodeTexImage')
        tex.image = im
        tex.label = suffix
        if suffix == 'Normal':
            normal = ns.new('ShaderNodeNormalMap')
            normal.inputs['Strength'].default_value = .3 if darken else .4
            mat.node_tree.links.new(tex.outputs['Color'], normal.inputs['Color'])
            mat.node_tree.links.new(normal.outputs['Normal'], bs.inputs['Normal'])
        else:
            mat.node_tree.links.new(tex.outputs['Color'], bs.inputs[socket])
    if kind!='Steel':
        for suffix in ['ORM','HDRP_Mask']:
            shutil.copy2(STATION/'Textures'/('Station_'+kind+'_'+suffix+'.png'), ROOT/'Textures'/(name+'_'+suffix+'.png'))
    return mat

steel = material('RollerFrame_Steel', 'Steel')
concrete = material('RollerFrame_Concrete', 'Concrete')
wheelmat = material('RollerFrame_DarkSteel', 'Steel', .38)
rust = bpy.data.materials.new('RollerFrame_OxidizedEdges')
rust.use_nodes = True
bs = next(n for n in rust.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
bs.inputs['Base Color'].default_value = (.13,.065,.025,1)
bs.inputs['Metallic'].default_value = .25
bs.inputs['Roughness'].default_value = .86

parts = {}
def part(name, mat):
    d = {'v':[], 'f':[], 'mat':mat}
    parts[name] = d
    return d

def mesh_add(d, verts, faces):
    start = len(d['v'])
    d['v'].extend(verts)
    d['f'].extend([tuple(start+i for i in f) for f in faces])

def box(d, center, size, rotation=None):
    c = Vector(center)
    rot = rotation or Matrix.Identity(3)
    v = [c + rot @ Vector((sx*size[0]/2,sy*size[1]/2,sz*size[2]/2)) for sx,sy,sz in
         [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    mesh_add(d,v,[(3,2,1,0),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])

def beam(d, a, b, width, depth):
    a,b = Vector(a),Vector(b)
    rot = (b-a).to_track_quat('Z','Y').to_matrix()
    box(d,(a+b)/2,(width,depth,(b-a).length),rot)

def ibeam(d,a,b,w=.25,depth=.28,t=.032):
    a,b = Vector(a),Vector(b)
    rot = (b-a).to_track_quat('Z','Y').to_matrix()
    c=(a+b)/2
    for y in [-(depth-t)/2,(depth-t)/2]:
        box(d,c+rot@Vector((0,y,0)),(w,t,(b-a).length),rot)
    box(d,c,(t,depth-2*t,(b-a).length),rot)

def lathe(d, center, profile, axis=(1,0,0), sides=48):
    # Closed radial cross-section revolved about the local X axis.
    rot = Vector(axis).to_track_quat('X','Z').to_matrix()
    c=Vector(center)
    verts=[c+rot@Vector((x,r*math.cos(j*2*math.pi/sides),r*math.sin(j*2*math.pi/sides))) for x,r in profile for j in range(sides)]
    faces=[]
    for k in range(len(profile)):
        for j in range(sides):
            faces.append((k*sides+j,k*sides+(j+1)%sides,((k+1)%len(profile))*sides+(j+1)%sides,((k+1)%len(profile))*sides+j))
    mesh_add(d,verts,faces)

def cylinder(d, center, radius, length, axis=(0,0,1), sides=16):
    rot=Vector(axis).to_track_quat('Z','Y').to_matrix();c=Vector(center)
    v=[c+rot@Vector((radius*math.cos(j*math.tau/sides),radius*math.sin(j*math.tau/sides),z)) for z in [-length/2,length/2] for j in range(sides)]
    f=[tuple(reversed(range(sides))),tuple(range(sides,2*sides))]
    f += [(j,(j+1)%sides,(j+1)%sides+sides,j+sides) for j in range(sides)]
    mesh_add(d,v,f)

fast=part('Fasteners',steel)
def bolt(p, axis=(0,0,1), r=.035):
    p=Vector(p);a=Vector(axis)
    cylinder(fast,p,r*1.45,.009,axis,16)
    cylinder(fast,p+a*.022,r,.035,axis,6)
    cylinder(fast,p+a*.046,r*.55,.017,axis,12)

frame=part('PortalSteelwork',steel)
plates=part('ConnectionPlates',steel)
cradle=part('RollerCradle',steel)
bearings=part('BearingHousings',steel)
axles=part('AxleHardware',wheelmat)

# Build the portal locally, then align its long axis with the sheave row in Y.
for side in [-1,1]:
    x=side*1.4
    foot=part('ConcreteFooting_'+('Front' if side<0 else 'Rear'),concrete)
    box(foot,(x,0,.23),(.82,.88,.46))
    box(plates,(x,0,.49),(.64,.65,.06))
    ibeam(frame,(x,0,.52),(x,0,3.82))
    for dx in [-.235,.235]:
        for dy in [-.24,.24]:bolt((x+dx,dy,.535),r=.037)
    for y in [-.164,.164]:
        box(plates,(x,y,.75),(.32,.035,.43))
        for xx in [-.095,.095]:
            for z in [.62,.86]:bolt((x+xx,y+math.copysign(.022,y),z),(0,math.copysign(1,y),0),.027)
    for dx in [-.14,.14]:
        beam(plates,(x+dx*1.65,0,.53),(x+dx,0,.91),.036,.17)
    beam(frame,(x,0,3.05),(x-side*.64,0,3.77),.135,.15)
    box(plates,(x,0,3.89),(.5,.48,.06))
    for y in [-.19,.19]:
        box(plates,(x,y,3.72),(.42,.035,.47))
        for dx in [-.12,.12]:
            for z in [3.58,3.83]:bolt((x+dx,y+math.copysign(.023,y),z),(0,math.copysign(1,y),0),.03)
    for dx in [-.18,.18]:
        for dy in [-.15,.15]:bolt((x+dx,dy,3.93),r=.03)

# Crosshead built from a web and two flanges with visible I section.
box(frame,(0,0,3.69),(3.26,.033,.29))
for z in [3.53,3.85]:box(frame,(0,0,z),(3.34,.35,.035))
for x in [-1.64,1.64]:box(plates,(x,0,3.69),(.035,.35,.33))

# User correction on September 28. Rotate only the lower frame and its fittings.
# The three wheel centers, bearings and cradle keep their original direction.
for d in parts.values():
    d['v']=[Vector((-v[1],v[0],v[2])) for v in d['v']]

# Longitudinal twin cradle rails and three grooved sheaves.
for x in [-.23,.23]:
    ibeam(cradle,(x,-1.26,4.02),(x,1.26,4.02),.15,.14,.023)
    for y in [-.24,.24]:
        box(plates,(x,y,3.95),(.22,.26,.12))
        bolt((x,y,4.10),r=.026)
for y in [-1.2,-.82,0,.82,1.2]:box(cradle,(0,y,3.96),(.61,.09,.045))

for i,y in enumerate([-.82,0,.82],1):
    z=4.45
    for side in [-1,1]:
        x=side*.225
        # Tapered bearing pedestal with round top housing.
        beam(bearings,(x,y-.13,4.08),(x,y,4.42),.065,.095)
        beam(bearings,(x,y+.13,4.08),(x,y,4.42),.065,.095)
        box(bearings,(x,y,4.085),(.16,.39,.055))
        cylinder(bearings,(x,y,z),.138,.075,(1,0,0),32)
        for dy in [-.14,.14]:bolt((x,y+dy,4.12),r=.025)
        bolt((side*.277,y,z),(side,0,0),.065)
        cylinder(axles,(side*.276,y,z),.097,.015,(1,0,0),32)
    cylinder(axles,(0,y,z),.064,.63,(1,0,0),24)
    wheel=part('Sheave_'+str(i).zfill(2),wheelmat)
    # Smooth U groove between two raised flanges and a recessed disc.
    profile=[(-.093,.077),(-.093,.135),(-.064,.17),(-.064,.307),(-.109,.328),(-.112,.366),(-.09,.385),(-.064,.378),(-.05,.35),(-.027,.334),(0,.328),(.027,.334),(.05,.35),(.064,.378),(.09,.385),(.112,.366),(.109,.328),(.064,.307),(.064,.17),(.093,.135),(.093,.077)]
    lathe(wheel,(0,y,z),profile,sides=64)
    for side in [-1,1]:
        lathe(wheel,(0,y,z),[(side*.066,.273),(side*.08,.28),(side*.079,.293),(side*.065,.296)],sides=64)
    wheel['pivot']=(0,y,z)

objects=[]
for name,d in parts.items():
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(d['v'],[],d['f']);me.update()
    ob=bpy.data.objects.new(name,me);asset.objects.link(ob);me.materials.append(d['mat']);me.materials.append(rust)
    bpy.context.view_layer.objects.active=ob;ob.select_set(True)
    if name not in ['Fasteners','AxleHardware'] and not name.startswith('Sheave'):
        bevel=ob.modifiers.new('SmallFabricationEdges','BEVEL')
        bevel.width=.018 if 'Concrete' in name else .004
        bevel.segments=2
        bevel.limit_method='ANGLE'
        bevel.angle_limit=math.radians(35)
        bevel.material=0 if 'Concrete' in name else 1
        bpy.ops.object.modifier_apply(modifier=bevel.name)
    me=ob.data
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    uv=me.uv_layers.new(name='UVMap')
    for p in me.polygons:
        drop=max(range(3),key=lambda k:abs(p.normal[k]));axes=[a for a in range(3) if a!=drop]
        for li in p.loop_indices:
            co=me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv=(co[axes[0]]/(.72 if 'Concrete' in name else 1.1)+.13,co[axes[1]]/(.72 if 'Concrete' in name else 1.1)+.28)
        p.use_smooth=name.startswith('Sheave')
    if name.startswith('Sheave'):
        me.set_sharp_from_angle(angle=math.radians(35))
        pivot=Vector(d['pivot'])
        for v in me.vertices:v.co-=pivot
        ob.location=pivot
        ob['rotation_axis']='Local X'
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
    ob.select_set(False);objects.append(ob)

# Studio objects are excluded from the asset exports.
def studio_link(ob):
    for col in list(ob.users_collection):col.objects.unlink(ob)
    studio.objects.link(ob)

bpy.ops.mesh.primitive_plane_add(size=200)
ground=bpy.context.object;ground.name='StudioGround';ground.location.z=-.012;studio_link(ground)
gm=bpy.data.materials.new('StudioWarmGray');gm.diffuse_color=(.23,.215,.195,1);ground.data.materials.append(gm)
world=bpy.data.worlds.new('RollerFrameStudioWorld');scene.world=world;world.use_nodes=True
world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.55,.59,.65,1)
world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.35
for name,loc,power,size in [('Key',(-4,-5,9),1400,5),('Fill',(5,-1,6),750,4),('Rim',(1,5,8),1600,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    ob=bpy.data.objects.new(name,data);studio.objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector((0,0,2.5))-ob.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('PreviewCamera');camera=bpy.data.objects.new('PreviewCamera',camdata);studio.objects.link(camera);scene.camera=camera
camdata.type='ORTHO';camdata.ortho_scale=6.4
camera.location=(10,-8,7);camera.rotation_euler=(Vector((0,0,2.45))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES'
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX';prefs.get_devices()
for device in prefs.devices:device.use=device.type=='OPTIX'
scene.cycles.device='GPU';scene.cycles.samples=48;scene.cycles.preview_samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1400;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.render.image_settings.file_format='PNG'

def inspect(meshes):
    bpy.context.view_layer.update()
    bounds=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    report={'mesh_count':len(meshes),'dimensions_xyz_m':[max(v[a] for v in bounds)-min(v[a] for v in bounds) for a in range(3)],'objects':[]}
    for o in meshes:
        o.data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(o.data)
        report['objects'].append({'name':o.name,'triangles':len(o.data.loop_triangles),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'zero_area_faces':sum(f.calc_area()<1e-10 for f in bm.faces),'uv_layers':len(o.data.uv_layers)})
        bm.free()
    report['triangles']=sum(o['triangles'] for o in report['objects'])
    return report

report={'asset':'CablewayStationRollerFrame','blender_version':bpy.app.version_string,'source':inspect(objects),'method':'Background Python, no MCP','unity_modified':False,'clearance_verified':False,'cable_geometry_included':False,'notes':['User-corrected alignment: lower frame and wheel row both extend along Y.','Existing Unity bucket height in saved report is 12.3699512 m; assembly scale unresolved.','Three sheaves independently editable around local X. Rope will be configured separately.']}
assert all(x['nonmanifold_edges']==0 and x['zero_area_faces']==0 for x in report['source']['objects']), report['source']
scene['asset_name']='CablewayStationRollerFrame'
scene['status']='Standalone model; site placement and bucket clearance pending'
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Exports/CablewayStationRollerFrame.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',use_tspace=True,mesh_smooth_type='FACE',bake_anim=False,path_mode='COPY',embed_textures=True)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'Exports/CablewayStationRollerFrame.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_normals=True,export_tangents=True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            region=area.spaces.active.region_3d
            region.view_distance=9
            region.view_location=Vector((0,0,2.4))
            region.view_rotation=camera.rotation_euler.to_quaternion()
scene.render.filepath=str(ROOT/'Previews/RollerFrame_Overall.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Blender/CablewayStationRollerFrame.blend'),compress=True)
(ROOT/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MODEL_SAVED',json.dumps(report['source']),flush=True)
bpy.ops.render.render(write_still=True)
camera.location=(7,-9,6.9);target=Vector((0,0,4.2));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camdata.ortho_scale=3.35
scene.render.filepath=str(ROOT/'Previews/RollerFrame_WheelDetail.png')
bpy.ops.render.render(write_still=True)
camera.location=(12,0,5.3);target=Vector((0,0,2.43));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camdata.ortho_scale=6.1
scene.render.filepath=str(ROOT/'Previews/RollerFrame_Front.png')
bpy.ops.render.render(write_still=True)
print('BUILD_AND_RENDERS_COMPLETE',flush=True)
