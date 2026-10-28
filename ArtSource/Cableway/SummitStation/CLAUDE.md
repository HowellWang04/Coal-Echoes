# ArtSource/Cableway/SummitStation/

## 用途

山顶终点站的装配脚本和检查记录。终点站在原 9 号塔旁的山脊上，9 号以后的塔架都已拆除，复用山脚站的两座站棚、两组滚轮架、一套回转轮和转运轨道，入口朝向 8 号塔，缆线在这里回转闭合。

## 内容

- 根目录是在原 11 号塔位置首次建站的脚本和记录：`survey.cs`、`plan_site.cs`、`inspect_geometry.cs` 测量和选址，`backup.cs` 备份，`place_station.cs` 创建站体和基础，`connect_cables.cs` 重做末端缆线并替换 11 号塔，`verify.cs` 静态对照，`save.cs` 保存场景，都是 Unity CLI `eval_file` 的一次性脚本，不参与游戏编译。场景里已没有 9 至 11 号塔，这些脚本不能直接重跑。
- `survey.json`、`site_plan.json`、`geometry.json`、`cable_paths.json`、`cable_connection.json`、`verification.json`：首次建站各步的结果。`console_tooling_error.json`：工具报错记录。
- `before.png`、`after.png`、`return-wheel.png`：首次建站时的 Scene 视图截图。
- `Before/`：首次建站前的磁盘版和现场版场景、两条原缆线网格、场景变换与地形哈希基线。
- `MoveToTower09/`：把站体挪到现址、重建基础和末段缆线的脚本和记录，详见其 CLAUDE.md。
- `FoundationConcrete/`：基础专用混凝土材质的生成记录，详见其 CLAUDE.md。

## 与其他部分的关系

- 场景对象是 `In-Scene Objects/索道群/站房/CablewaySummitStation`，子对象 `CablewayStationShell_LineA`、`_LineB`、`CablewayStationRollerFrame_LineA`、`_LineB`、`CablewayReturnTerminal`，以及三只吊斗 `CoalBucket_Approach`、`(1)`、`(2)`；全线吊斗后来按等距重排，这三只现在不一定在最后一跨上，见 `../Movement/`。
- 基础网格是 `Assets/Art Resources/Environment/Cableway/CablewaySummitStation/StationFoundation.asset`，用同目录的 `SummitFoundation_Concrete.mat`（见 `FoundationConcrete/`），带静态 MeshCollider。
- 基础顶面一圈栏杆和后沿接出去的木栈道是 `In-Scene Objects/山顶栈道/SummitBoardwalk`，见 `../../SummitBoardwalk/`；前沿栏杆在两条缆线进站处留了吊斗通过的缺口。
- 缆线 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset`、`Cable_1.asset` 从 8 号塔之后是最后一跨和进站段，`Cable_0` 承载回转轮外侧半圆接到 `Cable_1`。模型来源见 `../Tajia02_Metric15/`、`../CablewayStationShell/`、`../CablewayStationRollerFrame/`，吊斗运动见 `../Movement/`。

## 注意

- 站口在选址点沿下山方向 4 米处，选址点和朝向见 `MoveToTower09/`；基础 15 米乘 15.5 米，顶面水平，侧壁随地形延伸并埋入 0.4 米；缆线中心距站口设备基准面 4.85 米，线距 4 米。重摆站体要保持这些关系。
- 最后一跨回程线对地净高扣除吊斗高度后约 2.9 米，只核过这一跨，不代表全线或站内运动碰撞。
- 重跑装配脚本前要核对现场条件，脚本不是运行时组件。
- 没有改地形和 MapMagic 图；8 号塔没有随新转角转向。脱索上轨、装卸煤没做。上山线进站的滚轮架是这里的 `CablewayStationRollerFrame_LineA`，会擦乘坐时的镜头，所以第三关玩家坐的吊斗停在它前面的 `Ride Stop Point`，开不进站里；向右转身后瞬移到 `Summit Platform Spawn`，在地基前沿和 LineA 站棚前端之间的走道上，见 `../Movement/`。这里 LineA 站棚离缆线比 LineB 站棚近，落点左手边贴着站棚前端的立柱。Play Mode 待确认。
