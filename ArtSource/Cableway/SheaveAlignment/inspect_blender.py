import bpy
import json
import numpy as np
import sys
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parent.parent / 'Tajia02_Metric15'
reports = []
for filename in ['Tajia02_Tower_Metric15.blend', 'Tajia02_EntryTower.blend']:
    bpy.ops.wm.open_mainfile(filepath=str(root / 'Blender' / filename))
    bpy.data.collections['Tower_Fabricated_Parts'].hide_viewport = False
    bpy.context.view_layer.update()
    sheaves = [o for o in bpy.context.scene.objects if o.name.startswith('Rope sheave')]
    rows = []
    for o in sheaves:
        points = np.array([list(o.matrix_world @ v.co) for v in o.data.vertices])
        eigenvalues, eigenvectors = np.linalg.eigh(np.cov(points.T))
        axis = Vector(eigenvectors[:, np.argmin(eigenvalues)])
        rows.append({'name': o.name, 'position': points.mean(axis=0).tolist(), 'axis': list(axis), 'axle_dot_rope': abs(axis.dot(Vector((0, 1, 0)))), 'unity_vertices': [[-float(p[0]), float(p[2]), -float(p[1])] for p in points]})
    reports.append({'file': filename, 'scene': bpy.context.scene.name, 'total_height': bpy.context.scene.get('total_height_m'), 'sheaves': rows, 'axles_perpendicular_to_rope': bool(rows) and all(r['axle_dot_rope'] < .01 for r in rows), 'exports': [{'name': o.name, 'vertices': len(o.data.vertices), 'position': list(o.location), 'rotation': list(o.rotation_euler), 'scale': list(o.scale)} for o in bpy.context.scene.objects if o.type == 'MESH' and o.name.startswith('Cableway')], 'source_collections': [c.name for c in bpy.context.scene.collection.children]})
out = Path(__file__).resolve().parent / ('after.json' if '--after' in sys.argv else 'before.json')
out.write_text(json.dumps(reports, indent=2), encoding='utf8')
print(json.dumps([{'file': r['file'], 'axles_perpendicular_to_rope': r['axles_perpendicular_to_rope'], 'sheaves': [{k:v for k,v in s.items() if k!='unity_vertices'} for s in r['sheaves']]} for r in reports]))
