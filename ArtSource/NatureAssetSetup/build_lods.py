"""Create lower detail rock meshes in source coordinates, without changing LOD0."""
import bpy
import json
from pathlib import Path

work = Path(__file__).resolve().parent
sources = json.loads((work / 'Before' / 'gobi-mesh-input.json').read_text(encoding='utf-8-sig'))
output = work / 'Before' / 'Generated'
output.mkdir(exist_ok=True)
report = []
for source in sources:
    if not source['generate']:
        continue
    mesh = bpy.data.meshes.new(source['name'])
    indices = source['triangles']
    mesh.from_pydata(source['vertices'], [], [indices[i:i+3] for i in range(0, len(indices), 3)])
    mesh.update()
    uv_layer = mesh.uv_layers.new(name='UVMap')
    for loop in mesh.loops:
        uv_layer.data[loop.index].uv = source['uv'][loop.vertex_index]
    for face in mesh.polygons:
        face.use_smooth = True
    mesh.normals_split_custom_set_from_vertices(source['normals'])
    for level, ratio in enumerate((0.5, 0.2, 0.06), start=1):
        obj = bpy.data.objects.new(source['name'], mesh.copy())
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        decimate = obj.modifiers.new('Reduce', 'DECIMATE')
        decimate.ratio = ratio
        decimate.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=decimate.name)
        reduced = obj.data
        reduced.calc_loop_triangles()
        vertices, normals, uv, triangles = [], [], [], []
        lookup = {}
        for tri in reduced.loop_triangles:
            for loop_index in tri.loops:
                loop = reduced.loops[loop_index]
                pos = tuple(reduced.vertices[loop.vertex_index].co)
                normal = tuple(reduced.corner_normals[loop_index].vector)
                texcoord = tuple(reduced.uv_layers.active.data[loop_index].uv)
                key = pos + normal + texcoord
                if key not in lookup:
                    lookup[key] = len(vertices)
                    vertices.append(pos)
                    normals.append(normal)
                    uv.append(texcoord)
                triangles.append(lookup[key])
        name = f"{source['name']}_LOD{level}"
        (output / f'{name}.json').write_text(json.dumps(dict(name=name, vertices=vertices, normals=normals, uv=uv, triangles=triangles)), encoding='utf-8')
        report.append(dict(name=name, triangles=len(triangles)//3, vertices=len(vertices), ratio=ratio))
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.meshes.remove(reduced)
    bpy.data.meshes.remove(mesh)
    print('Finished', source['name'], flush=True)
(work / 'lod-generation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('Generated', len(report), 'LOD meshes', flush=True)
