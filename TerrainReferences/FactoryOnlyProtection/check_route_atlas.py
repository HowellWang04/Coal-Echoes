from pathlib import Path
import sys,json
r=Path('TerrainReferences/FactoryOnlyProtection');sys.path.insert(0,str(Path.cwd()/'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from scipy.ndimage import map_coordinates
old=np.fromfile(r/'Before/main_height.f32',np.float32).reshape(6209,6209)*1100-710
new=np.fromfile(r/'Prepared/main_height.f32',np.float32).reshape(6209,6209)*1100-710
out=[]
for p in json.loads((r/'route-positions.json').read_text()):
 co=np.array([[(p['z']+3945.249939)/(1000/1024)],[(p['x']+4955.25049)/(1000/1024)]])
 delta=float(map_coordinates(new-old,co,order=1)[0]);out.append(dict(name=p['name'],height_change_m=delta))
(r/'route-impact.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
