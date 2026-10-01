"""Publish transferred albedo and make surface maps follow the original rust."""
import bpy
import json
import shutil
import hashlib
import numpy as np
from pathlib import Path

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia01')
scene=bpy.data.scenes['Tajia01_Tower_Rebuilt']
bpy.context.window.scene=scene
tower=bpy.data.objects['Tajia01_Tower']
mat=tower.data.materials[0]
bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
base=bs.inputs['Base Color'].links[0].from_node.image
assert base.size[0]==4096
base.filepath_raw=str(ROOT/'Textures'/'Tower_BaseColor.png')
base.save();base.pack()
old=bpy.data.images.get('Tower_BaseColor')
if old is not None and old!=base:old.name='Tower_PreLegacyTransfer_BaseColor'
base.name='Tower_BaseColor'

def pixels(image):
    a=np.empty(len(image.pixels),np.float32)
    image.pixels.foreach_get(a)
    return a.reshape(-1,4)

color=pixels(base)
rust=np.clip(((color[:,0]-color[:,2])/(color[:,0]+.0001)-.25)/.30,0,1)
metal=.76*(1-rust)+.10*rust
rough=.86*(1-rust)+.97*rust
original_metal=bpy.data.images.load(str(ROOT/'Previous'/'Before_Corrosion_Detail'/'Textures'/'Tower_Metallic.png'),check_existing=False)
original_metal.colorspace_settings.name='Non-Color'
concrete=pixels(original_metal)[:,0]<.005
metal[concrete]=0
rust[concrete]=0
ao=pixels(bpy.data.images['Tower_AO'])[:,0]
data=np.ones_like(color)
maps={}
for channel in ['Metallic','Roughness','CorrosionMask','HDRP_Mask','OcclusionRoughnessMetallic']:
    data[:]=1
    if channel in ['Metallic','Roughness','CorrosionMask']:
        value={'Metallic':metal,'Roughness':rough,'CorrosionMask':rust}[channel]
        data[:,:3]=value[:,None]
    elif channel=='HDRP_Mask':
        data[:,0]=metal;data[:,1]=ao;data[:,2]=1;data[:,3]=1-rough
    else:
        data[:,0]=ao;data[:,1]=rough;data[:,2]=metal
    image=bpy.data.images.new('LegacySurface_'+channel,4096,4096,alpha=channel=='HDRP_Mask')
    image.colorspace_settings.name='Non-Color'
    image.alpha_mode='CHANNEL_PACKED';image.file_format='PNG'
    image.pixels.foreach_set(data.ravel())
    image.filepath_raw=str(ROOT/'Textures'/('Tower_'+channel+'.png'))
    image.save();image.pack()
    previous=bpy.data.images.get('Tower_'+channel)
    if previous is not None:previous.name='PreLegacySurface_'+channel
    image.name='Tower_'+channel
    maps[channel]=image

# Use the subtle original micro-normal rather than the rejected speckle relief.
normal_path=ROOT/'Textures'/'Tower_Normal.png'
shutil.copy2(ROOT/'Previous'/'Before_Corrosion_Detail'/'Textures'/'Tower_Normal.png',normal_path)
normal=bpy.data.images.load(str(normal_path),check_existing=False)
normal.colorspace_settings.name='Non-Color';normal.pack()
old=bpy.data.images.get('Tower_Normal')
if old is not None and old!=normal:old.name='Tower_PreLegacySurface_Normal'
normal.name='Tower_Normal';maps['Normal']=normal
for n in mat.node_tree.nodes:
    if n.type=='TEX_IMAGE' and n.image:
        stem=Path(n.image.filepath).stem.removeprefix('Tower_')
        if stem in maps:n.image=maps[stem]
    if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.45
scene['normal_strength']=.45
scene['palette_revision']='Original tajia03 surface transferred to new tower UV atlas'
scene['corrosion_revision']='Original clustered rust and paint wear; procedural speckle layer superseded'
scene.cycles.samples=48
scene.render.filepath=str(ROOT/'Previews'/'Tower_v01_Overview.png')
bpy.ops.render.render(write_still=True)
report=json.loads((ROOT/'legacy_surface_transfer.json').read_text())
report['maps_sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'Textures').glob('Tower_*.png') if 'LegacyTransfer' not in p.name}
report['normal_strength']=.45
report['uv_and_geometry_modified']=False
(ROOT/'legacy_surface_transfer.json').write_text(json.dumps(report,indent=2))
