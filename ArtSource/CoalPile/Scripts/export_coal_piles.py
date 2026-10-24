"""Export each pile as one FBX at the origin with LOD0 to LOD2; Unity builds the LODGroup from the suffixes."""
import bpy
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\CoalPile')
NAMES = ['CoalPile_A', 'CoalPile_B', 'CoalPile_C']
RATIOS = [1.0, 0.3, 0.08]

coll = bpy.data.collections['CoalPiles']
if bpy.context.mode != 'OBJECT':
    bpy.ops.object.mode_set(mode='OBJECT')
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
    bpy.ops.export_scene.fbx(filepath=str(ROOT / 'Exports' / f'{name}.fbx'), use_selection=True, object_types={'MESH'},
                             use_mesh_modifiers=True, use_triangles=True, mesh_smooth_type='OFF', use_tspace=True,
                             axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
                             path_mode='STRIP', bake_anim=False)
    deps = bpy.context.evaluated_depsgraph_get()
    tris = [sum(len(p.vertices) - 2 for p in ob.evaluated_get(deps).data.polygons) for ob in lods]
    print(name, 'tris per LOD', tris)
    # Export copies stay in the file but out of the way of the preview layout.
    for ob in lods:
        ob.hide_set(True)
