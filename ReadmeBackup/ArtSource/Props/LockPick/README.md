# Lock Pick

第一关从摩托车储物箱拿到的撬锁工具，用于撬开门卫室门上的旧式插芯锁（见[设计文档](../../../Docs/design.md)第一关步骤 3、7、8）。做成一小套随身工具：一把单钩撬片和一把 L 形张力扳手，钩片手柄缠着磨旧的黑色布胶带，两件用一根发硬的旧橡皮筋捆在一起，作为一个背包物品拾取。

## 文件

[可编辑 Blender 源文件](Blender/LockPick.blend)

[FBX 模型](Exports/LockPick.fbx)

[含嵌入贴图的 GLB 模型](Exports/LockPick.glb)

[整体预览](Previews/LockPick_Overall.png)

[俯视预览](Previews/LockPick_Top.png)

[钩尖局部预览](Previews/LockPick_HookDetail.png)

[透明底图标](Previews/LockPick_Icon.png)，512 × 512，可作为 Backpack 缩略图候选，目前未接入。

[GLB 重新导入后的预览](Previews/LockPick_GLB_Roundtrip.png)

## 尺寸与结构

整体长约 0.1148 米，宽约 0.0155 米，高约 0.0042 米。Blender 中 Z 轴朝上，工具平放，长度沿 X，钩尖朝 +X 并向 +Y 弯。原点在底面中心，导出文件采用 Y 轴朝上。

| 部件 | 说明 |
| --- | --- |
| HookPick | 0.7 毫米钢片，手柄宽 9 毫米，杆宽 2.2 毫米，末端短钩高出杆约 2.4 毫米 |
| TensionWrench | 1.0 毫米钢片切出的 L 形，长臂 62 毫米，短臂 12 毫米，压在胶带手柄上并略微偏转 |
| GripTape | 缠在钩片手柄上的布胶带，长 58 毫米 |
| RubberBand | 捆住两件工具的旧橡皮筋 |

整套靠底下的橡皮筋和钩尖着地，整体倾斜约 0.9 度。4 个网格，6,768 个三角面，3 个材质，全部有 UV，无非流形边和零面积面。

## 材质

贴图由制作脚本生成，1024 × 1024（橡皮筋 512 × 512）。HDRP Mask 的 R 为金属度、G 为白色 AO、B 为白色 Detail Mask、A 为光滑度。Normal 采用 OpenGL 约定。

| 材质 | 主要贴图与用途 |
| --- | --- |
| Steel_WornLockPick | Steel_BaseColor、Steel_Roughness、Steel_Metallic、Steel_Normal、Steel_HDRP_Mask。俯视平面 UV，沿长度方向拉丝，带暗沉氧化和少量锈点 |
| Grip_FrictionTape | Tape_BaseColor、Tape_Roughness、Tape_Normal、Tape_HDRP_Mask。螺旋搭接缝、布纹和拇指位置的磨白 |
| RubberBand_Aged | Rubber_BaseColor，褪色的红褐色和细小裂纹 |

## Unity

[预制体](../../../Assets/Art%20Resources/Props/LockPick/LockPick.prefab) 使用 HDRP/Lit 独立材质，根对象下的 `Model` 已平移到底面中心为轴心，平放时局部 +Y 朝上。没有碰撞体、拾取逻辑，也未放进场景。导入步骤见 [ImportLockPick.cs](UnityIntegration/ImportLockPick.cs)，通过 Unity MCP 执行。

## 制作环境

Blender 5.2.2 LTS 独立后台进程，Cycles 与 OptiX 渲染，制作日期 2026 年 10 月 21 日。

[制作脚本](Scripts/build_lockpick.py)生成材质、模型、预览和图标。[验证脚本](Scripts/verify_lockpick.py)重新导入两个导出文件并核对三角面数、尺寸、UV 和贴图。
