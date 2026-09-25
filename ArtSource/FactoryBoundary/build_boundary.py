"""Build the three basic factory boundary assets; no backup copies are created."""
from pathlib import Path
import sys
import json
import random
import os
import bpy
from mathutils import Vector, Matrix

SOURCE=Path(__file__).resolve().parent
GATE_SOURCE=SOURCE.parent/'FactoryGate'
ROOT=SOURCE.parent.parent
ASSET=ROOT/'Assets/Art Resources/Environment/Gate Area/FactoryBoundary'
MODELS=ASSET/'Models'
PREVIEWS=SOURCE/'Previews'
sys.path.insert(0,str(GATE_SOURCE))
import build_gate as gate
import texture_maps

MODELS.mkdir(parents=True,exist_ok=True)
PREVIEWS.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(GATE_SOURCE/'FactoryGate_Rebuilt.blend'))
asset=bpy.data.collections['Factory Gate Asset']
root=bpy.data.objects['FactoryGate_Rebuilt']
gate.asset_collection=asset
gate.materials={name:bpy.data.materials[name] for name in ('GatePaint','FramePaint','Iron','Brick','BrickWeathered','Concrete','Mortar','Whitewash')}

# Blender +X exports to Unity -X, the side away from the existing guard post.
wing=bpy.data.objects.get('Right_WingWall')
if wing:
    for obj in list(wing.children_recursive)+[wing]:
        bpy.data.objects.remove(obj,do_unlink=True)
gate.export_asset(root)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.file.pack_all()
temp=GATE_SOURCE/'FactoryGate_Modular_Save.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(temp),relative_remap=False)
os.replace(temp,GATE_SOURCE/'FactoryGate_Rebuilt.blend')

pillar_collection=bpy.data.collections.new('Factory Pillar Asset')
bpy.context.scene.collection.children.link(pillar_collection)
gate.asset_collection=pillar_collection
pillar=gate.empty('FactoryPillar')
pillar['grid_spacing']=3.0
original=bpy.data.objects['Right_Pier']
for source in original.children:
    if source.type!='MESH' or source.data.materials[0].name=='Iron':
        continue
    obj=source.copy()
    obj.data=source.data.copy()
    obj.name=source.name.replace('Right_Pier','FactoryPillar')
    pillar_collection.objects.link(obj)
    # Preserve the brick tint and UVs while recentering the entire pillar at ground level.
    obj.data.transform(Matrix.Translation((-3.17,0,0)) @ source.matrix_world)
    obj.parent=pillar
    obj.matrix_basis=Matrix.Identity(4)

wall_collection=bpy.data.collections.new('Factory Wall Asset')
bpy.context.scene.collection.children.link(wall_collection)
gate.asset_collection=wall_collection
gate.groups={}
gate.rng=random.Random(1973)
wall=gate.empty('FactoryWall_3m')
wall['grid_spacing']=3.0
wall['socket_start_blender']=[0,0,0]
wall['socket_end_blender']=[-3,0,0]
# Three metre pillar-centre spacing, with a small concealed overlap inside each pillar.
x0,x1=-2.625,-.375
gate.builder(wall,'Concrete').box(((x0+x1)/2,0,.14),(x1-x0,.62,.28),1/4.5,.008,offset=(.5,0))
gate.brickwork(wall,x0,x1,-.25,.25,.28,3.46,False)
gate.wall_whitewash(wall,x0+.003,x1-.003,True)
gate.wall_whitewash(wall,x0+.003,x1-.003,False)
for (name,mat),(mesh,parent) in list(gate.groups.items()):
    obj=mesh.object(f'{name}_{mat}',mat,parent,{'Brick':0,'BrickWeathered':0,'Concrete':.012,'Mortar':.001}[mat],1)
    if mat=='Concrete':
        gate.chip_concrete(obj,parent)
for obj in list(wall_collection.objects):
    if obj.type!='MESH':
        continue
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    for modifier in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=modifier.name)
for mat in gate.materials.values():
    objects=[o for o in wall_collection.objects if o.type=='MESH' and o.data.materials[0]==mat]
    if not objects:
        continue
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1:
        bpy.ops.object.join()
    bpy.context.object.name=f'FactoryWall_3m_{mat.name}'
for obj in wall_collection.objects:
    if obj.type!='MESH':
        continue
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    modifier=obj.modifiers.new('Stable export triangulation','TRIANGULATE')
    modifier.quad_method='FIXED'
    bpy.ops.object.modifier_apply(modifier=modifier.name)
texture_maps.tint_individual_bricks(wall_collection)

def inventory(collection):
    meshes=[o for o in collection.objects if o.type=='MESH']
    points=[o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    for obj in meshes:
        obj.data.calc_loop_triangles()
    return {'triangles':sum(len(o.data.loop_triangles) for o in meshes),
            'mesh_count':len(meshes),
            'bounds_min_blender':[min(p[i] for p in points) for i in range(3)],
            'bounds_max_blender':[max(p[i] for p in points) for i in range(3)],
            'missing_uv':[o.name for o in meshes if not o.data.uv_layers]}

bpy.context.view_layer.update()
report={'gate':inventory(asset),'pillar':inventory(pillar_collection),'wall':inventory(wall_collection),
        'pillar_centre_spacing':3.0,'wall_body_thickness':.50,
        'removed_gate_group':'Right_WingWall','retained_gate_group':'Left_WingWall',
        'gate_free_pillar_centre_unity':[-3.17,0,0],
        'wall_start_socket_unity':[0,0,0],'wall_end_socket_unity':[3,0,0],
        'material_randomization_implemented':False}

for collection,obj,name in ((pillar_collection,pillar,'FactoryPillar'),(wall_collection,wall,'FactoryWall_3m')):
    bpy.ops.object.select_all(action='DESELECT')
    for child in collection.objects:
        child.select_set(True)
    bpy.context.view_layer.objects.active=obj
    bpy.ops.export_scene.gltf(filepath=str(MODELS/(name+'.glb')),export_format='GLB',
        use_selection=True,export_yup=True,export_animations=False,export_extras=True,export_tangents=True,
        export_vertex_color='NAME',export_vertex_color_name='BrickTint',export_all_vertex_colors=False)
(SOURCE/'boundary_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')

# Arrange all three source modules for an actual Blender overview render.
pillar.location=(5,0,0)
wall.location=(9,0,0)
gate.stage_collection=bpy.data.collections['Preview Only']
scene=bpy.context.scene
scene.camera=gate.camera('Boundary Modules',(2,-27,10),(2.4,0,2),15.6)
scene.render.resolution_x,scene.render.resolution_y=2100,1100
scene.render.filepath=str(PREVIEWS/'Boundary_Modules.png')
bpy.ops.file.pack_all()
# Packed textures make the source portable; relink their unpacking paths to the new source directory.
for image in bpy.data.images:
    if image.source=='FILE':
        filename=Path(image.filepath).name
        image.filepath='//'+Path(os.path.relpath(gate.TEXTURES/filename,SOURCE)).as_posix()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'FactoryBoundary.blend'),relative_remap=False)
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX'
prefs.get_devices()
for device in prefs.devices:
    device.use=device.type=='OPTIX'
scene.cycles.device='GPU'
bpy.ops.render.render(write_still=True)

# A straight run and return corner, sharing exactly the same geometry and materials.
def instance_tree(source,new_position,angle=0):
    parent=source.copy()
    source.users_collection[0].objects.link(parent)
    parent.location=new_position
    parent.rotation_euler.z=angle
    for child in source.children:
        obj=child.copy()
        child.users_collection[0].objects.link(obj)
        obj.parent=parent
        obj.matrix_basis=child.matrix_basis.copy()
    return parent

pillar.location=(6.17,0,0)
wall.location=(6.17,0,0)
instance_tree(pillar,(9.17,0,0))
instance_tree(wall,(9.17,0,0))
instance_tree(pillar,(9.17,3,0))
instance_tree(wall,(9.17,3,0),1.57079632679)
scene.camera=gate.camera('Boundary Joined',(15,-25,13),(2.6,0,2),17)
scene.render.filepath=str(PREVIEWS/'Boundary_Joined.png')
bpy.ops.render.render(write_still=True)
print('BOUNDARY_BUILD_COMPLETE',json.dumps(report),flush=True)
