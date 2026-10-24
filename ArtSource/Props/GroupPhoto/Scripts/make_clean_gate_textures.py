"""The factory gate as it looked in 1980, for the Group Photo render only: the same steel paint,
frame, iron, brick, concrete and whitewash as ../../../FactoryGate/texture_maps.py, generated
with the same noise so the surfaces line up, but without forty years of rust, peeling, drips,
scrapes and grime. The game keeps the rusted gate; RenderGateBackground.cs swaps these in only
while it renders.
Writes <name>_BaseColor.png, <name>_ORM.png (glTF: G roughness, B metal) and, for the steel
leaves, <name>_Normal.png into ../CleanGate/. Runs in plain Python; bpy is stubbed because
texture_maps only needs it to save images.
Usage: python make_clean_gate_textures.py
"""
import os
import sys
import types
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
sys.modules['bpy'] = types.ModuleType('bpy')
sys.path.insert(0, str(HERE.parents[2] / 'FactoryGate'))
import texture_maps as tm  # noqa: E402

OUT = HERE.parent / 'CleanGate'
OUT.mkdir(exist_ok=True)
WANTED = ('_BaseColor', '_ORM', '_Normal')


def save_image(path, data, color=False):
    """Same values texture_maps writes through Blender's byte images, saved with PIL."""
    path = Path(path)
    if not path.stem.endswith(WANTED):
        return
    if path.stem.endswith('_Normal') and not path.stem.startswith('GatePaint'):
        return   # only the leaves had rust relief; the other normal maps stay as they are
    Image.fromarray((np.clip(data[..., :3], 0, 1) * 255 + .5).astype(np.uint8)).save(OUT / path.name)


tm.save_image = save_image


def clean_steel_panel(seed):
    """steel_panel without the damage: fresh paint with a faint orange peel and road dust at the foot."""
    nx, n = tm.SHEET_PIXELS
    rng = np.random.default_rng(seed)
    v = np.arange(n, dtype=np.float32)[:, None] / n
    broad = tm.fbm(n, seed + 43, 7, nx, tm.STRETCH)
    medium = tm.fbm(n, seed + 78, 35, nx, tm.STRETCH)
    fine = tm.noise(n, round(650 * tm.STRETCH), seed + 210, 650, nx)
    grain = rng.random((n, nx), dtype=np.float32)
    paint = np.zeros((n, nx, 3), dtype=np.float32) + (0.255, 0.30, 0.325)
    paint *= (0.95 + broad[..., None] * 0.08 + (fine[..., None] - 0.5) * 0.025)
    dust = (1 - tm.smoothstep(0, 0.1, v)) * (0.02 + medium * 0.05)
    rgb = tm.blend(paint, np.array((0.24, 0.22, 0.19), dtype=np.float32), dust)
    rough = 0.5 + medium * 0.08
    metal = np.zeros_like(rough)
    height = (fine - 0.5) * 0.00006 + (grain - 0.5) * 0.00001
    return rgb, rough, metal, height


def clean_frame_steel(name, seed, painted):
    n = 1024
    small = tm.fbm(n, seed + 12, 32)
    base = np.array((0.105, 0.112, 0.11) if painted else (0.16, 0.165, 0.16), dtype=np.float32)
    rgb = base[None, None, :] * (0.92 + small[..., None] * 0.15)
    rough = (0.5 if painted else 0.44) + small * 0.1
    metal = np.zeros_like(rough) + (0.0 if painted else 0.93)
    tm.maps(OUT, name, rgb, rough, metal, np.zeros_like(rough), (1.1, 1.1))


def clean_mineral(name, color, seed, span, n):
    """mineral() with the ground splash, roof-line streaks and stains at a third of their strength."""
    broad = tm.fbm(n, seed)
    fine = tm.noise(n, 700 if n == 2048 else 240, seed + 11)
    rng = np.random.default_rng(seed)
    grain = rng.random((n, n), dtype=np.float32)
    rgb = np.zeros((n, n, 3), dtype=np.float32) + color
    rgb *= (0.6 + broad * 0.65 + (fine - 0.5) * 0.26 + (grain - 0.5) * 0.12)[..., None]
    rough = 0.83 + fine * 0.15
    if name == 'Brick':
        rgb = tm.blend(rgb, np.array((.51, .445, .35)), tm.smoothstep(.40, .66, broad) * .30)
        speck = grain > 0.987
        rgb[speck] = rgb[speck] * 0.7 + np.array((.61, .54, .43)) * 0.3
    if name in ('Brick', 'Concrete'):
        z = np.arange(n, dtype=np.float32)[:, None] / n * 4.5
        streak = tm.noise(n, 170, seed + 60, cells_y=5)
        dirt = np.exp(-z / 0.65) * (.22 + streak * .46)
        for top in (3.45, 4.12):
            dirt += np.exp(-np.maximum(top - z, 0) / .27) * (z < top) * (.11 + streak * .25)
        rgb *= 1 - np.clip(dirt / 3, 0, .72)[..., None]
    if name == 'Concrete':
        rgb[grain < 0.018] *= 0.75
        rgb *= 1 - (tm.smoothstep(0.42, 0.74, tm.fbm(n, seed + 31, 18)) * 0.08)[..., None]
    if name == 'Whitewash':
        rgb *= 1 - tm.smoothstep(.42, .67, broad)[..., None] * .1
    tm.maps(OUT, name, rgb, rough, 0.0, np.zeros((n, n), np.float32), span)
    if name == 'Brick':
        # The weathered bricks were only darker; in 1980 they match the rest.
        save_image(OUT / 'BrickWeathered_BaseColor.png', rgb, color=True)


left, right = clean_steel_panel(104), clean_steel_panel(682)
tm.maps(OUT, 'GatePaint', *[np.concatenate((a, b), axis=1) for a, b in zip(left, right)], (2 * tm.SHEET_WIDTH, 2.815))
clean_frame_steel('FramePaint', 13, True)
clean_frame_steel('Iron', 111, False)
clean_mineral('Brick', (.565, .425, .305), 921, (9, 4.5), 2048)
clean_mineral('Concrete', (0.40, 0.39, 0.35), 213, (4.5, 4.5), 2048)
clean_mineral('Whitewash', (0.66, 0.63, 0.54), 671, (1.1, 1.1), 512)
print('saved', sorted(os.listdir(OUT)))
