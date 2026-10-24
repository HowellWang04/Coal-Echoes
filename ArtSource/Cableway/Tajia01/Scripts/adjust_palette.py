"""Rebake only base color to the approved quiet warm gray-brown palette."""
import bpy
import hashlib
import json
import shutil
import numpy as np
from pathlib import Path

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia01')
scene=bpy.data.scenes['Tajia01_Tower_Rebuilt']
bpy.context.window.scene=scene
tower=bpy.data.objects['Tajia01_Tower']
archive=ROOT/'Previous'/'Before_Palette_Adjustment'
archive.mkdir(parents=True,exist_ok=True)
for relative in ['Textures/Tower_BaseColor.png','Previews/Tower_v01_Overview.png','Previews/Tower_v01_Detail.png','verification.json']:
    shutil.copy2(ROOT/relative,archive/Path(relative).name)

def mesh_hash():
    h=hashlib.sha256()
    for collection,attribute,width,dtype in [(tower.data.vertices,'co',3,np.float32),(tower.data.loops,'vertex_index',1,np.int32),(tower.data.uv_layers.active.data,'uv',2,np.float32)]:
        data=np.empty(len(collection)*width,dtype=dtype)
        collection.foreach_get(attribute,data)
        h.update(data.tobytes())
    return h.hexdigest()

report={'mesh_before':mesh_hash(),'palette':{'steel':'#4B4945','rust':'#5B4B3F','old_paint':'#58453F'},'lighting_exposure_changed':False}
report['unchanged_maps_before']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'Textures').glob('*.png') if p.name!='Tower_BaseColor.png'}

def rgba(hex_color):
    srgb=[int(hex_color[i:i+2],16)/255 for i in (1,3,5)]
    return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in srgb)+(1,)

def grade(mat,input_color,preserve_footings=False):
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;added=[]
    def node(kind):
        n=nodes.new(kind);added.append(n);n.label='Approved warm gray brown palette';return n
    split=node('ShaderNodeSeparateColor');links.new(input_color,split.inputs[0])
    subtract=node('ShaderNodeMath');subtract.operation='SUBTRACT'
    links.new(split.outputs['Red'],subtract.inputs[0]);links.new(split.outputs['Green'],subtract.inputs[1])
    mask=node('ShaderNodeMapRange');mask.inputs['From Min'].default_value=.005;mask.inputs['From Max'].default_value=.070
    links.new(subtract.outputs[0],mask.inputs['Value'])
    palette=node('ShaderNodeValToRGB')
    palette.color_ramp.elements[0].color=rgba('#4B4945')
    palette.color_ramp.elements[1].color=rgba('#5B4B3F')
    palette.color_ramp.elements.new(.55).color=rgba('#58453F')
    links.new(mask.outputs[0],palette.inputs[0])
    luminance=node('ShaderNodeRGBToBW');links.new(input_color,luminance.inputs[0])
    detail=node('ShaderNodeMapRange')
    detail.inputs['From Min'].default_value=.01;detail.inputs['From Max'].default_value=.11
    detail.inputs['To Min'].default_value=.50;detail.inputs['To Max'].default_value=.70
    links.new(luminance.outputs[0],detail.inputs['Value'])
    multiply=node('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1
    links.new(palette.outputs[0],multiply.inputs[1]);links.new(detail.outputs[0],multiply.inputs[2])
    result=multiply.outputs[0]
    if preserve_footings:
        geometry=node('ShaderNodeNewGeometry');xyz=node('ShaderNodeSeparateXYZ');links.new(geometry.outputs['Position'],xyz.inputs[0])
        low=node('ShaderNodeMath');low.operation='LESS_THAN';low.inputs[1].default_value=.245;links.new(xyz.outputs['Z'],low.inputs[0])
        bright=node('ShaderNodeMath');bright.operation='GREATER_THAN';bright.inputs[1].default_value=.105;links.new(luminance.outputs[0],bright.inputs[0])
        both=node('ShaderNodeMath');both.operation='MULTIPLY';links.new(low.outputs[0],both.inputs[0]);links.new(bright.outputs[0],both.inputs[1])
        mix=node('ShaderNodeMixRGB');links.new(both.outputs[0],mix.inputs[0]);links.new(result,mix.inputs[1]);links.new(input_color,mix.inputs[2]);result=mix.outputs[0]
    return result,added

# Keep the editable steel materials consistent with the portable atlas.
for name in ['Tower_Weathered_RedOxide_Steel','Tower_DarkSteel_Hardware']:
    mat=bpy.data.materials[name]
    bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    result,_=grade(mat,bs.inputs['Base Color'].links[0].from_socket)
    mat.node_tree.links.new(result,bs.inputs['Base Color'])

mat=bpy.data.materials['Tajia01_Tower_PBR'];nodes=mat.node_tree.nodes;links=mat.node_tree.links
bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
base=bs.inputs['Base Color'].links[0].from_node
old_image=base.image
result,temporary=grade(mat,base.outputs['Color'],True)
output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL')
old_surface=output.inputs['Surface'].links[0].from_socket
emission=nodes.new('ShaderNodeEmission');links.new(result,emission.inputs['Color']);links.new(emission.outputs[0],output.inputs['Surface'])
image=bpy.data.images.new('Tower_PaletteBake',4096,4096,alpha=False)
image.colorspace_settings.name='sRGB'
image.file_format='PNG'
image.filepath_raw=str(ROOT/'Textures'/'Tower_BaseColor.png')
target=nodes.new('ShaderNodeTexImage');target.image=image;nodes.active=target
bpy.ops.object.select_all(action='DESELECT');tower.select_set(True);bpy.context.view_layer.objects.active=tower
scene.cycles.samples=1
bpy.ops.object.bake(type='EMIT')
image.save()
links.new(old_surface,output.inputs['Surface'])
nodes.remove(target);nodes.remove(emission)
for n in temporary:nodes.remove(n)
old_image.name='Tower_PreviousPalette'
old_image.filepath=str(archive/'Tower_BaseColor.png')
image.name='Tower_BaseColor';base.image=image
image.pack()
scene.cycles.samples=48
scene['palette_revision']='Muted warm gray steel and gray brown rust, approved 2026-09-25'
scene.render.filepath=str(ROOT/'Previews'/'Tower_v01_Overview.png')
bpy.ops.render.render(write_still=True)
report['mesh_after']=mesh_hash()
report['unchanged_maps_after']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'Textures').glob('*.png') if p.name!='Tower_BaseColor.png'}
assert report['mesh_before']==report['mesh_after']
assert report['unchanged_maps_before']==report['unchanged_maps_after']
(ROOT/'palette_revision.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Palette baked and rendered; geometry, UV and surface maps unchanged')
