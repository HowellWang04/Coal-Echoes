"""Export the metric tower at scale 1 (Unity units = metres) and save the source .blend."""
import bpy
import bmesh
import json
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia02_Metric15')
NAME = globals().get('EXPORT_NAME', 'CablewayTower_Metric15')
scene = bpy.data.scenes[globals().get('BAKE_SCENE', 'Tajia02_Tower_Metric15')]
bpy.context.window.scene = scene
tower = bpy.data.objects[globals().get('BAKE_OBJECT', 'Tajia02_Tower')]
tower.name = NAME
(ROOT / 'Exports').mkdir(exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
tower.select_set(True)
bpy.context.view_layer.objects.active = tower
tower.data.calc_loop_triangles()
bm = bmesh.new()
bm.from_mesh(tower.data)
pts = [v.co for v in tower.data.vertices]
report = {
    'name': tower.name,
    'vertices': len(tower.data.vertices),
    'triangles': len(tower.data.loop_triangles),
    'material_slots': len(tower.data.materials),
    'uv_layers': [u.name for u in tower.data.uv_layers],
    'zero_area_faces': sum(f.calc_area() < 1e-12 for f in bm.faces),
    'bounds_min': [min(p[i] for p in pts) for i in range(3)],
    'bounds_max': [max(p[i] for p in pts) for i in range(3)],
    'units': 'metres, export scale 1',
    'total_height_m': scene['total_height_m'],
    'rope_head_points_blender': [list(p) for p in scene['rope_head_points']],
    'ladder': scene['ladder'],
}
bm.free()
fbx = ROOT / 'Exports' / (NAME + '.fbx')
bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={'MESH'}, use_mesh_modifiers=True,
                         use_triangles=True, mesh_smooth_type='OFF', use_tspace=True, axis_forward='-Z', axis_up='Y',
                         apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, path_mode='STRIP', bake_anim=False)
for image in bpy.data.images:
    if image.name.startswith(globals().get('BAKE_PREFIX', 'Tower') + '_') and image.size[0] > 0 and image.filepath:
        image.pack()
bpy.data.libraries.write(str(ROOT / 'Blender' / (scene.name + '.blend')), {scene}, fake_user=True, compress=True)
(ROOT / ('verification_' + NAME + '.json')).write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report))
