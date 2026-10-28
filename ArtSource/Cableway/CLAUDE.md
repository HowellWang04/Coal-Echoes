# ArtSource/Cableway/

## 用途

运煤索道的全部制作源文件和场景装配记录：塔架、站棚、滚轮架、回转站、吊斗、两座站的摆放、缆线闭合和吊斗运动。主场景现用的是米制的 Tajia02_Metric15 一套。

## 内容

- `Tajia02_Metric15/`：现用的米制塔架、进站支架、加长腿、水泥墩、回转站和吊斗，Blender 脚本和导出。
- `SheaveAlignment/`：塔顶轮组方向修正的记录。
- `CablewayStationShell/`：站棚模型。`CablewayStationRollerFrame/`：站口滚轮架模型。
- `FoothillStationPlacement/`：山脚始发站摆放和同次厂区地形恢复的记录。`SummitStation/`：山顶终点站的装配脚本和记录，站在原 9 号塔旁的山脊上。
- `Movement/`：吊斗沿缆线循环运动的装配和检查记录。
- `DriveMotor/`：山脚站 LineA 站棚里的驱动电机组，第三关上油后启动索道，联轴器鼓是单独的转动件。
- `FoothillLowering/`：把山脚站站内缆线放低 1.5 米、滚轮架和回转站跟着下沉、重建出站那一跨和路线点的记录。
- `Tajia01/`：按旧模型重建的第一版塔架和吊斗，已被 Tajia02_Metric15 取代，只留存档，含 Unity 旧名对照 `Naming.md`。
- 各目录详见各自 CLAUDE.md。

## 与其他部分的关系

- Unity 资源都在 `Assets/Art Resources/Environment/Cableway/`，缆线网格在 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset`、`Cable_1.asset`。
- 场景对象在 `In-Scene Objects/索道群` 下：`塔架/CablewayTower_01` 至 `_08`（01 是进站支架），`站房/CablewayFoothillStation` 和 `CablewaySummitStation`，`索道移动管理`。山脚站下还有驱动电机 `Drive Motor`、第三关的道具 `Oil Can`（见 `../Props/OilCan/`），以及上车用的 `Boarding Zone` 和 `Boarding Point`；山顶站下有到站用的 `Ride Stop Point` 和 `Summit Platform Spawn`。
- 运行代码是 `Assets/Scripts/Environment/CablewayMovementManager.cs` 和它的 Editor 脚本，以及第三关乘坐用的 `CablewayRideManager.cs`，其余 C# 都是 Unity CLI 或 MCP 的一次性脚本。
- 煤堆在山脚站下方的卸煤场，见 `../CoalPile/`。

## 注意

- 线距 4 米贯穿塔架、站房、缆线和终点站，改线距要连带改。
- 改了绳索形状要重新生成 `Movement/` 的路线点；塔移动位置后要重算加长腿。
- 装卸煤、脱索上轨没做；玩家乘坐已做，上车段在山脚站 LineB 滚轮架北边，到站时吊斗停在山顶站门口、整条索道停住不再开，见 `Movement/`。自动启动已关，吊斗在游戏里先静止，第三关给山脚站电机上油后由 `Level3HintFlowManager` 启动，Play Mode 没有验证过。站在山脚站朝山上看是右上左下：山脚站 LineB 是上山一侧，LineA 是下山一侧，见 `Movement/`。
- 山脚站站内缆线比站棚格栅高约 3.1 米，比山顶站低，吊斗经过 LineA 站台时斗沿在沙面以上约 1.1 到 1.3 米，见 `FoothillLowering/`。山脚站的站棚、滚轮架和回转站模型都没有碰撞体，两座站棚的后墙和滚轮架在 `LineA Station Colliders`、`LineB Station Colliders` 上另加了 BoxCollider，LineB 那组还多一块盖住走道格栅的地板，上车段旁另有挡人的薄板 `Boarding Point`；LineA 站棚的格栅被积沙埋着，LineB 站棚一带地面是平的，格栅高出地面约 0.3 米。驱动电机和 Oil Can 在 LineB 站棚里。
