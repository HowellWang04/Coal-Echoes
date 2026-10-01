"""Give the metric tower the same worn steel look as Tajia01 and pack HDRP maps.

Steel faces sample the inspected worn-metal strip of the original tajia03 texture
along each member's own axis (as Tajia01 refine_legacy_members.py does, with
lengths now in real metres). Concrete keeps its baked procedural colour.
Metallic and roughness follow the rust amount, as in Tajia01 finalize_legacy_surface.py.
"""
import bpy
import numpy as np
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia02_Metric15')
LEGACY = Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\Assets\Art Resources\Environment\Cableway\texture\texture_pbr_20250901.002 1.png')
SIZE = globals().get('BAKE_SIZE', 4096)
OBJ = globals().get('BAKE_OBJECT', 'Tajia02_Tower')
PREFIX = globals().get('BAKE_PREFIX', 'Tower')
scene = bpy.data.scenes[globals().get('BAKE_SCENE', 'Tajia02_Tower_Metric15')]
bpy.context.window.scene = scene
tower = bpy.data.objects[OBJ]
mesh = tower.data
concrete_index = next((i for i, m in enumerate(mesh.materials) if 'Concrete' in m.name), -1)

# Connected parts and a principal-axis frame for each.
parent = list(range(len(mesh.vertices)))


def root(i):
    while parent[i] != i:
        parent[i] = parent[parent[i]]
        i = parent[i]
    return i


for e in mesh.edges:
    a, b = map(root, e.vertices)
    parent[b] = a
groups = {}
for v in mesh.vertices:
    groups.setdefault(root(v.index), []).append(v.index)
points = np.array([v.co[:] for v in mesh.vertices])
frames = {}
for i, (key, ids) in enumerate(groups.items()):
    p = points[ids]
    center = p.mean(0)
    _, _, basis = np.linalg.svd(p - center, full_matrices=False)
    frames[key] = (center, basis, (i * .61803398875) % 1)

attr = mesh.attributes.new('LegacyMemberCoordinates', 'FLOAT_VECTOR', 'CORNER')
coordinates = np.zeros((len(mesh.loops), 3), np.float32)
for poly in mesh.polygons:
    ids = [mesh.loops[l].vertex_index for l in poly.loop_indices]
    center, basis, phase = frames[root(ids[0])]
    normal = np.array(poly.normal)
    cross = basis[1] if abs(normal @ basis[1]) < abs(normal @ basis[2]) else basis[2]
    if abs(normal @ basis[0]) > .9:
        along = basis[1]
        cross = basis[2]
    else:
        along = basis[0]
    p = points[ids] - center
    # Tajia01 used /.20 and /1.6 in authoring units of about 0.82 m.
    coordinates[list(poly.loop_indices), 0] = p @ cross / .245 + phase
    coordinates[list(poly.loop_indices), 1] = p @ along / 1.96 + phase * 3
    coordinates[list(poly.loop_indices), 2] = float(poly.material_index == concrete_index)
attr.data.foreach_set('vector', coordinates.ravel())

mat = bpy.data.materials.new('LegacyMemberBake_Temporary')
n = mat.node_tree.nodes
l = mat.node_tree.links
out = next(v for v in n if v.type == 'OUTPUT_MATERIAL')


def math(op, a=None, b=None):
    v = n.new('ShaderNodeMath')
    v.operation = op
    if a is not None:
        l.new(a, v.inputs[0])
    if b is not None:
        v.inputs[1].default_value = b
    return v.outputs[0]


attribute = n.new('ShaderNodeAttribute')
attribute.attribute_name = attr.name
sep = n.new('ShaderNodeSeparateXYZ')
l.new(attribute.outputs['Vector'], sep.inputs[0])
combine = n.new('ShaderNodeCombineXYZ')
u = math('ADD', math('MULTIPLY', math('PINGPONG', sep.outputs['X'], 1), 20 / 1024), 261 / 1024)
v = math('ADD', math('MULTIPLY', math('PINGPONG', sep.outputs['Y'], 1), 112 / 1024), 1 - 182 / 1024)
l.new(u, combine.inputs[0])
l.new(v, combine.inputs[1])
legacy = n.new('ShaderNodeTexImage')
legacy.image = bpy.data.images.load(str(LEGACY), check_existing=True)
l.new(combine.outputs[0], legacy.inputs['Vector'])
tone = n.new('ShaderNodeMixRGB')
weather = n.new('ShaderNodeTexNoise')
weather.inputs['Scale'].default_value = 1.4
weather.inputs['Detail'].default_value = 2
l.new(attribute.outputs['Vector'], weather.inputs['Vector'])
l.new(math('ADD', math('MULTIPLY', weather.outputs['Fac'], .70), .08), tone.inputs[0])
l.new(legacy.outputs['Color'], tone.inputs[1])
tone.inputs[2].default_value = (.115, .095, .063, 1)
baked = n.new('ShaderNodeTexImage')
baked.image = bpy.data.images.load(str(ROOT / 'Textures' / (PREFIX + '_BaseColor.png')), check_existing=False)
mix = n.new('ShaderNodeMixRGB')
l.new(sep.outputs['Z'], mix.inputs[0])
l.new(tone.outputs[0], mix.inputs[1])
l.new(baked.outputs[0], mix.inputs[2])
emit = n.new('ShaderNodeEmission')
l.new(mix.outputs[0], emit.inputs[0])
l.new(emit.outputs[0], out.inputs['Surface'])
image = bpy.data.images.new(PREFIX + '_MemberRefined', SIZE, SIZE, alpha=False)
image.colorspace_settings.name = 'sRGB'
target = n.new('ShaderNodeTexImage')
target.image = image
n.active = target
originals = list(mesh.materials)
for i in range(len(mesh.materials)):
    mesh.materials[i] = mat
bpy.ops.object.select_all(action='DESELECT')
tower.select_set(True)
bpy.context.view_layer.objects.active = tower
scene.cycles.samples = 1
try:
    bpy.ops.object.bake(type='EMIT')
finally:
    for i, m in enumerate(originals):
        mesh.materials[i] = m
    mesh.attributes.remove(attr)
    bpy.data.materials.remove(mat)
image.filepath_raw = str(ROOT / 'Textures' / (PREFIX + '_BaseColor.png'))
image.file_format = 'PNG'
image.save()


def pixels(img):
    a = np.empty(len(img.pixels), np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(-1, 4)


color = pixels(image)
rust = np.clip(((color[:, 0] - color[:, 2]) / (color[:, 0] + .0001) - .25) / .30, 0, 1)
metal = .76 * (1 - rust) + .10 * rust
rough = .86 * (1 - rust) + .97 * rust
# Concrete texels: mark them by baking the material index as emission.
idx_mat = [bpy.data.materials.new('IndexBake_%d' % i) for i in range(len(originals))]
for i, m in enumerate(idx_mat):
    e = m.node_tree.nodes.new('ShaderNodeEmission')
    e.inputs['Color'].default_value = (1, 1, 1, 1) if i == concrete_index else (0, 0, 0, 1)
    o = next(x for x in m.node_tree.nodes if x.type == 'OUTPUT_MATERIAL')
    m.node_tree.links.new(e.outputs[0], o.inputs['Surface'])
    t = m.node_tree.nodes.new('ShaderNodeTexImage')
    mesh.materials[i] = m
mask_img = bpy.data.images.new(PREFIX + '_ConcreteIndex', SIZE, SIZE, alpha=False)
mask_img.colorspace_settings.name = 'Non-Color'
for m in idx_mat:
    t = next(x for x in m.node_tree.nodes if x.type == 'TEX_IMAGE')
    t.image = mask_img
    m.node_tree.nodes.active = t
try:
    bpy.ops.object.bake(type='EMIT')
finally:
    for i, m in enumerate(originals):
        mesh.materials[i] = m
    for m in idx_mat:
        bpy.data.materials.remove(m)
concrete = pixels(mask_img)[:, 0] > .5
bpy.data.images.remove(mask_img)
metal[concrete] = 0
rough[concrete] = .92
ao = pixels(bpy.data.images.load(str(ROOT / 'Textures' / (PREFIX + '_AO.png')), check_existing=False))[:, 0]
data = np.ones_like(color)
data[:, 0] = metal
data[:, 1] = ao
data[:, 2] = 1
data[:, 3] = 1 - rough
out_img = bpy.data.images.new(PREFIX + '_HDRP_Mask', SIZE, SIZE, alpha=True)
out_img.colorspace_settings.name = 'Non-Color'
out_img.alpha_mode = 'CHANNEL_PACKED'
out_img.pixels.foreach_set(data.ravel())
out_img.filepath_raw = str(ROOT / 'Textures' / (PREFIX + '_HDRP_Mask.png'))
out_img.file_format = 'PNG'
out_img.save()

# Single portable material for export and previews.
pbr = bpy.data.materials.new(OBJ + '_PBR')
nodes = pbr.node_tree.nodes
links = pbr.node_tree.links
bs = next(x for x in nodes if x.type == 'BSDF_PRINCIPLED')
base = nodes.new('ShaderNodeTexImage')
base.image = image
links.new(base.outputs['Color'], bs.inputs['Base Color'])
mask = nodes.new('ShaderNodeTexImage')
mask.image = out_img
split = nodes.new('ShaderNodeSeparateColor')
links.new(mask.outputs['Color'], split.inputs[0])
links.new(split.outputs['Red'], bs.inputs['Metallic'])
inv = nodes.new('ShaderNodeMath')
inv.operation = 'SUBTRACT'
inv.inputs[0].default_value = 1
links.new(mask.outputs['Alpha'], inv.inputs[1])
links.new(inv.outputs[0], bs.inputs['Roughness'])
nimg = nodes.new('ShaderNodeTexImage')
nimg.image = bpy.data.images.load(str(ROOT / 'Textures' / (PREFIX + '_Normal.png')), check_existing=False)
nimg.image.colorspace_settings.name = 'Non-Color'
nm = nodes.new('ShaderNodeNormalMap')
nm.inputs['Strength'].default_value = .45
links.new(nimg.outputs['Color'], nm.inputs['Color'])
links.new(nm.outputs['Normal'], bs.inputs['Normal'])
mesh.materials.clear()
mesh.materials.append(pbr)
for p in mesh.polygons:
    p.material_index = 0
print('SURFACE done, concrete texels', int(concrete.sum()))
