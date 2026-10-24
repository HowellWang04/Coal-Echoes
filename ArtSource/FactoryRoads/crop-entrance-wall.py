"""Shorten Wall_031 at the widened gate while retaining brick UVs and colors."""
from pathlib import Path
import bpy
import bmesh
from mathutils import Matrix

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes')
previous=bpy.context.scene
scene=bpy.data.scenes.new('EntranceWall_Crop')
bpy.context.window.scene=scene
bpy.ops.import_scene.gltf(filepath=str(ROOT/'Assets/Art Resources/Environment/Gate Area/FactoryBoundary/Models/FactoryWall_9m_06.glb'))
sources=[o for o in scene.objects if o.type=='MESH']
result=[]
for original in sources:
    mesh=original.data.copy()
    mesh.transform(original.matrix_world)
    mesh.transform(Matrix.Diagonal((.954000056,1,.998895645,1)))
    bm=bmesh.new();bm.from_mesh(mesh)
    # Unity's local X points from the original gate pier toward Pillar_031.
    for point,normal in [((-8.28,0,0),(-1,0,0)),((-6.48,0,0),(1,0,0))]:
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=point,plane_no=normal,clear_outer=True,clear_inner=False)
    bm.to_mesh(mesh);bm.free();mesh.update()
    if not mesh.polygons:
        bpy.data.meshes.remove(mesh);continue
    obj=bpy.data.objects.new('EntranceWall_'+original.name.rsplit('_',1)[-1],mesh)
    scene.collection.objects.link(obj);result.append(obj)
bpy.ops.object.select_all(action='DESELECT')
for obj in result:obj.select_set(True)
path=ROOT/'Assets/Art Resources/Environment/Gate Area/FactoryBoundary/Models/FactoryWall_GateShort.glb'
color=next(o.data.color_attributes.active_color.name for o in result if o.data.color_attributes.active_color)
bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True,export_yup=True,export_animations=False,export_image_format='NONE',export_vertex_color='NAME',export_vertex_color_name=color,export_all_vertex_colors=False)
print('Cropped entrance wall',len(result),'meshes, length 1.8 metres')
for obj in list(scene.objects):bpy.data.objects.remove(obj,do_unlink=True)
bpy.context.window.scene=previous
bpy.data.scenes.remove(scene)
