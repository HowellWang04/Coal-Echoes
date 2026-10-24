"""Run the checked atlas workflow for the independent coal bucket."""
import bpy
import runpy
from pathlib import Path

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia01\CoalBucket')
status=ROOT/'Blender'/'pipeline_status.txt'
status.write_text('preparing mesh and UV',encoding='utf-8')
runpy.run_path(str(ROOT/'Scripts'/'prepare_export.py'))
bake=runpy.run_path(str(ROOT/'Scripts'/'bake_textures.py'))['bake_channel']
for channel in ['BaseColor','Metallic','Roughness','Normal','AO']:
    status.write_text('baking '+channel,encoding='utf-8')
    bake(channel)
    result=(ROOT/'Blender'/('bake_'+channel+'_status.txt')).read_text(encoding='utf-8')
    if result.strip()!='complete':raise RuntimeError(result)
status.write_text('assembling PBR material',encoding='utf-8')
runpy.run_path(str(ROOT/'Scripts'/'assemble_pbr.py'))
status.write_text('rendering portable material',encoding='utf-8')
bpy.ops.render.render(write_still=True)
status.write_text('complete',encoding='utf-8')
