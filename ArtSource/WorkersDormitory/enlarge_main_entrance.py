"""Replace only the ground-floor central doorway and its two flanking windows."""
from pathlib import Path
import hashlib
import json
import runpy
import bpy
import bmesh

SOURCE=Path(__file__).resolve().parent
code=(SOURCE/'build_dormitory.py').read_text(encoding='utf-8').split("\nscene=bpy.data.scenes.new")[0]
ns={'__file__':str(SOURCE/'build_dormitory.py')}
exec(compile(code,str(SOURCE/'build_dormitory.py'),'exec'),ns)
scene=bpy.data.scenes['WorkersDormitory']
bpy.context.window.scene=scene
asset=bpy.data.collections['WorkersDormitory_Asset']
root=bpy.data.objects['WorkersDormitory']
ns.update(asset=asset,root=root,materials={n:bpy.data.materials['Dorm_'+n] for n in ('BuffBrick','Concrete','FrameBrown','FrameGreen','FrameGrey','Glass2')})

def fingerprint(obj):
    digest=hashlib.sha256()
    for v in obj.data.vertices:digest.update(str(tuple(v.co)).encode())
    for p in obj.data.polygons:digest.update(str(tuple(p.vertices)).encode())
    return digest.hexdigest()

before={o.name:fingerprint(o) for o in asset.objects if o.type=='MESH'}
changed=[]
groups=('Windows__','Sills__','Lintels__','RoomPockets__','EntranceSteps__','EntranceSurround__','Canopies__','Doors__','DoorPanels__','DoorHardware__')
for obj in list(asset.objects):
    if obj.type!='MESH' or not obj.name.startswith(groups):continue
    bm=bmesh.new()
    bm.from_mesh(obj.data)
    unseen=set(bm.verts)
    remove=[]
    while unseen:
        seed=unseen.pop()
        component={seed}
        todo=[seed]
        while todo:
            for edge in todo.pop().link_edges:
                other=edge.other_vert(next(v for v in edge.verts if v in component))
                if other in unseen:
                    unseen.remove(other);component.add(other);todo.append(other)
        if all(abs(v.co.x)<2.10 and v.co.y < -3.20 and v.co.z<3.20 for v in component):
            remove.extend(component)
    if remove:
        # BMesh uses its own documented VERTS context.
        bmesh.ops.delete(bm,geom=remove,context='VERTS')
        bm.to_mesh(obj.data)
        changed.append(obj.name)
    bm.free()

holes=[]
for floor in range(3):
    base=.45+floor*3
    for x in (-12,-4,4,12):holes.append((x-1.3,x+1.3,base+.18,base+2.66))
    for x in (-15,-9.25,-6.75,-1.25,1.25,6.75,9.25,15):
        if floor==0 and abs(x)==1.25:continue
        holes.append((x-.59,x+.59,base+.86,base+2.4))
    if floor==0:
        holes.extend([(-8.58,-7.42,.42,2.76),(-1.49,1.49,.42,3.07),(7.42,8.58,.42,2.76)])
old=asset.objects['FrontMasonry__BuffBrick']
bpy.data.objects.remove(old,do_unlink=True)
changed.append('FrontMasonry__BuffBrick')
ns['wall']('FrontMasonry','X',-4.8,-16,16,holes)
ns['main_entrance']()
for (group,mat),mesh in ns['batches'].items():
    mesh.make(group+'__'+mat,mat,asset,root,.008 if mat in ('Concrete','FrameBrown','FrameGrey','FrameGreen') else 0)

unchanged=[name for name,value in before.items() if name not in changed and name!='SillRunoff' and fingerprint(asset.objects[name])==value]
assert len(unchanged)==len(before)-len(changed)-1
report={'changed_existing_meshes':changed,'unchanged_existing_mesh_count':len(unchanged),'door_opening_width_m':2.98,'door_opening_height_m':2.65,'removed_ground_floor_window_centers_x_m':[-1.25,1.25]}
(SOURCE/'entrance_revision.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
runpy.run_path(str(SOURCE/'finish_dormitory.py'),run_name='__main__')
print('ENTRANCE_REVISION',json.dumps(report))
