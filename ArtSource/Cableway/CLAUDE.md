# ArtSource/Cableway/

## 用途

运煤索道的全部制作源文件和场景装配记录：塔架、站棚、滚轮架、回转站、吊斗、两座站的摆放、缆线闭合和吊斗运动。主场景现用的是米制的 Tajia02_Metric15 一套。

## 内容

- `Tajia02_Metric15/`：现用的米制塔架、进站支架、加长腿、水泥墩、回转站和吊斗，Blender 脚本和导出。
- `SheaveAlignment/`：塔顶轮组方向修正的记录。
- `CablewayStationShell/`：站棚模型。`CablewayStationRollerFrame/`：站口滚轮架模型。
- `FoothillStationPlacement/`：山脚始发站摆放和同次厂区地形恢复的记录。`SummitStation/`：山顶终点站替换 11 号塔的装配脚本和记录。
- `Movement/`：吊斗沿缆线循环运动的装配和检查记录。
- `Tajia01/`：按旧模型重建的第一版塔架和吊斗，已被 Tajia02_Metric15 取代，只留存档，含 Unity 旧名对照 `Naming.md`。
- 各目录详见各自 CLAUDE.md。

## 与其他部分的关系

- Unity 资源都在 `Assets/Art Resources/Environment/Cableway/`，缆线网格在 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset`、`Cable_1.asset`。
- 场景对象在 `In-Scene Objects/索道群` 下：`塔架/CablewayTower_01` 至 `_10`（01 是进站支架），`站房/CablewayFoothillStation` 和 `CablewaySummitStation`，`索道移动管理`。
- 运行代码只有 `Assets/Scripts/Environment/CablewayMovementManager.cs` 和它的 Editor 脚本，其余 C# 都是 Unity CLI 或 MCP 的一次性脚本。
- 煤堆在山脚站下方的卸煤场，见 `../CoalPile/`。

## 注意

- 线距 4 米贯穿塔架、站房、缆线和终点站，改线距要连带改。
- 改了绳索形状要重新生成 `Movement/` 的路线点；塔移动位置后要重算加长腿。
- 装卸煤、脱索上轨、玩家乘坐都没做，运动由哪个关卡操作触发未定，Play Mode 没有验证过。
