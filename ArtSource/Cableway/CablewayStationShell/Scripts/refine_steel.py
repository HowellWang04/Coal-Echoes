import bpy, numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=ROOT.parents[2]/'Assets/Art Resources/Environment/Cableway/texture/texture_pbr_20250901.002 1.png'
im=bpy.data.images.load(str(source),check_existing=True)
w,h=im.size;pixels=np.empty(w*h*4,np.float32);im.pixels.foreach_get(pixels)
crop=pixels.reshape(h,w,4)[h-728:h-280,1044:1124,:3]
N=2048
x=np.linspace(0,crop.shape[1]-1,N);y=np.linspace(0,crop.shape[0]-1,N)
result=np.empty((N,N,4),np.float32);result[:,:,3]=1
for c in range(3):
 rows=np.array([np.interp(x,np.arange(crop.shape[1]),row[:,c]) for row in crop],np.float32)
 result[:,:,c]=np.array([np.interp(y,np.arange(crop.shape[0]),rows[:,i]) for i in range(N)],np.float32).T
result[:,:,:3]=result[:,:,:3]*np.array([1,.98,.90])*.68+np.array([.31,.295,.26])*.32
image=bpy.data.images['Station_Steel_BaseColor'];image.pixels.foreach_set(result.ravel());image.filepath_raw=str(ROOT/'Textures/Station_Steel_BaseColor.png');image.save();image.pack()
print('Station steel uses the same inspected original metal strip as the rebuilt cableway tower.')
