"""Per-tower leg extensions (unequal legs) and ladder extensions for sloped sites.

Exec in the same namespace right after build_tower.py so its helpers and materials exist.
Reads leg_extensions.json (measured in Unity from the terrain): for each tower the extension
length L of each leg below the body base (z = 0) and of the ladder. Each leg continues its
own taper down to a sole plate on a concrete pier whose top is 0.4 m above the ground.
Towers are built side by side (offset along X) so they bake into one atlas; split_and_export()
separates them again and moves each back to the tower origin.
"""
import bpy
import bmesh
import json
from pathlib import Path

EXT_ROOT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Cableway\Tajia02_Metric15')
DATA = json.loads((EXT_ROOT / 'leg_extensions.json').read_text(encoding='utf-8-sig'))
# Towers built with this body geometry; the entry tower has its own body and is built in a separate pass.
DATA = [t for t in DATA if t['name'] in globals().get('EXT_NAMES', [t['name'] for t in DATA])]
EXT_SCENE = globals().get('EXT_SCENE', 'Tajia02_LegExt')
EXT_FBX = globals().get('EXT_FBX', 'CablewayTower_LegExtensions')
KX = (HALF_BASE[0] - HALF_TOP[0]) / (SHAFT_TOP_Z - SHAFT_BASE_Z)
KY = (HALF_BASE[1] - HALF_TOP[1]) / (SHAFT_TOP_Z - SHAFT_BASE_Z)
SPACING = 16.0

ext_scene = bpy.data.scenes.new(EXT_SCENE)
bpy.context.window.scene = ext_scene
parts = bpy.data.collections.new('LegExt_Parts')
ext_scene.collection.children.link(parts)


def leg_foot(sx, sy, L):
    return (sx * (HALF_BASE[0] + KX * L), sy * (HALF_BASE[1] + KY * L), -L)


for index, tower in enumerate(DATA):
    ox = index * SPACING
    for leg in tower['legs']:
        sx, sy, L = leg['sx'], leg['sy'], leg['L']
        fx, fy, fz = leg_foot(sx, sy, L)
        fx += ox
        if L > .03:
            angle('Leg extension angle', (fx, fy, fz + .025), (ox + sx * HALF_BASE[0], sy * HALF_BASE[1], SHAFT_BASE_Z), LEG_W, LEG_T, (-sx, -sy))
            # Splice plates bolted over the joint with the body leg.
            box('Leg splice plate', (ox + sx * (HALF_BASE[0] + .075), sy * HALF_BASE[1], .02), (.012, .15, .42), paint, .002)
            box('Leg splice plate', (ox + sx * HALF_BASE[0], sy * (HALF_BASE[1] + .075), .02), (.15, .012, .42), paint, .002)
            for dz in (-.13, 0, .13):
                bolt((ox + sx * (HALF_BASE[0] + .081), sy * HALF_BASE[1] - sy * .03, .02 + dz), (sx, 0, 0))
                bolt((ox + sx * HALF_BASE[0] - sx * .03, sy * (HALF_BASE[1] + .081), .02 + dz), (0, sy, 0))
        if L > .6:
            a = (fx, fy, fz + .35)
            angle('Extension brace', a, (ox, sy * (HALF_BASE[1] - .02), .05), .063, .005, (1, 1), bare)
            angle('Extension brace', a, (ox + sx * (HALF_BASE[0] - .02), 0, .05), .063, .005, (1, 1), bare)
        box('Column sole plate', (fx, fy, fz + .0125), (.50, .50, .025), bare, .003)
        for bx in (-.17, .17):
            for by in (-.17, .17):
                bolt((fx + bx, fy + by, fz + .025), (0, 0, 1), .030)
    # Ladder stiles and rungs continue down at the same pitch.
    lL = tower['ladder_L']
    if lL > .03:
        for x in (-RAIL_X, RAIL_X):
            box('Ladder stile extension', (ox + x + (.005 if x > 0 else -.005), LADDER_Y, -lL / 2 + .05), (.010, .060, lL + .10), bare)
        z = .35 - pitch
        while z > -lL + .20:
            rod('Ladder rung', (ox - RAIL_X, LADDER_Y, z), (ox + RAIL_X, LADDER_Y, z), .011, 8, bare)
            z -= pitch
    for x in (-RAIL_X, RAIL_X):
        box('Ladder foot plate', (ox + x, LADDER_Y, -lL + .005), (.12, .12, .010), bare)

print('LEG EXTENSIONS', len(DATA), 'towers', len(parts.objects), 'parts')


def split_and_export():
    joined = bpy.data.objects[globals().get('BAKE_OBJECT', 'LegExtensions')]
    mesh = joined.data
    out = bpy.data.collections.new('LegExt_Export')
    ext_scene.collection.children.link(out)
    made = []
    for index, tower in enumerate(DATA):
        bm = bmesh.new()
        bm.from_mesh(mesh)
        # Split by position: bevel faces do not reliably inherit the tower_id attribute.
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if abs(f.calc_center_median().x - index * SPACING) > SPACING / 2], context='FACES')
        bmesh.ops.translate(bm, verts=bm.verts, vec=(-index * SPACING, 0, 0))
        me = bpy.data.meshes.new('LegExt_' + tower['name'])
        bm.to_mesh(me)
        bm.free()
        ob = bpy.data.objects.new('LegExt_' + tower['name'], me)
        out.objects.link(ob)
        made.append(ob)
    joined.hide_render = True
    joined.hide_viewport = True
    bpy.ops.object.select_all(action='DESELECT')
    for ob in made:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = made[0]
    bpy.ops.export_scene.fbx(filepath=str(EXT_ROOT / 'Exports' / (EXT_FBX + '.fbx')), use_selection=True, object_types={'MESH'},
                             use_triangles=True, mesh_smooth_type='OFF', use_tspace=True, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, path_mode='STRIP', bake_anim=False)
    for image in bpy.data.images:
        if image.name.startswith(globals().get('BAKE_PREFIX', 'LegExt') + '_') and image.size[0] > 0 and image.filepath:
            image.pack()
    bpy.data.libraries.write(str(EXT_ROOT / 'Blender' / (EXT_SCENE + '.blend')), {ext_scene}, fake_user=True, compress=True)
    print('EXPORTED', [(o.name, len(o.data.polygons)) for o in made])
