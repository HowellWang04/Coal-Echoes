"""Draws the high-res TextArea frame and five themed scrapbook stickers.

Usage: python -I draw_scrapbook_art.py <out_dir>
"""
import math
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)

INK = (92, 72, 50)
TAN = (226, 203, 159)
CREAM = (255, 238, 206)
PAPER = (247, 238, 219)
RUST = (175, 116, 74)
COAL = (46, 42, 39)
COAL_HI = (78, 72, 66)
NAVY = (47, 62, 85)
RED = (178, 58, 46)
LAMP = (242, 193, 78)


# ---------------------------------------------------------------- TextArea frame
def draw_text_area():
    """Redraw TextArea.png (204x113 original) at 8x, point-symmetric frame."""
    scale = 8
    ss = 4  # supersampling
    k = scale * ss
    W, H = 204, 113
    img = Image.new("RGBA", (W * k, H * k), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    white = (242, 242, 242, 255)
    black = (18, 18, 18, 255)

    def P(x, y, rot):
        if rot:
            x, y = 207 - x, 113 - y
        return (x * k, y * k)

    def line(pts, width, color, rot):
        pts = [P(x, y, rot) for x, y in pts]
        d.line(pts, fill=color, width=int(width * k), joint="curve")
        r = width * k / 2
        for (x, y) in (pts[0], pts[-1]):
            d.ellipse((x - r, y - r, x + r, y + r), fill=color)

    def quote(cx, rot, color, grow):
        # "6"-shaped opening quote: filled blob at the bottom, stem curving up to the top right
        bx, by = cx + 1.6, 20.4
        r = 1.65 + grow
        x, y = P(bx, by, rot)
        d.ellipse((x - r * k, y - r * k, x + r * k, y + r * k), fill=color)
        stem = []
        for i in range(13):
            t = i / 12
            sx = cx + 0.9 + 0.2 * math.sin(t * math.pi) + 1.6 * t * t
            sy = 20.0 - 6.6 * t
            stem.append((sx, sy))
        line(stem, 1.1 + grow * 2, color, rot)

    for rot in (False, True):
        # thick white bar with its corner turning down on the right
        line([(10.5, 9.5), (187.5, 9.5), (187.5, 35.8)], 3.2, white, rot)
        # thin black line with a 1px white outline: outline pass first
        for width, color, grow in ((4.0, white, 1.0), (2.0, black, 0.0)):
            line([(18.6, 17.0), (26.2, 17.0)], width, color, rot)
            line([(45.6, 17.0), (200.0, 17.0), (200.0, 43.2)], width, color, rot)
            quote(30.3, rot, color, grow)
            quote(37.3, rot, color, grow)

    img = img.resize((W * scale, H * scale), Image.LANCZOS)
    img.save(os.path.join(OUT, "TextArea.png"))


# ---------------------------------------------------------------- sticker helpers
S = 4096  # supersampled canvas, design space is 0..1000
F = S / 1000


def P(x, y):
    return (x * F, y * F)


def box(x0, y0, x1, y1):
    return (x0 * F, y0 * F, x1 * F, y1 * F)


def poly(d, pts, fill):
    d.polygon([P(x, y) for x, y in pts], fill=fill)


def circle(d, cx, cy, r, fill):
    d.ellipse(box(cx - r, cy - r, cx + r, cy + r), fill=fill)


def thick(d, pts, width, fill):
    d.line([P(x, y) for x, y in pts], fill=fill, width=int(width * F), joint="curve")
    r = width * F / 2
    for x, y in (pts[0], pts[-1]):
        x, y = x * F, y * F
        d.ellipse((x - r, y - r, x + r, y + r), fill=fill)


def new_layer():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def finish(art, name, die_cut=True, border=26):
    """Add a die-cut white border and a soft shadow, then downsample to 1024."""
    alpha = art.getchannel("A")
    result = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    shape = alpha
    if die_cut:
        grown = alpha.filter(ImageFilter.GaussianBlur(border * F * 0.55))
        grown = grown.point(lambda v: 255 if v > 18 else 0)
        grown = grown.filter(ImageFilter.GaussianBlur(F * 1.2))
        shape = ImageChops.lighter(grown, alpha)
    shadow = shape.filter(ImageFilter.GaussianBlur(14 * F)).point(lambda v: int(v * 0.32))
    shadow_img = Image.new("RGBA", (S, S), (40, 28, 18, 0))
    shadow_img.putalpha(shadow)
    result.alpha_composite(shadow_img, (int(6 * F), int(10 * F)))
    if die_cut:
        base = Image.new("RGBA", (S, S), (255, 251, 242, 0))
        base.putalpha(shape)
        result.alpha_composite(base)
    result.alpha_composite(art)
    result = result.resize((1024, 1024), Image.LANCZOS)
    result.save(os.path.join(OUT, name + ".png"))


# ---------------------------------------------------------------- 1. cableway coal bucket
def cableway_bucket():
    art, d = new_layer()
    rope = lambda x: 300 - (x - 60) * 130 / 880
    thick(d, [(60, rope(60)), (940, rope(940))], 16, (58, 51, 44, 255))

    # coal heap (behind the rim)
    for cx, cy, r in ((360, 500, 58), (440, 468, 66), (530, 455, 70), (615, 478, 60), (675, 505, 40), (480, 430, 40), (575, 425, 36)):
        circle(d, cx, cy, r, COAL + (255,))
    for cx, cy, r in ((430, 448, 18), (540, 430, 16), (610, 462, 14), (370, 490, 12)):
        circle(d, cx, cy, r, COAL_HI + (255,))

    # bucket body, highlight stripe, rim with rivets
    poly(d, [(300, 530), (700, 530), (645, 860), (355, 860)], (181, 104, 63, 255))
    poly(d, [(600, 545), (690, 545), (640, 850), (575, 850)], (150, 84, 52, 255))
    poly(d, [(345, 570), (395, 570), (418, 840), (382, 840)], (205, 138, 92, 255))
    poly(d, [(282, 505), (718, 505), (708, 565), (292, 565)], (140, 78, 49, 255))
    for i in range(7):
        circle(d, 320 + i * 60, 535, 9, CREAM + (255,))
    poly(d, [(355, 845), (645, 845), (640, 870), (360, 870)], (120, 66, 42, 255))

    # yoke and hanger
    thick(d, [(325, 540), (325, 455), (675, 455), (675, 540)], 20, INK + (255,))
    circle(d, 325, 540, 19, (58, 51, 44, 255))
    circle(d, 675, 540, 19, (58, 51, 44, 255))
    thick(d, [(500, 455), (500, rope(500) + 30)], 24, INK + (255,))

    # trolley plate and wheels riding on the rope
    a = math.atan2(-130, 880)
    def rot(px, py, ox=500, oy=rope(500)):
        return (ox + px * math.cos(a) - py * math.sin(a), oy + px * math.sin(a) + py * math.cos(a))
    poly(d, [rot(-105, -55), rot(105, -55), rot(110, 30), rot(-110, 30)], (140, 90, 60, 255))
    for px in (-62, 62):
        cx, cy = rot(px, -34)
        circle(d, cx, cy, 36, (107, 74, 54, 255))
        circle(d, cx, cy, 14, TAN + (255,))
    finish(art, "Sticker_CablewayBucket")


# ---------------------------------------------------------------- 2. miner's helmet with lamp
def miner_helmet():
    art, d = new_layer()
    main, shade, hi = (217, 164, 65, 255), (180, 128, 46, 255), (240, 200, 110, 255)
    d.ellipse(box(110, 615, 890, 735), fill=shade)
    d.pieslice(box(195, 320, 805, 960), 180, 360, fill=main)
    d.pieslice(box(195, 320, 805, 960), 290, 360, fill=shade)
    poly(d, [(482, 322), (518, 322), (530, 640), (470, 640)], hi)
    poly(d, [(300, 420), (330, 400), (360, 620), (325, 620)], hi)
    d.ellipse(box(120, 595, 880, 690), fill=main)
    d.ellipse(box(170, 600, 830, 650), fill=hi)

    # lamp glow, rays, housing and lens
    glow = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    circle(gd, 500, 470, 150, (255, 226, 140, 150))
    glow = glow.filter(ImageFilter.GaussianBlur(40 * F))
    art.alpha_composite(glow)
    for ang in (-160, -125, -90, -55, -20):
        r0, r1 = 125, 185
        t = math.radians(ang)
        thick(d, [(500 + r0 * math.cos(t), 470 + r0 * math.sin(t)), (500 + r1 * math.cos(t), 470 + r1 * math.sin(t))], 20, LAMP + (255,))
    poly(d, [(462, 540), (538, 540), (548, 610), (452, 610)], (58, 51, 44, 255))
    circle(d, 500, 470, 92, (58, 51, 44, 255))
    circle(d, 500, 470, 66, (252, 236, 178, 255))
    circle(d, 478, 448, 18, (255, 255, 240, 255))
    finish(art, "Sticker_MinerHelmet")


# ---------------------------------------------------------------- 3. night ridge badge
def ridge_night():
    art, d = new_layer()
    sky = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    sd = ImageDraw.Draw(sky)
    top, bottom = (31, 42, 68), (88, 82, 104)
    for y in range(S):
        t = y / S
        sd.line([(0, y), (S, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).ellipse(box(70, 70, 930, 930), fill=255)
    art.paste(sky, (0, 0), mask)
    d = ImageDraw.Draw(art)

    rng = random.Random(7)
    for _ in range(46):
        x, y = rng.uniform(150, 850), rng.uniform(150, 520)
        if (x - 500) ** 2 + (y - 500) ** 2 < 400 ** 2:
            circle(d, x, y, rng.uniform(3, 7), (255, 244, 214, 255))
    circle(d, 680, 230, 62, (255, 240, 200, 255))
    circle(d, 708, 212, 58, top + (255,))

    poly(d, [(70, 600), (190, 520), (290, 570), (400, 480), (520, 560), (640, 500), (760, 560), (930, 520), (930, 700), (70, 700)], (59, 66, 96, 255))

    lights = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ld = ImageDraw.Draw(lights)
    for _ in range(40):
        x, y = rng.uniform(380, 760), rng.uniform(640, 700)
        circle(ld, x, y, rng.uniform(4, 9), (255, 214, 120, 255))
    glow = lights.filter(ImageFilter.GaussianBlur(14 * F))
    art.alpha_composite(glow)
    art.alpha_composite(glow)
    art.alpha_composite(lights)
    d = ImageDraw.Draw(art)
    poly(d, [(560, 700), (560, 600), (575, 600), (575, 700)], (36, 40, 60, 255))
    circle(d, 567, 596, 7, (230, 90, 70, 255))

    poly(d, [(70, 760), (230, 700), (400, 730), (560, 690), (760, 740), (930, 720), (930, 930), (70, 930)], (21, 26, 42, 255))
    # two figures sitting on the ridge
    for cx in (330, 380):
        circle(d, cx, 662, 15, (21, 26, 42, 255))
        poly(d, [(cx - 20, 720), (cx - 14, 680), (cx + 14, 680), (cx + 22, 720)], (21, 26, 42, 255))

    ring = Image.new("L", (S, S), 0)
    rd = ImageDraw.Draw(ring)
    rd.ellipse(box(70, 70, 930, 930), fill=255)
    rd.ellipse(box(92, 92, 908, 908), fill=0)
    art.paste(Image.new("RGBA", (S, S), TAN + (255,)), (0, 0), ring)
    clip = Image.new("L", (S, S), 0)
    ImageDraw.Draw(clip).ellipse(box(70, 70, 930, 930), fill=255)
    art.putalpha(ImageChops.multiply(art.getchannel("A"), clip))
    finish(art, "Sticker_RidgeNight")


# ---------------------------------------------------------------- 4. enamel mug
def enamel_mug():
    art, d = new_layer()
    enamel, enamel_shade = (244, 238, 223, 255), (222, 212, 192, 255)
    d.ellipse(box(615, 390, 815, 710), fill=NAVY + (255,))
    d.ellipse(box(645, 425, 785, 675), fill=enamel)
    d.ellipse(box(672, 458, 758, 642), fill=(0, 0, 0, 0))
    d.rectangle(box(250, 310, 700, 820), fill=enamel)
    d.rectangle(box(610, 310, 700, 820), fill=enamel_shade)
    d.ellipse(box(250, 790, 700, 850), fill=NAVY + (255,))
    d.rectangle(box(250, 790, 700, 820), fill=NAVY + (255,))
    d.ellipse(box(240, 270, 710, 350), fill=NAVY + (255,))
    d.ellipse(box(265, 284, 685, 336), fill=(70, 52, 38, 255))
    # plum blossom
    for i in range(5):
        t = math.radians(-90 + i * 72)
        circle(d, 470 + 56 * math.cos(t), 560 + 56 * math.sin(t), 50, RED + (255,))
    circle(d, 470, 560, 26, LAMP + (255,))
    for i in range(5):
        t = math.radians(-54 + i * 72)
        circle(d, 470 + 14 * math.cos(t), 560 + 14 * math.sin(t), 4, (150, 40, 32, 255))
    # chipped enamel
    poly(d, [(300, 760), (330, 748), (345, 772), (318, 790), (296, 780)], (36, 40, 52, 255))
    poly(d, [(655, 340), (680, 352), (668, 372), (648, 362)], (36, 40, 52, 255))
    finish(art, "Sticker_EnamelMug")


# ---------------------------------------------------------------- 5. coal plant stamp
def plant_stamp():
    art, d = new_layer()
    d.rectangle(box(170, 110, 830, 890), fill=PAPER + (255,))
    # perforated edge
    holes = Image.new("L", (S, S), 255)
    hd = ImageDraw.Draw(holes)
    step = 44
    for x in range(170, 831, step):
        for y in (110, 890):
            hd.ellipse(box(x - 16, y - 16, x + 16, y + 16), fill=0)
    for y in range(110, 891, step):
        for x in (170, 830):
            hd.ellipse(box(x - 16, y - 16, x + 16, y + 16), fill=0)
    art.putalpha(ImageChops.multiply(art.getchannel("A"), holes))
    d = ImageDraw.Draw(art)

    # picture: warm dusk sky, sun, hills, plant, cableway
    x0, y0, x1, y1 = 225, 165, 775, 705
    top, bottom = (222, 150, 102), (245, 214, 160)
    for y in range(int(y0 * F), int(y1 * F)):
        t = (y / F - y0) / (y1 - y0)
        d.line([(x0 * F, y), (x1 * F, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
    circle(d, 610, 400, 70, (250, 226, 168, 255))
    poly(d, [(x0, 540), (330, 470), (430, 520), (560, 430), (680, 500), (x1, 460), (x1, 620), (x0, 620)], (183, 118, 79, 255))
    d.rectangle(box(x0, 610, x1, y1), fill=(201, 149, 95, 255))
    # cableway line and towers up the hill
    thick(d, [(470, 520), (740, 405)], 5, INK + (255,))
    for tx, ty in ((560, 482), (660, 440)):
        poly(d, [(tx - 14, ty + 70), (tx - 4, ty), (tx + 4, ty), (tx + 14, ty + 70)], INK + (255,))
    # plant buildings and chimney
    d.rectangle(box(300, 500, 470, 640), fill=INK + (255,))
    poly(d, [(300, 500), (360, 450), (420, 500)], INK + (255,))
    d.rectangle(box(470, 540, 560, 640), fill=(110, 86, 62, 255))
    poly(d, [(420, 520), (520, 470), (530, 484), (430, 534)], INK + (255,))
    d.rectangle(box(250, 330, 280, 640), fill=INK + (255,))
    for yb in (360, 410):
        d.rectangle(box(250, yb, 280, yb + 16), fill=(220, 120, 90, 255))
    for wx in range(320, 460, 34):
        d.rectangle(box(wx, 540, wx + 16, 556), fill=(250, 214, 140, 255))
    d.rectangle(box(x0, y0, x1, y1), outline=INK + (255,), width=int(4 * F))

    # denomination bars and a postmark
    d.rectangle(box(230, 750, 330, 790), fill=RED + (255,))
    d.rectangle(box(230, 810, 450, 826), fill=INK + (200,))
    d.rectangle(box(230, 840, 390, 852), fill=INK + (160,))
    # postmark in the empty bottom-right corner, wavy lines in the gap left of it
    mark = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    md = ImageDraw.Draw(mark)
    md.ellipse(box(615, 722, 775, 882), outline=(60, 50, 70, 150), width=int(8 * F))
    md.ellipse(box(645, 752, 745, 852), outline=(60, 50, 70, 150), width=int(5 * F))
    for i in range(4):
        pts = [(472 + t * 4.5, 752 + i * 26 + 8 * math.sin(t * 0.45)) for t in range(0, 31)]
        md.line([P(x, y) for x, y in pts], fill=(60, 50, 70, 140), width=int(6 * F))
    paper = Image.new("L", (S, S), 0)
    ImageDraw.Draw(paper).rectangle(box(170, 110, 830, 890), fill=255)
    mark.putalpha(ImageChops.multiply(mark.getchannel("A"), paper))
    art.alpha_composite(mark)
    art.putalpha(ImageChops.multiply(art.getchannel("A"), holes))
    finish(art, "Sticker_PlantStamp", die_cut=False)


draw_text_area()
cableway_bucket()
miner_helmet()
ridge_night()
enamel_mug()
plant_stamp()
print("done")
