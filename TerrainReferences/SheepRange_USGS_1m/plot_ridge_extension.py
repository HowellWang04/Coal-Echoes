"""Draw the measured route on a native DEM relief map."""
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

folder = Path(__file__).parent
plan = json.loads((folder / 'ridge_extension.json').read_text())
old = json.loads((folder / 'layout_config.json').read_text(encoding='utf-8-sig'))
Image.MAX_IMAGE_PIXELS = None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    source = np.array(im.crop((4806, 2006, 10006, 8006)))
width, height = 1250, 1450
bounds = (-3000, -1600, -2100, -650)
xs = np.linspace(bounds[0], bounds[1], width)
zs = np.linspace(bounds[3], bounds[2], height)
ix = xs + 4924.00049 - .5
iz = 2085.999939 - zs - .5
c, r = ix.astype(int), iz.astype(int)
fx, fz = ix-c, iz-r
h = ((source[r[:,None],c]*(1-fx)+source[r[:,None],c+1]*fx)*(1-fz[:,None])
     +(source[r[:,None]+1,c]*(1-fx)+source[r[:,None]+1,c+1]*fx)*fz[:,None])
h -= old['source_base_elevation']+710
gz, gx = np.gradient(h, (bounds[3]-bounds[2])/height, (bounds[1]-bounds[0])/width)
shade = np.clip((.6*gx-.4*gz+.707)/np.sqrt(gx*gx+gz*gz+1), 0, 1)
rgb = np.stack([(.36+.64*shade)*211, (.36+.64*shade)*194, (.36+.64*shade)*158], -1)
bands = np.floor(h/20)
contour = np.zeros_like(h, dtype=bool)
contour[1:] |= bands[1:] != bands[:-1]
contour[:,1:] |= bands[:,1:] != bands[:,:-1]
rgb[contour] *= .8
image = Image.fromarray(rgb.astype('uint8'))
draw = ImageDraw.Draw(image)
font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 22)
small = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 18)

def pt(p):
    return ((p[0]-bounds[0])/(bounds[1]-bounds[0])*width,
            (bounds[3]-p[1])/(bounds[3]-bounds[2])*height)

def label(p, text, colour='white', f=font):
    draw.text(p, text, font=f, fill=colour, stroke_width=2, stroke_fill='#17232d')

draw.polygon([pt(p) for p in plan['factory']['corners']], fill='#295b65', outline='#66f0e8', width=4)
label(pt([-2130,-1640]), '当前厂区保持不动', '#b9fffb')
towers = plan['towers']
points = np.array([t['xz'] for t in towers])
draw.line([pt(p) for p in points], fill='#d8ff53', width=5)
for i, tower in enumerate(towers):
    u, v = pt(tower['xz'])
    colour = '#ff5577' if tower['fixed_xz'] else '#ffdb60'
    draw.ellipse((u-7,v-7,u+7,v+7), fill=colour, outline='#14212a', width=2)
    label((u+15,v-12), tower['name'], colour)
    if i:
        mid = pt((points[i]+points[i-1])/2)
        label((mid[0]-95,mid[1]-10), f'{np.linalg.norm(points[i]-points[i-1]):.0f}米', '#d8ff53', small)
label((25,20), '山脊延长线路   北向上   等高距20米')
label((25,57), '粉色为四个水平固定点   黄色为重新分配的塔架', f=small)
draw.rectangle((15,height-122,width-15,height-15), fill='#17232d')
label((30,height-112), f'线路约 {plan["route_length_m"]:.0f} 米   11座塔架   间距约90至166米')
label((30,height-77), '原下段密集塔架移至上段   新终点 tajia03 (8)', f=small)
label((30,height-45), '底图显示原始DEM   塔脚小平台以Unity生成结果为准', f=small)
image.save(folder / 'ridge_extension_layout.png')
