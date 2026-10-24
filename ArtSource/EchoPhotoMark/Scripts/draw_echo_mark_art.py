"""Draw the Echo photo marks: ink stamp, note paper and tape.

Usage: python -I draw_echo_mark_art.py <output dir>
Pillow only; drawn at 4x and scaled down.
"""
import random
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

SS = 4
ROOT = Path(__file__).resolve().parents[3]
IMPRIMA = ROOT / "Assets/Art Resources/UI/Fonts/Imprima 400.ttf"

STAMP_INK = (168, 67, 44)
PAPER = (247, 237, 213)
TAPE = (226, 192, 120)


def noise_mask(size, blur, seed, lo, hi):
    """Blurred random field remapped so values below lo vanish and above hi stay solid."""
    rnd = random.Random(seed)
    w, h = size
    small = Image.new("L", (max(1, w // blur), max(1, h // blur)))
    small.putdata([rnd.randint(0, 255) for _ in range(small.width * small.height)])
    field = small.resize(size, Image.BICUBIC).filter(ImageFilter.GaussianBlur(blur / 2))
    return field.point(lambda v: 0 if v < lo else 255 if v > hi else int((v - lo) * 255 / (hi - lo)))


def draw_stamp(out):
    w, h = 512 * SS, 240 * SS
    shape = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(shape)
    m = 10 * SS
    outer = 22 * SS
    d.rounded_rectangle((m, m, w - m, h - m), radius=30 * SS, outline=255, width=outer)
    inset = m + outer + 12 * SS
    d.rounded_rectangle((inset, inset, w - inset, h - inset), radius=16 * SS, outline=255, width=8 * SS)

    font = ImageFont.truetype(str(IMPRIMA), 118 * SS)
    letters = "ECHO"
    spacing = 22 * SS
    widths = [d.textlength(c, font=font) for c in letters]
    x = (w - (sum(widths) + spacing * (len(letters) - 1))) / 2
    for c, cw in zip(letters, widths):
        d.text((x, h / 2), c, font=font, fill=255, anchor="lm", stroke_width=3 * SS, stroke_fill=255)
        x += cw + spacing

    # Uneven ink: big pale patches plus fine speckle, like a worn rubber stamp.
    patches = noise_mask((w, h), 60 * SS, 9, 40, 120)
    speckle = noise_mask((w, h), 3 * SS, 4, 30, 70)
    ink = ImageChops.multiply(shape, ImageChops.multiply(patches, speckle))
    ink = ink.point(lambda v: int(v * 0.92))

    img = Image.new("RGBA", (w, h), STAMP_INK + (0,))
    img.putalpha(ink)
    img.resize((w // SS, h // SS), Image.LANCZOS).save(out / "Echo_Stamp.png")


def draw_note_paper(out):
    w, h = 256 * SS, 256 * SS
    rnd = random.Random(21)
    torn = 18 * SS
    pts = [(0, 0), (w, 0), (w, h - torn)]
    x = w
    while x > 0:
        x -= rnd.randint(6, 14) * SS
        pts.append((max(x, 0), h - torn + rnd.randint(0, torn - 2 * SS)))
    pts.append((0, h - torn))
    alpha = Image.new("L", (w, h), 0)
    ImageDraw.Draw(alpha).polygon(pts, fill=255)

    grain = noise_mask((w, h), 2 * SS, 5, 0, 255).point(lambda v: v // 32 - 4)
    base = Image.new("RGB", (w, h), PAPER)
    tint = Image.merge("RGB", [grain.point(lambda v, c=c: max(0, min(255, c + (v if v < 128 else v - 256)))) for c in PAPER])
    paper = Image.blend(base, tint, 0.6)
    paper.putalpha(alpha)
    paper.resize((w // SS, h // SS), Image.LANCZOS).save(out / "Echo_Note_Paper.png")


def draw_tape(out):
    w, h = 256 * SS, 72 * SS
    rnd = random.Random(33)
    pts = []
    for y in range(0, h + 1, 6 * SS):  # ragged torn ends
        pts.append((rnd.randint(0, 8) * SS, y))
    right = []
    for y in range(0, h + 1, 6 * SS):
        right.append((w - rnd.randint(0, 8) * SS, y))
    alpha = Image.new("L", (w, h), 0)
    ImageDraw.Draw(alpha).polygon(pts + right[::-1], fill=int(255 * 0.7))

    img = Image.new("RGB", (w, h), TAPE)
    d = ImageDraw.Draw(img)
    for _ in range(90):  # faint fibers along the tape
        x = rnd.randint(0, w)
        d.line((x, 0, x + rnd.randint(-6, 6) * SS, h), fill=(236, 206, 140), width=SS)
    img.putalpha(alpha)
    img.resize((w // SS, h // SS), Image.LANCZOS).save(out / "Echo_Note_Tape.png")


if __name__ == "__main__":
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    draw_stamp(out)
    draw_note_paper(out)
    draw_tape(out)
