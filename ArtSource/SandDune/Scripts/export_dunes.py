"""Export each dune as one FBX at its origin with LOD0 to LOD2; Unity builds the LODGroup from the suffixes."""
import bpy
import shutil
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\SandDune')
UNITY = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\Assets\Art Resources\Environment\SandDune')
NAMES = ['SandDune_MainRoad', 'SandDune_Left', 'SandDune_Right']
RATIOS = [1.0, 0.3, 0.08]

coll = bpy.data.collections['SandDunes']
if bpy.context.mode != 'OBJECT':
    bpy.ops.object.mode_set(mode='OBJECT')
(ROOT / 'Exports').mkdir(exist_ok=True)
UNITY.mkdir(parents=True, exist_ok=True)
for name in NAMES:
    source = bpy.data.objects[name]
    lods = []
    for level, ratio in enumerate(RATIOS):
        lod_name = f'{name}_LOD{level}'
        ob = bpy.data.objects.get(lod_name) or bpy.data.objects.new(lod_name, source.data)
        if ob.name not in coll.objects:
            coll.objects.link(ob)
        ob.data = source.data
        ob.location = (0, 0, 0)
        ob.modifiers.clear()
        if ratio < 1:
            ob.modifiers.new('Decimate', 'DECIMATE').ratio = ratio
        ob.hide_set(False)
        lods.append(ob)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in lods:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = lods[0]
    fbx = ROOT / 'Exports' / f'{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(fbx), use_selection=True, object_types={'MESH'},
                             use_mesh_modifiers=True, use_triangles=True, mesh_smooth_type='OFF', use_tspace=True,
                             axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             path_mode='STRIP', bake_anim=False)
    shutil.copy2(fbx, UNITY / fbx.name)
    deps = bpy.context.evaluated_depsgraph_get()
    tris = [sum(len(p.vertices) - 2 for p in ob.evaluated_get(deps).data.polygons) for ob in lods]
    print(name, 'tris per LOD', tris)
    # Export copies stay in the file but out of the way of the preview.
    for ob in lods:
        ob.hide_set(True)
