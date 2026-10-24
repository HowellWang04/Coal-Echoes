"""内心独白字幕效果图：在大门玩家视角截图上画无底衬柔影的几种方案。

A 到 C 比较底衬做法；D 到 F 比较怎样让玩家一看就知道是主人公心里的话；G 到 I 比较三种思考 icon。

python build_mockups.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
FONTS = PROJECT / "Assets/Art Resources/UI/Fonts"
IMPRIMA = FONTS / "Imprima 400.ttf"
PLAYER_HAND = FONTS / "Elegant Bloom.otf"      # 外公日记里玩家便签的字体
BACKGROUND = ROOT / "Previews/Background_GateView.png"
OUT = ROOT / "Previews"

PAPER_WHITE = (232, 223, 205)   # 品牌暖纸白 #E8DFCD
COAL_BLACK = (41, 39, 36)       # 品牌煤灰黑 #292724
OCHRE_RED = (154, 97, 70)       # 品牌暗赭红 #9A6146
NOTE_YELLOW = (242, 225, 128)   # 玩家便签贴图的纸色

LINES = ["Same gate, same spot.", "Just no one standing here anymore."]
ITALIC_SHEAR = 0.2              # TMP 的 Italic 也是按斜切做的，Imprima 没有斜体字形
CENTER_Y = 0.86                 # 字幕块中心在画面高度的位置


def text_layer(size, lines, font, line_gap, center_y, color, alpha=255, shear=ITALIC_SHEAR):
    """返回文字 RGBA 图层和 (首行左端, 顶, 最宽行右端, 底)。"""
    w, h = size
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    ascent, descent = font.getmetrics()
    line_h = ascent + descent
    block_h = line_h * len(lines) + line_gap * (len(lines) - 1)
    top = int(h * center_y - block_h / 2)
    widest = max(draw.textlength(line, font=font) for line in lines)
    for i, line in enumerate(lines):
        lw = draw.textlength(line, font=font)
        draw.text(((w - lw) / 2, top + i * (line_h + line_gap)), line, font=font, fill=color + (alpha,))
    if shear:
        # 以文字块中线为轴斜切，保持居中
        mid = top + block_h / 2
        layer = layer.transform(size, Image.AFFINE, (1, shear, -shear * mid, 0, 1, 0), Image.BICUBIC)
    first_left = (w - draw.textlength(lines[0], font=font)) / 2
    return layer, (first_left, top, (w + widest) / 2, top + block_h)


def soft_shadow(text, blur, alpha, offset=(0, 2)):
    """从文字图层的 alpha 生成煤灰黑柔影。"""
    mask = text.getchannel("A").filter(ImageFilter.GaussianBlur(blur))
    mask = mask.point(lambda v: min(255, int(v * alpha)))
    shadow = Image.new("RGBA", text.size, COAL_BLACK + (0,))
    shadow.putalpha(mask)
    moved = Image.new("RGBA", text.size, (0, 0, 0, 0))
    moved.paste(shadow, offset)
    return moved


def dark_band(size, block, pad_y, strength, width_ratio):
    """横向两端渐隐、纵向上下渐隐的暗带。"""
    w, h = size
    top, bottom = block[1] - pad_y, block[3] + pad_y
    band = Image.new("L", size, 0)
    px = band.load()
    half_w = w * width_ratio / 2
    cx, cy, half_h = w / 2, (top + bottom) / 2, (bottom - top) / 2
    for y in range(max(0, int(top - half_h)), min(h, int(bottom + half_h))):
        fy = max(0.0, 1 - abs(y - cy) / (half_h * 1.6))
        for x in range(w):
            fx = max(0.0, 1 - abs(x - cx) / half_w)
            px[x, y] = int(255 * strength * (fx * fx * (3 - 2 * fx)) * (fy * fy * (3 - 2 * fy)))
    band = band.filter(ImageFilter.GaussianBlur(18))
    layer = Image.new("RGBA", size, COAL_BLACK + (0,))
    layer.putalpha(band)
    return layer


def inward_vignette(base, strength, desaturate):
    """独白出现时画面四周压暗、褪色，像注意力收回到自己心里。"""
    w, h = base.size
    small = Image.new("L", (192, 108), 0)
    px = small.load()
    for y in range(108):
        for x in range(192):
            dx, dy = (x - 95.5) / 96, (y - 53.5) / 54
            d = min(1.0, max(0.0, ((dx * dx + dy * dy) ** 0.5 - 0.55) / 0.75))
            px[x, y] = int(255 * d * d * (3 - 2 * d))
    mask = small.resize((w, h), Image.BICUBIC)
    faded = ImageEnhance.Color(base).enhance(1 - desaturate)
    base = Image.composite(faded, base, mask)
    dark = Image.new("RGBA", (w, h), COAL_BLACK + (0,))
    dark.putalpha(mask.point(lambda v: int(v * strength)))
    base.alpha_composite(dark)
    return base


def sticky_note_mark(base, block, size=34):
    """第一行左侧一张微斜的黄色小便签，和外公日记里玩家便签同色。"""
    note = Image.new("RGBA", (size * 2, size * 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(note)
    o = size // 2
    d.rectangle([o, o, o + size, o + size], fill=NOTE_YELLOW + (240,))
    d.polygon([(o + size, o + size - 9), (o + size, o + size), (o + size - 9, o + size)], fill=(214, 194, 104, 240))
    for i in range(3):
        y = o + 9 + i * 6
        d.line([(o + 6, y), (o + size - 7 - (i == 2) * 8, y)], fill=OCHRE_RED + (200,), width=2)
    note = note.rotate(-8, Image.BICUBIC)
    shadow = soft_shadow(note, 4, 1.2, (1, 3))
    x, y = int(block[0] - size * 2 + 4), int(block[1] - 8)
    base.alpha_composite(shadow, (x, y))
    base.alpha_composite(note, (x, y))


ICONS = ROOT / "Icons"


def thought_icon(kind, px):
    """白色透明底的思考 icon，按 4 倍超采样画再缩小。kind 为 cloud、dots 或 trail。"""
    k = 4
    n = px * k
    t = 0.065                                       # 线宽，占 icon 边长的比例
    shape = Image.new("L", (n, n), 0)
    hole = Image.new("L", (n, n), 0)
    sd, hd = ImageDraw.Draw(shape), ImageDraw.Draw(hole)

    def circle(draw, cx, cy, r):
        draw.ellipse([(cx - r) * n, (cy - r) * n, (cx + r) * n, (cy + r) * n], fill=255)

    dots = []
    if kind == "cloud":
        # 五个圆拼成云朵轮廓，左下两颗渐小的圆点
        for cx, cy, r in [(0.40, 0.40, 0.18), (0.61, 0.34, 0.21), (0.76, 0.50, 0.14), (0.56, 0.54, 0.15), (0.34, 0.53, 0.13)]:
            circle(sd, cx, cy, r)
        # 按整体轮廓向内收一圈线宽，拼缝处不留弧线；阈值 0.98 约等于 2.05 倍 sigma
        hole = shape.filter(ImageFilter.GaussianBlur(t * n / 2.05)).point(lambda v: 255 if v >= 250 else 0)
        dots = [(0.22, 0.78, 0.06), (0.11, 0.90, 0.035)]
    elif kind == "dots":
        # 圆角气泡里三个点，尾巴是两颗圆点
        box = [0.24, 0.20, 0.92, 0.62]
        sd.rounded_rectangle([v * n for v in box], radius=0.21 * n, fill=255)
        hd.rounded_rectangle([(box[0] + t) * n, (box[1] + t) * n, (box[2] - t) * n, (box[3] - t) * n], radius=(0.21 - t) * n, fill=255)
        dots = [(0.44, 0.41, 0.04), (0.58, 0.41, 0.04), (0.72, 0.41, 0.04), (0.18, 0.76, 0.06), (0.08, 0.89, 0.035)]
    elif kind == "trail":
        # 只留思考气泡的尾巴：三颗由小到大的圆点
        dots = [(0.20, 0.80, 0.07), (0.43, 0.57, 0.105), (0.72, 0.29, 0.15)]
    ring = Image.composite(Image.new("L", (n, n), 0), shape, hole)
    rd = ImageDraw.Draw(ring)
    for cx, cy, r in dots:
        circle(rd, cx, cy, r)
    icon = Image.new("RGBA", (px, px), (255, 255, 255, 0))
    icon.putalpha(ring.resize((px, px), Image.LANCZOS))
    return icon


def tint(icon, color):
    out = Image.new("RGBA", icon.size, color + (0,))
    out.putalpha(icon.getchannel("A"))
    return out


def thought_mark(base, block, kind, size=54):
    """第一行左侧的思考 icon，和字幕同色同影。"""
    icon = tint(thought_icon(kind, size), PAPER_WHITE)
    x, y = int(block[0] - size - 14), int(block[1] - size * 0.15)
    base.alpha_composite(soft_shadow(icon, 3, 1.4, (0, 2)), (x, y))
    base.alpha_composite(icon, (x, y))


def icon_sheet(kinds, labels):
    """三个 icon 在煤灰黑和暖纸白上的大图，以及 24 像素实际尺寸。"""
    font = ImageFont.truetype(str(IMPRIMA), 26)
    cell = 300
    sheet = Image.new("RGBA", (cell * len(kinds), cell * 2 + 20), COAL_BLACK + (255,))
    draw = ImageDraw.Draw(sheet)
    draw.rectangle([0, cell + 60, sheet.width, sheet.height], fill=PAPER_WHITE + (255,))
    for i, (kind, label) in enumerate(zip(kinds, labels)):
        x = i * cell
        big = thought_icon(kind, 180)
        sheet.alpha_composite(tint(big, PAPER_WHITE), (x + 60, 20))
        small = thought_icon(kind, 32)
        sheet.alpha_composite(tint(small, PAPER_WHITE), (x + 134, 214))
        draw.text((x + 20, cell + 14), label, font=font, fill=PAPER_WHITE)
        sheet.alpha_composite(tint(big, COAL_BLACK), (x + 60, cell + 80))
        sheet.alpha_composite(tint(small, COAL_BLACK), (x + 134, cell + 274))
    sheet.convert("RGB").save(OUT / "ThoughtIcon_Sheet.png")


def render(name, font_size, line_gap, shadow_blur, shadow_alpha, band=None, rule=False, text_alpha=255,
           font_path=IMPRIMA, shear=ITALIC_SHEAR, vignette=None, note=False, icon=None):
    base = Image.open(BACKGROUND).convert("RGBA")
    if vignette:
        base = inward_vignette(base, *vignette)
    font = ImageFont.truetype(str(font_path), font_size)
    text, block = text_layer(base.size, LINES, font, line_gap, CENTER_Y, PAPER_WHITE, text_alpha, shear)
    if band:
        base.alpha_composite(dark_band(base.size, block, *band))
    base.alpha_composite(soft_shadow(text, shadow_blur, shadow_alpha))
    base.alpha_composite(soft_shadow(text, 1.5, 0.9, (0, 1)))
    if rule:
        # 字幕上方一道暗赭红短线，标出这是主人公心里的声音
        draw = ImageDraw.Draw(base)
        cx, y = base.width / 2, block[1] - 24
        draw.line([(cx - 36, y), (cx + 36, y)], fill=OCHRE_RED + (255,), width=3)
    if note:
        sticky_note_mark(base, block)
    if icon:
        thought_mark(base, block, icon)
    base.alpha_composite(text)
    out = OUT / f"{name}.png"
    base.convert("RGB").save(out)
    return out


def contact_sheet(paths, labels, out_name):
    thumbs = [Image.open(p).resize((960, 540), Image.LANCZOS) for p in paths]
    font = ImageFont.truetype(str(IMPRIMA), 30)
    sheet = Image.new("RGB", (960, (540 + 56) * len(thumbs)), COAL_BLACK)
    draw = ImageDraw.Draw(sheet)
    for i, (thumb, label) in enumerate(zip(thumbs, labels)):
        y = i * (540 + 56)
        draw.text((20, y + 12), label, font=font, fill=PAPER_WHITE)
        sheet.paste(thumb, (0, y + 56))
    sheet.save(OUT / out_name)


def band_sprite(width=512, height=128):
    """游戏里字幕暗带用的白色渐变图，Unity 里用 Image 颜色染成煤灰黑并控制浓度。"""
    band = Image.new("L", (width, height), 0)
    px = band.load()
    for y in range(height):
        fy = max(0.0, 1 - abs(y - (height - 1) / 2) / (height / 2))
        for x in range(width):
            fx = max(0.0, 1 - abs(x - (width - 1) / 2) / (width / 2))
            px[x, y] = int(255 * (fx * fx * (3 - 2 * fx)) * (fy * fy * (3 - 2 * fy)))
    sprite = Image.new("RGBA", (width, height), (255, 255, 255, 0))
    sprite.putalpha(band.filter(ImageFilter.GaussianBlur(4)))
    return sprite


if __name__ == "__main__":
    a = render("Monologue_A_ShadowOnly", 40, 8, 6, 1.8)
    b = render("Monologue_B_ShadowBand", 40, 8, 5, 1.3, band=(34, 0.62, 0.7))
    c = render("Monologue_C_BandRule", 36, 8, 5, 1.3, band=(40, 0.62, 0.6), rule=True, text_alpha=240)
    contact_sheet([a, b, c], ["A  Shadow only", "B  Shadow + soft band", "C  Band + ochre rule, smaller"],
                  "Monologue_Compare.png")

    hand = dict(font_path=PLAYER_HAND, shear=0, band=(34, 0.62, 0.7))
    d = render("Monologue_D_PlayerHand", 46, 2, 5, 1.3, **hand)
    e = render("Monologue_E_HandNote", 46, 2, 5, 1.3, note=True, **hand)
    f = render("Monologue_F_HandVignette", 46, 2, 5, 1.3, vignette=(0.45, 0.5), **hand)
    contact_sheet([d, e, f], ["D  Player's sticky-note handwriting", "E  D + small sticky note mark",
                              "F  D + inward vignette while speaking"], "Monologue_Cues_Compare.png")

    kinds = ["cloud", "dots", "trail"]
    ICONS.mkdir(exist_ok=True)
    for kind in kinds:
        thought_icon(kind, 256).save(ICONS / f"ThoughtIcon_{kind.capitalize()}.png")
    icon_sheet(kinds, ["G  Cloud", "H  Dots bubble", "I  Trail"])
    g = render("Monologue_G_IconCloud", 46, 2, 5, 1.3, icon="cloud", **hand)
    h = render("Monologue_H_IconDots", 46, 2, 5, 1.3, icon="dots", **hand)
    i = render("Monologue_I_IconTrail", 46, 2, 5, 1.3, icon="trail", **hand)
    contact_sheet([g, h, i], ["G  D + cloud icon", "H  D + dots bubble icon", "I  D + trail icon"],
                  "Monologue_Icons_Compare.png")
    band_sprite().save(ICONS / "MonologueBand.png")
