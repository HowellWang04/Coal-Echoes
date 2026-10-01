"""Finish the approved compact scene through the live Blender MCP session.

Run once after loading the approved gray model. The editable final blend is
the authoritative source. No silhouette or enclosure changes are made here.
"""
import bpy, bmesh, math
from mathutils import Vector

ns=bpy.app.driver_namespace['station_build']
scene=ns['SCENE']
scene.view_layers[0].material_override=None
objects=ns['objects']

# Project the concrete at a consistent physical scale, without face stretching.
base=next(o for o in objects if o.name=='ConcretePlinth')
bpy.context.view_layer.objects.active=base
modifier=base.modifiers.new('WeatheredEdgeBevel','BEVEL')
ns['enum'](modifier,'limit_method','ANGLE')
modifier.angle_limit=math.radians(35)
modifier.width=.026
modifier.segments=2
bpy.ops.object.modifier_apply(modifier=modifier.name)
mesh=base.data
uv=mesh.uv_layers.active.data
for face in mesh.polygons:
    drop=max(range(3),key=lambda a:abs(face.normal[a]))
    axes=[a for a in range(3) if a!=drop]
    for index in face.loop_indices:
        p=mesh.vertices[mesh.loops[index].vertex_index].co
        uv[index].uv=(p[axes[0]]/.8,p[axes[1]]/.8)

# Orient the worn steel grain along each individual fabricated member.
for ob in objects:
    if ob.name not in ['StructuralSteel','PlatesAndFasteners','WalkwayGrating']:
        continue
    mesh=ob.data
    adjacency=[[] for _ in mesh.vertices]
    for edge in mesh.edges:
        a,b=edge.vertices
        adjacency[a].append(b);adjacency[b].append(a)
    groups={};seen=set()
    for start in range(len(mesh.vertices)):
        if start in seen:continue
        stack=[start];seen.add(start);indices=[]
        while stack:
            i=stack.pop();indices.append(i)
            for j in adjacency[i]:
                if j not in seen:seen.add(j);stack.append(j)
        minimum=[min(mesh.vertices[i].co[a] for i in indices) for a in range(3)]
        extent=[max(mesh.vertices[i].co[a] for i in indices)-minimum[a] for a in range(3)]
        long_axis=max(range(3),key=lambda a:extent[a])
        for i in indices:groups[i]=(long_axis,minimum)
    uv=mesh.uv_layers.active.data
    for face in mesh.polygons:
        long_axis,minimum=groups[face.vertices[0]]
        other=[a for a in range(3) if a!=long_axis]
        cross_axis=min(other,key=lambda a:abs(face.normal[a]))
        for index in face.loop_indices:
            p=mesh.vertices[mesh.loops[index].vertex_index].co
            uv[index].uv=((p[cross_axis]-minimum[cross_axis])/.8+.19,(p[long_axis]-minimum[long_axis])/5+.12)

for ob in objects:
    bm=bmesh.new();bm.from_mesh(ob.data)
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(ob.data);bm.free();ob.data.update()

scene['design_status']='Textured compact station shell with fully open opposite side and ends'
scene['enclosure']='One clad long side; single slope roof; no open-side railing or infill'
scene['asset_name']='CablewayStationShell'
scene['model_units']='metres'
scene.cycles.samples=48
scene.render.resolution_x=1500
scene.render.resolution_y=1100
camera=ns['camera']
for label,position in [('CladSide',(13,-18,7)),('OpenSide',(-13,-18,7))]:
    camera.location=position
    camera.rotation_euler=(Vector((0,0,2))-camera.location).to_track_quat('-Z','Y').to_euler()
    scene.render.filepath=str(ns['ROOT']/('Previews/Station_'+label+'.png'))
    bpy.ops.render.render(write_still=True)
print('Finished surfaces and rendered both sides')
