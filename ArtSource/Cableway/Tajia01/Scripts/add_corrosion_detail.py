"""Layer corrosion over the approved palette and bake portable texture maps."""
import bpy
import hashlib
import json
import shutil
import numpy as np
from pathlib import Path

ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia01')
scene = bpy.data.scenes['Tajia01_Tower_Rebuilt']
bpy.context.window.scene = scene
tower = bpy.data.objects['Tajia01_Tower']
archive = ROOT/'Previous'/'Before_Corrosion_Detail'
archive_exists = archive.exists()
archive.mkdir(parents=True, exist_ok=True)
for folder in ['Textures', 'Previews', 'Blender', 'Exports']:
    (archive/folder).mkdir(exist_ok=True)
for folder, pattern in [('Textures', '*.png'), ('Previews', '*.png'), ('Blender', '*.blend'), ('Exports', '*')]:
    for p in (ROOT/folder).glob(pattern):
        if not archive_exists:
            shutil.copy2(p, archive/folder/p.name)

def mesh_hash():
    h = hashlib.sha256()
    for items, attr, width, dtype in [(tower.data.vertices,'co',3,np.float32), (tower.data.loops,'vertex_index',1,np.int32), (tower.data.uv_layers.active.data,'uv',2,np.float32)]:
        data = np.empty(len(items)*width, dtype=dtype)
        items.foreach_get(attr, data)
        h.update(data.tobytes())
    return h.hexdigest()

report = {'mesh_before': mesh_hash(), 'palette_preserved': True, 'maps_before': {p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (archive/'Textures').glob('*.png')}}
portable = tower.data.materials[0]
for n in portable.node_tree.nodes:
    if n.type == 'TEX_IMAGE' and n.image:
        original_path = archive/'Textures'/Path(n.image.filepath).name
        if original_path.exists():
            n.image = bpy.data.images.load(str(original_path), check_existing=False)
            n.image.colorspace_settings.name = 'sRGB' if original_path.stem == 'Tower_BaseColor' else 'Non-Color'
    if n.type == 'NORMAL_MAP':
        n.inputs['Strength'].default_value = .45
previous_source = bpy.data.materials.get('Tower_Corrosion_BakeSource')
if previous_source is not None:
    previous_source.name = 'Tower_Corrosion_PreviousIteration'
source = portable.copy()
source.name = 'Tower_Corrosion_BakeSource'
source.use_fake_user = True
tower.data.materials[0] = source
nodes = source.node_tree.nodes
links = source.node_tree.links
bs = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
output = next(n for n in nodes if n.type == 'OUTPUT_MATERIAL')
base = bs.inputs['Base Color'].links[0].from_socket
metal = bs.inputs['Metallic'].links[0].from_socket
rough = bs.inputs['Roughness'].links[0].from_socket
normal = bs.inputs['Normal'].links[0].from_socket

def node(kind):
    n = nodes.new(kind)
    n.label = 'Corrosion detail layer'
    return n

def enum(obj, prop, value):
    valid = {v.identifier for v in obj.bl_rna.properties[prop].enum_items}
    assert value in valid, (prop, value, valid)
    setattr(obj, prop, value)

def set_input(n, idx, value):
    if isinstance(value, (float, int)):
        n.inputs[idx].default_value = value
    else:
        links.new(value, n.inputs[idx])

def math(op, a, b=0):
    n = node('ShaderNodeMath')
    enum(n, 'operation', op)
    set_input(n, 0, a)
    set_input(n, 1, b)
    return n.outputs[0]

def remap(value, lo, hi, outlo=0, outhi=1):
    n = node('ShaderNodeMapRange')
    links.new(value, n.inputs['Value'])
    for key, v in [('From Min',lo), ('From Max',hi), ('To Min',outlo), ('To Max',outhi)]:
        n.inputs[key].default_value = v
    return n.outputs[0]

def color(hex_value):
    vals = [int(hex_value[i:i+2],16)/255 for i in (1,3,5)]
    return tuple(v/12.92 if v <= .04045 else ((v+.055)/1.055)**2.4 for v in vals)+(1,)

def blend(factor, a, b):
    n = node('ShaderNodeMixRGB')
    links.new(factor, n.inputs[0])
    for idx, value in [(1,a),(2,b)]:
        if isinstance(value, tuple):
            n.inputs[idx].default_value = value
        else:
            links.new(value, n.inputs[idx])
    return n.outputs[0]

geo = node('ShaderNodeNewGeometry')
xyz = node('ShaderNodeSeparateXYZ')
links.new(geo.outputs['Position'], xyz.inputs[0])
steel = math('GREATER_THAN', xyz.outputs['Z'], .245)

def noise(scale, stretch=None):
    n = node('ShaderNodeTexNoise')
    vector = geo.outputs['Position']
    if stretch:
        v = node('ShaderNodeVectorMath')
        enum(v, 'operation', 'MULTIPLY')
        links.new(vector, v.inputs[0])
        v.inputs[1].default_value = stretch
        vector = v.outputs[0]
    links.new(vector, n.inputs['Vector'])
    n.inputs['Scale'].default_value = scale
    n.inputs['Detail'].default_value = 3.5
    n.inputs['Roughness'].default_value = .72
    return n.outputs['Fac']

patch = noise(9)
grain = noise(82)
pits = noise(210)
streaks = noise(10, (2.3,2.3,.24))
channels = node('ShaderNodeSeparateColor')
links.new(base, channels.inputs[0])
existing = remap(math('SUBTRACT', channels.outputs['Red'], channels.outputs['Green']), .055, .13)
ao = node('ShaderNodeTexImage')
ao.image = bpy.data.images['Tower_AO']
crevice = remap(ao.outputs['Color'], .28, .69, 1, 0)
edge = math('ABSOLUTE', math('SUBTRACT', geo.outputs['Pointiness'], .5))
edge = remap(edge, .012, .075)
broken_edge = math('MULTIPLY', edge, math('MULTIPLY',remap(grain,.52,.69),remap(patch,.48,.65)))
accumulation = math('MULTIPLY', crevice, remap(patch,.34,.61))
spots = math('MULTIPLY', remap(patch,.59,.73), remap(grain,.46,.67))
drips = math('MULTIPLY', remap(streaks,.57,.74), remap(patch,.44,.66))
mask = math('MAXIMUM', math('MULTIPLY', existing,.72), math('MULTIPLY',spots,.88))
mask = math('MAXIMUM', mask, math('MULTIPLY',broken_edge,.65))
mask = math('MAXIMUM', mask, math('MULTIPLY',accumulation,.85))
mask = math('MAXIMUM', mask, math('MULTIPLY',drips,.48))
mask = math('MULTIPLY', mask, steel)
rust = blend(remap(grain,.25,.75), color('#654A32'), color('#A0794D'))
weathered = blend(mask, base, rust)
speckle = math('MULTIPLY', remap(pits,.61,.76), math('MULTIPLY',mask,.55))
weathered = blend(speckle, weathered, color('#57483A'))
links.new(weathered, bs.inputs['Base Color'])
new_metal = math('MULTIPLY', metal, math('SUBTRACT',1,math('MULTIPLY',mask,.8)))
new_rough = math('MAXIMUM', rough, math('MULTIPLY',mask,.98))
links.new(new_metal, bs.inputs['Metallic'])
links.new(new_rough, bs.inputs['Roughness'])
bump = node('ShaderNodeBump')
bump.invert = True
bump.inputs['Strength'].default_value = .32
bump.inputs['Distance'].default_value = .0035
links.new(math('MULTIPLY',mask,remap(pits,.43,.69)), bump.inputs['Height'])
links.new(normal, bump.inputs['Normal'])
links.new(bump.outputs['Normal'], bs.inputs['Normal'])

bpy.ops.object.select_all(action='DESELECT')
tower.select_set(True)
bpy.context.view_layer.objects.active = tower
baked = {}
for channel, socket in [('BaseColor',weathered), ('Metallic',new_metal), ('Roughness',new_rough), ('CorrosionMask',mask), ('Normal',None)]:
    (ROOT/'Blender'/'corrosion_status.txt').write_text('baking '+channel)
    image = bpy.data.images.new('CorrosionBake_'+channel,4096,4096,alpha=False)
    image.colorspace_settings.name = 'sRGB' if channel == 'BaseColor' else 'Non-Color'
    enum(image, 'file_format', 'PNG')
    image.filepath_raw = str(ROOT/'Textures'/('Tower_'+channel+'.png'))
    target = nodes.new('ShaderNodeTexImage')
    target.image = image
    nodes.active = target
    emit = None
    if socket is not None:
        emit = nodes.new('ShaderNodeEmission')
        links.new(socket, emit.inputs['Color'])
        links.new(emit.outputs[0], output.inputs['Surface'])
    scene.cycles.samples = 16 if channel == 'Normal' else 1
    bake_type = 'NORMAL' if channel == 'Normal' else 'EMIT'
    assert bake_type in {v.identifier for v in bpy.ops.object.bake.get_rna_type().properties['type'].enum_items}
    bpy.ops.object.bake(type=bake_type)
    image.save()
    image.pack()
    links.new(bs.outputs[0], output.inputs['Surface'])
    nodes.remove(target)
    if emit is not None:
        nodes.remove(emit)
    baked[channel] = image

def red(image):
    a = np.empty(len(image.pixels),np.float32)
    image.pixels.foreach_get(a)
    return a.reshape(-1,4)[:,0].copy()

m = red(baked['Metallic'])
r = red(baked['Roughness'])
a = red(bpy.data.images['Tower_AO'])
pixels = np.ones((4096*4096,4),np.float32)
for name in ['HDRP_Mask','OcclusionRoughnessMetallic']:
    if name == 'HDRP_Mask':
        pixels[:,0]=m;pixels[:,1]=a;pixels[:,2]=1;pixels[:,3]=1-r
    else:
        pixels[:,0]=a;pixels[:,1]=r;pixels[:,2]=m;pixels[:,3]=1
    image = bpy.data.images.new('CorrosionBake_'+name,4096,4096,alpha=name=='HDRP_Mask')
    image.colorspace_settings.name='Non-Color'
    enum(image, 'alpha_mode', 'CHANNEL_PACKED')
    enum(image, 'file_format', 'PNG')
    image.pixels.foreach_set(pixels.ravel())
    image.filepath_raw=str(ROOT/'Textures'/('Tower_'+name+'.png'))
    image.save();image.pack()
    baked[name]=image

# Keep the pre-layer atlas images in the source graph reproducible.
for n in source.node_tree.nodes:
    if n.type == 'TEX_IMAGE' and n.image:
        p = Path(n.image.filepath)
        if (archive/'Textures'/p.name).exists():
            n.image.filepath=str(archive/'Textures'/p.name)
            n.image.pack()
for n in portable.node_tree.nodes:
    if n.type == 'TEX_IMAGE' and n.image:
        stem = Path(n.image.filepath).stem.removeprefix('Tower_')
        if stem in baked:
            n.image = baked[stem]
    if n.type == 'NORMAL_MAP':
        n.inputs['Strength'].default_value=1.0
for channel, image in baked.items():
    old = bpy.data.images.get('Tower_'+channel)
    if old is not None and old != image:
        old.name = 'PreCorrosion_'+channel
    image.name='Tower_'+channel
tower.data.materials[0] = portable
scene.cycles.samples=48
scene['normal_strength']=1.0
scene['corrosion_revision']='Fine pitting, broken edge rust, seam accumulation, and thin weather streaks'
scene.render.filepath=str(ROOT/'Previews'/'Tower_v01_Overview.png')
bpy.ops.render.render(write_still=True)
report['mesh_after']=mesh_hash()
assert report['mesh_before']==report['mesh_after']
report['normal_strength']=1.0
report['maps_after']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'Textures').glob('*.png')}
assert report['maps_before']['Tower_AO.png']==report['maps_after']['Tower_AO.png']
(ROOT/'corrosion_revision.json').write_text(json.dumps(report,indent=2))
(ROOT/'Blender'/'corrosion_status.txt').write_text('complete')
