"""Rebuild only the two gate leaves and the steel sheet textures in the saved blend.

blender --background ArtSource/FactoryGate/FactoryGate_Rebuilt.blend --python-exit-code 1 --python ArtSource/FactoryGate/rebuild_leaves.py
Optional script argument after -- is --skip-render.
Piers and walls keep their approved brick geometry and tints and only move out to the new hinges.
"""

from pathlib import Path
import sys

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_gate as gate
import texture_maps

skip_render = '--skip-render' in sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else False
gate.asset_collection = bpy.data.collections['Factory Gate Asset']
gate.stage_collection = bpy.data.collections['Preview Only']
for name in ('GatePaint', 'FramePaint', 'Iron', 'Brick', 'BrickWeathered', 'Concrete', 'Mortar', 'Whitewash'):
    gate.materials[name] = bpy.data.materials[name]
root = bpy.data.objects['FactoryGate_Rebuilt']

texture_maps.steel_panels(gate.TEXTURES)
for node in [n for n in gate.materials['GatePaint'].node_tree.nodes if n.type == 'TEX_IMAGE']:
    colorspace = node.image.colorspace_settings.name
    bpy.data.images.remove(node.image)
    node.image = bpy.data.images.load(str(gate.TEXTURES / f'GatePaint_{node.label}.png'))
    node.image.colorspace_settings.name = colorspace

leaves = []
for side, label in ((-1, 'Left'), (1, 'Right')):
    leaf = bpy.data.objects[f'{label}_GateLeaf']
    for child in list(leaf.children_recursive):
        bpy.data.objects.remove(child, do_unlink=True)
    leaf.location = (0, 0, 0)
    gate.gate_leaf(leaf, side)
    leaves.append(leaf)
    for part in ('Pier', 'WingWall'):
        obj = bpy.data.objects.get(f'{label}_{part}')
        if obj:
            obj.location.x = side * (gate.HINGE_X - 2.70)
gate.finish_parts(root, leaves)
bpy.ops.outliner.orphans_purge(do_recursive=True)
gate.export_asset(root)

for obj in list(gate.stage_collection.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.outliner.orphans_purge(do_recursive=True)
gate.save_and_render(skip_render)
