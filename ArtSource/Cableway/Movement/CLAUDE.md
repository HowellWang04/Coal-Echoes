# ArtSource/Cableway/Movement/

## 用途

索道吊斗沿缆线循环运动的装配脚本和检查记录。运行代码是 `Assets/Scripts/Environment/CablewayMovementManager.cs` 和 `Editor/CablewayMovementManagerEditor.cs`。

## 内容

- `setup.cs`：一次性装配脚本，创建管理对象、提取路线并绑定吊斗。`setup.json`：装配结果。
- `verify_logic.cs`、`verify_preview.cs`、`verify_route.cs`、`start_live_preview.cs`：组件逻辑、Inspector 预览、路线连续性和真实编辑器预览的检查脚本，对应 `logic_verification.json`、`preview_verification.json`、`route_verification.json`、`live_preview_start.json`、`live_preview_verification.json`、`final_verification.json`。`live_preview.png` 是预览截图。
- `respace.cs`：把全线吊斗排匀、补足数量的脚本，开头的 `apply` 设为 false 时只空跑，结果在 `respace.json`，通过 Unity MCP 的 execute_code 运行，不保存场景。
- `Before/`：装配前的磁盘版和现场版场景、12 个吊斗的初始变换，以及排匀前的场景 `Main Game Scene (before respace).unity`。
- `ride_clearance.cs`：临时给索道网格加 MeshCollider，沿上山半圈查几个候选站位转一圈时镜头会擦到什么，再查吊斗自己的吊架，结果在 `ride_clearance.json`。`ride_preview.cs`：把一只吊斗临时挪到上车段，渲染站位点的眼高画面和从站台看吊斗的画面。`arrive_preview.cs`：把一只吊斗临时挪到山顶站停车点、临时切成月夜，渲染停车时朝前、转身后、站台落点的画面和停车的俯视。三个都不保存场景。
- `Previews/`：第三关开局停在山脚站门口那只吊斗的眼高和俯视截图，`ride-*.png` 乘坐站位的眼高截图，这两组是反转运行方向以前拍的，吊斗和站位都在另一侧；`arrive-*.png` 山顶站停车和落点的截图，是现在的。
- 目录内 C# 文件都是 Unity CLI `eval_file` 或 MCP `execute_code` 的一次性脚本，不参与游戏编译。

## 与其他部分的关系

- 管理对象是主场景 `In-Scene Objects/索道群/索道移动管理`，绑定全部 36 个吊斗。原来的 12 个父级不变：8 个是各塔预制体自带的子物体 `塔架/CablewayTower_0N/CoalBucket`，3 个在 `站房/CablewaySummitStation` 下，站门口那只是 `塔架/CablewayTower_02/CoalBucket (1)`；后加的 24 个是 `索道群/吊斗/CoalBucket_01` 至 `_24`。父级只是放在哪，和吊斗挂在哪座塔附近已经无关。吊斗保持竖直，随线路改变水平朝向，在两站回转后继续循环。
- 路线从 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset` 和 `Cable_1.asset` 每 8 个顶点的截面中心提取，两条反向拼接后去重，序列化在管理组件里；运行时按累计世界距离采样，跨距和坡度变化时速度不变。
- Inspector 可设自动启动和速度，Play Mode 有启停按钮；编辑模式有 Scene 预览按钮和进度滑块，停止预览、关闭 Inspector、保存场景、重新编译或进入 Play Mode 时恢复原位。
- 场景里自动启动已关掉，开场和游戏开始时吊斗静止在摆放位置。第三关由 `Gameplay Root/Managers/Level3HintFlowManager` 在主菜单关掉后看驱动电机用过就调 `StartMovement()`，不挂在使用点的 onUsed 或 onRestored 上，读档时也就不会在主菜单背后开走。
- 行程从山脚站回转轮起算、往上增加：吊斗先沿 LineB 上山，绕过山顶站回转轮，再沿 LineA 下山回到山脚站。站在山脚站朝山上看是右上左下，山脚站的 LineB 站棚是上山一侧，LineA 是下山进站一侧；到了山顶站，上山线从 LineA 滚轮架进站。
- 每只吊斗的起点就是它在场景里的摆放位置，`PrepareMovement` 按位置投到路线上，挪吊斗不用改代码。第三关要上的那只 `塔架/CablewayTower_02/CoalBucket (1)` 摆在山脚站北端、两座站棚之间的 LineA 进站口，是一只刚下山要进站的吊斗，过回转轮沿 LineB 往北、穿过滚轮架，约 27 米到上车段。以它为起点，36 只沿整圈等距排开，间距约 59.5 米，按现在的速度每 24 秒左右过一只；它后面那只在下山线上约 60 米外。

## 注意

- 改了绳索形状要重新生成路线点。`setup.cs` 会拒绝在已有管理器时重复创建，不能直接用来重建路线，它拼出来的还是反转以前的方向；只重建路线点用 `../FoothillLowering/movement.cs`（和 `../SummitStation/MoveToTower09/movement.cs` 相同，只是结果写在各自目录），这两个按 Cable_1 正向接 Cable_0 反向拼，方向和现在一致。
- 预览会让场景出现未保存标记，保存时先恢复原位，不会把预览位置存成摆放位置。
- 速度按真实运煤索道设成每秒几米，第三关要靠它留出走到站台的反应时间，不要为了预览调快后忘了改回。错过一只等下一只的时间由间距和速度一起定，改其中一个要看第三关 Spec 的上吊斗。
- 改了路线长度后，吊斗间距不会自己跟着变，要重跑 `respace.cs`；它以站门口那只为准，那只不动。
- 玩家乘坐由 `Assets/Scripts/Environment/CablewayRideManager.cs` 做，挂在 `Gameplay Root/Managers/CablewayRideManager`，读这里的吊斗列表，玩家每帧跟着吊斗本地坐标里的固定站位点。乘坐时只有山脚站 LineB 滚轮架和山顶站 LineA 滚轮架会擦镜头，塔架都不碰；上车段因此在山脚站滚轮架北边，停车点 `CablewaySummitStation/Ride Stop Point` 在山顶那个之前。站位点偏在吊斗上山时行进方向的右侧，离下山线远的一边。改了路线、站位或站房摆放后重跑 `ride_clearance.cs`，挪了停车点重跑 `arrive_preview.cs` 看一眼。
- 到站前的黑屏里乘坐组件停掉索道，最后三秒由它按速度推 `SampleMovement` 走到停车点，之后索道一直停着，`StartMovement` 不会再被调；`DistanceUntil` 是给它求还差多远用的。
- 没有滑轮旋转、装卸煤、站内脱索上轨。Play Mode 和站内通行效果待确认。
