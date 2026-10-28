# ArtSource/Portfolio/NightRide/

## 用途

作品集里的月夜乘索道图：玩家站在吊斗里的视角，沿上山线拍的五张月夜画面。不是游戏截图，是 Play Mode 里用临时相机渲染的。

## 内容

- `render_night_ride.cs`：Unity MCP `execute_code` 的一次性脚本，只在 Play Mode 里跑。切成月夜，重拍门卫室窗玻璃的反射探针，停住索道，把全部吊斗挪到玩家那只离山顶站停车点还差 k 米的位置，再用复制 Main Camera 的临时相机从吊斗里人眼的位置拍，写进 `Renders/`。
- `Renders/`：五张 2560 × 1440 的成图，文件名前的数字是沿途顺序。`1_leaving_the_plant` 刚离开山脚站回看黑着的厂区，`2_tower_and_moon` 中段回看塔架、对面线上的吊斗和月亮，`3_moon_over_the_ridges` 和 `4_near_the_top` 快到山顶时回看山谷和月亮，`5_summit_station` 朝前看山头上的山顶站。

## 与其他部分的关系

- 月夜靠 `MoonlightController.SetNight(true)`，夜景参数见 `Docs/Specs/Systems/moonlight-spec.md`；吊斗位置用 `CablewayMovementManager` 的 `SampleMovement` 和 `DistanceUntil`，人眼位置是 `CablewayRideManager.standOffset` 加 Main Camera 在 Player 下的本地偏移，和游戏里坐吊斗时一样。
- 作品集 Figma 文件“王冠豪-总排版”里，`美术-第一版` 右边新建的 `Night Ride` 画板（节点 `750:90`）用了这五张：左边标题和一段说明，中间大图是 `2_tower_and_moon`，右边 2 × 2 是其余四张。

## 注意

- 游戏里坐吊斗上山是白天，只在快到站的黑屏过场里才切成月夜；这组图是整条线都放在月夜里拍的，不是玩家实际看到的乘坐过程。
- 门卫室窗玻璃的反射探针 `Window Glass/Guardhouse Reflection Probe` 只在启用时拍一次，切到月夜不重拍的话窗玻璃在夜里是一块白光，从索道上远远看得见；`MoonlightController` 现在只重拍全图那个探针，所以脚本里自己重拍了一次。
- 脚本开头检查 Play Mode，不在 Play Mode 就什么都不做；在编辑模式下挪吊斗会改场景。
