"""Prepare the agreed terrain layout without touching Unity scene files."""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).parent
TEMP = Path('Temp/SheepRangeIntegration')
ASSETS = Path('Assets/TerrainData/SheepRangeDEM')
ASSETS.mkdir(parents=True, exist_ok=True)
Image.MAX_IMAGE_PIXELS = None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as tif:
    source = np.array(tif.crop((4806,2006,10006,8006)))
assert np.all(source > -999000), 'The extended source contains missing data'
layout = json.loads((TEMP / 'layout_candidates.json').read_text())[1]
origin = np.array([-3924.00049, -2913.999939])
base_elevation = 1363.599609375
height_range = 1000.0
n = 6144
step = 6000 / n
x = np.arange(n, dtype=np.float32) * step - 1000
z = x[::-1].copy()

def sample(e, north):
    ix = np.clip(np.asarray(e) + 999.5, 0, 5199)
    iz = np.clip(4999.5 - np.asarray(north), 0, 5999)
    x0 = ix.astype(int); z0 = iz.astype(int)
    x1 = np.minimum(x0 + 1, 5199); z1 = np.minimum(z0 + 1, 5999)
    fx = ix - x0; fz = iz - z0
    return ((source[z0, x0] * (1-fx) + source[z0, x1] * fx) * (1-fz)
            + (source[z1, x0] * (1-fx) + source[z1, x1] * fx) * fz)

height = np.empty((n, n), np.float32)
for i, north in enumerate(z):
    height[i] = sample(x, north)
fx, fz = layout['factory']
# Include existing entrance, railway, player spawn and the factory footprint.
rect = [fx-360, fx+185, fz-245, fz+135]
dx = np.maximum(np.maximum(rect[0]-x, x-rect[1]), 0)[None, :]
dz = np.maximum(np.maximum(rect[2]-z, z-rect[3]), 0)[:, None]
distance = np.sqrt(dx*dx + dz*dz)
t = np.clip(1-distance/100, 0, 1)
weight = t*t*(3-2*t)
height += weight * (layout['elevation']-height)
platform_max_edit = max(float(np.max(np.abs(height[i]-sample(x,north)))) for i,north in enumerate(z))

# Small flat tower pads; full terrain shape remains elsewhere.
towers = []
names = ['tajia03', 'tajia03 (1)', 'tajia01', 'tajia03 (3)', 'tajia03 (4)',
         'tajia03 (5)', 'tajia03 (6)', 'tajia03 (7)', 'tajia03 (10)', 'tajia03 (11)']
for i, (tx, tz) in enumerate(layout['towers']):
    on_platform = rect[0] <= tx <= rect[1] and rect[2] <= tz <= rect[3]
    yy = layout['elevation'] if on_platform else float(sample(tx, tz))
    col = np.where(abs(x-tx)<38)[0]; row = np.where(abs(z-tz)<38)[0]
    dist = np.hypot(x[col][None, :]-tx, z[row][:, None]-tz)
    t = np.clip((36-dist)/22, 0, 1); w = t*t*(3-2*t)
    sub = height[np.ix_(row,col)]
    height[np.ix_(row,col)] = sub+w*(yy-sub)
    towers.append({'name':names[i], 'local_xz':[tx,tz], 'world_xz':(origin+[tx,tz]).tolist(),
                   'ground_y':yy-base_elevation-710, 'source_elevation':yy})

edge = np.minimum(np.minimum(x+1000,4200-x)[None,:], np.minimum(z+1000,5000-z)[:,None])
t = np.clip(edge/700,0,1); mask = t*t*(3-2*t)
np.rint(np.clip((height-base_elevation)/height_range,0,65535/65536)*65536).astype('<u2').tofile(ASSETS/'SheepRange_Height.raw')
np.rint(mask*65535).astype('<u2').tofile(ASSETS/'SheepRange_Coverage.raw')
np.save(TEMP/'prepared_elevation.npy',height)
factory_world = origin+layout['factory']
delta = [float(factory_world[0]+1342), float(layout['elevation']-base_elevation), float(factory_world[1]+1104)]
config = {'status':'prepared', 'source_bounds_utm':[654800,4072000,660000,4078000],
          'size_m':6000, 'core_size_m':4000, 'resolution':n, 'sample_spacing_m':step, 'world_origin_xz':(origin-1000).tolist(),
          'core_world_origin_xz':origin.tolist(), 'import_offset':[-2000,-3000], 'import_scale':6, 'world_base_y':-710,
          'height_range_m':height_range, 'source_base_elevation':base_elevation,
          'factory_world_xz':factory_world.tolist(), 'factory_y':layout['elevation']-base_elevation-710,
          'factory_translation':delta, 'factory_rect_local':rect, 'factory_blend_m':100,
          'factory_max_grading_m':platform_max_edit, 'tower_pad_radius_m':14,'tower_blend_outer_m':36,
          'towers':towers,'route_length_m':layout['length'],'route_gain_m':towers[-1]['ground_y']-towers[0]['ground_y'],
          'dem_edge_blend_m':700, 'main_resolution':1025,'draft_resolution':129}
(OUT/'layout_config.json').write_text(json.dumps(config,indent=2))
small=height[1024:5120:5,1024:5120:5]; gy,gx=np.gradient(small,step*5)
shade=np.clip((-.5*gx+.5*gy+.7071)/np.sqrt(gx*gx+gy*gy+1),0,1)
im=Image.fromarray(np.uint8((.25+.75*shade)*240)).convert('RGB').resize((1000,1000))
draw=ImageDraw.Draw(im); font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
def pt(v): return (v[0]/4,1000-v[1]/4)
draw.rectangle((pt([rect[0],rect[3]]),pt([rect[1],rect[2]])),outline='#59dfec',width=4)
draw.text(pt([fx-360,fz+245]),'FACTORY',font=font,fill='#125c7f')
draw.line([pt(t['local_xz']) for t in towers],fill='#e99113',width=4)
for i,tower in enumerate(towers):
    px,py=pt(tower['local_xz']);draw.ellipse((px-5,py-5,px+5,py+5),fill='#e99113')
    draw.text((px-5,py+8),str(i+1),font=font,fill='#843a00')
draw.text((20,20),'4 km x 4 km | north up | terrain-first layout',font=font,fill='#20262e')
im.save(OUT/'layout_preview.png')
print(json.dumps({k:v for k,v in config.items() if k!='towers'},indent=2))
