"""Make the dune base colour from the desert's gravelly sand: less saturated and flatter, like fine blown sand.

Run with plain Python (Pillow and numpy). Gains are per channel in linear space and were tuned against Unity
screenshots, so the lit slip face renders close to the factory ground around it.
"""
import numpy as np
from pathlib import Path
from PIL import Image

PROJECT = Path(r'D:\Development\Game Dev\Project\Grad App Project\Coal Echoes')
SOURCE = PROJECT / 'Assets/Art Resources/Environment/Terrain/MyTextures/gravelly_sand_diff_4k.jpg'
TARGET = PROJECT / 'Assets/Art Resources/Environment/SandDune/Textures/SandDune_BaseColor.png'
SIZE = 2048
GAIN = np.array([0.57, 0.79, 0.99])  # linear RGB, keeps the hue and lands sunlit slopes near the ground colour
FLATTEN = 0.5  # pull each pixel this far toward the mean, fine sand has less contrast than gravel


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


image = Image.open(SOURCE).convert('RGB').resize((SIZE, SIZE), Image.LANCZOS)
linear = to_linear(np.asarray(image, dtype=np.float64) / 255.0)
mean = linear.reshape(-1, 3).mean(axis=0)
linear = mean + (linear - mean) * (1 - FLATTEN)
linear = np.clip(linear * GAIN, 0.0, 1.0)
out = np.round(to_srgb(linear) * 255.0).astype(np.uint8)
TARGET.parent.mkdir(parents=True, exist_ok=True)
Image.fromarray(out).save(TARGET)
print('saved', TARGET, 'mean sRGB', out.reshape(-1, 3).mean(axis=0).round(1))
