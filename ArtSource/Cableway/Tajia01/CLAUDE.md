# ArtSource/Cableway/Tajia01/

## 用途

按旧模型 tajia01 重建的独立索道塔架和配套吊斗，钢构、连接板、螺栓、平台格栅、爬梯齐全，表面取自旧塔架的锈蚀钢材贴图。主场景的塔架已换成 `../Tajia02_Metric15/` 的米制模型，这套只留在 Assets 的 CablewayTower_Rebuilt 预制体里，没有场景实例。

## 内容

- `Blender/Tajia01_Tower_v01.blend`：源文件，保留放大的建模场景、隐藏的独立构件集合、烘焙后的塔架和预览灯光；同目录 `*_status.txt` 是各步脚本写出的状态记录。
- `Scripts/`：`build_tower.py` 通过 Blender MCP 建模，`prepare_export.py` 合并成单网格并展 UV，`bake_textures.py` 烘焙，`assemble_pbr.py` 打包 HDRP 和 glTF 贴图，`export_tower.py` 导出，`verify_exports.py` 重新导入检查；`transfer_legacy_surface.py`、`refine_legacy_members.py`、`finalize_legacy_surface.py` 把旧塔架的锈蚀贴图按构件主轴映射到新 UV 并生成表面贴图；`adjust_palette.py`、`match_old_tower_palette.py`、`add_corrosion_detail.py` 是早先的配色和锈蚀方案脚本。
- `Exports/`：塔架 FBX 和 GLB，原点在塔底中央。
- `Textures/`：`Tower_BaseColor.png` sRGB 颜色图，`Tower_Normal.png` 切线空间法线，`Tower_HDRP_Mask.png` R 金属度、G 环境遮蔽、B 全白、A 光滑度，`Tower_OcclusionRoughnessMetallic.png` 给 GLB 用；Metallic、Roughness、AO 单通道图，`Tower_CorrosionMask.png` 侵蚀遮罩，`Tower_LegacyTransfer_*`、`Tower_MemberRefined_*` 是贴图转移的中间图集。
- `Previews/`：导出 GLB 重新导入后的整体、近景、背面渲染，以及贴图转移各阶段的检查图。
- `References/`：旧模型视图、旧塔架颜色和锈斑参考。`Concepts/`：imagegen 生成的塔架和吊斗概念图，提示词在 `generation-prompts.md`。
- `Naming.md`：场景对象和 Unity 资源的旧名新名对照。
- `UnityIntegration/`：Unity CLI 一次性脚本（`prepare_assets.cs` 配材质，`prepare_prefab.cs` 建预制体，`replace_instance.cs`、`replace_all_towers.cs` 替换场景塔架，`finish_integration.cs` 收尾），替换前的场景备份、各阶段场景截图和 JSON 检查结果。
- `*.json`：配色、贴图转移各轮的记录和导出检查。
- `CoalBucket/`：配套吊斗，见其 CLAUDE.md。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/Cableway/CablewayTower_Rebuilt/`：`CablewayTower_v01.fbx`、`CoalBucket_v01.fbx`、`CablewayTower_Rebuilt.prefab`、`Materials/CablewayTower.mat` 和 `CoalBucket.mat`，颜色贴图开 sRGB，法线和遮罩关 sRGB。
- 主场景 `In-Scene Objects/索道群/塔架/CablewayTower_01` 至 `_08` 现在都是 `../Tajia02_Metric15/` 的预制体，不引用这个预制体。
- 表面纹理取自旧塔架 tajia03 的颜色图 `Assets/Art Resources/Environment/Cableway/texture/texture_pbr_20250901.002 1.png`。

## 注意

- 导出缩放 0.10034278072840624 是为对齐旧 FBX 的导入尺度，包围盒高 1.035036 与旧 FBX 一致，都不是米制。
- `Scripts/` 和 `UnityIntegration/` 里写的是制作时的旧名（`tajia01`、`Tajia01_Rebuilt`），对现在的场景或资源重跑前要按 `Naming.md` 改对象查询和资源路径。
- `Naming.md` 表里的 CablewayTower_Legacy 三份旧 FBX 已删除，只作对照。
- 模型没有碰撞体、LOD 和动画。
