# ArtSource/Cableway/FoothillLowering/

## 用途

把山脚站站内的缆线放低 1.5 米的脚本和记录，让吊斗经过 LineA 站台时斗沿大约在站台沙面以上 1.1 到 1.3 米，第三关玩家够得着吊斗。

## 内容

- `lower_station.cs`：把两个滚轮架和回转站往下沉，站内的缆线圈跟着平移，出站到 1 号塔的那一跨按新起点重算，挂在这一段上的吊斗跟着降，结果在 `lower_station.json`、`cable_paths.json`。开头的 `apply` 设为 false 时只空跑报告。
- `movement.cs`：按新缆线网格重新生成吊斗路线点，结果在 `movement.json`。
- 两个脚本都通过 Unity MCP 的 execute_code 运行，按上面的顺序执行，都不保存场景。
- `Previews/boarding-bucket-eye.png`：把站门口那只吊斗临时推到 LineA 站台旁后，从站台眼高看过去的截图。
- `Before/`：操作前的磁盘版场景和两条缆线网格。

## 与其他部分的关系

- 下沉的是 `In-Scene Objects/索道群/站房/CablewayFoothillStation` 下的 `CablewayStationRollerFrame_LineA`、`_LineB` 和 `CablewayReturnTerminal`，水泥基础和支腿下段埋进地里；两座站棚和驱动电机没动。站棚、滚轮架、回转站的摆放见 `../FoothillStationPlacement/`。
- 缆线 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset`、`Cable_1.asset` 只改了从山脚站到 1 号塔轮组之前的圈，每圈的序号和水平位置都不变，三角面没动；1 号塔以后原样。出站那一跨照 `../SummitStation/connect_cables.cs` 的规矩，抛物线，垂度取水平跨距的 2.2%。
- 路线点和吊斗运动见 `../Movement/`。站门口那只 `塔架/CablewayTower_02/CoalBucket (1)` 跟着缆线降了；全线吊斗后来以它为起点按等距重排，也见 `../Movement/`。

## 注意

- 站里缆线比站棚格栅高约 3.1 米。吊斗底离地最少约 0.26 米，在 LineA 一侧；再往下放，吊斗会擦到回转轮那头的地面。
- 第三关的上车段不在这段站台上，在上山一侧的 LineB 滚轮架北边，那里地面是平的，斗沿比地面高约 1.7 米，见 `../Movement/`。
- 脚本按“站内段等高、到 1 号塔轮组为止”找要改的圈，已经跑过一遍再跑会在新高度上再降一次；要恢复原状用 `Before/` 里的场景和网格。
- Play Mode 里吊斗进出站和经过滚轮架的效果还没看过。
