"""Check linked rings and the closed locking route using their construction curves."""
from pathlib import Path
import json
from mathutils import Vector

SOURCE=Path(__file__).resolve().parent
data=json.loads((SOURCE/'chain_geometry.json').read_text(encoding='utf-8'))
rings=[[Vector(p) for p in ring] for ring in data['chain_rings']]
shackle=[Vector(p) for p in data['closed_lock_shackle']]


def links_through_ring(ring,curve):
    center=sum(ring[:-1],Vector())/(len(ring)-1)
    axis=ring[0]-center
    across=ring[8]-center
    normal=axis.cross(across).normalized()
    count=0
    for a,b in zip(curve,curve[1:]):
        da,db=(a-center).dot(normal),(b-center).dot(normal)
        if da*db<0:
            p=a.lerp(b,da/(da-db))-center
            x,y=p.dot(axis)/axis.length_squared,p.dot(across)/across.length_squared
            if x*x+y*y<1:
                count+=1 if db>da else -1
    return abs(count)


def segment_distance(p1,q1,p2,q2):
    d1,d2,r=q1-p1,q2-p2,p1-p2
    a,e,b,c,f=d1.dot(d1),d2.dot(d2),d1.dot(d2),d1.dot(r),d2.dot(r)
    denominator=a*e-b*b
    s=max(0,min(1,(b*f-c*e)/denominator)) if denominator>1e-16 else 0
    t=(b*s+f)/e
    if t<0:
        t=0
        s=max(0,min(1,-c/a))
    elif t>1:
        t=1
        s=max(0,min(1,(b-c)/a))
    return (p1+d1*s-p2-d2*t).length


def clearance(a,b,radii):
    return min(segment_distance(p,q,r,s) for p,q in zip(a,a[1:]) for r,s in zip(b,b[1:]))-radii


adjacent=[links_through_ring(a,b) for a,b in zip(rings,rings[1:])]
end_links=[links_through_ring(rings[i],shackle) for i in (0,-1)]
gaps=[clearance(rings[i],rings[j],.010) for i in range(len(rings)) for j in range(i+1,min(i+3,len(rings)))]
route=[Vector(p) for p in data['route']]
route.append(route[0])
handle_crossings=[]
handle_gaps=[]
for hx in (-.19,.19):
    handle=[Vector(p) for p in [(hx-.075,-.11,1.66),(hx-.075,-.20,1.66),(hx-.065,-.225,1.66),
            (hx+.065,-.225,1.66),(hx+.075,-.20,1.66),(hx+.075,-.11,1.66)]]
    handle_gaps.append(min(clearance(ring,handle,.019) for ring in rings))
    # Handle aperture is in the horizontal plane z = 1.66.
    crossings=0
    for a,b in zip(route,route[1:]):
        da,db=a.z-1.66,b.z-1.66
        if da*db<0:
            p=a.lerp(b,da/(da-db))
            if hx-.060<p.x<hx+.060 and -.210<p.y<-.124:
                crossings+=1 if db>da else -1
    handle_crossings.append(abs(crossings))
result={'chain_links':len(rings),'adjacent_ring_linking_numbers':adjacent,
        'terminal_links_threaded_by_shackle':end_links,
        'closed_route_through_handle_apertures':handle_crossings,
        'minimum_neighbor_wire_clearance_m':min(gaps),
        'terminal_shackle_clearance_m':[clearance(rings[i],shackle,.013) for i in (0,-1)],
        'handle_wire_clearance_m':handle_gaps}
(SOURCE/'chain_verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('CHAIN_VERIFICATION',json.dumps(result),flush=True)
assert all(n==1 for n in adjacent)
assert end_links==[1,1]
assert handle_crossings==[1,1]
assert min(gaps)>-.0005
assert min(result['terminal_shackle_clearance_m'])>-.0005
assert min(handle_gaps)>-.0005
assert max(handle_gaps)<.001
print('CHAIN_CHECKS_PASSED',flush=True)
