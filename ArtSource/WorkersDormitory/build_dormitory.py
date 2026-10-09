"""Build the approved three-storey workers' dormitory in a separate Blender scene.

Run inside Blender with runpy.run_path. Export files contain the building only.
Existing scenes and objects are preserved. Image processing is material preparation
from the project's existing Industrial Harbour textures, not the concept picture.
"""
from pathlib import Path
import json
import math
import random
import shutil

import bpy
import bmesh
import numpy as np
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parent.parent
TEX = SOURCE / 'Textures'
PACK = ROOT / 'Assets/Art Resources/ThirdPartyPacks/Industrial_Harbour/HDRP/Art/Textures'
rng = random.Random(19731004)
materials = {}
batches = {}


def enum_set(owner, prop, value):
    valid = [item.identifier for item in owner.bl_rna.properties[prop].enum_items]
    if value not in valid:
        raise ValueError((prop, value, valid))
    setattr(owner, prop, value)


def read_pixels(path):
    image = bpy.data.images.load(str(path), check_existing=False)
    enum_set(image.colorspace_settings, 'name', 'Non-Color')
    w, h = image.size
    values = np.empty(w*h*4, dtype=np.float32)
    image.pixels.foreach_get(values)
    bpy.data.images.remove(image)
    return values.reshape(h, w, 4)


def save_pixels(name, pixels):
    h, w = pixels.shape[:2]
    image = bpy.data.images.new(name, width=w, height=h, alpha=True)
    enum_set(image.colorspace_settings, 'name', 'Non-Color')
    image.pixels.foreach_set(np.ascontiguousarray(pixels, dtype=np.float32).ravel())
    enum_set(image, 'file_format', 'PNG')
    image.filepath_raw = str(TEX / (name + '.png'))
    image.save()
    bpy.data.images.remove(image)


def prepare_textures():
    TEX.mkdir(exist_ok=True)
    brick = read_pixels(PACK / 'T_BrickWall_B.PNG')
    rgb = brick[:, :, :3]
    luminance = rgb @ np.array([.2126, .7152, .0722], dtype=np.float32)
    # Preserve the real brick grain and mortar while shifting red brick to dusty buff.
    variation = np.clip((luminance-.28)*.82, -.16, .15)
    mortar = np.clip((.065-(rgb[:, :, 0]-rgb[:, :, 1]))/.055, 0, 1)
    buff = np.array([.57, .535, .46], dtype=np.float32)+variation[:, :, None]
    grey = np.array([.46, .445, .405], dtype=np.float32)+(luminance-.35)[:, :, None]*.55
    brick[:, :, :3] = np.clip(buff*(1-mortar[:, :, None])+grey*mortar[:, :, None], 0, 1)
    brick[:, :, 3] = 1
    save_pixels('Dorm_BuffBrick_BaseColor', brick)
    for material, original in [('BuffBrick', 'T_BrickWall'), ('Concrete', 'T_CrackedConcrete')]:
        for suffix, source_suffix in [('Normal','N'), ('ORM','ORM')]:
            shutil.copyfile(PACK / f'{original}_{source_suffix}.PNG', TEX / f'Dorm_{material}_{suffix}.png')
        normal = read_pixels(TEX / f'Dorm_{material}_Normal.png')
        xy = normal[:, :, :2]*2-1
        normal[:, :, 2] = np.sqrt(np.clip(1-np.sum(xy*xy,axis=2),0,1))*.5+.5
        normal[:, :, 3] = 1
        save_pixels(f'Dorm_{material}_Normal', normal)
        if material == 'Concrete':
            shutil.copyfile(PACK / f'{original}_B.PNG', TEX / 'Dorm_Concrete_BaseColor.png')
        orm = read_pixels(TEX / f'Dorm_{material}_ORM.png')
        mask = np.ones_like(orm)
        mask[:, :, 0] = 0
        mask[:, :, 1] = orm[:, :, 0]
        mask[:, :, 3] = 1-orm[:, :, 1]
        save_pixels(f'Dorm_{material}_HDRPMask', mask)


def material(name, color, rough=.8, metal=0, texture=None):
    mat = bpy.data.materials.new('Dorm_' + name)
    mat.use_nodes = True
    mat.diffuse_color = (*color, 1)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metal
    if texture:
        for suffix in ('BaseColor','Normal','ORM'):
            im = bpy.data.images.load(str(TEX / f'Dorm_{texture}_{suffix}.png'), check_existing=True)
            enum_set(im.colorspace_settings, 'name', 'sRGB' if suffix == 'BaseColor' else 'Non-Color')
            node = nodes.new('ShaderNodeTexImage')
            node.image = im
            node.label = suffix
            if suffix == 'BaseColor':
                links.new(node.outputs['Color'], bsdf.inputs['Base Color'])
            elif suffix == 'Normal':
                normal = nodes.new('ShaderNodeNormalMap')
                normal.inputs['Strength'].default_value = .65
                links.new(node.outputs['Color'], normal.inputs['Color'])
                links.new(normal.outputs['Normal'], bsdf.inputs['Normal'])
            else:
                channels = nodes.new('ShaderNodeSeparateColor')
                links.new(node.outputs['Color'], channels.inputs['Color'])
                links.new(channels.outputs['Green'], bsdf.inputs['Roughness'])
    materials[name] = mat
    return mat


class Mesh:
    def __init__(self):
        self.vertices, self.faces, self.uvs = [], [], []

    def face(self, points, uv=None):
        start = len(self.vertices)
        self.vertices.extend(points)
        self.faces.append(tuple(range(start, start+len(points))))
        self.uvs.append(uv or [(p[0], p[2]) for p in points])

    def box(self, center, dims, span=(2,2), jitter=0):
        x,y,z = center
        a,b,c = [v/2 for v in dims]
        v = [(x-a,y-b,z-c),(x+a,y-b,z-c),(x+a,y+b,z-c),(x-a,y+b,z-c),
             (x-a,y-b,z+c),(x+a,y-b,z+c),(x+a,y+b,z+c),(x-a,y+b,z+c)]
        if jitter:
            v = [tuple(t+rng.uniform(-jitter,jitter) for t in p) for p in v]
        for face in ((0,3,2,1),(4,5,6,7),(0,1,5,4),(2,3,7,6),(3,0,4,7),(1,2,6,5)):
            points = [v[i] for i in face]
            normal = (Vector(points[1])-Vector(points[0])).cross(Vector(points[2])-Vector(points[0]))
            axis = max(range(3), key=lambda i: abs(normal[i]))
            axes = (0,1) if axis == 2 else ((0,2) if axis == 1 else (1,2))
            self.face(points, [(p[axes[0]]/span[0],p[axes[1]]/span[1]) for p in points])

    def rod(self, a, b, radius, sides=8):
        a,b = Vector(a),Vector(b)
        axis = (b-a).normalized()
        u = axis.cross(Vector((0,0,1)))
        if u.length < .01:
            u = axis.cross(Vector((1,0,0)))
        u.normalize()
        v = axis.cross(u)
        rings = [[tuple(p+radius*(u*math.cos(i*math.tau/sides)+v*math.sin(i*math.tau/sides))) for i in range(sides)] for p in (a,b)]
        self.face(list(reversed(rings[0])))
        self.face(rings[1])
        for i in range(sides):
            j=(i+1)%sides
            self.face([rings[0][i],rings[0][j],rings[1][j],rings[1][i]])

    def make(self, name, mat, collection, parent=None, bevel=0):
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(self.vertices,[],self.faces)
        mesh.update()
        uv = mesh.uv_layers.new(name='UVMap')
        for face,coords in zip(mesh.polygons,self.uvs):
            for loop,co in zip(face.loop_indices,coords):
                uv.data[loop].uv = co
        bm=bmesh.new()
        bm.from_mesh(mesh)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
        bm.to_mesh(mesh)
        bm.free()
        obj = bpy.data.objects.new(name,mesh)
        collection.objects.link(obj)
        obj.parent = parent
        mesh.materials.append(materials[mat])
        if bevel:
            mod = obj.modifiers.new('Edge highlights','BEVEL')
            mod.width = bevel
            mod.segments = 2
        return obj


def batch(group, mat):
    key=(group,mat)
    if key not in batches:
        batches[key]=Mesh()
    return batches[key]


def box(group,mat,center,dims,jitter=0):
    batch(group,mat).box(center,dims,(1.92,1.60) if mat=='BuffBrick' else (2.6,2.6),jitter)


def wall(group, axis, fixed, start, end, openings):
    edges=sorted(set([start,end]+[p for h in openings for p in h[:2]]))
    for lo,hi in zip(edges[:-1],edges[1:]):
        mid=(lo+hi)/2
        gaps=sorted((h[2],h[3]) for h in openings if h[0]<mid<h[1])
        levels=[.42]
        for low,high in gaps:
            levels.extend([low,high])
        levels.append(9.45)
        for bottom,top in zip(levels[::2],levels[1::2]):
            if top-bottom < .001:
                continue
            center=(mid,fixed,(bottom+top)/2) if axis=='X' else (fixed,mid,(bottom+top)/2)
            dims=(hi-lo,.38,top-bottom) if axis=='X' else (.38,hi-lo,top-bottom)
            box(group,'BuffBrick',center,dims)


def orient(s,depth,z,side):
    if side=='front': return (s,-4.8-depth,z)
    if side=='rear': return (-s,4.8+depth,z)
    if side=='left': return (-16-depth,-s,z)
    return (16+depth,s,z)


def local_box(group,mat,s,depth,z,w,d,h,side):
    dims=(w,d,h) if side in ('front','rear') else (d,w,h)
    box(group,mat,orient(s,depth,z,side),dims)


def window(s,z,w,h,side,index,depth=-.1,group='Windows',enclosure=False):
    frame=['FrameBrown','FrameGreen','FrameGrey'][index%3]
    beam=.055 if enclosure else .065
    for xx in (s-w/2,s+w/2):
        local_box(group,frame,xx,depth,z,beam,.105,h+.08,side)
    for zz in (z-h/2,z+h/2):
        local_box(group,frame,s,depth,zz,w+.08,.105,beam,side)
    cols = 4 if w>2.5 else 2
    for j in range(1,cols):
        local_box(group,frame,s-w/2+j*w/cols,depth,z,.04,.1,h,side)
    split=z+h*.18
    local_box(group,frame,s,depth,split,w,.09,.035,side)
    for j in range(cols):
        x0=s-w/2+j*w/cols+.034
        x1=s-w/2+(j+1)*w/cols-.034
        for k,(z0,z1) in enumerate(((z-h/2+.04,split-.022),(split+.022,z+h/2-.04))):
            glass='Glass'+str((index+j*2+k)%6)
            if (index*7+j*3+k)%17==0:
                # Small triangular shard left in a genuinely missing pane.
                points=[orient(x0,depth-.025,z0,side),orient(x1,depth-.025,z0,side),orient(x0+.11,depth-.025,z0+.24,side)]
                batch(group,glass).face(points)
            else:
                local_box(group,glass,(x0+x1)/2,depth-.025,(z0+z1)/2,x1-x0,.012,z1-z0,side)
    if not enclosure:
        local_box('Sills','Concrete',s,.065,z-h/2-.08,w+.24,.58,.12,side)
        local_box('Lintels','Concrete',s,.015,z+h/2+.095,w+.2,.42,.14,side)
        # Short room pocket behind the window prevents a see-through empty shell.
        local_box('RoomPockets','Interior',s,-1.4,z,w+.4,.10,h+.5,side)
        for edge in (s-w/2-.08,s+w/2+.08):
            local_box('RoomPockets','Interior',edge,-.7,z,.1,1.5,h+.5,side)


def main_entrance():
    for j in range(3):
        box('MainEntranceSteps','Concrete',(0,-5.04-j*.31,.39-j*.13),(3.8,.98+j*.42,.13))
    for x in (-1.61,1.61):
        box('MainEntranceSurround','Concrete',(x,-4.99,1.76),(.24,.60,2.68))
    box('MainEntranceSurround','Concrete',(0,-4.99,3.16),(3.46,.60,.22))
    box('MainEntranceCanopy','Concrete',(0,-5.43,3.30),(4.05,1.85,.18))
    for x in (-1.44,1.44):
        box('MainEntranceFrame','FrameBrown',(x,-4.94,1.76),(.09,.16,2.60))
    for z in (.48,3.04):
        box('MainEntranceFrame','FrameBrown',(0,-4.94,z),(2.94,.16,.08))
    # Separate framed door leaves with lower solid panels and upper glazing.
    for x in (-.71,.71):
        for dx in (-.665,.665):
            box('MainEntranceLeaves','FrameGreen',(x+dx,-4.94,1.75),(.085,.105,2.50))
        for z in (.52,1.79,2.98):
            box('MainEntranceLeaves','FrameGreen',(x,-4.94,z),(1.38,.105,.09))
        box('MainEntrancePanels','FrameGreen',(x,-4.93,1.13),(1.27,.065,1.17))
        box('MainEntranceGlass','Glass2',(x,-4.945,2.38),(1.25,.022,1.10))
        box('MainEntranceMullions','FrameGreen',(x,-4.97,2.38),(.048,.075,1.12))
        hx=-.13 if x<0 else .13
        batch('MainEntranceHandles','FrameGrey').rod((hx,-5.075,1.28),(hx,-5.075,1.67),.022)
        for z in (1.30,1.65):
            batch('MainEntranceHandles','FrameGrey').rod((hx,-5.075,z),(hx,-4.99,z),.018)


def geometry():
    balcony_x=[-12,-4,4,12]
    plain_x=[-15,-9.25,-6.75,-1.25,1.25,6.75,9.25,15]
    holes=[]
    for floor in range(3):
        base=.45+floor*3
        for x in balcony_x:
            holes.append((x-1.30,x+1.30,base+.18,base+2.66))
            window(x,base+1.68,2.38,1.75,'front',floor+int(x)+18,depth=-.22,group='BalconyInnerWindows')
        for idx,x in enumerate(plain_x):
            if floor==0 and abs(x)==1.25:
                continue
            holes.append((x-.59,x+.59,base+.86,base+2.4))
            window(x,base+1.63,1.1,1.46,'front',idx+floor*9)
        if floor==0:
            for x in (-8,0,8):
                # Doors sit between the flanking windows without overlapping their openings.
                holes.append((-1.49,1.49,.42,3.07) if x==0 else (x-.58,x+.58,.42,2.76))
    wall('FrontMasonry','X',-4.8,-16,16,holes)
    for side in ('rear','left','right'):
        positions=[-14,-10,-6,-2,2,6,10,14] if side=='rear' else [-2.65,2.65]
        side_holes=[]
        for floor in range(3):
            base=.45+floor*3
            for idx,x in enumerate(positions):
                side_holes.append((x-.59,x+.59,base+.86,base+2.4))
                window(x,base+1.63,1.1,1.46,side,idx+floor*7+30)
        wall(side.title()+'Masonry','X' if side=='rear' else 'Y',4.8 if side=='rear' else (-16 if side=='left' else 16),-16 if side=='rear' else -4.61,16 if side=='rear' else 4.61,side_holes)
    box('Foundation','Concrete',(0,0,.21),(32.3,9.95,.42))
    for z in (.44,3.44,6.44,9.44):
        box('FloorSlabs','Concrete',(0,0,z),(31.65,9.25,.16))
    box('Roof','Roof',(0,0,9.52),(32.15,9.85,.16))
    for y in (-4.80,4.80):
        box('Parapet','BuffBrick',(0,y,9.77),(32,.30,.42))
        box('Coping','Concrete',(0,y,10.0),(32.2,.46,.10))
    for x in (-15.9,15.9):
        box('Parapet','BuffBrick',(x,0,9.77),(.3,9.3,.42))
        box('Coping','Concrete',(x,0,10.0),(.46,9.55,.10))
    for idx,x in enumerate(balcony_x):
        for floor in range(3):
            base=.45+floor*3
            group=f'Balcony_{idx+1}_Floor_{floor+1}'
            box(group,'Concrete',(x,-5.55,base),(3.45,1.9,.18))
            box(group,'Concrete',(x,-6.36,base+.55),(3.32,.16,1.0))
            box(group,'Concrete',(x,-6.37,base+1.075),(3.44,.24,.085))
            for side_x in (x-1.59,x+1.59):
                box(group,'Concrete',(side_x,-5.60,base+.56),(.16,1.4,1.02))
                box(group,'Concrete',(side_x,-6.32,base+1.93),(.13,.20,1.70))
            box(group,'Concrete',(x,-6.3,base+2.86),(3.4,.3,.20))
            if (idx,floor) not in ((0,1),(2,2),(3,0)):
                window(x,base+1.95,3.10,1.67,'front',idx*3+floor+1,depth=1.53,group=group,enclosure=True)
                # Return glazing closes both sides of the enclosed balcony.
                for sx in (x-1.58,x+1.58):
                    box(group,'FrameBrown',(sx,-5.55,base+1.12),(.065,1.45,.055))
                    box(group,'FrameBrown',(sx,-5.55,base+2.77),(.065,1.45,.055))
                    box(group,'FrameBrown',(sx,-5.55,base+1.94),(.065,.045,1.65))
                    box(group,'Glass2',(sx,-5.93,base+1.94),(.014,.66,1.58))
                    box(group,'Glass4',(sx,-5.17,base+1.94),(.014,.66,1.58))
            else:
                for offset in (-.55,.55):
                    batch(group,'FrameBrown').rod((x+offset,-6.35,base+1.08),(x+offset,-6.35,base+1.48),.018)
                batch(group,'FrameBrown').rod((x-1.55,-6.35,base+1.48),(x+1.55,-6.35,base+1.48),.021)
        box('BalconyRoof','Concrete',(x,-5.55,9.49),(3.52,1.98,.2))
    for index,x in enumerate((-8,0,8)):
        if x==0:
            main_entrance()
            continue
        for j in range(3):
            box('EntranceSteps','Concrete',(x,-5.0-j*.28,.39-j*.13),(1.8,.8+j*.38,.13),.007)
        for edge in (x-.64,x+.64):
            box('EntranceSurround','Concrete',(edge,-4.98,1.66),(.16,.55,2.4))
        box('EntranceSurround','Concrete',(x,-4.98,2.85),(1.45,.58,.19))
        box('Canopies','Concrete',(x,-5.32,2.98),(2.15,1.58,.15),.012)
        box('Doors','FrameGreen',(x,-4.66,1.5),(1.09,.085,2.08))
        for dx in (-.27,.27):
            box('DoorPanels','FrameBrown',(x+dx,-4.72,1.03),(.40,.024,.86))
            box('DoorPanels','Glass1',(x+dx,-4.72,2.00),(.40,.024,.70))
        batch('DoorHardware','FrameGrey').rod((x+.12,-4.81,1.34),(x+.12,-4.81,1.60),.018)
    for x in (-15.6,-5.9,5.9,15.6):
        batch('RainPipes','FrameGrey').rod((x,-5.045,.22),(x,-5.045,9.92),.055,10)
        for z in (1,3.1,5.9,8.5):
            box('PipeClamps','FrameBrown',(x,-5.05,z),(.15,.15,.05))
    for x in (-12,-4,4,12):
        box('RoofVents','BuffBrick',(x,1.8,10.10),(.7,.72,1.0))
        box('RoofVents','Interior',(x,1.8,10.6),(.76,.78,.18))
        box('RoofVents','Concrete',(x,1.8,10.76),(.95,.96,.13))
    # The interior is only light-blocking partitions, not playable rooms.
    for x in (-8,0,8):
        box('RoomPockets','Interior',(x,0,4.8),(.16,8.9,8.65))
    for (group,mat),mesh in batches.items():
        mesh.make(group+'__'+mat,mat,asset,root,.008 if mat in ('Concrete','FrameBrown','FrameGrey','FrameGreen') else 0)


def setup_stage():
    ground=Mesh()
    ground.box((0,0,-.16),(2000,2000,.20),(10,10))
    ground.make('PreviewGround','Ground',stage)
    data=bpy.data.lights.new('Dorm_Daylight','SUN')
    data.energy=2.2
    data.angle=.13
    sun=bpy.data.objects.new('Dorm_Daylight',data)
    stage.objects.link(sun)
    sun.rotation_euler=Vector((.4,.5,-1)).to_track_quat('-Z','Y').to_euler()
    scene.world=bpy.data.worlds.new('Dorm_OvercastWorld')
    scene.world.use_nodes=True
    bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
    bg.inputs['Color'].default_value=(.65,.72,.80,1)
    bg.inputs['Strength'].default_value=.40
    views=[('Hero',(-27,-46,16),(0,-.3,4.6),48),
           ('Front',(0,-53,8),(0,-1,4.7),48),
           ('Rear',(29,42,18),(0,0,4.8),48),
           ('Detail',(-17,-20,9),(-10,-5.1,4.8),55),
           ('Entrance',(4.7,-15,4.6),(0,-4.9,1.9),48)]
    for name,pos,target,lens in views:
        cam=bpy.data.cameras.new('Dorm_'+name)
        cam.lens=lens
        ob=bpy.data.objects.new('Dorm_'+name,cam)
        stage.objects.link(ob)
        ob.location=pos
        ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
    scene.camera=bpy.data.objects['Dorm_Hero']
    try:
        scene.render.engine='CYCLES'
    except TypeError as exc:
        raise RuntimeError(str(exc))
    scene.cycles.samples=48
    scene.cycles.use_denoising=True
    prefs=bpy.context.preferences.addons['cycles'].preferences
    if 'OPTIX' in [item[0] for item in prefs.get_device_types(bpy.context)]:
        prefs.compute_device_type='OPTIX'
        prefs.get_devices()
        available=[d for d in prefs.devices if d.type=='OPTIX']
        if available:
            for device in prefs.devices: device.use=device.type=='OPTIX'
            enum_set(scene.cycles,'device','GPU')
    scene.render.resolution_x=1800
    scene.render.resolution_y=1120
    scene.render.resolution_percentage=100
    enum_set(scene.render.image_settings,'file_format','PNG')
    scene.render.filepath=str(SOURCE/'Previews/Dormitory_Hero.png')


def finish():
    bpy.context.view_layer.update()
    meshes=[o for o in asset.objects if o.type=='MESH']
    for obj in meshes:
        obj.select_set(True)
    root.select_set(True)
    bpy.context.view_layer.objects.active=root
    # Enumerate exporter enums before assigning their values.
    gltf=bpy.ops.export_scene.gltf.get_rna_type().properties
    fbx=bpy.ops.export_scene.fbx.get_rna_type().properties
    print('GLTF formats',[i.identifier for i in gltf['export_format'].enum_items])
    print('FBX axes',[i.identifier for i in fbx['axis_forward'].enum_items])
    bpy.ops.export_scene.gltf(filepath=str(SOURCE/'Exports/WorkersDormitory.glb'),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True)
    bpy.ops.export_scene.fbx(filepath=str(SOURCE/'Exports/WorkersDormitory.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',global_scale=1.0,apply_unit_scale=True,use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False)
    points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
    report={'building_dimensions_m':[round(max(p[i] for p in points)-min(p[i] for p in points),4) for i in range(3)],'mesh_objects':len(meshes),'materials':len(set(m.name for o in meshes for m in o.data.materials)),'source_triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),'storeys':3,'entrances':3,'balcony_stacks':4,'balconies':12,'interior':'Light blocking pockets only; exterior asset','texture_source':'Existing project Industrial_Harbour textures, recolored brick derivative','unity_status':'Not placed or runtime tested'}
    (SOURCE/'asset_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    bpy.ops.file.pack_all()
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                space=area.spaces.active
                enum_set(space.shading,'type','MATERIAL')
                space.shading.use_scene_world=False
                space.shading.use_scene_lights=False
                space.region_3d.view_location=Vector((0,0,4.7))
                space.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
                space.region_3d.view_distance=46
                enum_set(space.region_3d,'view_perspective','PERSP')
                space.overlay.show_overlays=False
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'WorkersDormitory.blend'))
    print(json.dumps(report,indent=2))


scene=bpy.data.scenes.new('WorkersDormitory')
bpy.context.window.scene=scene
enum_set(scene.unit_settings,'system','METRIC')
scene.unit_settings.scale_length=1
asset=bpy.data.collections.new('WorkersDormitory_Asset')
stage=bpy.data.collections.new('WorkersDormitory_PreviewOnly')
scene.collection.children.link(asset)
scene.collection.children.link(stage)
root=bpy.data.objects.new('WorkersDormitory',None)
asset.objects.link(root)
prepare_textures()
material('BuffBrick',(.45,.42,.35),texture='BuffBrick')
material('Concrete',(.42,.41,.36),texture='Concrete')
material('Roof',(.20,.205,.19),.95,texture='Concrete')
material('FrameBrown',(.085,.069,.048),.78,.25)
material('FrameGreen',(.105,.145,.126),.8,.12)
material('FrameGrey',(.19,.20,.19),.75,.25)
material('Interior',(.043,.037,.031),.96)
material('Ground',(.21,.195,.165),.95)
for i,color in enumerate(((.075,.09,.093),(.11,.12,.115),(.075,.078,.07),(.145,.15,.14),(.042,.05,.048),(.18,.19,.18))):
    material('Glass'+str(i),color,.26+(i%3)*.07,.18)
geometry()
setup_stage()
finish()
