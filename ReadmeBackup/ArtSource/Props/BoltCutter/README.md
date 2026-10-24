# Bolt Cutter

第一关门卫室里的断线钳，玩家拾取后剪断工厂大门的铁链（见[设计文档](../../../Docs/design.md)第一关步骤 9、11 和[开场与第一关 Spec](../../../Docs/Specs/opening-level-one-spec.md)）。大门铁链线径 10 毫米（`ArtSource/FactoryGate/chain_geometry.json`），按能剪这种链条的 30 英寸规格制作。

外观是门卫室里放了多年的老工具：黑色锻钢钳头带褐色锈斑，两条刃口留着磨过的亮钢；手柄是褪色的红漆钢管，漆面成片剥落露出底铁和锈；末端是干裂的黑色橡胶棱纹握套，凹槽里积着灰。

## 文件

[可编辑 Blender 源文件](Blender/BoltCutter.blend)

[FBX 模型](Exports/BoltCutter.fbx)

[含嵌入贴图的 GLB 模型](Exports/BoltCutter.glb)

[整体预览](Previews/BoltCutter_Overall.png)

[俯视预览](Previews/BoltCutter_Top.png)

[钳头局部预览](Previews/BoltCutter_JawDetail.png)

[透明底图标](Previews/BoltCutter_Icon.png)，512 × 512，可作为 Backpack 缩略图候选，目前未接入。

[GLB 重新导入后的预览](Previews/BoltCutter_GLB_Roundtrip.png)

## 尺寸与结构

整体长约 0.768 米，两握把外缘宽约 0.119 米，平放高约 0.033 米。Blender 中 Z 轴朝上，工具平放，钳口朝 +X，钳口闭合、握把略微分开。原点在底面中心，导出文件采用 Y 轴朝上。

| 部件 | 说明 |
| --- | --- |
| Jaw_Upper、Jaw_Lower | 14 毫米厚锻造钳口，刃口闭合时留约 2 毫米缝 |
| HeadPlate_Top、HeadPlate_Bottom | 上下两片 4 毫米夹板，盖住钳口枢轴和连杆 |
| HandleHead_Upper、HandleHead_Lower | 连接钳口尾部与手柄钢管的锻件 |
| Bolts | 五组螺栓：两个钳口枢轴、两个钳口与手柄连接点、一个中心枢轴；上面六角螺母，下面圆头 |
| HandleTube、Ferrule | 直径 24 毫米红漆钢管和根部套箍，各上下两件 |
| Grip_Upper、Grip_Lower | 直径约 31 毫米、长约 24 厘米的棱纹橡胶握套，末端圆头 |

整把钳靠握套和钳头底面着地，倾斜约 0.3 度。13 个网格，31,140 个三角面（两只握套占约 2 万），3 个材质，全部有 UV，无非流形边和零面积面。各部件按零件分开，没有做开合骨骼或动画；剪链时如需张开钳口，需要另外拆分枢轴。

## 材质

贴图由制作脚本生成。HDRP Mask 的 R 为金属度、G 为白色 AO、B 为白色 Detail Mask、A 为光滑度。Normal 采用 OpenGL 约定。

| 材质 | 主要贴图与用途 |
| --- | --- |
| Forged_BlackSteel | Forged_*，2048 × 2048。俯视平面 UV 覆盖整把钳，刃口位置在贴图里画成亮钢条 |
| Handle_FadedRedPaint | Paint_*，1024 × 1024。沿钢管展开，大块剥漆露底铁和锈 |
| Grip_DriedRubber | Rubber_*，1024 × 1024。棱纹与几何对齐，裂纹和凹槽积灰 |

## Unity

[预制体](../../../Assets/Art%20Resources/Props/BoltCutter/BoltCutter.prefab) 使用 HDRP/Lit 独立材质，根对象下的 `Model` 已平移到底面中心为轴心，平放时局部 +Y 朝上。没有碰撞体、拾取逻辑，也未放进门卫室；Spec 里门卫室内的具体摆放仍待定。导入步骤见 [ImportBoltCutter.cs](UnityIntegration/ImportBoltCutter.cs)，通过 Unity MCP 执行。

## 制作环境

Blender 5.2.2 LTS 独立后台进程，Cycles 与 OptiX 渲染，制作日期 2026 年 10 月 21 日。

[制作脚本](Scripts/build_boltcutter.py)生成材质、模型、预览和图标。[验证脚本](Scripts/verify_boltcutter.py)重新导入两个导出文件并核对三角面数、尺寸、UV 和贴图。
