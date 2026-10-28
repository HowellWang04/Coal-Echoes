# ArtSource/FactoryRoads/StationRoad/

## 用途

从厂区主路西端通到索道山脚始发站两座站棚的土路，以及它爬上卸煤场边坡时的局部路基整平。路线按用户在俯视图上画的红线走：主路分出一条干道穿过两栋厂房之间的空地，贴着煤堆 B 走到坡脚，再分成两条小路分别上到 LineA、LineB 站棚的北端开口。

## 内容

- `survey.cs`：采集 `route.py` 的输入，写 `survey.json`（煤场和站房一带 0.5 米网格的地形高度和站房、煤堆、厂房、塔架、塑料围挡的网格角点）、`blocked.json`（离地 0.3 到 2.1 米有碰撞体的格子）和 `lanes.json`（主路两条车道每米一点）。
- `route.py`：在本目录下运行，定两条路的控制点和宽度，按坡度上限 20 度用最小二乘算两段上坡的纵断面，换算成 0.25 米网格的高度改动；输出 `route.json`、`grading.json` 和 `route-plan.png`（整平前后的坡度图）。需要 numpy、scipy、matplotlib。
- `apply.cs`：按 `route.json` 在 `工厂支路` 下建两条路，可重复执行，重跑会替换这两条，不保存场景。
- `grade.cs`：按 `grading.json` 改高度源和地形，带 `apply` 开关，先用 false 干跑。
- `Previews/`：`request-top.webp` 是用户画线的原图；`before-*`、`after-*` 是改前改后的同机位俯视、站房近景和眼高视角。
- 脚本都通过 Unity MCP 的 execute_code 运行。

## 与其他部分的关系

- 场景对象在 `In-Scene Objects/工厂支路` 下：`支路 至索道站 LineB` 是干道，从主路中线起步，在两栋厂房间宽 5.5 米，到煤堆 B 前收窄到 4 米，一直通到 LineB 站棚北端；`支路 至索道站 LineA` 宽 4 米，从煤堆 B 西北角的分叉点起步，走西侧较缓的坡到 LineA 站棚北端。
- 两条都挂 `Assets/Scripts/Environment/FactoryDirtRoadSpline.cs`，材质是其他支路共用的 `DirtPathSpline.mat`，干道收窄靠组件的 Width Multiplier 曲线，首尾渐隐 4 米。
- 站棚和煤堆见 `../../Cableway/FoothillStationPlacement/`、`../../CoalPile/`，主路和其余支路见 `../MainRoadReroute/`。
- 整平改的是 Tile 0,-2 的 Main 和 Draft 地形以及 MapMagic 高度源 `SheepRange_Native_Main`、`_Draft`，做法同 `../smooth-gate-road-crest.cs`。路边煤灰由 `Tools > Coal Echoes > Paint Factory Ground` 按所有样条路刷。

## 注意

- 整平只在两段上坡的路基和两侧 3 米内，最多垫高约 0.38 米、削低约 0.34 米，最陡的 2 米路段从 32 度降到约 22 度；站房平台、塔架和煤堆的碰撞范围外扩 0.6 米内不改，煤堆 B 西侧坡脚散煤被埋入最多约 13 厘米。
- 站棚北侧平台边是陡坎，LineB 那条因此比红线往西让了约 2 米再拐上平台；两条上坡小路在分叉点附近连成一片。
- 干道贴着煤堆 B 走，比煤堆摆放时用的离路规则近得多，再挪煤堆要重看这里。
- `grade.cs` 加的是相对改动，拿现有的 `grading.json` 再跑一次会把整平叠加一遍。改路线要依次重跑 `survey.cs`、`route.py`、`apply.cs`、`grade.cs`，这样新的 `grading.json` 只含相对当前地形的改动。之后重刷 Paint Factory Ground，`grade.cs` 已包含热浪刷新。
- 路面观感、两条小路的辨识度和上坡手感待 Play Mode 确认。
