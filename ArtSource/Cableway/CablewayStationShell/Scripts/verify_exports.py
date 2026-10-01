"""Reimport both formats and compare geometry and texture references."""
import bpy, json

ns=bpy.app.driver_namespace['station_build']
source=ns['SCENE'];root=ns['ROOT'];report=ns['final_report']
report['roundtrip']={}
for extension in ['fbx','glb']:
    before=set(bpy.data.objects)
    temporary=bpy.data.scenes.new('StationExportVerification')
    bpy.context.window.scene=temporary
    path=str(root/('Exports/CablewayStationShell.'+extension))
    if extension=='fbx':bpy.ops.import_scene.fbx(filepath=path)
    else:bpy.ops.import_scene.gltf(filepath=path)
    created=set(bpy.data.objects)-before
    meshes=[o for o in created if o.type=='MESH']
    result=ns['inspect_meshes'](meshes)
    result['dimension_error_m']=max(abs(a-b) for a,b in zip(result['dimensions_xyz_m'],report['source']['dimensions_xyz_m']))
    result['texture_references']=[]
    for material in {m for o in meshes for m in o.data.materials}:
        images=[n.image for n in material.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
        result['texture_references'].append({'material':material.name,'images':[{'name':im.name,'size':list(im.size),'loaded':im.has_data} for im in images]})
    result['passed']=result['mesh_count']==6 and result['total_triangles']==report['source']['total_triangles'] and result['dimension_error_m']<.0001 and all(o['uv_layers']>=1 and o['zero_area_faces']==0 for o in result['objects']) and all(len(m['images'])>=3 and all(im['loaded'] for im in m['images']) for m in result['texture_references'])
    # Imported vertices split at UV and normal seams, so raw edge manifold
    # counts are recorded but are not used as a source topology test.
    report['roundtrip'][extension]=result
    bpy.context.window.scene=source
    if extension=='glb':
        for ob in ns['objects']:ob.hide_render=True
        try:
            for ob in meshes:source.collection.objects.link(ob)
            source.render.filepath=str(root/'Previews/Station_GLB_Roundtrip.png')
            bpy.ops.render.render(write_still=True)
        finally:
            for ob in ns['objects']:ob.hide_render=False
    for ob in created:bpy.data.objects.remove(ob,do_unlink=True)
    bpy.data.scenes.remove(temporary)
(root/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({key:{'passed':value['passed'],'mesh_count':value['mesh_count'],'triangles':value['total_triangles'],'dimension_error_m':value['dimension_error_m']} for key,value in report['roundtrip'].items()},indent=2))
