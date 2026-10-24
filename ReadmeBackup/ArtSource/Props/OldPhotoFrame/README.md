# 旧木相框

根据项目苍凉、温情、怀旧的美术方向制作。深棕木框采用四角斜接和浅凹线脚，表面有不均匀的旧漆色差、细小划痕与边缘磨损。背面包含纤维背板、固定片、铆钉和挂环。

玻璃仅在右上角局部开裂，大部分玻璃保留，照片主体清楚。没有掉落碎片、破碎动画或碰撞功能。用于第一关门卫室的街道照片回响。

## 文件

[可编辑 Blender 源文件](Blender/OldPhotoFrame.blend)

[FBX 模型](Exports/OldPhotoFrame.fbx)

[含嵌入贴图的 GLB 模型](Exports/OldPhotoFrame.glb)

[整体预览](Previews/OldPhotoFrame_Overall.png)

[正面预览](Previews/OldPhotoFrame_Front.png)

[玻璃局部预览](Previews/OldPhotoFrame_GlassDetail.png)

[GLB 重新导入后的预览](Previews/OldPhotoFrame_GLB_Roundtrip.png)

预览均由交付模型在 Blender 中实际渲染。背景墙、相机与灯光保存在源文件的 PreviewStudio 集合中，未包含在模型导出中。

## 尺寸与照片

模型宽 0.320 米，高约 0.25188 米，包含背部挂环的总厚度约 0.02797 米。底边位于原点高度。Blender 中 Z 轴朝上，正面朝向负 Y。导出文件采用 Y 轴朝上。

照片来自[已归档的街市原图](../../HistoricalPhotos/StreetMarket/StreetMarket_Original.png)，分辨率为 686 × 509。Textures 内保存原始照片副本。开口宽 0.264 米，高约 0.19588 米，按原图比例制作。照片的拍摄地点、年代及出处仍未确定。

模型共有 17 个网格对象、3,636 个三角面和 7 个材质。木框、背板、照片、玻璃、裂纹细边及背部五金可以分别编辑。所有网格均有 UV。

## 材质

木纹、旧漆磨损与玻璃裂纹由制作脚本生成并保存为普通 2048 × 2048 PNG 贴图。照片保持原始分辨率。源文件已经打包所用图像。

| 材质 | 主要贴图与用途 |
| --- | --- |
| Frame_AgedWalnut | Wood_BaseColor、Wood_Roughness、Wood_Normal |
| WornWood_Edges | Wood_EdgeColor，局部露出较浅的木质边缘 |
| Backboard_Fiberboard | Backboard_BaseColor |
| Photo_OriginalBlackAndWhite | StreetMarket_Original |
| Glass_LocalCracks | Glass_Transmission、Glass_Roughness、Glass_Normal |
| Glass_FractureEdges | 细小实体裂纹边缘，用于补充折射和高光细节 |
| Hardware_OxidizedIron | 常量颜色、金属度与粗糙度 |

玻璃厚约 1.3 毫米，Blender 材质 IOR 为 1.47。裂纹遮罩影响透射、粗糙度与法线，框边有轻微污渍。GLB 保留了 transmission 与 IOR 扩展。

为后续 Unity 配置提供 Wood_HDRP_Mask、Glass_HDRP_Mask 与 Glass_BaseColorOpacity。HDRP Mask 的 R 为金属度、G 为白色 AO、B 为白色 Detail Mask、A 为光滑度。这些图没有烘焙环境遮蔽。Glass_BaseColorOpacity 的 RGB 为玻璃颜色，Alpha 为低不透明度玻璃与较高不透明度裂纹的组合，可用于透明材质方案。

颜色与照片贴图使用 sRGB，法线、粗糙度、透射、裂纹遮罩及打包 Mask 作为数据贴图处理。Normal 文件采用 OpenGL 法线约定。Unity 导入后需单独配置透明玻璃材质。

## Unity 场景试放

相框已导入主场景并保存，平放在大门旁门卫室的现有桌面上。Hierarchy 路径为 `In-scene Objects/Factory Entrance/OldPhotoFrame_StreetMarket`。

[可复用预制体](../../../Assets/Art%20Resources/Props/OldPhotoFrame/OldPhotoFrame.prefab)

[Unity 场景近景](UnityIntegration/Previews/Unity_Closeup.png)

使用 HDRP/Lit 独立材质。玻璃通过透明混合、裂纹 Alpha、法线和光滑度显示，未启用折射，不依赖 Blender Cycles。照片在场景现有光照下清楚可见，木框在室内阴影下较暗。

Unity 预制体停用了 FineFractureEdges 的渲染。原本极细的裂纹几何在此处约两千米量级的世界坐标下出现点状瑕疵，停用后瑕疵消失，玻璃裂纹由贴图保留。Unity 当前启用 16 个网格渲染器，共 2,772 个可见三角面。

相框保持原尺寸，四角落在桌面支撑区域并避开中央凹槽，模型最低点距支撑面约 1 毫米。

## 制作环境

本资源由 Blender 5.2.2 LTS 独立后台进程制作，使用 Cycles 与 OptiX 渲染。制作日期为 2026 年 9 月 28 日。

[制作脚本](Scripts/build_frame.py)读取归档照片，生成材质、模型与预览。[验证脚本](Scripts/verify_frame.py)重新导入两个导出文件并检查结果。
