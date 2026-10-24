# ArtSource/Props/OldPhotoFrame/

## 用途

第一关门卫室墙上的旧木相框，用于街道照片的回响。深棕木框四角斜接带浅凹线脚，旧漆色差、划痕和边缘磨损，背面有纤维背板、固定片、铆钉和挂环；玻璃只在右上角局部开裂，照片主体清楚。

## 内容

- `Blender/OldPhotoFrame.blend`：源文件，图像已打包，背景墙、相机和灯光在 PreviewStudio 集合里，不导出。
- `Exports/`：FBX 和带内嵌贴图的 GLB，GLB 保留 transmission 和 IOR 扩展。
- `References/StreetMarket_Original.png`：归档的街市黑白照片原图，686 × 509，沿街商铺、招牌、遮阳棚、摊位、推车和行人，未裁剪或修图，`build_frame.py` 从这里读。
- `Textures/`：Wood（BaseColor、EdgeColor、Roughness、Normal、HDRP_Mask）、Backboard_BaseColor、Glass（Transmission、Roughness、Normal、CrackMask、HDRP_Mask、BaseColorOpacity），以及原始照片副本 `StreetMarket_Original.png`。
- `Previews/`：整体、正面、玻璃局部和 GLB 回导的 Blender 渲染。
- `Scripts/build_frame.py`、`verify_frame.py`：读归档照片生成材质、模型和预览，以及重新导入导出文件检查。`build.log`、`verify.log`、`verification.json` 是运行输出。
- `UnityIntegration/`：`ImportAndPlace.cs` 导入并平放到旧门卫室桌上（现已改挂墙，摆放见 `../GuardhouseFurniture/`），`VerifyAndSave.cs` 检查并保存，`placement.json`、`verification.json`、`console_status.json` 是结果，`Previews/` 是 Unity 近景和桌面环境截图，`Before/` 是摆放前的场景。

## 与其他部分的关系

- 相框开口按 `References/` 里原图的比例做。
- 预制体 `Assets/Art Resources/Props/OldPhotoFrame/OldPhotoFrame.prefab`，HDRP/Lit 独立材质；场景实例是 `In-Scene Objects/工厂大门/Guard Post/室内陈设/OldPhotoFrame_StreetMarket`，歪挂在门卫室后墙上，背面贴墙。根对象 +Z 是照片正面，回响的只认正面按它判断，挪动时保持 +Z 朝屋里。
- 玻璃在 Unity 里用透明混合、裂纹 Alpha、法线和光滑度表现，没有启用折射。

## 注意

- 模型宽 0.320 米，底边在原点高度；Blender 里 Z 朝上、正面朝 -Y，导出文件 Y 朝上。
- 预制体停用了 FineFractureEdges 的渲染：极细的裂纹几何在这里约两千米量级的世界坐标下出点状瑕疵，玻璃裂纹由贴图保留。
- 颜色和照片贴图用 sRGB，法线、粗糙度、透射、裂纹遮罩和 Mask 按数据贴图处理；Normal 用 OpenGL 约定；HDRP_Mask 没有烘焙 AO。
- 没有碎片掉落、破碎动画和碰撞。照片的拍摄地点、年代和出处未定。
