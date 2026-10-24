# ArtSource/Cableway/SummitStation/

## 用途

山顶终点站的装配脚本和检查记录。终点站替换了原来最高处的 11 号塔，复用山脚站的两座站棚、两组滚轮架、一套回转轮和转运轨道，入口朝向 10 号塔，缆线在这里回转闭合。

## 内容

- `survey.cs`、`plan_site.cs`、`inspect_geometry.cs`：现场测量和选址。`backup.cs`：备份。`place_station.cs`：创建站体和基础。`connect_cables.cs`：重做末端缆线并替换 11 号塔。`verify.cs`：静态对照。`save.cs`：保存场景。都是 Unity CLI `eval_file` 的一次性脚本，不参与游戏编译。
- `survey.json`、`site_plan.json`、`geometry.json`、`cable_paths.json`、`cable_connection.json`、`verification.json`：各步的结果。`console_tooling_error.json`：工具报错记录。
- `before.png`、`after.png`、`return-wheel.png`：Scene 视图截图。
- `Before/`：操作前的磁盘版和现场版场景、两条原缆线网格、场景变换与地形哈希基线。

## 与其他部分的关系

- 场景对象是 `In-Scene Objects/索道群/站房/CablewaySummitStation`，子对象 `CablewayStationShell_LineA`、`_LineB`、`CablewayStationRollerFrame_LineA`、`_LineB`、`CablewayReturnTerminal`，原 11 号塔的吊斗保留为 `CoalBucket_Approach`。
- 基础网格是 `Assets/Art Resources/Environment/Cableway/CablewaySummitStation/StationFoundation.asset`，用站棚混凝土材质，带静态 MeshCollider。
- 缆线 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset`、`Cable_1.asset` 只重做了 10 号塔之后的最后一跨和进站段，`Cable_0` 承载回转轮外侧半圆接到 `Cable_1`。模型来源见 `../Tajia02_Metric15/`、`../CablewayStationShell/`、`../CablewayStationRollerFrame/`，吊斗运动见 `../Movement/`。

## 注意

- 站口在原 11 号塔沿下山方向 4 米处；基础 15 米乘 15.5 米，顶面水平，侧壁随地形延伸并埋入 0.4 米；缆线中心距站口设备基准面 4.85 米，线距 4 米。重摆站体要保持这些关系。
- 最后一跨缆线对地净高扣除吊斗高度后约 4.48 米，只核过这一跨，不代表全线或站内运动碰撞。
- 重跑装配脚本前要核对现场条件，脚本不是运行时组件。
- 没有改地形、MapMagic 图和其余塔架位置。脱索上轨、装卸煤、玩家乘坐都没做，Play Mode 待确认。
