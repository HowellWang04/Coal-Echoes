# ArtSource/Cableway/SummitStation/MoveToTower09/

## 用途

把山顶终点站挪到原 9 号塔旁、缩短索道的脚本和检查记录。9 号塔立在约 35 度的横坡上，站体按用户意见沿上山方向往右挪到山脊隆起处；9、10 号塔拆除，缆线在 8 号塔之后直接进站。

## 内容

- `plan_site.cs`：在 9 号塔周围搜索基础范围内高差最小的位置，记录选定的站体坐标、朝向和最后一跨净空，结果在 `site_plan.json`。
- `backup.cs`：备份，写 `Before/`。`move_station.cs`：整体移动站体并就地重建基础网格。`connect_cables.cs`：重做 8 号塔之后的两条缆线、拆掉 9 和 10 号塔、安置吊斗，结果在 `cable_connection.json`、`cable_paths.json`。`movement.cs`：重新生成吊斗路线点，结果在 `movement.json`。`verify.cs`：对照基线做静态检查，结果在 `verification.json`。`save.cs`：保存场景。都通过 Unity MCP 的 execute_code 运行，按上面的顺序执行。
- `Previews/`：`before-*` 和 `after-*` 是同机位的 Scene 视图截图，另有站口近景、站上往山下看和最后一跨全景。
- `Before/`：操作前的磁盘版和现场版场景、两条缆线网格、基础网格，以及全场景变换、吊斗绑定和 60 份地形资源哈希的基线。

## 与其他部分的关系

- 站体是 `In-Scene Objects/索道群/站房/CablewaySummitStation`，原样复用原山顶站的站棚、滚轮架和回转轮，只改了根对象的位置和朝向；基础网格 `Assets/Art Resources/Environment/Cableway/CablewaySummitStation/StationFoundation.asset` 原地重建，GUID 不变。站体做法见 `../CLAUDE.md`。
- 缆线 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset`、`Cable_1.asset` 保留到 8 号塔为止的顶点，之后照 `../connect_cables.cs` 的规则重做。
- 吊斗：8 号塔的吊斗挪到新最后一跨离 8 号塔 8 米处，`CoalBucket_Approach` 在进站前 8 米处，都在上行线；原 9、10 号塔的吊斗换成站体下的 `CoalBucket_Approach (1)`、`(2)`，挂在回程线这一跨的三分之一和三分之二处。其余 8 只没动。这些是当时的摆位，全线吊斗后来已按等距重排，现状见 `../../Movement/`。

## 注意

- 选址点是原 9 号塔沿上山方向右移 15 米、往山下 2 米，站口在选址点沿下山方向 4 米处，朝向 8 号塔。基础范围内地面高差约 8.4 米，基础侧壁最高约 8.9 米（含埋入 0.4 米），没有改地形，所以没同步 Main 和 Draft，也没刷新热浪。
- 回程线进站前约 8 米处绳索离地最少约 5.5 米，扣掉吊斗高度后约 2.9 米；只核过这一跨。
- 8 号塔没有跟着新转角转向，塔顶轮组和新的出线方向差约 6 度；转塔要重做 7 号塔那一跨并重算加长腿。
- `connect_cables.cs` 和 `move_station.cs` 依赖 9 号塔还在，已经不能重跑；要恢复原状用 `Before/` 里的场景和网格。
- Play Mode 和吊斗进站碰撞没有验证。
