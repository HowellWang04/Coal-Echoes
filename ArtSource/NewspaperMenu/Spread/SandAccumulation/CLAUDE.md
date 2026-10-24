# ArtSource/NewspaperMenu/Spread/SandAccumulation/

## 用途

主菜单照片转场期间三堆沙丘从地下升起的记录。运行代码是 `Assets/Scripts/MainMenu/FactorySandAccumulation.cs`，地形全程不动。

## 内容

无，只有本说明。

## 与其他部分的关系

- 组件挂在 `In-game UI Root/MainMenuUIRoot/MenuPresentation/Newspaper` 上，Dunes 指向 `In-Scene Objects/沙丘` 下的 SandDune_MainRoad、SandDune_Left、SandDune_Right，Depth 是开始时下沉的深度。
- `Prepare()` 读每堆沙丘 LODGroup 的三级网格，记下原位后整体下沉；`SetProgress()` 按零到一的进度升回，到一时直接恢复场景里保存的位置；`Restore()` 和 OnDisable 负责中途关闭时的清理。
- `NewspaperMenuUI.Show()` 调用 `Prepare()`，点击 Start New Game 后 `EnterPhoto()` 的 DOTween Sequence 在停顿、放大、消退的总时长内用 InOutSine 把进度推到 1，与 `../SkyMotion/` 的天空和 `../../../WorkersDormitory/CleanAging/` 的宿舍楼共用同一进度。
- 沙丘本身见 `../../../SandDune/`，进度 0、0.5、1 的对照图是 `../../../SandDune/Previews/opening-rise-compare.png`。

## 注意

- 只移动可见的 LOD 网格，沙丘根节点的 MeshCollider 和 `Blocker` 始终在最终位置；开场期间玩家输入锁定，碰撞不用跟着动。
- Depth 要比最高的沙丘高，否则进度 0 时沙丘露出地面；新增沙丘要加进 Dunes。
- 菜单相机离沙丘远，升起的变化比较含蓄，完整演出观感待 Play Mode 确认。
