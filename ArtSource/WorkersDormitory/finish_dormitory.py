"""Add localized runoff, prepare clean exports, and save the final source file."""
from pathlib import Path
import math
import json
import random
import bpy
import numpy as np
from mathutils import Vector

SOURCE=Path(__file__).resolve().parent
scene=bpy.data.scenes['WorkersDormitory']
bpy.context.window.scene=scene
asset=bpy.data.collections['WorkersDormitory_Asset']
root=bpy.data.objects['WorkersDormitory']
previous=asset.objects.get('SillRunoff')
if previous is not None:bpy.data.objects.remove(previous,do_unlink=True)

# Alpha is strongest immediately below the sill and fades into the brick.
size=256
x=np.linspace(0,1,size)[None,:]
y=np.linspace(0,1,size)[:,None]
randoms=np.random.default_rng(43)
streaks=np.zeros((size,size),dtype=np.float32)
for _ in range(36):
    center=randoms.uniform(.10,.90)
    width=randoms.uniform(.004,.035)
    length=randoms.uniform(.2,.9)
    streaks+=np.exp(-((x-center)/width)**2)*np.clip((y-(1-length))/length,0,1)*randoms.uniform(.08,.24)
alpha=np.clip(streaks,0,.34)*np.minimum(y*7,1)*np.minimum((1-y)*35,1)
rgba=np.ones((size,size,4),dtype=np.float32)
rgba[:,:,:3]=(.19,.16,.11)
rgba[:,:,3]=alpha
im=bpy.data.images.new('Dorm_Runoff_BaseColor',width=size,height=size,alpha=True)
im.colorspace_settings.name='sRGB'
im.pixels.foreach_set(rgba.ravel())
im.filepath_raw=str(SOURCE/'Textures/Dorm_Runoff_BaseColor.png')
im.file_format='PNG'
im.save()
im.pack()
mat=bpy.data.materials.new('Dorm_SillRunoff')
mat.use_nodes=True
bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bsdf.inputs['Roughness'].default_value=.98
node=mat.node_tree.nodes.new('ShaderNodeTexImage')
node.image=im
mat.node_tree.links.new(node.outputs['Color'],bsdf.inputs['Base Color'])
mat.node_tree.links.new(node.outputs['Alpha'],bsdf.inputs['Alpha'])
vertices=[]
faces=[]
def plane(cx,y,z,w,h):
    start=len(vertices)
    vertices.extend([(cx-w/2,y,z-h),(cx+w/2,y,z-h),(cx+w/2,y,z),(cx-w/2,y,z)])
    faces.append((start,start+1,start+2,start+3))
rng=random.Random(28)
for floor in range(3):
    base=.45+floor*3
    for x in (-15,-9.25,-6.75,-1.25,1.25,6.75,9.25,15):
        height=rng.uniform(.45,.7)
        if floor==0 and abs(x)==1.25:continue
        plane(x,-4.994,base+.78,1.55,height)
    for x in (-12,-4,4,12):
        plane(x,-6.454,base+1.02,3.15,.80)
mesh=bpy.data.meshes.new('SillRunoff')
mesh.from_pydata(vertices,[],faces)
mesh.update()
uv=mesh.uv_layers.new(name='UVMap')
for p in mesh.polygons:
    for loop,co in zip(p.loop_indices,((0,0),(1,0),(1,1),(0,1))):uv.data[loop].uv=co
ob=bpy.data.objects.new('SillRunoff',mesh)
asset.objects.link(ob)
ob.parent=root
mesh.materials.append(mat)
scene.view_settings.exposure=.65
scene.render.resolution_x=1800
scene.render.resolution_y=1120
scene.render.filepath=str(SOURCE/'Previews/Dormitory_Hero.png')
scene.camera=bpy.data.objects['Dorm_Hero']

# Export a temporary scene with evaluated meshes combined by material.
# This preserves the editable component structure in the .blend source.
export_scene=bpy.data.scenes.new('Dormitory_ExportTemporary')
export_scene.unit_settings.system='METRIC'
export_root=bpy.data.objects.new('WorkersDormitory_Export',None)
export_scene.collection.objects.link(export_root)
dg=bpy.context.evaluated_depsgraph_get()
groups={}
for obj in asset.objects:
    if obj.type!='MESH':continue
    mesh=bpy.data.meshes.new_from_object(obj.evaluated_get(dg),depsgraph=dg)
    copy=bpy.data.objects.new(obj.name+'_Export',mesh)
    export_scene.collection.objects.link(copy)
    copy.matrix_world=obj.matrix_world.copy()
    groups.setdefault(mesh.materials[0].name,[]).append(copy)
bpy.context.window.scene=export_scene
for name,objects in groups.items():
    for ob in export_scene.objects:ob.select_set(False)
    for ob in objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1:bpy.ops.object.join()
    objects[0].name=name
    objects[0].parent=export_root
for ob in export_scene.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=export_root
bpy.ops.export_scene.gltf(filepath=str(SOURCE/'Exports/WorkersDormitory.glb'),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True)
bpy.ops.export_scene.fbx(filepath=str(SOURCE/'Exports/WorkersDormitory.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False)
meshes=[ob for ob in export_scene.objects if ob.type=='MESH']
report=json.loads((SOURCE/'asset_report.json').read_text(encoding='utf-8'))
report['export_mesh_objects']=len(meshes)
report['export_triangles']=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
report['export_materials']=len(groups)
report['normal_format']='Reconstructed tangent XYZ from source RG channels'
report['source_mesh_objects']=sum(o.type=='MESH' for o in asset.objects)
report['main_entrance']={'opening_width_m':2.98,'opening_height_m':2.65,'type':'Double leaf glazed steel entrance with concrete surround, canopy and steps'}
report['mesh_objects']=report['source_mesh_objects']
report['materials']=len(groups)
report['source_triangles']=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in asset.objects if o.type=='MESH')
points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
report['building_dimensions_m']=[round(max(p[i] for p in points)-min(p[i] for p in points),5) for i in range(3)]
(SOURCE/'asset_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
temporary_objects=list(export_scene.objects)
bpy.context.window.scene=scene
bpy.data.scenes.remove(export_scene)
for ob in temporary_objects:bpy.data.objects.remove(ob,do_unlink=True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.region_3d.view_perspective='CAMERA'
            space.region_3d.view_camera_zoom=10
            space.shading.type='MATERIAL'
            space.overlay.show_overlays=False
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'WorkersDormitory.blend'))
print(json.dumps(report,indent=2))
