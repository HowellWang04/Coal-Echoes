"""Compare the old platform's splat weights with the current-height MapMagic result."""
from pathlib import Path
import json
import sys
import numpy as np
P=Path(__file__).parent
suffix=sys.argv[1] if len(sys.argv)>1 else 'original'
results=[]
for t in json.loads((P/'candidate.json').read_text()):
    ar=t['ar']; name=t['name']
    def read(part,channels=3):
        shape=(ar,ar,channels) if channels>1 else (ar,ar)
        return np.fromfile(P/f'{name}.{part}.f32','<f4').reshape(shape)
    original=read('original'); actual=read(suffix); expected=read('expected'); mask=read('mask',1)
    difference=np.abs(actual-expected)[mask==1]
    outside_changes=np.count_nonzero(actual[mask==0]!=original[mask==0])
    result=dict(tile=name,mean_error=float(difference.mean()),max_error=float(difference.max()),outside_changed_channels=int(outside_changes))
    result['pass']=bool(result['max_error']<=2/255+1e-6 and outside_changes==0)
    results.append(result)
passed=all(r['pass'] for r in results)
(P/f'check_{suffix}.json').write_text(json.dumps(dict(passed=passed,tiles=results),indent=2))
print(json.dumps(dict(passed=passed,max_error=max(r['max_error'] for r in results),outside_changed_channels=sum(r['outside_changed_channels'] for r in results),tiles=len(results))))
raise SystemExit(0 if passed else 1)
