"""Fetch the adjacent surveyed tiles so the existing fragments can continue east."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
from urllib.request import urlretrieve
import hashlib,json
ROOT=Path(__file__).resolve().parent
DEST=ROOT.parents[1]/'Library/NativeDEMOnlySources'
DEST.mkdir(parents=True,exist_ok=True)
BASE='https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/1m/Projects/NV_Southern_D23/TIFF/'
def download(key):
    name=f'USGS_1M_11_{key}_NV_Southern_D23.tif'
    path=DEST/name
    expected={'x66y407':293721182,'x66y408':297931569,'x65y406':151965382,'x66y406':276892047}[key]
    if not path.exists() or path.stat().st_size!=expected:
        print('downloading',name,flush=True)
        urlretrieve(BASE+name,path)
    assert path.stat().st_size==expected
    record=dict(file=name,url=BASE+name,bytes=expected,sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    print('complete',name,flush=True)
    return record
with ThreadPoolExecutor(max_workers=2) as pool:
    rows=list(pool.map(download,['x66y407','x66y408','x65y406','x66y406']))
(ROOT/'source_downloads.json').write_text(json.dumps(rows,indent=2))
