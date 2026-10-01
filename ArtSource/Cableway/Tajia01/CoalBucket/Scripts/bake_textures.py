"""Bake the procedural materials to an atlas without lighting in base color."""
import bpy
from pathlib import Path
import traceback

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Project Coal\ArtSource\Cableway\Tajia01\CoalBucket')

def bake_channel(channel):
    scene=bpy.context.scene
    tower=bpy.data.objects['Tajia01_CoalBucket']
    bpy.ops.object.select_all(action='DESELECT')
    tower.select_set(True)
    bpy.context.view_layer.objects.active=tower
    image=bpy.data.images.new('CoalBucket_'+channel,4096,4096,alpha=False)
    colors={i.identifier for i in image.colorspace_settings.bl_rna.properties['name'].enum_items}
    image.colorspace_settings.name=next(c for c in colors if c==('sRGB' if channel=='BaseColor' else 'Non-Color'))
    image.filepath_raw=str(ROOT/'Textures'/('CoalBucket_'+channel+'.png'))
    fmts={i.identifier for i in image.bl_rna.properties['file_format'].enum_items}
    image.file_format=next(f for f in fmts if f=='PNG')
    saved=[]
    try:
        for mat in tower.data.materials:
            nodes=mat.node_tree.nodes;links=mat.node_tree.links
            bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
            output=next(n for n in nodes if n.type=='OUTPUT_MATERIAL')
            target=nodes.new('ShaderNodeTexImage');target.image=image
            nodes.active=target
            original=output.inputs['Surface'].links[0].from_socket
            emit=None
            if channel in ('BaseColor','Metallic','Roughness'):
                emit=nodes.new('ShaderNodeEmission')
                socket=bs.inputs[{'BaseColor':'Base Color','Metallic':'Metallic','Roughness':'Roughness'}[channel]]
                if socket.is_linked:
                    links.new(socket.links[0].from_socket,emit.inputs['Color'])
                else:
                    v=socket.default_value
                    emit.inputs['Color'].default_value=tuple(v) if channel=='BaseColor' else (v,v,v,1)
                links.new(emit.outputs[0],output.inputs['Surface'])
            saved.append((mat,output,original,target,emit))
        mode='NORMAL' if channel=='Normal' else ('AO' if channel=='AO' else 'EMIT')
        modes={i.identifier for i in bpy.ops.object.bake.get_rna_type().properties['type'].enum_items}
        scene.cycles.samples=32 if channel in ('Normal','AO') else 1
        bpy.ops.object.bake(type=next(v for v in modes if v==mode))
        image.save()
        (ROOT/'Blender'/('bake_'+channel+'_status.txt')).write_text('complete',encoding='utf-8')
    except Exception:
        (ROOT/'Blender'/('bake_'+channel+'_status.txt')).write_text(traceback.format_exc(),encoding='utf-8')
    finally:
        for mat,output,original,target,emit in saved:
            mat.node_tree.links.new(original,output.inputs['Surface'])
            mat.node_tree.nodes.remove(target)
            if emit is not None:
                mat.node_tree.nodes.remove(emit)
    return None
