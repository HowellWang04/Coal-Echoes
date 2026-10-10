"""Read-only check that the old factory/pad area in the RAW still matches native DEM."""
from pathlib import Path
import json
import numpy as np
from PIL import Image
P=Path(__file__).parent
root=P.parents[3]
config=json.loads((root/'TerrainReferences/SheepRange_USGS_1m/layout_config.json').read_text())
raw=np.memmap(root/'Assets/TerrainData/SheepRangeDEM/SheepRange_Height.raw',dtype='<u2',mode='r',shape=(6144,6144))
step=6000/6144
xs=np.arange(6144,dtype=np.float32)*step+config['world_origin_xz'][0]
zs=xs[::-1]-config['world_origin_xz'][0]+config['world_origin_xz'][1]
cols=np.where((xs>=-2860)&(xs<=-1590))[0][::4]
rows=np.where((zs>=-2090)&(zs<=-760))[0][::4]
Image.MAX_IMAGE_PIXELS=None
with Image.open('C:/Users/Guanhao/Downloads/USGS_1M_11_x65y408_NV_Southern_D23.tif') as im:
    source=np.array(im.crop((4806,2006,10006,8006)))
xx=np.clip(xs[cols][None,:]+4924.00049-.5,0,5199)
zz=np.clip(2085.999939-zs[rows][:,None]-.5,0,5999)
ix=xx.astype(int); iz=zz.astype(int); fx=xx-ix; fz=zz-iz
native=(source[iz,ix]*(1-fx)+source[iz,ix+1]*fx)*(1-fz)+(source[iz+1,ix]*(1-fx)+source[iz+1,ix+1]*fx)*fz
expected=np.rint(np.clip((native-config['source_base_elevation'])/1000,0,65535/65536)*65536).astype(np.int32)
error=abs(raw[np.ix_(rows,cols)].astype(np.int32)-expected)*1000/65536
report=dict(samples=int(error.size),max_error_m=float(error.max()),mean_error_m=float(error.mean()),passed=bool(error.max()<.031),region_xz=[[-2860,-1590],[-2090,-760]])
(P/'generation_source_native_check.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report));raise SystemExit(0 if report['passed'] else 1)
