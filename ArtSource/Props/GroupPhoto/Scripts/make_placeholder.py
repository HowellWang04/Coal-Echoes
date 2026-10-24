"""Temporary Group Photo: a render of the present-day factory gate aged into a 1980 black and
white print, with the commemorative title written across the top as on group photos of the time.
There are no people in it; replace with the real group photo when it is drawn.
Usage: python make_placeholder.py <gate_render.png> <output.png>
"""
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

src, out = sys.argv[1], sys.argv[2]
# The album shows photos at 16:9 like camera shots, so the whole print including its border is 1920 x 1080.
TOTAL, border = (1920, 1080), 80
inner = (TOTAL[0] - 2 * border, TOTAL[1] - 2 * border)
img = Image.open(src).convert('L')
img = img.resize((inner[0], round(img.height * inner[0] / img.width)), Image.LANCZOS)
img = img.crop((0, 0, inner[0], inner[1])).filter(ImageFilter.GaussianBlur(1.1))
a = np.asarray(img, np.float32) / 255
a = np.clip((a - .5) * 1.35 + .6, 0, 1) ** 1.05          # punchier mid tones of old film
h, w = a.shape
rng = np.random.default_rng(1980)
a += rng.normal(0, .045, a.shape)                          # film grain
yy, xx = np.mgrid[0:h, 0:w]
r = np.hypot((xx - w / 2) / (w / 2), (yy - h / 2) / (h / 2))
a *= 1 - .35 * np.clip(r - .45, 0, 1) ** 1.5               # lens falloff
a = np.clip(a, 0, 1)
tone = np.dstack([a * .98 + .02, a * .93 + .03, a * .82 + .05])  # faded warm silver print
photo = Image.fromarray((tone * 255).astype(np.uint8))
draw = ImageDraw.Draw(photo)
font = ImageFont.truetype(r'C:\Windows\Fonts\simhei.ttf', 54)
title = '石炭井洗煤厂一九八〇年新职工入厂留念'
tw = draw.textlength(title, font=font)
draw.text(((w - tw) / 2, 40), title, font=font, fill=(236, 230, 214))
small = ImageFont.truetype(r'C:\Windows\Fonts\simhei.ttf', 34)
draw.text((w - 260, h - 70), '1980.10.3', font=small, fill=(236, 230, 214))
# White border of a darkroom print, yellowed with age.
print_ = Image.new('RGB', (w + 2 * border, h + 2 * border), (214, 204, 178))
print_.paste(photo, (border, border))
p = np.asarray(print_, np.float32)
stain = Image.fromarray((rng.random((9, 16)) * 255).astype(np.uint8)).resize(print_.size, Image.BICUBIC).filter(ImageFilter.GaussianBlur(40))
p *= (1 - .08 * np.asarray(stain, np.float32)[:, :, None] / 255)
Image.fromarray(np.clip(p, 0, 255).astype(np.uint8)).save(out)
print('saved', out, print_.size)
