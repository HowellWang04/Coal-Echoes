"""Move horizontal concrete faces of the existing gate and wall assets to the clean middle of the concrete map.

blender --background --factory-startup --python-exit-code 1 --python ArtSource/FactoryBoundary/fix_concrete_top_uv.py
Add -- --dry-run to only count. The concrete map darkens upward from V 0, so tops mapped around V 0
showed base grime streaks and a seam. Faces already moved are skipped, so a second run changes nothing.
"""
from pathlib import Path
import json
import struct
import sys

import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
GATE_AREA = ROOT / 'Assets/Art Resources/Environment/Gate Area'
DRY_RUN = '--dry-run' in sys.argv
sys.path.insert(0, str(ROOT / 'ArtSource/FactoryGate'))
import build_gate as gate


def shift_blend(path):
    bpy.ops.wm.open_mainfile(filepath=str(path))
    moved = 0
    for mesh in bpy.data.meshes:
        if not mesh.materials or not mesh.materials[0] or mesh.materials[0].name != 'Concrete':
            continue
        uv = mesh.uv_layers.active.data
        for face in mesh.polygons:
            if abs(face.normal.z) <= .9:
                continue
            for loop in face.loop_indices:
                if abs(uv[loop].uv[1]) < .3:
                    uv[loop].uv[1] += .5
                    moved += 1
    print('BLEND', path.name, moved, flush=True)
    if DRY_RUN:
        return
    if path.name == 'FactoryGate_Rebuilt.blend':
        gate.asset_collection = bpy.data.collections['Factory Gate Asset']
        gate.materials = {name: bpy.data.materials[name] for name in
                          ('GatePaint', 'FramePaint', 'Iron', 'Brick', 'BrickWeathered', 'Concrete', 'Mortar', 'Whitewash')}
        gate.export_asset(bpy.data.objects['FactoryGate_Rebuilt'])
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path))


def shift_glb(path):
    data = bytearray(path.read_bytes())
    json_length = struct.unpack_from('<I', data, 12)[0]
    doc = json.loads(data[20:20 + json_length])
    binary = 28 + json_length

    def floats(index, width):
        accessor = doc['accessors'][index]
        view = doc['bufferViews'][accessor['bufferView']]
        assert accessor['componentType'] == 5126 and 'byteStride' not in view and 'min' not in accessor
        start = binary + view.get('byteOffset', 0) + accessor.get('byteOffset', 0)
        values = np.frombuffer(data, '<f4', accessor['count'] * width, start).reshape(-1, width).copy()
        return start, values

    moved = 0
    for mesh in doc['meshes']:
        for primitive in mesh['primitives']:
            if 'material' not in primitive or not doc['materials'][primitive['material']]['name'].startswith('Concrete'):
                continue
            _, normals = floats(primitive['attributes']['NORMAL'], 3)
            start, uv = floats(primitive['attributes']['TEXCOORD_0'], 2)
            # glTF is Y up and stores V as 1 minus the Blender V.
            mask = (np.abs(normals[:, 1]) > .9) & (np.abs(1 - uv[:, 1]) < .3)
            uv[mask, 1] -= .5
            moved += int(mask.sum())
            data[start:start + uv.nbytes] = uv.tobytes()
    print('GLB', path.name, moved, flush=True)
    if not DRY_RUN:
        path.write_bytes(data)


# The gate source re-exports its GLB and FBX; derived models are patched in place.
for blend in (ROOT / 'ArtSource/FactoryGate/FactoryGate_Rebuilt.blend',
              ROOT / 'ArtSource/FactoryBoundary/FactoryBoundary.blend',
              ROOT / 'ArtSource/FactoryBoundary/FactoryWall_9m_Variants.blend'):
    shift_blend(blend)
for glb in (GATE_AREA / 'Guardhouse_Rebuilt/Guardhouse_WallReturns.glb',
            *sorted((GATE_AREA / 'FactoryBoundary/Models').glob('*.glb'))):
    shift_glb(glb)
