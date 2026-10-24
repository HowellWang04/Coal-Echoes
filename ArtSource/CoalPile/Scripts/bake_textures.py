"""Bake the shared coal material of all three piles into one 4096 atlas, then pack the HDRP mask map."""
import bpy
import numpy as np
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\CoalPile')
NAMES = ['CoalPile_A', 'CoalPile_B', 'CoalPile_C']
SIZE = 4096

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
prefs = bpy.context.preferences.addons['cycles'].preferences
prefs.get_devices()
if any(d.type == 'OPTIX' for d in prefs.devices):
    prefs.compute_device_type = 'OPTIX'
    for d in prefs.devices:
        d.use = d.type == 'OPTIX'
    scene.cycles.device = 'GPU'
scene.render.bake.margin = 16

objects = [bpy.data.objects[n] for n in NAMES]
bpy.ops.object.select_all(action='DESELECT')
for ob in objects:
    ob.select_set(True)
bpy.context.view_layer.objects.active = objects[0]
mat = bpy.data.materials['CoalPile']
nodes, links = mat.node_tree.nodes, mat.node_tree.links
bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
output = next(n for n in nodes if n.type == 'OUTPUT_MATERIAL')
surface = output.inputs['Surface'].links[0].from_socket


def bake(channel, mode, socket=None, samples=4):
    image = bpy.data.images.get('CoalPile_' + channel) or bpy.data.images.new('CoalPile_' + channel, SIZE, SIZE, alpha=False)
    image.colorspace_settings.name = 'sRGB' if channel == 'BaseColor' else 'Non-Color'
    target = nodes.new('ShaderNodeTexImage')
    target.image = image
    nodes.active = target
    emit = None
    if socket:
        # Colour channels bake through an emission shader so no lighting ends up in them.
        emit = nodes.new('ShaderNodeEmission')
        links.new(bsdf.inputs[socket].links[0].from_socket, emit.inputs['Color'])
        links.new(emit.outputs[0], output.inputs['Surface'])
    scene.cycles.samples = samples
    try:
        bpy.ops.object.bake(type=mode)
    finally:
        links.new(surface, output.inputs['Surface'])
        if emit:
            nodes.remove(emit)
        nodes.remove(target)
    image.filepath_raw = str(ROOT / 'Textures' / ('CoalPile_' + channel + '.png'))
    image.file_format = 'PNG'
    image.save()
    return image


(ROOT / 'Textures').mkdir(exist_ok=True)
bake('BaseColor', 'EMIT', 'Base Color')
roughness = bake('Roughness', 'EMIT', 'Roughness')
bake('Normal', 'NORMAL', samples=16)
ao = bake('AO', 'AO', samples=64)


def pixels(image):
    data = np.empty(SIZE * SIZE * 4, dtype=np.float32)
    image.pixels.foreach_get(data)
    return data.reshape(SIZE, SIZE, 4)


# HDRP mask: R metallic, G ambient occlusion, B detail mask, A smoothness.
r, a = pixels(roughness), pixels(ao)
mask = np.zeros((SIZE, SIZE, 4), dtype=np.float32)
mask[..., 1] = a[..., 0]
mask[..., 2] = 1.0
mask[..., 3] = 1.0 - r[..., 0]
packed = bpy.data.images.get('CoalPile_HDRP_Mask') or bpy.data.images.new('CoalPile_HDRP_Mask', SIZE, SIZE, alpha=True)
packed.colorspace_settings.name = 'Non-Color'
packed.pixels.foreach_set(mask.ravel())
packed.filepath_raw = str(ROOT / 'Textures' / 'CoalPile_HDRP_Mask.png')
packed.file_format = 'PNG'
packed.save()
print('baked', sorted(p.name for p in (ROOT / 'Textures').glob('*.png')))
