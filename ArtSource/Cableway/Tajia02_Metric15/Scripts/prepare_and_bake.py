"""Join the metric tower into one mesh, unwrap it and bake the procedural channels.

Run after build_tower.py. Set STEP before exec: 'prepare', then 'AO', 'Normal', 'BaseColor'.
Each bake writes Textures/Tower_<channel>.png and Blender/bake_<channel>_status.txt.
"""
import bpy
import math
import os
import traceback
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia02_Metric15')
SIZE = globals().get('BAKE_SIZE', 4096)
OBJ = globals().get('BAKE_OBJECT', 'Tajia02_Tower')
PREFIX = globals().get('BAKE_PREFIX', 'Tower')
scene = bpy.data.scenes[globals().get('BAKE_SCENE', 'Tajia02_Tower_Metric15')]
bpy.context.window.scene = scene
STEP = globals().get('STEP', 'prepare')


def prepare():
    source = bpy.data.collections[globals().get('BAKE_SOURCE', 'Tower_Fabricated_Parts')]
    export = bpy.data.collections.new(OBJ + '_Export')
    scene.collection.children.link(export)
    bpy.ops.object.select_all(action='DESELECT')
    dg = bpy.context.evaluated_depsgraph_get()
    clones = []
    for ob in source.objects:
        me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
        clone = bpy.data.objects.new(ob.name + '_Export', me)
        export.objects.link(clone)
        clone.matrix_world = ob.matrix_world
        clones.append(clone)
        clone.select_set(True)
    bpy.context.view_layer.objects.active = clones[0]
    bpy.ops.object.join()
    tower = bpy.context.object
    tower.name = OBJ
    tower.data.name = OBJ + '_Mesh'
    source.hide_render = True
    source.hide_viewport = True
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=.0015, margin_method='FRACTION', area_weight=.5, correct_aspect=True, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode='OBJECT')
    tower.data.uv_layers.active.name = 'TowerUV'
    tower.data.calc_loop_triangles()
    scene.render.engine = 'CYCLES'
    prefs = bpy.context.preferences.addons['cycles'].preferences
    prefs.refresh_devices()
    types = [t[0] for t in prefs.get_device_types(bpy.context)]
    for kind in ('OPTIX', 'CUDA'):
        if kind in types:
            prefs.compute_device_type = kind
            prefs.refresh_devices()
            for device in prefs.devices:
                device.use = device.type == kind
            scene.cycles.device = 'GPU'
            break
    scene.render.bake.use_selected_to_active = False
    scene.render.bake.margin = 8
    scene.render.bake.margin_type = 'EXTEND'
    scene.render.bake.use_clear = True
    (ROOT / 'Textures').mkdir(parents=True, exist_ok=True)
    (ROOT / 'Blender').mkdir(parents=True, exist_ok=True)
    print('PREPARED', len(tower.data.vertices), 'vertices', len(tower.data.loop_triangles), 'triangles', [m.name for m in tower.data.materials], 'device', scene.cycles.device)


def bake(channel):
    tower = bpy.data.objects[OBJ]
    bpy.ops.object.select_all(action='DESELECT')
    tower.select_set(True)
    bpy.context.view_layer.objects.active = tower
    image = bpy.data.images.new(PREFIX + '_' + channel, SIZE, SIZE, alpha=False)
    image.colorspace_settings.name = 'sRGB' if channel == 'BaseColor' else 'Non-Color'
    image.filepath_raw = str(ROOT / 'Textures' / (PREFIX + '_' + channel + '.png'))
    image.file_format = 'PNG'
    saved = []
    status = ROOT / 'Blender' / ('bake_' + PREFIX + '_' + channel + '_status.txt')
    try:
        for mat in tower.data.materials:
            nodes = mat.node_tree.nodes
            links = mat.node_tree.links
            bs = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
            output = next(n for n in nodes if n.type == 'OUTPUT_MATERIAL')
            target = nodes.new('ShaderNodeTexImage')
            target.image = image
            nodes.active = target
            original = output.inputs['Surface'].links[0].from_socket
            emit = None
            if channel == 'BaseColor':
                emit = nodes.new('ShaderNodeEmission')
                links.new(bs.inputs['Base Color'].links[0].from_socket, emit.inputs['Color'])
                links.new(emit.outputs[0], output.inputs['Surface'])
            saved.append((mat, output, original, target, emit))
        mode = {'Normal': 'NORMAL', 'AO': 'AO'}.get(channel, 'EMIT')
        scene.cycles.samples = 64 if channel == 'AO' else (16 if channel == 'Normal' else 1)
        bpy.ops.object.bake(type=mode)
        image.save()
        status.write_text('complete', encoding='utf-8')
    except Exception:
        status.write_text(traceback.format_exc(), encoding='utf-8')
        raise
    finally:
        for mat, output, original, target, emit in saved:
            mat.node_tree.links.new(original, output.inputs['Surface'])
            mat.node_tree.nodes.remove(target)
            if emit is not None:
                mat.node_tree.nodes.remove(emit)


if STEP == 'prepare':
    prepare()
else:
    bake(STEP)
    print('BAKED', STEP)
