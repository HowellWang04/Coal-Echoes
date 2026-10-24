"""Build the Coal Echoes wordmarks from the licensed source fonts."""
from pathlib import Path
import json
import random
import sys

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parents[1] / 'Library' / 'BrandingTools'))
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from PIL import Image, ImageDraw, ImageFont

FONTS = ROOT / 'Fonts'
OUT = ROOT / 'Exports'
PREVIEWS = ROOT / 'Previews'
EN = FONTS / 'Bitter-Regular.ttf'
CN = FONTS / 'SourceHanSerifSC-Medium.otf'
PAPER = '#E8DFCD'
COAL = '#292724'
RUST = '#9A6146'
SCALE = 2
W = 1200

def text(draw, label, path, size, x, baseline, spacing, fill):
    font = ImageFont.truetype(str(path), round(size * SCALE))
    for char in label:
        draw.text((round(x * SCALE), round(baseline * SCALE)), char,
                  font=font, anchor='ls', fill=fill)
        x += font.getlength(char) / SCALE + spacing

def outline(label, path, size, x, baseline, spacing):
    font = TTFont(path)
    glyphs = font.getGlyphSet()
    cmap = font.getBestCmap()
    scale = size / font['head'].unitsPerEm
    result = []
    for char in label:
        name = cmap[ord(char)]
        pen = SVGPathPen(glyphs)
        glyphs[name].draw(TransformPen(pen, (scale, 0, 0, -scale, x, baseline)))
        result.append('<path d="' + pen.getCommands() + '"/>')
        x += glyphs[name].width * scale + spacing
    font.close()
    return ''.join(result)

def build(name, color, bilingual=False, compact=False):
    height = 880 if bilingual else 750
    mask = Image.new('L', (W * SCALE, height * SCALE))
    draw = ImageDraw.Draw(mask)
    paths = []
    for label, size, baseline, spacing in [('COAL', 270, 380, 8), ('ECHOES', 270, 630, 5)]:
        text(draw, label, EN, size, 30, baseline, spacing, 255)
        paths.append(outline(label, EN, size, 30, baseline, spacing))
    distress = []
    if not compact:
        rng = random.Random(1047)
        for _ in range(950):
            x, y = rng.uniform(35, 1155), rng.uniform(108, 695)
            r = rng.uniform(.35, 1.25)
            if mask.getpixel((int(x*SCALE), int(y*SCALE))) > 250:
                draw.ellipse(((x-r)*SCALE, (y-r*.55)*SCALE, (x+r)*SCALE, (y+r*.55)*SCALE), fill=0)
                distress.append(f'<ellipse cx="{x:.2f}" cy="{y:.2f}" rx="{r:.2f}" ry="{r*.55:.2f}" fill="black"/>')
        # A suspended industrial bucket, tucked into the space beyond COAL.
        curve = []
        for i in range(241):
            t = i / 240
            curve.append(((810 + 340*t)*SCALE, ((1-t)**2*195 + 2*(1-t)*t*230 + t*t*190)*SCALE))
        draw.line(curve, fill=255, width=9)
        draw.line([(1010*SCALE, 210*SCALE), (1010*SCALE, 300*SCALE)], fill=255, width=9)
        draw.line([(997*SCALE, 300*SCALE), (997*SCALE, 314*SCALE)], fill=255, width=9)
        draw.line([(1023*SCALE, 300*SCALE), (1023*SCALE, 314*SCALE)], fill=255, width=9)
        draw.line([(997*SCALE, 300*SCALE), (1023*SCALE, 300*SCALE)], fill=255, width=9)
        bucket = [(987,314),(1033,314),(1027,346),(993,346)]
        draw.polygon([(x*SCALE,y*SCALE) for x,y in bucket], fill=255)
        cable = '<path d="M810 195 Q980 230 1150 190 M1010 210 V300 M997 314 V300 H1023 V314" fill="none" stroke="'+color+'" stroke-width="4.5"/><path d="M987 314 H1033 L1027 346 H993 Z" fill="'+color+'"/>'
    else:
        cable = ''
    if bilingual:
        text(draw, '煤火回声', CN, 66, 32, 838, 28, 255)
        paths.append(outline('煤火回声', CN, 66, 32, 838, 28))
    image = Image.new('RGBA', mask.size, color)
    image.putalpha(mask)
    if bilingual:
        ImageDraw.Draw(image).rectangle((434*SCALE, 802*SCALE, 450*SCALE, 818*SCALE), fill=RUST)
    image.save(OUT / (name + '.png'))
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{height}" viewBox="0 0 {W} {height}"><title>Coal Echoes'+(' 煤火回声' if bilingual else '')+'</title>'
    svg += '<defs><mask id="wear"><rect width="1200" height="880" fill="white"/>'+''.join(distress)+'</mask></defs>'
    svg += f'<g fill="{color}" mask="url(#wear)">'+''.join(paths)+'</g>'+cable
    if bilingual:
        svg += f'<rect x="434" y="802" width="16" height="16" fill="{RUST}"/>'
    svg += '</svg>'
    (OUT / (name + '.svg')).write_text(svg, encoding='utf-8')
    if compact:
        image = image.crop(image.getbbox())
        image.thumbnail((240, 160), Image.Resampling.LANCZOS)
        image.save(OUT / (name + '-240.png'))
    return image

for folder in (OUT, PREVIEWS):
    folder.mkdir(exist_ok=True)
english = build('coal-echoes-english-paper', PAPER)
build('coal-echoes-english-coal', COAL)
chinese = build('coal-echoes-bilingual-paper', PAPER, bilingual=True)
dark = build('coal-echoes-bilingual-coal', COAL, bilingual=True)
small = build('coal-echoes-small-paper', PAPER, compact=True)

# A fresh presentation board assembled from the vector wordmark's own exports.
board = Image.new('RGB', (1800, 1330), '#24221F')
d = ImageDraw.Draw(board)
d.rectangle((900, 0, 1800, 1000), fill=PAPER)
label_font = ImageFont.truetype(str(EN), 24)
body_font = ImageFont.truetype(str(EN), 28)
d.text((70, 58), 'COAL ECHOES   /   WORDMARK STUDY 01', font=label_font, fill='#B2A897')
d.text((970, 58), 'BILINGUAL LOCKUP   /   PAPER EDITION', font=label_font, fill='#726757')
for asset, pos in [(chinese, (70, 210)), (dark, (970, 210))]:
    sample = asset.copy()
    sample.thumbnail((760, 620), Image.Resampling.LANCZOS)
    board.paste(sample, pos, sample)
d.line((70, 1010, 1730, 1010), fill='#5C5449', width=1)
d.text((70, 1055), 'SMALL WORDMARK   /   240 PX', font=label_font, fill='#B2A897')
board.paste(small, (70, 1120), small)
d.text((465, 1060), 'Bitter Regular', font=body_font, fill=PAPER)
d.text((465, 1110), 'Source Han Serif SC Medium', font=body_font, fill=PAPER)
for i, (color, label) in enumerate([(PAPER, 'PAPER  E8DFCD'), (COAL, 'COAL  292724'), (RUST, 'EMBER  9A6146')]):
    x = 1070 + i*230
    d.rectangle((x, 1060, x+150, 1140), fill=color, outline='#756B5C')
    d.text((x, 1160), label, font=label_font, fill=PAPER)
board.save(PREVIEWS / 'identity-board.png')
report = {'fonts': [ImageFont.truetype(str(p), 32).getname() for p in [EN, CN]],
          'exports': [{'name': p.name, 'size': Image.open(p).size, 'mode': Image.open(p).mode} for p in sorted(OUT.glob('*.png'))],
          'english': 'COAL ECHOES', 'chinese': '煤火回声', 'svg_text': 'Outlined from original fonts; no font installation needed.'}
(ROOT / 'verification.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False))
