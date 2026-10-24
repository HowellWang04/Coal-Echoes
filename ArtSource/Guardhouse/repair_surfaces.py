"""Apply the grey cement finish after build_guardhouse.

Run in the open source Blender scene, then run export_guardhouse.py.
"""
import ast
from pathlib import Path
import bpy
import numpy as np

SOURCE = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes\ArtSource\Guardhouse')
TEXTURES = SOURCE.parents[1] / 'Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/Textures'
tree = ast.parse((SOURCE / 'build_guardhouse.py').read_text(encoding='utf-8'))
helpers = ast.Module(body=[n for n in tree.body if isinstance(n, ast.FunctionDef)
                          and n.name in ('save_image', 'textured')], type_ignores=[])
exec(compile(helpers, str(SOURCE / 'build_guardhouse.py'), 'exec'))

def periodic_noise(size, cells, seed):
    grid = np.random.default_rng(seed).random((cells, cells))
    t = np.arange(size) * cells / size
    index = t.astype(int)
    blend = t - index
    blend = blend * blend * (3 - 2 * blend)
    a = grid[index[:, None], index] * (1 - blend) + grid[index[:, None], (index + 1) % cells] * blend
    b = grid[(index[:, None] + 1) % cells, index] * (1 - blend) + grid[(index[:, None] + 1) % cells, (index + 1) % cells] * blend
    return a * (1 - blend[:, None]) + b * blend[:, None]

cement = bpy.data.materials.get('GH_GreyWhiteCement')
if cement is None:
    broad = periodic_noise(1024, 8, 97)
    medium = periodic_noise(1024, 48, 98)
    grain = periodic_noise(1024, 320, 99)
    tone = (broad - .5) * .055 + (medium - .5) * .035 + (grain - .5) * .025
    rgb = np.array([.43, .42, .39]) + tone[:, :, None]
    cement = textured('GH_GreyWhiteCement', rgb, medium * .04 + grain * .07, rough=.92, normal_strength=1.2)

objects = [o for o in bpy.data.collections['Guardhouse | game meshes'].objects if o.type == 'MESH']
walls = [o for o in objects if o.name.startswith(('FrontWall_', 'RearWall_', 'LeftWall_', 'RightWall_'))]
plinths = [o for o in objects if 'Plinth' in o.name]

# The former 27 cm deep plinth met the interior wall at the exact same plane.
# Only the exterior 4 cm render strip is needed. Side strips join behind the
# front and rear strips, with their end caps embedded in the wall thickness.
for obj in plinths:
    coords = [v.co.copy() for v in obj.data.vertices]
    low = [min(v[i] for v in coords) for i in range(3)]
    high = [max(v[i] for v in coords) for i in range(3)]
    target_low, target_high = low.copy(), high.copy()
    if obj.name.startswith('SideCement'):
        sign = 1 if low[0] > 0 else -1
        target_low[0], target_high[0] = sorted((sign * 2.78, sign * 2.82))
        target_low[1], target_high[1] = -2.35, 2.35
    else:
        sign = 1 if low[1] > 0 else -1
        target_low[1], target_high[1] = sorted((sign * 2.33, sign * 2.37))
    for vertex, co in zip(obj.data.vertices, coords):
        for axis in range(3):
            vertex.co[axis] = target_low[axis] + (co[axis] - low[axis]) / (high[axis] - low[axis]) * (target_high[axis] - target_low[axis])
    # Keep the edge rounding smaller than the 2 cm embedded overlap.
    for mod in obj.modifiers:
        if mod.type == 'BEVEL':
            mod.width = .004
    obj.data.update()

for obj in walls + plinths:
    obj.data.materials.clear()
    obj.data.materials.append(cement)
    uv = obj.data.uv_layers.active.data
    for poly in obj.data.polygons:
        poly.material_index = 0
        axes = (0, 1) if abs(poly.normal.z) > .5 else ((0, 2) if abs(poly.normal.y) > .5 else (1, 2))
        for loop in poly.loop_indices:
            co = obj.data.vertices[obj.data.loops[loop].vertex_index].co
            uv[loop].uv = (co[axes[0]] * .4, co[axes[1]] * .4)

# Concrete is an atlas with horizontal trim bands and drainage streaks.
# Keep these small architectural boxes inside its plain central concrete patch
# instead of repeating the entire atlas across floors and wall corners.
for obj in objects:
    if not obj.data.uv_layers.active:
        continue
    uv = obj.data.uv_layers.active.data
    low = [min(v.co[i] for v in obj.data.vertices) for i in range(3)]
    high = [max(v.co[i] for v in obj.data.vertices) for i in range(3)]
    for poly in obj.data.polygons:
        if obj.data.materials[poly.material_index].name != 'Concrete':
            continue
        axes = (0, 1) if abs(poly.normal.z) > .5 else ((0, 2) if abs(poly.normal.y) > .5 else (1, 2))
        scale = .42 / max(high[i] - low[i] for i in axes)
        for loop in poly.loop_indices:
            co = obj.data.vertices[obj.data.loops[loop].vertex_index].co
            uv[loop].uv = tuple(.5 + (co[i] - (low[i] + high[i]) / 2) * scale for i in axes)

bpy.data.objects['Door_Hinge'].rotation_euler.z = 0
bpy.context.view_layer.update()
old = bpy.context.preferences.filepaths.save_version
bpy.context.preferences.filepaths.save_version = 0
try:
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'Guardhouse_Rebuilt.blend'), compress=True)
finally:
    bpy.context.preferences.filepaths.save_version = old
print('Updated', len(walls), 'wall sections and', len(plinths), 'exterior plinths; door closed')
