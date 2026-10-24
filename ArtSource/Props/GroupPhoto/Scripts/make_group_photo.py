"""Turn a colour image of the gate into the 1980 Group Photo print the player carries: a
black-and-white silver print on panchromatic film, toned and faded by forty-six years, with a
white border, the date written into the corner, and the wear of a photo kept in a drawer.
The render's 42-degree view is wider than the standard lens a group photo of the time was taken
with, so the print shows only the middle of it, as a narrower lens from the same spot would.
The album shows photos at 16:9 and stores them without alpha, so the whole print including its
straight-cut border is an opaque 1920 x 1080; the image is scaled to the print width and trimmed
top and bottom, more from the ground than the sky.
Usage: python make_group_photo.py <colour.png> <output.png>
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
from scipy.ndimage import gaussian_filter

TOTAL = (1920, 1080)
BORDER = 78                       # white margin around the image
CROP = .8                         # share of the render the lens takes in, about 34 degrees vertical
CROP_LEFT, CROP_TOP = .069, .116  # where that window starts: both gate pillars stay in
src, out = sys.argv[1], sys.argv[2]
rng = np.random.default_rng(19801003)
inner_w, inner_h = TOTAL[0] - 2 * BORDER, TOTAL[1] - 2 * BORDER

# ---- the photograph ----
img = Image.open(src).convert('RGB')
x0, y0 = round(img.width * CROP_LEFT), round(img.height * CROP_TOP)
img = img.crop((x0, y0, x0 + round(img.width * CROP), y0 + round(img.height * CROP)))
img = img.resize((inner_w, round(img.height * inner_w / img.width)), Image.LANCZOS)
extra = img.height - inner_h
top = round(extra * .3)
rgb = np.asarray(img.crop((0, top, inner_w, top + inner_h)), np.float32) / 255

# Panchromatic black-and-white film sees red darker than the eye does, so the banner reads as
# dark grey with white characters.
lin = rgb ** 2.2
lum = (lin @ np.array([.30, .59, .11], np.float32)) ** (1 / 2.2)
# A medium-format lens of the time, slightly soft, and a print that lost some contrast.
lum = gaussian_filter(lum, .9)
lum = lum + (lum - gaussian_filter(lum, 12)) * .35          # darkroom local contrast
lum = np.clip((lum - .5) * 1.4 + .53, 0, 1)
lum = 1 - (1 - lum) ** 1.15                                  # highlights roll off into the sky
h, w = lum.shape
yy, xx = np.mgrid[0:h, 0:w]
r = np.hypot((xx - w / 2) / (w / 2), (yy - h / 2) / (h / 2))
lum *= 1 - .28 * np.clip(r - .4, 0, 1) ** 1.6               # lens falloff
# Film grain, stronger in the mid tones, slightly clumped.
grain = gaussian_filter(rng.normal(0, 1, (h, w)), .7) * 1.6 + rng.normal(0, .35, (h, w))
lum += grain * .028 * (1 - np.abs(lum - .5) * 1.2)
# Dust on the negative printed as white specks, and a few hairline scratches.
for _ in range(90):
    y, x = rng.integers(0, h), rng.integers(0, w)
    rad = rng.uniform(.6, 2.2)
    lum[max(0, y - 3):y + 4, max(0, x - 3):x + 4] += np.exp(-((yy[:7, :7] - 3) ** 2 + (xx[:7, :7] - 3) ** 2) / rad ** 2)[
        :min(7, h - max(0, y - 3)), :min(7, w - max(0, x - 3))] * .5
scratch = Image.new('L', (w, h), 0)
d = ImageDraw.Draw(scratch)
for _ in range(5):
    x0, y0 = rng.uniform(0, w), rng.uniform(0, h)
    ang, length = rng.uniform(1.2, 1.9), rng.uniform(120, 420)
    d.line([(x0, y0), (x0 + np.cos(ang) * length, y0 + np.sin(ang) * length)], fill=110, width=1)
lum += np.asarray(scratch.filter(ImageFilter.GaussianBlur(.6)), np.float32) / 255 * .35
lum = np.clip(lum, 0, 1)
# Faded silver print: blacks lifted, a warm brown tone that strengthens toward the shadows.
lum = .05 + lum * .9
tone = np.dstack([lum * .99 + .01, lum * .93 + .025, lum * .80 + .05])
photo = Image.fromarray((np.clip(tone, 0, 1) * 255).astype(np.uint8))

# The date was written on the negative, so it prints white in the lower right corner.
draw = ImageDraw.Draw(photo)
font = ImageFont.truetype(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                          '../../../../Assets/Art Resources/UI/NewspaperMenu/Fonts/MaShanZheng-Regular.ttf'), 40)
draw.text((w - 200, h - 70), '1980.10.3', font=font, fill=(232, 226, 210))

# ---- the print ----
paper = np.ones((TOTAL[1], TOTAL[0], 3), np.float32) * np.array([240, 235, 221], np.float32) / 255
canvas = Image.fromarray((paper * 255).astype(np.uint8))
canvas.paste(photo, (BORDER, BORDER))
p = np.asarray(canvas, np.float32) / 255
H, W = p.shape[:2]
YY, XX = np.mgrid[0:H, 0:W]
# Yellowing: blotchy, stronger toward the edges where hands held it.
blotch = gaussian_filter(rng.random((H, W)), 55)
blotch = (blotch - blotch.min()) / (blotch.max() - blotch.min())
edge = np.clip(1 - np.minimum.reduce([XX, YY, W - XX, H - YY]) / 160, 0, 1)
yellow = .03 + .05 * blotch + .07 * edge
p = p * (1 - yellow[..., None] * np.array([.0, .35, 1.0]))
p *= 1 - .05 * edge[..., None]
# Foxing spots on the white border.
for _ in range(26):
    cy, cx = rng.integers(0, H), rng.integers(0, W)
    if BORDER < cx < W - BORDER and BORDER < cy < H - BORDER:
        continue
    s = rng.uniform(2, 7)
    spot = np.exp(-((YY - cy) ** 2 + (XX - cx) ** 2) / (2 * s * s)) * rng.uniform(.08, .2)
    p *= 1 - spot[..., None] * np.array([.2, .45, .8])
# A soft crease across one corner, where it was once bent.
dist = (XX * .62 + YY * .78 - 1700) / 2
crease = np.exp(-dist ** 2) * .10 - np.exp(-(dist - 2) ** 2 / 4) * .05
p *= 1 - crease[..., None]

result = Image.fromarray((np.clip(p, 0, 1) * 255).astype(np.uint8))
result.save(out)
print('saved', out, result.size)
