"""右下角拾取提示：手 icon 和效果图。

手 icon 是一只手从上往下抓住一件小东西。画法和内心独白的云朵思考 icon 一致
（见 ../InnerMonologue/build_mockups.py）：4 倍超采样，线宽占边长 0.065，外形向内收一圈线宽成描边；
小物件是实心的，对应云朵下面的实心圆点。

效果图把三行提示按场景里的尺寸画在独白效果图 G 上，用来定底板浓度。

python build_pickup_notice.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
IMPRIMA = PROJECT / "Assets/Art Resources/UI/Fonts/Imprima 400.ttf"
THOUGHT_CLOUD = PROJECT / "ArtSource/InnerMonologue/Icons/ThoughtIcon_Cloud.png"
BAND = PROJECT / "ArtSource/InnerMonologue/Icons/MonologueBand.png"
GATE_WITH_MONOLOGUE = PROJECT / "ArtSource/InnerMonologue/Previews/Monologue_G_IconCloud.png"
OUT_ICON = ROOT / "Icons/PickupIcon_Hand.png"
OUT_SHEET = ROOT / "Previews/PickupIcon_Sheet.png"
OUT_MOCKUP = ROOT / "Previews/PickupNotice_GateView.png"

# 和场景里 PickupNoticeUIRoot 的数值一致，1920×1080 参考分辨率
ROWS = ["Grandpa's Diary", "Group Photo", "Lock Pick"]   # 从上到下
RIGHT_MARGIN, BOTTOM_MARGIN = 48, 96
ROW_HEIGHT, ROW_SPACING = 40, 8
ICON_SIZE, ICON_GAP, FONT_SIZE = 30, 10, 24
BAND_PAD = (56, 14, 28, 14)                                # 左、上、右、下
BAND_ALPHA = 0.55

PAPER_WHITE = (232, 223, 205)   # 品牌暖纸白 #E8DFCD
COAL_BLACK = (41, 39, 36)       # 品牌煤灰黑 #292724
T = 0.065                       # 线宽，占 icon 边长的比例，和思考 icon 相同


def hand_icon(px):
    k = 4
    n = px * k

    def blank():
        img = Image.new("L", (n, n), 0)
        return img, ImageDraw.Draw(img)

    def box(draw, x0, y0, x1, y1, r):
        draw.rounded_rectangle([x0 * n, y0 * n, x1 * n, y1 * n], radius=r * n, fill=255)

    def capsule(draw, x0, y0, x1, y1, r):
        draw.line([(x0 * n, y0 * n), (x1 * n, y1 * n)], fill=255, width=int(2 * r * n))
        for cx, cy in [(x0, y0), (x1, y1)]:
            draw.ellipse([(cx - r) * n, (cy - r) * n, (cx + r) * n, (cy + r) * n], fill=255)

    def grow(img, r):
        return img.filter(ImageFilter.GaussianBlur(r * n / 2.05)).point(lambda v: 255 if v > 5 else 0)

    # 手背、四根圆头手指和拇指拼成整体外形，向内收一圈线宽，拼缝处不留线
    shape, sd = blank()
    box(sd, 0.30, 0.06, 0.78, 0.46, 0.13)
    fingers = [0.355, 0.465, 0.575, 0.685]
    tips = [0.62, 0.66, 0.65, 0.60]
    for x, y in zip(fingers, tips):
        capsule(sd, x, 0.36, x, y, 0.056)
    capsule(sd, 0.36, 0.30, 0.21, 0.50, 0.065)
    hole = shape.filter(ImageFilter.GaussianBlur(T * n / 2.05)).point(lambda v: 255 if v >= 250 else 0)
    ring = Image.composite(Image.new("L", (n, n), 0), shape, hole)

    # 指缝：从指根往下到较短那根指尖
    rd = ImageDraw.Draw(ring)
    for x0, x1, y in zip(fingers, fingers[1:], tips):
        mx = (x0 + x1) / 2
        capsule(rd, mx, 0.47, mx, min(y, 0.64) - 0.01, T / 2)

    # 被抓住的小物件，实心；指尖压住的地方挖掉一圈，看得出手在它前面
    item, idraw = blank()
    box(idraw, 0.36, 0.66, 0.72, 0.94, 0.07)
    item = Image.composite(Image.new("L", (n, n), 0), item, grow(shape, 0.045))
    mask = Image.composite(Image.new("L", (n, n), 255), ring, item)

    icon = Image.new("RGBA", (px, px), (255, 255, 255, 0))
    icon.putalpha(mask.resize((px, px), Image.LANCZOS))
    return icon


def tint(icon, color):
    out = Image.new("RGBA", icon.size, color + (0,))
    out.putalpha(icon.getchannel("A"))
    return out


def sheet():
    """手 icon 和云朵思考 icon 并排，在煤灰黑和暖纸白上各一份大图和 32 像素小图。"""
    font = ImageFont.truetype(str(IMPRIMA), 26)
    cloud = Image.open(THOUGHT_CLOUD).convert("RGBA")
    icons = [("Pickup hand", hand_icon(256)), ("Thought cloud", cloud)]
    cell = 300
    out = Image.new("RGBA", (cell * len(icons), cell * 2 + 20), COAL_BLACK + (255,))
    draw = ImageDraw.Draw(out)
    draw.rectangle([0, cell + 60, out.width, out.height], fill=PAPER_WHITE + (255,))
    for i, (label, icon) in enumerate(icons):
        x = i * cell
        big = icon.resize((180, 180), Image.LANCZOS)
        small = icon.resize((32, 32), Image.LANCZOS)
        out.alpha_composite(tint(big, PAPER_WHITE), (x + 60, 20))
        out.alpha_composite(tint(small, PAPER_WHITE), (x + 134, 214))
        draw.text((x + 20, cell + 14), label, font=font, fill=PAPER_WHITE)
        out.alpha_composite(tint(big, COAL_BLACK), (x + 60, cell + 80))
        out.alpha_composite(tint(small, COAL_BLACK), (x + 134, cell + 274))
    out.convert("RGB").save(OUT_SHEET)


def soft_shadow(layer, blur, alpha, offset=(0, 2)):
    """从图层的 alpha 生成煤灰黑柔影，近似 TMP 的 Underlay。"""
    mask = layer.getchannel("A").filter(ImageFilter.GaussianBlur(blur))
    mask = mask.point(lambda v: min(255, int(v * alpha)))
    shadow = Image.new("RGBA", layer.size, COAL_BLACK + (0,))
    shadow.putalpha(mask)
    moved = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    moved.paste(shadow, offset)
    return moved


def mockup():
    base = Image.open(GATE_WITH_MONOLOGUE).convert("RGBA")
    font = ImageFont.truetype(str(IMPRIMA), FONT_SIZE)
    band_sprite = Image.open(BAND).convert("RGBA")
    icon = tint(hand_icon(256).resize((ICON_SIZE, ICON_SIZE), Image.LANCZOS), PAPER_WHITE)
    for slot, name in enumerate(reversed(ROWS)):
        right = base.width - RIGHT_MARGIN
        bottom = base.height - BOTTOM_MARGIN - slot * (ROW_HEIGHT + ROW_SPACING)
        top = bottom - ROW_HEIGHT
        text_w = int(ImageDraw.Draw(base).textlength(name, font=font))
        left = right - text_w - ICON_GAP - ICON_SIZE

        pad_l, pad_t, pad_r, pad_b = BAND_PAD
        band = band_sprite.resize((right - left + pad_l + pad_r, ROW_HEIGHT + pad_t + pad_b), Image.LANCZOS)
        band = tint(band, COAL_BLACK)
        band.putalpha(band.getchannel("A").point(lambda v: int(v * BAND_ALPHA)))
        base.alpha_composite(band, (left - pad_l, top - pad_t))

        layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
        layer.alpha_composite(icon, (left, top + (ROW_HEIGHT - ICON_SIZE) // 2))
        ascent, descent = font.getmetrics()
        ImageDraw.Draw(layer).text((right - text_w, top + (ROW_HEIGHT - ascent - descent) // 2), name,
                                   font=font, fill=PAPER_WHITE + (255,))
        base.alpha_composite(soft_shadow(layer, 3, 1.4))
        base.alpha_composite(layer)
    base.convert("RGB").save(OUT_MOCKUP)


if __name__ == "__main__":
    OUT_ICON.parent.mkdir(exist_ok=True)
    OUT_SHEET.parent.mkdir(exist_ok=True)
    hand_icon(256).save(OUT_ICON)
    sheet()
    mockup()
