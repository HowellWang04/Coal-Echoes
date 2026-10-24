import json,numpy as np
from pathlib import Path
from PIL import Image
root=Path('TerrainReferences/FactoryHighway'); (root/'Prepared').mkdir(exist_ok=True)
g=np.array([-2223.747,-1718.958]); f=np.array([.731,.6824]); f/=np.linalg.norm(f); side=np.array([-f[1],f[0]])
main=np.fromfile(root/'Before/main.f32',dtype='<f4').reshape(6209,6209)
origin=np.array([-2924-2031.25,-914-3031.25]); size=6062.5

def sample(x,z,arr=main):
 n=arr.shape[0]; xx=np.clip((x-origin[0])/size*(n-1),0,n-1.001);zz=np.clip((z-origin[1])/size*(n-1),0,n-1.001)
 ix=xx.astype(int);iz=zz.astype(int);tx=xx-ix;tz=zz-iz
 return ((arr[iz,ix]*(1-tx)+arr[iz,ix+1]*tx)*(1-tz)+(arr[iz+1,ix]*(1-tx)+arr[iz+1,ix+1]*tx)*tz)*1100-710
stations=np.arange(-650,281,2,dtype=float); xy=g+350*f+stations[:,None]*side
raw=sample(xy[:,0],xy[:,1]);k=np.arange(-55,56);w=np.exp(-.5*(k/18)**2);w/=w.sum();h=np.convolve(np.pad(raw,(55,55),mode='edge'),w,mode='valid')+.35
# Limit vertical grades while retaining a long, continuous vertical profile.
for _ in range(3):
 for i in range(1,len(h)):h[i]=np.clip(h[i],h[i-1]-.16,h[i-1]+.16)
 for i in range(len(h)-2,-1,-1):h[i]=np.clip(h[i],h[i+1]-.16,h[i+1]+.16)
h=np.convolve(np.pad(h,(10,10),mode='edge'),np.ones(21)/21,mode='valid')
endheight=float(np.interp(0,stations,h))
# Cubic Hermite interpolation for the approach centre and vertical profile.
def hermite(q,t,y):
 t=np.array(t,dtype=float);y=np.array(y,dtype=float); slopes=np.diff(y)/np.diff(t);m=np.r_[slopes[0],(slopes[:-1]+slopes[1:])/2,slopes[-1]]
 for i in range(1,len(m)-1):
  if slopes[i-1]*slopes[i]<=0:m[i]=0
 idx=np.clip(np.searchsorted(t,q)-1,0,len(t)-2);dt=t[idx+1]-t[idx];a=np.clip((q-t[idx])/dt,0,1)
 return (2*a**3-3*a*a+1)*y[idx]+(a**3-2*a*a+a)*dt*m[idx]+(-2*a**3+3*a*a)*y[idx+1]+(a**3-a*a)*dt*m[idx+1]
def centre(v):return hermite(v,[-14,0,35,80,140,210,275,330,350],[0,0,4,18,7,-20,-25,-3,0])
def approach(v):return hermite(v,[-14,0,20,85,180,265,330,350],[-430.6525,-430.6525,-431.4,-441.8,-458,endheight+5,endheight-.03,endheight+.155])
def smooth(x):x=np.clip(x,0,1);return x*x*(3-2*x)
def grade(x,z,old):
 dx=x-g[0];dz=z-g[1];u=dx*side[0]+dz*side[1];v=dx*f[0]+dz*f[1]
 # End stations stay finite; their last 65 m are concealed by sand and distance.
 ht=np.interp(u,stations,h);cross=np.abs(v-350)
 target=ht+.155*np.maximum(0,1-cross/7.75)-.04*np.maximum(0,cross-7.75)
 feather=np.maximum(8,np.minimum(30,np.abs(target-old)*1.8))
 weight=(1-smooth((cross-10.5)/feather))*smooth((u-stations[0]+8)/12)*smooth((stations[-1]+8-u)/12)
 highway=old+(target-old)*weight
 au=centre(v);dist=np.abs(u-au)
 roadhalf=3.85+2.4*smooth(v/75)+6*smooth((v-318)/25)
 at=approach(v)-.012*np.minimum(dist,roadhalf)
 # Match the highway profile and crown at the mouth.
 blend=smooth((v-330)/12);at=at*(1-blend)+target*blend
 af=np.maximum(7,np.minimum(42,np.abs(at-old)*1.7))
 aw=(1-smooth((dist-roadhalf)/af))*smooth((v+10)/10)*(1-smooth((v-342)/8))
 result=highway+(at-highway)*aw
 return result, np.maximum(weight,aw)
# Only evaluate the road bounding rectangle in the height source.
ends=np.array([g+350*f+stations[0]*side,g+350*f+stations[-1]*side,g]);low=ends.min(0)-65;high=ends.max(0)+65
stats=[]
for lod,n in [('main',6209),('draft',777)]:
 arr=np.fromfile(root/f'Before/{lod}.f32',dtype='<f4').reshape(n,n)
 lo=np.maximum(0,np.floor((low-origin)/size*(n-1)).astype(int)); hi=np.minimum(n,np.ceil((high-origin)/size*(n-1)).astype(int)+1)
 zz,xx=np.mgrid[lo[1]:hi[1],lo[0]:hi[0]];x=origin[0]+xx*size/(n-1);z=origin[1]+zz*size/(n-1)
 before=arr[lo[1]:hi[1],lo[0]:hi[0]].copy();world=before.astype(float)*1100-710
 new,mask=grade(x,z,world); after=((new+710)/1100).astype('<f4');arr[lo[1]:hi[1],lo[0]:hi[0]]=after
 arr.tofile(root/f'Prepared/{lod}.f32')
 stats.append(dict(lod=lod,changed=int(np.count_nonzero(after!=before)),maxFill=float(np.max(new-world)),maxCut=float(np.min(new-world))))
# Paired dirt splines keep the same material and edge algorithm as the factory.
v=np.array([-12,0,20,45,80,110,140,175,210,245,275,305,330,342,349],float);u=centre(v);p=g+v[:,None]*f+u[:,None]*side; a=approach(v)
route=dict(gate=g.tolist(),forward=f.tolist(),side=side.tolist(),offset=350,start=-650,spacing=2,heights=h.tolist(),approach=np.column_stack([p[:,0],a,p[:,1]]).tolist(),bounds=[low.tolist(),high.tolist()],stats=stats)
(root/'route.json').write_text(json.dumps(route,indent=2));print(json.dumps(dict(stats=stats,length=930,junctionHeight=endheight,maxHighwayGrade=float(np.max(np.abs(np.diff(h)/2)))),indent=2))
# HDRP metallic, AO, detail mask, smoothness.
tex=Path('Assets/Art Resources/Environment/FactoryRoads/Highway/Textures')
r=np.array(Image.open(tex/'clean_asphalt_rough_2k.jpg').convert('L'));mask=np.zeros((*r.shape,4),dtype=np.uint8);mask[:,:,1:3]=255;mask[:,:,3]=255-r
Image.fromarray(mask).save(tex/'CleanAsphalt_Mask.png')

