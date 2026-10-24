# 开场积沙

2026 年 10 月 21 日，`FactorySandAccumulation` 改为在主菜单照片转场期间把三堆沙丘从地下升起，地形全程保持现在的高度。已接入的天空掠过见 `../SkyMotion/README.md`，宿舍楼新旧材质混合见 `../../../WorkersDormitory/CleanAging/README.md`，沙丘本身见 `../../../SandDune/README.md`。

## 为什么不再降地形

10 月 12 日的第一版把厂区一圈地形先降低 3 米再升回。整体降低时主菜单报纸照片里的建筑会浮空，设计改为地形不动、只让挡路的沙丘隆起，所以换成现在的做法。

## 做法

`Prepare()` 读取每堆沙丘 LODGroup 里的三级网格，记下原始位置，然后整体下沉 Depth。Depth 比最高的沙丘（主路约 5.5 米）还深，下沉后整堆沙都在地面以下，从菜单相机看不到。

`SetProgress()` 接受零到一的进度，按进度把网格从下沉位置升回原位。进度到一时直接恢复原始位置，终点就是场景里保存的沙丘。`Restore()` 和 OnDisable 负责中断清理，菜单从其他路径关闭时也会恢复。

只移动可见的 LOD 网格，沙丘根节点上的 MeshCollider 和 `Blocker` 隐形碰撞始终留在最终位置。开场期间玩家输入是锁定的，不需要让碰撞跟着动。

调用方式没有变，`NewspaperMenuUI.Show()` 调用 `Prepare()`，`EnterPhoto()` 的 DOTween Sequence 从点击 Start Game 起，在 0.8 秒停顿、1.8 秒放大和 1.4 秒纸墨消退共 4.0 秒内用 InOutSine 把进度推到 1。

## Inspector

对象位于 `MainMenuUIRoot/MenuPresentation/Newspaper`，组件为 `Factory Sand Accumulation`。

| 属性 | 当前值 | 用途 |
| --- | --- | --- |
| Dunes | `In-Scene Objects/沙丘` 下的 SandDune_MainRoad、SandDune_Left、SandDune_Right | 要升起的沙丘，每个需要带 LODGroup |
| Depth | 6 米 | 开始时下沉的深度，零表示关闭 |

旧版的 Terrain Root、Center、Radius、Edge Width 已删除。Depth 沿用了旧字段名，场景里原来存的是 3 米，已改为 6 米。

## 检查

在编辑器里直接调用 `Prepare()`、`SetProgress(0.5)`、`SetProgress(1)`，用 `MainMenuCamera` 各拍一张，见 `../../../SandDune/Previews/opening-rise-compare.png`（自上而下为 0、0.5、1）。进度 0 时三堆沙都看不到，大门后能一直看到厂区深处的主路；0.5 时主路上冒出一道低矮的沙梁；1 时沙丘填满两栋楼之间的主路，右侧围墙远端也被沙盖住一部分。左侧沙丘在厂房背后，菜单相机看不到。结束后三堆沙丘的 LOD 网格都回到原位，场景实例没有留下位置覆盖。

菜单相机在大门外约 89 米、离地约 13 米，沙丘在画面里只有二三十像素高，升起的变化比较含蓄。完整演出的观感要在 Play Mode 里确认。

## 旧版文件

`Verify.cs`、`VerifyFactory.cs`、`checks.json` 和 `factory-checks.json` 是第一版地形积沙的检查脚本和结果，引用了已删除的 Terrain Root 等字段，不能再对现在的组件运行。这几个文件没有进过 git，暂时留作记录，确认不需要后可以删掉。
