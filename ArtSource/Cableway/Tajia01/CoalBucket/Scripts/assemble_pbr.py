"""Pack HDRP and glTF surface maps and attach a portable atlas material."""
import bpy
import numpy as np
from pathlib import Path
from io_scene_gltf2.blender.com.material_helpers import create_settings_group

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia01\CoalBucket')
SIZE=4096
def read_red(name):
    pixels=np.empty(SIZE*SIZE*4,dtype=np.float32)
    bpy.data.images['CoalBucket_'+name].pixels.foreach_get(pixels)
    return pixels.reshape(-1,4)[:,0].copy()

def save_image(name,pixels,alpha):
    image=bpy.data.images.new(name,SIZE,SIZE,alpha=alpha)
    image.colorspace_settings.name='Non-Color'
    image.alpha_mode='CHANNEL_PACKED'
    image.pixels.foreach_set(pixels.reshape(-1))
    image.filepath_raw=str(ROOT/'Textures'/(name+'.png'))
    image.file_format='PNG'
    image.save()
    return image

metal=read_red('Metallic');rough=read_red('Roughness');ao=read_red('AO')
pixels=np.ones((SIZE*SIZE,4),dtype=np.float32)
pixels[:,0]=metal;pixels[:,1]=ao;pixels[:,2]=1;pixels[:,3]=1-rough
mask=save_image('CoalBucket_HDRP_Mask',pixels,True)
pixels[:,0]=ao;pixels[:,1]=rough;pixels[:,2]=metal;pixels[:,3]=1
orm=save_image('CoalBucket_OcclusionRoughnessMetallic',pixels,False)
del pixels,metal,rough,ao

mat=bpy.data.materials.new('Tajia01_CoalBucket_PBR')
nodes=mat.node_tree.nodes;links=mat.node_tree.links
bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
bs.location=(400,0)
out=next(n for n in nodes if n.type=='OUTPUT_MATERIAL');out.location=(710,0)
def tex(image,loc,label):
    n=nodes.new('ShaderNodeTexImage');n.image=image;n.location=loc;n.label=label
    return n
base=tex(bpy.data.images['CoalBucket_BaseColor'],(-650,350),'sRGB base color, no lighting')
normal=tex(bpy.data.images['CoalBucket_Normal'],(-650,-200),'Tangent space normal')
surface=tex(orm,(-650,50),'R occlusion, G roughness, B metallic')
split=nodes.new('ShaderNodeSeparateColor');split.location=(-280,50)
links.new(surface.outputs['Color'],split.inputs[0])
links.new(base.outputs['Color'],bs.inputs['Base Color'])
links.new(split.outputs['Green'],bs.inputs['Roughness'])
links.new(split.outputs['Blue'],bs.inputs['Metallic'])
nm=nodes.new('ShaderNodeNormalMap');nm.location=(-150,-200)
nm.inputs['Strength'].default_value=.45
links.new(normal.outputs['Color'],nm.inputs['Color']);links.new(nm.outputs['Normal'],bs.inputs['Normal'])
group=create_settings_group('glTF Material Output')
node=nodes.new('ShaderNodeGroup');node.node_tree=group;node.location=(120,-400)
links.new(split.outputs['Red'],node.inputs['Occlusion'])
tower=bpy.data.objects['Tajia01_CoalBucket']
tower.data.materials.clear();tower.data.materials.append(mat)
for p in tower.data.polygons:p.material_index=0
mat.diffuse_color=(.055,.059,.057,1)
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=48
scene.cycles.use_denoising=True
scene.render.filepath=str(ROOT/'Previews'/'CoalBucket_v01_Overview.png')
print('Packed maps created and portable single material assigned')
