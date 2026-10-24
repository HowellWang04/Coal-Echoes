# ArtSource/CoalPile/

## 用途

索道山脚站下方卸煤场的三个中小煤堆，Blender 脚本程序化生成，共用一个材质和一张 4096 贴图图集。A 是单峰小锥堆，B 是卸车形成的双峰长堆，C 是一侧被装载机铲出内凹断面的大堆。

## 内容

- `Blender/CoalPile_v01.blend`：源文件。`Exports/`：三个堆的 FBX，各带 `_LOD0` 到 `_LOD2`。
- `Textures/`：烘焙出的 BaseColor、Normal、Roughness、AO 和打包后的 HDRP_Mask。
- `Scripts/`：在 Blender 里按顺序执行。`build_coal_piles.py` 用高度场建堆体，坡度按煤的自然休止角 36 度，顶点色 `CoalMask` 的 R 是坡脚积灰权重、G 是新鲜断面；`material_uv.py` 建程序化材质并打包 UV，堆体从正上方投影，煤块智能展开并缩小，三个堆进同一张图集；`bake_textures.py` 用 Cycles 烘焙并打包 HDRP 遮罩；`export_coal_piles.py` 每堆导出一个 FBX，后两级 LOD 用 Decimate 30% 和 8%。
- `Previews/`：卸煤场的游戏相机截图。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/CoalPile/`：FBX、`Materials/CoalPile.mat`（HDRP/Lit）、`Textures/` 和三个预制体。预制体带 FBX 自动生成的 LODGroup，切换阈值为屏幕占比 12%、4%、0.5%，用 LOD2 网格做 MeshCollider。沙丘预制体沿用同样的 LOD 阈值。
- 场景实例在 `In-Scene Objects/工厂建筑群/05 堆放物与杂物/煤堆` 下，位于索道山脚站平台下方、厂区南角围墙内侧的平地上。C 挖掘面朝东北、背靠站房山坡，A 紧挨围墙南端外侧。
- `Tools > Coal Echoes > Paint Factory Ground` 会找名字以 `CoalPile_` 开头的 LODGroup，在其下方和周围刷煤灰层，墙内墙外都生效。

## 注意

- 参考尺寸（长 × 宽 × 高，含坡脚散煤和埋入边缘）：A 约 5.5 × 4.5 × 1.6 米，B 约 9.7 × 7.8 × 2.4 米，C 约 10.3 × 9.8 × 3.2 米。原点在堆底中心，网格边缘低于原点 6 厘米，放在平地上地形会盖住接缝。
- 摆放时按脚本搜索的条件：占地内地面高差不超过 0.4 米，离道路中线至少半个煤堆宽加 5.5 米，占地加 0.8 米余量内没有碰撞体或模型（头顶缆索除外），煤堆之间至少隔 1.5 米。
- 堆体是开口曲面，脚本强制主体法线朝上，否则 Unity 里背面剔除看不见。
- 用脚本移动预制体实例时先 `Undo.RecordObject` 再 `PrefabUtility.RecordPrefabInstancePropertyModifications`，否则重新导入预制体时位置会被重置。
- 只适合放平地，放斜坡上主体会被地形埋掉一侧。移动或增减煤堆后重跑刷地菜单更新煤灰。
