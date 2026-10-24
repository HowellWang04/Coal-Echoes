# ArtSource/Cableway/Tajia01/CoalBucket/

## 用途

Tajia01 重建方案配套的独立运煤吊斗模型，带实际壁厚的敞口斗身、翻边、侧转轴、弯折吊架、顶部双槽轮和吊销，配色沿用同方案塔架的暖深灰钢材、灰褐锈迹和旧漆。主场景的索道已换成 `../../Tajia02_Metric15/` 的米制模型，这只吊斗只留在 Assets 的 CablewayTower_Rebuilt 预制体里，没有场景实例。

## 内容

- `Blender/Tajia01_CoalBucket_v01.blend`：可编辑源文件，保留独立构件和烘焙后的导出网格；同目录 `*_status.txt` 是各步脚本写出的状态记录。
- `Scripts/`：`build_bucket.py` 建模，`prepare_export.py` 应用构件并展成单个导出网格，`bake_textures.py` 烘焙贴图，`assemble_pbr.py` 打包 HDRP 和 glTF 贴图，`bake_package.py` 串起烘焙流程，`export_bucket.py` 导出，`verify_exports.py` 重新导入导出文件并对比渲染。
- `Exports/`：FBX 和 GLB，GLB 内嵌颜色、法线和 ORM 贴图。
- `Textures/`：`CoalBucket_BaseColor.png` 为 sRGB 颜色图；`CoalBucket_Normal.png` 切线空间法线；`CoalBucket_HDRP_Mask.png` R 金属度、G 环境遮蔽、B 全白细节遮罩、A 光滑度，关闭 sRGB；`CoalBucket_OcclusionRoughnessMetallic.png` 给 GLB 用，R 遮蔽、G 粗糙度、B 金属度；另有 Metallic、Roughness、AO 单通道烘焙图。
- `Previews/`：导出 GLB 重新导入 Blender 后渲染的整体、斗身内壁、滑轮吊点和背面。
- `verification.json`：导出检查结果。

## 与其他部分的关系

- Unity 资源是 `Assets/Art Resources/Environment/Cableway/CablewayTower_Rebuilt/CoalBucket_v01.fbx` 和 `Materials/CoalBucket.mat`，由同目录 `CablewayTower_Rebuilt.prefab` 引用，制作时的旧名对照见 `../Naming.md`。
- 概念图是 `../Concepts/CoalBucket_Concept_v01.png`，塔架部分见 `../CLAUDE.md`。
- 导出缩放与塔架相同，为 0.10034278072840624；导出原点在两个滑轮下方的钢缆接触中心，Blender 建模场景里对应 (0, 0, 3.72)。

## 注意

- 导出尺寸是对齐旧资产的导入尺度，不是米制，不能当真实吊斗尺寸用。
- 静态模型，没有翻斗动画、关节、碰撞体和 LOD。
- FBX 要和相邻 `Textures/` 一起用，FBX 自动生成的 Unity 材质不保证 HDRP 绑定正确。
