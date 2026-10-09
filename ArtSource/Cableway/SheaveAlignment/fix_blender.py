import bpy
import hashlib
import json
import math
import shutil
from pathlib import Path
from mathutils import Matrix, Vector

work = Path(__file__).resolve().parent
root = work.parent / 'Tajia02_Metric15'
unity = root.parents[2] / 'Assets/Art Resources/Environment/Cableway/CablewayTower_Metric15'
reports = []
for filename, name in [('Tajia02_Tower_Metric15.blend', 'CablewayTower_Metric15'), ('Tajia02_EntryTower.blend', 'CablewayEntryTower')]:
    source_file = root / 'Blender' / filename
    for src in [source_file, root / 'Exports' / (name + '.fbx'), unity / (name + '.fbx')]:
        dst = work / 'Before' / (('Unity_' if src.parent == unity else '') + src.name)
        if dst.exists():
            if hashlib.sha256(src.read_bytes()).digest() != hashlib.sha256(dst.read_bytes()).digest():
                raise RuntimeError('Source already differs from backup ' + str(dst))
        else:
            shutil.copy2(src, dst)
    bpy.ops.wm.open_mainfile(filepath=str(source_file))
    scene = bpy.context.scene
    height = scene['total_height_m']
    cross_top = height - .51
    parts = bpy.data.collections['Tower_Fabricated_Parts']
    parts.hide_viewport = False
    bpy.context.view_layer.update()
    export = bpy.data.objects[name]
    assert export.matrix_world == Matrix.Identity(4)
    prefixes = ('Sheave bracket plate', 'Rocker beam side', 'Rocker pivot pin', 'Rope sheave', 'Sheave rubber lining', 'Sheave hub', 'Sheave axle block')
    chosen = []
    for ob in parts.objects:
        center = sum((ob.matrix_world @ v.co for v in ob.data.vertices), Vector()) / len(ob.data.vertices)
        bolt = ob.name.startswith(('Bolt washer', 'Hex bolt')) and abs(abs(center.x) - 2) < .02 and abs(center.z - (cross_top + .06)) < .02 and abs(center.y) < .14
        if ob.name.startswith(prefixes) or bolt:
            chosen.append((ob, -2 if center.x < 0 else 2))
    assert len(chosen) == 38, [(o.name, x) for o, x in chosen]
    # Match the evaluated source parts to their vertices in the baked, UV mapped export.
    # Connectivity validation below prevents accidentally rotating adjacent tower steel.
    vertex_side = {}
    deps = bpy.context.evaluated_depsgraph_get()
    def key(v):
        return tuple(round(c, 5) for c in v)
    for ob, cx in chosen:
        evaluated = ob.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        for v in mesh.vertices:
            vertex_side[key(ob.matrix_world @ v.co)] = cx
        evaluated.to_mesh_clear()
    mesh = export.data
    original = [v.co.copy() for v in mesh.vertices]
    selected = {v.index: vertex_side[key(v.co)] for v in mesh.vertices if key(v.co) in vertex_side}
    adjacency = [[] for _ in mesh.vertices]
    for e in mesh.edges:
        a, b = e.vertices
        adjacency[a].append(b)
        adjacency[b].append(a)
    visited = set()
    components = 0
    for i in range(len(mesh.vertices)):
        if i in visited:
            continue
        stack, component = [i], []
        visited.add(i)
        while stack:
            j = stack.pop()
            component.append(j)
            for n in adjacency[j]:
                if n not in visited:
                    visited.add(n)
                    stack.append(n)
        matched = [j for j in component if j in selected]
        assert not matched or len(matched) == len(component), ('Partial component match', i, len(matched), len(component))
        if matched:
            components += 1
    assert components == 38, components
    def uv_hash():
        return hashlib.sha256(b''.join(bytes(memoryview(__import__('array').array('f', [x for loop in layer.data for x in loop.uv]))) for layer in mesh.uv_layers)).hexdigest()
    uv_before = uv_hash()
    normals = [n.vector.copy() for n in mesh.corner_normals]
    turn = Matrix.Rotation(math.pi / 2, 4, 'Z')
    transforms = {cx: Matrix.Translation(Vector((cx, 0, 0))) @ turn @ Matrix.Translation(Vector((-cx, 0, 0))) for cx in [-2, 2]}
    for i, cx in selected.items():
        mesh.vertices[i].co = transforms[cx] @ original[i]
    for ob, cx in chosen:
        ob.matrix_world = transforms[cx] @ ob.matrix_world
    mesh.update()
    mesh.normals_split_custom_set([turn.to_3x3() @ normals[i] if loop.vertex_index in selected else normals[i] for i, loop in enumerate(mesh.loops)])
    outside_delta = max(((v.co - original[v.index]).length for v in mesh.vertices if v.index not in selected), default=0)
    assert outside_delta == 0 and uv_hash() == uv_before
    bpy.ops.object.select_all(action='DESELECT')
    export.select_set(True)
    bpy.context.view_layer.objects.active = export
    output = root / 'Exports' / (name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=str(output), use_selection=True, object_types={'MESH'}, use_mesh_modifiers=True,
        use_triangles=True, mesh_smooth_type='OFF', use_tspace=True, axis_forward='-Z', axis_up='Y',
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, path_mode='STRIP', bake_anim=False)
    parts.hide_viewport = True
    bpy.data.libraries.write(str(source_file), {scene}, fake_user=True, compress=True)
    shutil.copy2(output, unity / output.name)
    reports.append({'asset': name, 'rotated_source_parts': len(chosen), 'rotated_connected_components': components, 'changed_vertices': len(selected), 'total_vertices': len(mesh.vertices), 'unchanged_tower_maximum_displacement': outside_delta, 'uv_hash_unchanged': uv_hash() == uv_before, 'rotation_degrees': 90, 'subassembly_pivots': [[-2, 0, height], [2, 0, height]]})
    (work / 'fix_report.json').write_text(json.dumps(reports, indent=2), encoding='utf8')
print(json.dumps(reports))
