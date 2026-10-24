# ArtSource/Props/BoltCutter/

## 用途

第一关门卫室里的断线钳，玩家拾取后剪断工厂大门的铁链，流程见 `Docs/design.md` 第一关和 `Docs/Specs/Levels/opening-level-one-spec.md`。外观是放了多年的老工具：黑色锻钢钳头带锈斑，刃口留亮钢，褪色红漆钢管手柄成片剥落，末端是干裂的黑色橡胶棱纹握套。

## 内容

- `Blender/BoltCutter.blend`：源文件，贴图已打包。
- `Exports/`：FBX 和带内嵌贴图的 GLB。
- `Textures/`：Forged、Paint、Rubber 三套，各有 BaseColor、Normal、Roughness、Metallic 和 HDRP_Mask。
- `Previews/`：整体、俯视、钳头局部和 GLB 回导的渲染；`BoltCutter_Icon.png` 是 512 × 512 透明底图标，复制到 `Assets/Art Resources/Props/BoltCutter/Icons/` 作背包缩略图。
- `Scripts/build_boltcutter.py`、`verify_boltcutter.py`：在后台 Blender 进程里生成材质、模型、预览和图标，以及重新导入导出文件核对三角面、尺寸、UV 和贴图。`build.log`、`verify.log`、`verification.json` 是运行输出。
- `UnityIntegration/ImportBoltCutter.cs`：通过 Unity MCP 执行的导入脚本。

## 与其他部分的关系

- 预制体 `Assets/Art Resources/Props/BoltCutter/BoltCutter.prefab`，HDRP/Lit 独立材质，根对象下的 `Model` 已平移到底面中心为轴心，平放时局部 +Y 朝上。
- 场景实例 `In-Scene Objects/工厂大门/Guard Post/室内陈设/BoltCutter`，钳头着地，斜靠在观察窗下的侧墙上；实例上加了 BoxCollider 和 `InspectableItem`（`backpackItem` 填下面的资源），按 F 拾取进背包。大门 `Chain_And_Padlock` 上的 `ItemUsePoint` 要求这件道具。
- 背包物品资源 `Assets/Art Resources/Props/BoltCutter/BoltCutter_BackpackItem.asset`，编号 `bolt_cutter`，引用上面的预制体和图标。
- 大门铁链线径 10 毫米，记录在 `../../FactoryGate/chain_geometry.json`，断线钳按能剪这种链条的 30 英寸规格做，整体长约 0.768 米。
- 材质配色和 `../LockPick/` 一样走旧工具方向。

## 注意

- Blender 里 Z 朝上、工具平放、钳口朝 +X，原点在底面中心；导出文件 Y 朝上。
- 各部件按零件分开，没有开合骨骼或动画；剪链时要张开钳口需另外拆分枢轴。
- HDRP_Mask 的 AO 和细节遮罩通道是全白，Normal 用 OpenGL 约定。
- 预制体本身没有碰撞体，碰撞体和拾取组件只加在场景实例上。导出后局部 +X 是握套一端，钳头在 −X。
