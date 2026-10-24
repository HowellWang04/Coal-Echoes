"""Trim the existing wall module into two short returns without squeezing its bricks."""
import bpy
import bmesh
from pathlib import Path
from mathutils import Matrix

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes')
previous=bpy.context.scene
scene=bpy.data.scenes.new('Guardhouse_WallReturns_Export')
bpy.context.window.scene=scene
bpy.ops.import_scene.gltf(filepath=str(ROOT/'Assets/Art Resources/Environment/Gate Area/FactoryBoundary/Models/FactoryWall_9m_06.glb'))
sources=[o for o in scene.objects if o.type=='MESH']
result=[]
# Coordinates are relative to Pillar_030. The old scale is baked before cropping.
for suffix,low,high in [('Pillar',-1.2125,-.325),('Gate',-7.66,-6.7725)]:
    for original in sources:
        mesh=original.data.copy()
        mesh.transform(original.matrix_world)
        mesh.transform(Matrix.Diagonal((.8869445,1,.998895645,1)))
        bm=bmesh.new();bm.from_mesh(mesh)
        for point,normal in [((low,0,0),(-1,0,0)),((high,0,0),(1,0,0)),((0,0,3.06),(0,0,1))]:
            bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=point,plane_no=normal,clear_outer=True,clear_inner=False)
        bm.to_mesh(mesh);bm.free();mesh.update()
        if not mesh.polygons:
            bpy.data.meshes.remove(mesh)
            continue
        material_name=original.name.rsplit('_',1)[-1]
        obj=bpy.data.objects.new('GuardhouseReturn_'+suffix+'_'+material_name,mesh)
        scene.collection.objects.link(obj);result.append(obj)
    bpy.ops.mesh.primitive_cube_add(size=1,location=((low+high)/2,0,3.12))
    cap=bpy.context.object;cap.name='GuardhouseReturn_'+suffix+'_Cap'
    cap.dimensions=(high-low,.62,.12)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    cap.data.materials.append(bpy.data.materials['Concrete'])
    mod=cap.modifiers.new('Cap edge','BEVEL');mod.width=.01;mod.segments=2
    result.append(cap)
bpy.ops.object.select_all(action='DESELECT')
for obj in result:obj.select_set(True)
path=ROOT/'Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/Guardhouse_WallReturns.glb'
color_name=next(o.data.color_attributes.active_color.name for o in result if o.type=='MESH' and o.data.color_attributes.active_color)
bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,use_active_scene=True,export_apply=True,export_yup=True,export_animations=False,export_image_format='NONE',export_vertex_color='NAME',export_vertex_color_name=color_name,export_all_vertex_colors=False)
print('Cropped returns',len(result),'meshes',path)
for obj in list(scene.objects):bpy.data.objects.remove(obj,do_unlink=True)
bpy.context.window.scene=previous
bpy.data.scenes.remove(scene)
