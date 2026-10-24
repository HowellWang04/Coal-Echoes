"""Align an inspected original worn-metal strip along individual steel members."""
import bpy
import numpy as np
import json
from pathlib import Path

ROOT=Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia01')
SIZE=globals().get('REFINE_SIZE',4096)
scene=bpy.data.scenes['Tajia01_Tower_Rebuilt'];bpy.context.window.scene=scene
tower=bpy.data.objects['Tajia01_Tower'];mesh=tower.data;portable=mesh.materials[0]
parent=list(range(len(mesh.vertices)))
def root(i):
    while parent[i]!=i:
        parent[i]=parent[parent[i]];i=parent[i]
    return i
for e in mesh.edges:
    a,b=map(root,e.vertices);parent[b]=a
groups={}
for v in mesh.vertices:groups.setdefault(root(v.index),[]).append(v.index)
points=np.array([v.co[:] for v in mesh.vertices])
frames={}
for i,(key,ids) in enumerate(groups.items()):
    p=points[ids];center=p.mean(0);_,_,basis=np.linalg.svd(p-center,full_matrices=False)
    extent=np.ptp(p,axis=0)
    keep=p[:,2].max()<.245
    frames[key]=(center,basis,keep,(i*.61803398875)%1)
attr=mesh.attributes.new('LegacyMemberCoordinates','FLOAT_VECTOR','CORNER')
coordinates=np.zeros((len(mesh.loops),3),np.float32)
for poly in mesh.polygons:
    ids=[mesh.loops[l].vertex_index for l in poly.loop_indices]
    center,basis,keep,phase=frames[root(ids[0])]
    normal=np.array(poly.normal)
    cross=basis[1] if abs(normal@basis[1])<abs(normal@basis[2]) else basis[2]
    if abs(normal@basis[0])>.9:
        along=basis[1];cross=basis[2]
    else:along=basis[0]
    p=points[ids]-center
    coordinates[list(poly.loop_indices),0]=p@cross/.20+phase
    coordinates[list(poly.loop_indices),1]=p@along/1.6+phase*3
    coordinates[list(poly.loop_indices),2]=float(keep)
attr.data.foreach_set('vector',coordinates.ravel())
mat=bpy.data.materials.new('LegacyMemberBake_Temporary');n=mat.node_tree.nodes;l=mat.node_tree.links
out=next(v for v in n if v.type=='OUTPUT_MATERIAL')
def node(t):return n.new(t)
def math(op,a=None,b=None):
    v=node('ShaderNodeMath')
    assert op in {e.identifier for e in v.bl_rna.properties['operation'].enum_items}
    v.operation=op
    if a is not None:l.new(a,v.inputs[0])
    if b is not None:v.inputs[1].default_value=b
    return v.outputs[0]
attribute=node('ShaderNodeAttribute');attribute.attribute_name=attr.name
sep=node('ShaderNodeSeparateXYZ');l.new(attribute.outputs['Vector'],sep.inputs[0])
combine=node('ShaderNodeCombineXYZ')
# Sample inside one continuous, inspected old column strip, without atlas edges.
u=math('ADD',math('MULTIPLY',math('PINGPONG',sep.outputs['X'],1),20/1024),261/1024)
v=math('ADD',math('MULTIPLY',math('PINGPONG',sep.outputs['Y'],1),112/1024),1-182/1024)
l.new(u,combine.inputs[0]);l.new(v,combine.inputs[1])
legacy=node('ShaderNodeTexImage')
legacy.image=bpy.data.images.load(str(ROOT.parents[2]/'Assets/Art Resources/Environment/Cableway/texture/texture_pbr_20250901.002 1.png'),check_existing=True)
l.new(combine.outputs[0],legacy.inputs['Vector'])
tone=node('ShaderNodeMixRGB');tone.blend_type='MIX'
weather=node('ShaderNodeTexNoise');weather.inputs['Scale'].default_value=1.4;weather.inputs['Detail'].default_value=2
l.new(attribute.outputs['Vector'],weather.inputs['Vector'])
weight=math('ADD',math('MULTIPLY',weather.outputs['Fac'],.70),.08)
l.new(weight,tone.inputs[0])
l.new(legacy.outputs['Color'],tone.inputs[1])
# Subtle warm brown retained from the old material, without bright silver.
tone.inputs[2].default_value=(.115,.095,.063,1)
old=node('ShaderNodeTexImage');old.image=bpy.data.images.load(str(ROOT/'Textures/Tower_LegacyTransfer_4096.png'),check_existing=True)
mix=node('ShaderNodeMixRGB');mix.blend_type='MIX'
l.new(sep.outputs['Z'],mix.inputs[0]);l.new(tone.outputs[0],mix.inputs[1]);l.new(old.outputs[0],mix.inputs[2])
emit=node('ShaderNodeEmission');l.new(mix.outputs[0],emit.inputs[0]);l.new(emit.outputs[0],out.inputs['Surface'])
image=bpy.data.images.new('Tower_MemberRefined_'+str(SIZE),SIZE,SIZE,alpha=False);image.colorspace_settings.name='sRGB'
target=node('ShaderNodeTexImage');target.image=image;n.active=target
mesh.materials[0]=mat
bpy.ops.object.select_all(action='DESELECT');tower.select_set(True);bpy.context.view_layer.objects.active=tower
scene.cycles.samples=1
try:bpy.ops.object.bake(type='EMIT')
finally:
    mesh.materials[0]=portable;mesh.attributes.remove(attr);bpy.data.materials.remove(mat)
image.filepath_raw=str(ROOT/'Textures'/('Tower_MemberRefined_'+str(SIZE)+'.png'));image.file_format='PNG';image.save();image.pack()
bs=next(x for x in portable.node_tree.nodes if x.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].links[0].from_node.image=image
report=json.loads((ROOT/'legacy_surface_transfer.json').read_text());report['member_refinement']='Steel uses longitudinal mapping within an inspected original column texture strip, with broad warm-brown weathering variation. Concrete preserves its baseline. This supersedes closest-surface projection to avoid stretching between structural members.'
report['original_atlas_region_pixels']=[1044,280,1124,728]
(ROOT/'legacy_surface_transfer.json').write_text(json.dumps(report,indent=2))
scene.cycles.samples=32
scene.render.filepath=str(ROOT/'Previews/Tower_MemberRefined_Overview.png');bpy.ops.render.render(write_still=True)
