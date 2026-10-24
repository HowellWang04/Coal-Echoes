"""Inspect the two reported surface defects in the live Blender source."""
import bpy
import json

objects = [o for o in bpy.data.collections['Guardhouse | game meshes'].objects if o.type == 'MESH']
walls = [o for o in objects if o.name.startswith(('FrontWall_', 'RearWall_', 'LeftWall_', 'RightWall_'))]
collapsed = []
for obj in walls:
    uv = obj.data.uv_layers.active.data
    for poly in obj.data.polygons:
        xy = [uv[i].uv for i in poly.loop_indices]
        area = abs(sum(a.x * b.y - b.x * a.y for a, b in zip(xy, xy[1:] + xy[:1]))) / 2
        if area < 1e-10:
            collapsed.append([obj.name, poly.index])
coincident = []
for obj in objects:
    if 'Plinth' not in obj.name:
        continue
    axis = 0 if obj.name.startswith('SideCement') else 1
    interior = 2.55 if axis == 0 else 2.1
    # A plinth face lying on the room-facing wall plane is a z-fight candidate.
    if any(abs(abs(v.co[axis]) - interior) < 1e-5 for v in obj.data.vertices):
        coincident.append(obj.name)
report = {'collapsed_wall_uv_faces': len(collapsed), 'coplanar_interior_plinths': coincident,
          'wall_materials': sorted({m.name for o in walls for m in o.data.materials})}
print(json.dumps(report, indent=2))
assert not collapsed, 'Wall end faces have collapsed UVs'
assert not coincident, 'Plinth faces share the interior wall plane'
