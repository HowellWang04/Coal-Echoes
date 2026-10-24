"""Banner texture for the 1980 Group Photo: red cotton cloth with the commemorative line
石炭井洗煤厂一九八〇年新职工入厂留念 across it, the way factory banners of the time were made.
Styles:
  heiti  white paper characters cut by hand in bold 黑体 and pasted on the cloth (Noto Sans SC Black)
  song   the same cut paper in heavy 宋体 (Source Han Serif Heavy)
  brush  written straight onto the cloth with a brush in pale paint (Ma Shan Zheng)
All three fonts are SIL OFL. The texture is 12:1 for a 9.6 m by 0.8 m banner.
Usage: python make_banner.py <style> <output.png>
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
from scipy.ndimage import gaussian_filter, map_coordinates

TEXT = '石炭井洗煤厂一九八〇年新职工入厂留念'
W, H = 4800, 400
MARGIN_X, GLYPH = 190, 236
PROJECT_FONTS = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                             '../../../../Assets/Art Resources/UI/NewspaperMenu/Fonts')
FONTS = {
    'heiti': (r'C:\Windows\Fonts\NotoSansSC-VF.ttf', 900),
    'song': (os.path.join(PROJECT_FONTS, 'SourceHanSerifCN-Heavy.otf'), None),
    'brush': (os.path.join(PROJECT_FONTS, 'MaShanZheng-Regular.ttf'), None),
}

style, out = sys.argv[1], sys.argv[2]
rng = np.random.default_rng(19801003)


def smooth_noise(shape, scale, amount):
    n = gaussian_filter(rng.normal(0, 1, shape), scale)
    return n / (np.abs(n).max() + 1e-6) * amount


def cloth():
    """Dyed cotton: thread weave, uneven dye and a faint crease where it was folded in storage."""
    yy, xx = np.mgrid[0:H, 0:W]
    weave = (np.sin(xx * 2.1) * np.sin(yy * 2.1)) * .025 + rng.normal(0, .03, (H, W))
    dye = smooth_noise((H, W), 60, .07)
    crease = np.exp(-((xx - W * .37) / 9) ** 2) * .08 + np.exp(-((xx - W * .69) / 9) ** 2) * .06
    shade = 1 + weave + dye - crease
    red = np.array([172, 30, 32], np.float32) / 255
    return np.clip(shade[..., None] * red, 0, 1)


def glyph_mask(ch, font, size):
    """One character on its own canvas, 2x supersampled, centred on its ink."""
    s = size * 2
    img = Image.new('L', (int(s * 1.6), int(s * 1.6)), 0)
    d = ImageDraw.Draw(img)
    if ch == '〇' and style == 'brush':
        # The brush face has no 〇; a writer would draw a quick oval ring.
        c, r = img.width / 2, s * .4
        d.ellipse((c - r * .86, c - r, c + r * .86, c + r), outline=255, width=int(s * .11))
    else:
        d.text((img.width / 2, img.height / 2), ch, font=font, fill=255, anchor='mm')
    box = img.getbbox()
    ink = img.crop(box)
    k = s / max(ink.size) * (.92 if style != 'brush' else 1.0)
    if ch in '一':
        k = s / ink.size[0] * .9   # keep 一 as wide as the others instead of scaling up its thickness
    return ink.resize((max(1, int(ink.width * k)), max(1, int(ink.height * k))), Image.LANCZOS)


def rough(mask, amount):
    """Hand-cut or hand-painted edges: push the outline around with a smooth displacement field."""
    a = np.asarray(mask, np.float32) / 255
    h, w = a.shape
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    dx = smooth_noise((h, w), 6, amount)
    dy = smooth_noise((h, w), 6, amount)
    return map_coordinates(a, [yy + dy, xx + dx], order=1, mode='constant')


path, weight = FONTS[style]
font = ImageFont.truetype(path, GLYPH * 2)
if weight:
    font.set_variation_by_axes([weight])

ink = np.zeros((H, W), np.float32)
pitch = (W - 2 * MARGIN_X) / len(TEXT)
for i, ch in enumerate(TEXT):
    g = glyph_mask(ch, font, GLYPH)
    # Each paper character was cut and pasted on its own, so none sits quite straight.
    angle = rng.normal(0, .9 if style != 'brush' else .5)
    g = g.rotate(angle, resample=Image.BICUBIC, expand=True)
    g = g.resize((g.width // 2, g.height // 2), Image.LANCZOS)
    a = rough(g, 1.4 if style != 'brush' else 2.2)
    cx = MARGIN_X + pitch * (i + .5) + rng.normal(0, 3)
    cy = H / 2 + rng.normal(0, 4)
    x0, y0 = int(cx - a.shape[1] / 2), int(cy - a.shape[0] / 2)
    ink[y0:y0 + a.shape[0], x0:x0 + a.shape[1]] = np.maximum(ink[y0:y0 + a.shape[0], x0:x0 + a.shape[1]], a)

ink = np.clip((ink - .5) * 3 + .5, 0, 1)
ink = gaussian_filter(ink, .6)
base = cloth()
if style == 'brush':
    # Pale paint soaks unevenly into cotton; dry streaks run along the brush direction.
    streak = gaussian_filter(rng.normal(0, 1, (H, W)), (0.6, 14))
    cover = np.clip(.88 + streak * 1.6, .55, 1) * ink
    paint = np.array([238, 226, 196], np.float32) / 255
    rgb = base * (1 - cover[..., None]) + paint * cover[..., None]
else:
    # Cut newsprint-white paper: slightly warm, with fibre noise and a thin shadow on the cloth.
    shadow = gaussian_filter(np.roll(np.roll(ink, 2, 0), 2, 1), 1.5) * (1 - ink)
    paper = np.array([240, 236, 224], np.float32) / 255 * (1 + rng.normal(0, .02, (H, W)))[..., None]
    rgb = base * (1 - .35 * shadow[..., None])
    rgb = rgb * (1 - ink[..., None]) + paper * ink[..., None]

Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8)).save(out)
print('saved', out, (W, H))
