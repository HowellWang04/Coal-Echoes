"""One concrete leg pier for the metric cableway tower, as its own model.

z = 0 is the pier top, where a sole plate sits. The pier is 0.9 m square and 3.5 m deep.
In Unity one pier is placed under each leg foot with its top 0.4 m above the highest
ground in its footprint; the rest is buried. A scaled copy serves the ladder foot.
Slopes are taken up by the leg extensions (build_leg_extensions.py), not by the piers.
Bakes BaseColor, AO, Normal and an HDRP mask at 2048, then exports FBX at scale 1.
"""
import bpy
import numpy as np
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia02_Metric15')
SIZE = 2048
PIER_DEPTH = 3.5

scene = bpy.data.scenes.new('Tajia02_Foundation')
bpy.context.window.scene = scene
col = bpy.data.collections.new('Foundation_Parts')
scene.collection.children.link(col)

mat = bpy.data.materials.new('Foundation_Concrete')
nodes = mat.node_tree.nodes
links = mat.node_tree.links
bs = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
pos = nodes.new('ShaderNodeNewGeometry')
coarse = nodes.new('ShaderNodeTexNoise')
coarse.inputs['Scale'].default_value = 1.6
coarse.inputs['Detail'].default_value = 6.0
links.new(pos.outputs['Position'], coarse.inputs['Vector'])
fine = nodes.new('ShaderNodeTexNoise')
fine.inputs['Scale'].default_value = 90.0
fine.inputs['Detail'].default_value = 4.0
links.new(pos.outputs['Position'], fine.inputs['Vector'])
ramp = nodes.new('ShaderNodeValToRGB')
cr = ramp.color_ramp
cr.elements[0].position, cr.elements[0].color = .30, (.20, .19, .17, 1)
cr.elements[1].position, cr.elements[1].color = .70, (.36, .345, .315, 1)
links.new(coarse.outputs['Fac'], ramp.inputs['Fac'])
# Darker rust run-off and soil staining towards the top edge and the lower buried part.
sep = nodes.new('ShaderNodeSeparateXYZ')
links.new(pos.outputs['Position'], sep.inputs[0])
top = nodes.new('ShaderNodeMapRange')
top.inputs['From Min'].default_value = -.9
top.inputs['From Max'].default_value = 0.0
links.new(sep.outputs['Z'], top.inputs['Value'])
streak = nodes.new('ShaderNodeTexNoise')
streak.inputs['Scale'].default_value = 6.0
streak_map = nodes.new('ShaderNodeMapping')
streak_map.inputs['Scale'].default_value = (8.0, 8.0, .6)
links.new(pos.outputs['Position'], streak_map.inputs['Vector'])
links.new(streak_map.outputs['Vector'], streak.inputs['Vector'])
mul = nodes.new('ShaderNodeMath')
mul.operation = 'MULTIPLY'
links.new(top.outputs['Result'], mul.inputs[0])
links.new(streak.outputs['Fac'], mul.inputs[1])
rust = nodes.new('ShaderNodeMixRGB')
rust.blend_type = 'MULTIPLY'
links.new(mul.outputs[0], rust.inputs[0])
links.new(ramp.outputs['Color'], rust.inputs[1])
rust.inputs[2].default_value = (.55, .40, .30, 1)
links.new(rust.outputs[0], bs.inputs['Base Color'])
rough = nodes.new('ShaderNodeMapRange')
rough.inputs['To Min'].default_value = .82
rough.inputs['To Max'].default_value = .96
links.new(fine.outputs['Fac'], rough.inputs['Value'])
links.new(rough.outputs['Result'], bs.inputs['Roughness'])
bs.inputs['Metallic'].default_value = 0.0
bump = nodes.new('ShaderNodeBump')
bump.inputs['Strength'].default_value = .35
bump.inputs['Distance'].default_value = .006
links.new(fine.outputs['Fac'], bump.inputs['Height'])
links.new(bump.outputs['Normal'], bs.inputs['Normal'])


def pier(name, cx, cy, w, d):
    mesh = bpy.data.meshes.new(name)
    x0, x1, y0, y1 = cx - w / 2, cx + w / 2, cy - d / 2, cy + d / 2
    verts = [(x0, y0, -PIER_DEPTH), (x1, y0, -PIER_DEPTH), (x1, y1, -PIER_DEPTH), (x0, y1, -PIER_DEPTH),
             (x0, y0, 0), (x1, y0, 0), (x1, y1, 0), (x0, y1, 0)]
    faces = [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(mat)
    ob = bpy.data.objects.new(name, mesh)
    col.objects.link(ob)
    bev = ob.modifiers.new('Chamfer', 'BEVEL')
    bev.width = .025
    bev.segments = 1
    bev.limit_method = 'ANGLE'
    ob.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL').keep_sharp = True
    return ob


pier('Leg pier', 0, 0, .90, .90)

dg = bpy.context.evaluated_depsgraph_get()
bpy.ops.object.select_all(action='DESELECT')
clones = []
for ob in col.objects:
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
    c = bpy.data.objects.new(ob.name + '_Export', me)
    scene.collection.objects.link(c)
    c.matrix_world = ob.matrix_world
    c.select_set(True)
    clones.append(c)
bpy.context.view_layer.objects.active = clones[0]
bpy.ops.object.join()
found = bpy.context.object
found.name = 'CablewayTowerPier'
col.hide_render = True
col.hide_viewport = True
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=.004, scale_to_bounds=True)
bpy.ops.object.mode_set(mode='OBJECT')

scene.render.engine = 'CYCLES'
scene.cycles.device = 'GPU'
scene.render.bake.margin = 8
scene.render.bake.margin_type = 'EXTEND'


def bake(channel):
    img = bpy.data.images.new('Foundation_' + channel, SIZE, SIZE, alpha=False)
    img.colorspace_settings.name = 'sRGB' if channel == 'BaseColor' else 'Non-Color'
    out = next(n for n in nodes if n.type == 'OUTPUT_MATERIAL')
    original = out.inputs['Surface'].links[0].from_socket
    target = nodes.new('ShaderNodeTexImage')
    target.image = img
    nodes.active = target
    emit = None
    if channel in ('BaseColor', 'Roughness'):
        emit = nodes.new('ShaderNodeEmission')
        src = rust.outputs[0] if channel == 'BaseColor' else rough.outputs['Result']
        links.new(src, emit.inputs['Color'])
        links.new(emit.outputs[0], out.inputs['Surface'])
    scene.cycles.samples = {'AO': 64, 'Normal': 16}.get(channel, 1)
    bpy.ops.object.select_all(action='DESELECT')
    found.select_set(True)
    bpy.context.view_layer.objects.active = found
    try:
        bpy.ops.object.bake(type={'AO': 'AO', 'Normal': 'NORMAL'}.get(channel, 'EMIT'))
    finally:
        links.new(original, out.inputs['Surface'])
        nodes.remove(target)
        if emit:
            nodes.remove(emit)
    return img


maps = {c: bake(c) for c in ('BaseColor', 'AO', 'Normal', 'Roughness')}


def px(img):
    a = np.empty(len(img.pixels), np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(-1, 4)


mask = np.ones((SIZE * SIZE, 4), np.float32)
mask[:, 0] = 0
mask[:, 1] = px(maps['AO'])[:, 0]
mask[:, 3] = 1 - px(maps['Roughness'])[:, 0]
m = bpy.data.images.new('Foundation_HDRP_Mask', SIZE, SIZE, alpha=True)
m.colorspace_settings.name = 'Non-Color'
m.alpha_mode = 'CHANNEL_PACKED'
m.pixels.foreach_set(mask.ravel())
maps['HDRP_Mask'] = m
for name in ('BaseColor', 'Normal', 'HDRP_Mask'):
    img = maps[name]
    img.filepath_raw = str(ROOT / 'Textures' / ('Foundation_' + name + '.png'))
    img.file_format = 'PNG'
    img.save()
    img.pack()

bpy.ops.object.select_all(action='DESELECT')
found.select_set(True)
bpy.context.view_layer.objects.active = found
bpy.ops.export_scene.fbx(filepath=str(ROOT / 'Exports' / 'CablewayTowerPier.fbx'), use_selection=True, object_types={'MESH'},
                         use_triangles=True, mesh_smooth_type='OFF', use_tspace=True, axis_forward='-Z', axis_up='Y',
                         apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, path_mode='STRIP', bake_anim=False)
bpy.data.libraries.write(str(ROOT / 'Blender' / 'Tajia02_Foundation.blend'), {scene}, fake_user=True, compress=True)
print('FOUNDATION', len(found.data.vertices), 'vertices', [round(v, 3) for v in found.dimensions])
