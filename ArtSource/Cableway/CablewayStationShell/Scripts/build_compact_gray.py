# Revised compact one-side station shell, approved reference controls proportions.
# Execute in the station MCP namespace after the primitive helpers are loaded.
for ob in objects:
    mesh=ob.data;bpy.data.objects.remove(ob,do_unlink=True)
    if mesh.users==0:bpy.data.meshes.remove(mesh)
bpy.data.collections.remove(collection)
collection=bpy.data.collections.new('CablewayStationShell_Compact');SCENE.collection.children.link(collection)
parts={}
steel=part('StructuralSteel','Steel');fast=part('PlatesAndFasteners','Steel');wall=part('CorrugatedWalls','Cladding');roof=part('SingleSlopeRoof','Cladding');floor=part('WalkwayGrating','Steel');base=part('ConcretePlinth','Concrete')
W=3.2;L=10.6;HIGH=4.65;LOW=4.1;bays=[-5.3,-2.65,0,2.65,5.3]
# A low narrow foundation, not the previous wide station platform.
for side in [-1,1]:
    for j in range(6):box(base,(side*1.5,-4.6+j*1.84,.09),(.62,1.82,.42))
for y in [-5.4,5.4]:box(base,(0,y,.07),(3.63,.52,.38))
for y in np.arange(-5.05,5.2,.85):
    for x in np.arange(-1.35,1.40,.135):box(floor,(x,y,.32),(.026,.81,.04))
    for yy in [-.39,.39]:box(floor,(0,y+yy,.29),(2.83,.025,.07))
for side in [-1,1]:beam(steel,(side*1.45,-5.3,.28),(side*1.45,5.3,.28),.13,.18)
for y in bays:
    beam(steel,(-1.55,y,.30),(-1.55,y,HIGH),.23,.29)
    beam(steel,(1.55,y,.30),(1.55,y,LOW),.23,.29)
    beam(steel,(-1.7,y,HIGH),(1.8,y,LOW-.035),.18,.24)
    for side in [-1,1]:
        h=HIGH if side==-1 else LOW
        box(fast,(side*1.55,y,.34),(.48,.48,.065))
        for dx in [-.17,.17]:
            for dy in [-.17,.17]:cylinder(fast,(side*1.55+dx,y+dy,.4),.036,.065,sides=6)
        box(fast,(side*1.55,y-.17,h-.18),(.40,.04,.60));bolts((side*1.55,y-.20,h-.18),rows=2)
    beam(steel,(-1.55,y,HIGH-.75),(-.75,y,HIGH-.1),.095,.12)
    beam(steel,(1.55,y,LOW-.70),(.85,y,LOW+.1),.095,.12)
for x in [-1.65,-.75,.15,1.05,1.8]:
    z=HIGH+(x+1.55)/(3.1)*(LOW-HIGH)
    beam(steel,(x,-5.55,z),(x,5.55,z),.075,.11)
# Longitudinal bracing is limited to the clad side so the other side stays open.
for j in [0,3]:beam(steel,(1.50,bays[j],.65),(1.50,bays[j+1],3.65),.075,.10)
for z in [1.05,3.30,3.93]:
    beam(steel,(1.64,-4.9,z),(1.64,5.25,z),.075,.14)
    for y in bays[1:]:cylinder(fast,(1.72,y,z),.035,.03,(1,0,0),6)
# Only one long wall, with a small setback from the open end frame.
for i in range(13):
    y=-4.9+i*.775
    if i==8: # localized missing upper piece rather than a full doorway
        sheet(wall,(1.64,y,.48),(0,1,0),(0,0,1),.805,1.15,.3,i*.23)
    else:sheet(wall,(1.64,y,.48),(0,1,0),(0,0,1),.805,3.43,.1 if i not in [3,9] else .5,i*.23)
# One continuous roof plane falling from open side toward clad side. No ridge or second slope.
u=Vector((0,1,0));v=Vector((3.64,0,-.645));roofDepth=v.length;v.normalize()
for i in range(14):
    if i==9:continue
    sheet(roof,(-1.80,-5.55+i*.80,HIGH+.12),u,v,.84,roofDepth if i!=3 else roofDepth*.78,.30 if i in [2,3,8,10] else .06,i*.173)
# Open side has no railing or waist-high infill.
objects=[]
import bmesh
for name,d in parts.items():
    mesh=bpy.data.meshes.new('StationCompact_'+name+'_Mesh');mesh.from_pydata(d['v'],[],d['f']);mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for polygon,values in zip(mesh.polygons,d['uv']):
        for li,val in zip(polygon.loop_indices,values):uv.data[li].uv=val
    ob=bpy.data.objects.new(name,mesh);collection.objects.link(ob);mesh.materials.append(materials[d['kind']]);objects.append(ob)
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free();mesh.update()
SCENE['model_units']='metres'
SCENE['design_status']='Compact gray model for silhouette approval, not a final asset'
SCENE['approved_reference']='References/Approved_CompactLeanTo.png'
SCENE['enclosure']='One clad long side, open opposite side and both ends; one narrow single-pitch roof plane'
SCENE['provisional_dimensions']='Approximately 11.5 m long, 3.7 m deep and 4.9 m high including protrusions'
gray=bpy.data.materials.get('Station_GrayReview') or bpy.data.materials.new('Station_GrayReview');gray.use_nodes=True
bs=next(n for n in gray.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.36,.35,.33,1);bs.inputs['Roughness'].default_value=.85
SCENE.view_layers[0].material_override=gray
camera.location=(13,-18,8);camera.rotation_euler=(Vector((0,0,2.0))-camera.location).to_track_quat('-Z','Y').to_euler();enum(cam,'type','ORTHO');cam.ortho_scale=15
SCENE.render.resolution_x=1300;SCENE.render.resolution_y=950;SCENE.cycles.samples=24
SCENE.render.filepath=str(ROOT/'Previews/Compact_Gray_CladSide.png')
bpy.ops.render.render(write_still=True)
camera.location=(-13,-18,8);camera.rotation_euler=(Vector((0,0,2.0))-camera.location).to_track_quat('-Z','Y').to_euler()
SCENE.render.filepath=str(ROOT/'Previews/Compact_Gray_OpenSide.png');bpy.ops.render.render(write_still=True)
bpy.data.libraries.write(str(ROOT/'Blender/CablewayStationShell_Compact_Gray.blend'),{SCENE},fake_user=True,compress=True)
print('Compact single-pitch gray model saved')
