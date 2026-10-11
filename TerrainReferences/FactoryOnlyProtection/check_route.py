from pathlib import Path
import sys,json
r=Path('TerrainReferences/FactoryOnlyProtection');sys.path.insert(0,str(Path.cwd()/'Library/GradientTerrainExampleTools/python-deps'))
import numpy as np
from scipy.ndimage import map_coordinates
old=np.load(r/'current.npy');new=np.load(r/'candidate.npy');fade=np.load(r/'blend.npy');new=old*(1-fade)+new*fade
for p in json.loads((r/'route-positions.json').read_text()):
 co=np.array([[(p['z']+3986)/2],[(p['x']+4996.00049)/2]])
 print(p['name'],round(float(map_coordinates(new-old,co,order=1)[0]),2))
