# 第二关 Spec

对照[设计文档](../../design.md)的“关卡2：宿舍楼”一节，按当前代码、场景和美术资源列出已实现和未实现的部分。设计以设计文档为准，提示和独白文字以 [json-hints.md](../../json-hints.md)、[json-inner-monologue.md](../../json-inner-monologue.md) 为准，回响规则见[回响系统 Spec](../Systems/echo-system-spec.md)。状态分三种：已实现、部分、未实现。

## 范围与完成条件

- 起点是第一关剪链开门之后，玩家走进大门；终点是读完离开宿舍的过场文字，对应设计文档详细流程第 9 步。同一次黑屏里接着读第三关开头的过场，淡入时玩家已在主路尽头，这部分归[第三关 Spec](level-three-spec.md)。
- 第一关怎么算完成见[第一关 Spec](opening-level-one-spec.md) 第 11 步，第二关只读它的结果，不改第一关的流程。
- 为了收尾，流程按精简版做：宿舍里只有 207 一间，进出都是过场文字加传送，宿舍楼道、楼梯和后门外绕沙丘那段路都不做；没有 B 类回响；抽屉一直半开，不做开合。
- 下面先写开头衔接（设计文档第 1 步），再按步骤写其余流程、传送和过场、207 室、山路图和读档。开头衔接、第 2 到 9 步、进出宿舍的过场和传送、207 室、山路图和读档已实现，Play Mode 待用户测；淡入后的第三关标题归第三关，还没做。

## 开头衔接

设计要求的顺序：剪链开门，中下方出 Chain cut，淡出后播 M1.8，第一关结束，右上角出第一关最后一条提示 T1.10，叫玩家穿过大门走进厂区；玩家走进大门，T1.10 勾上，提示框消失，渐显 Chapter Two / The Dormitory，标题消失后播 M2.1，再出第二关的 T2.1。提示编号写成 T 加关卡号和本关序号，第一关是 T1.1 到 T1.10，第二关是 T2.1 到 T2.6，两关不会重名。

| 设计要求 | 状态 | 实现位置与现状 |
| --- | --- | --- |
| 剪链开门、播完 M1.8 后第一关结束，出 T1.10 | 已实现 | 铁链使用点 saveId `gate_chain` 用过就算第一关完成，查 `Level1HintFlowManager.LevelComplete`；T1.9 勾完后等 M1.8 播完，出第一关最后一条提示 T1.10 Walk through the Factory Gate into the plant，关卡号 `level` 仍是 1 |
| 玩家走进大门勾 T1.10，第二关开始；第一关没完成时不触发 | 已实现 | `FactoryGate` 的子物体 `Level 2 Entry Zone` 是一个勾了 Is Trigger 的 BoxCollider，在厂内一侧横跨大门，从门内约 5 米到 20 米，左右各 20 米，玩家剪完链站在门口不会进去；场景里所有射线都忽略触发器，它不挡交互和拍照。`Gameplay Root/Managers/Level2HintFlowManager` 每帧看玩家位置在不在盒内，不用物理回调；关卡号为 1、`LevelComplete` 为真时，人在盒内就勾 `T1.10_enter_plant`，T1.10 出现前就已经进门的，它一出现就勾上。T1.10 勾完、提示框关掉、第一关提示全部显示完（`HintPopupUIManager.Finished`，`Progress/Progress.json` 的 `hintIndex` 存为 8，第一关共 8 条提示）以后才换到第二关、出标题，这时不再看位置，所以第一关的最后一步不会写进第二关的进度 |
| 进门时从左到右写出 Chapter Two / The Dormitory，停一会儿再渐隐 | 已实现 | `In-game UI Root/ChapterTitleUIRoot` 挂只管显示的 `ChapterTitleUI`，子物体 `ChapterTitle` 从开场的 `MainMenuUIRoot/OpeningUIRoot/ChapterTitle` 复制，样式相同，排在 PlayerPanelUIRoot 前面，打开面板时被盖住。给它两行字就由 `ChapterTitle` 上的 `ChapterTitleWriter` 先写小字再写大字，停 2 秒、渐隐 1 秒，和开场一致；不锁操作，玩家可以继续走。两行字填在 `Level2HintFlowManager` 的 Inspector 里，文字见[游戏文本](../../game-text.md)第二关的章节标题 |
| 标题消失后播 M2.1 | 已实现 | `Level2HintFlowManager` 等 `ChapterTitleUI.IsShowing` 变回 false 再 `Play("M2.1")`；独白会排队，M1.8 还没播完时 M2.1 自动等它 |
| M2.1 播完出第二关 T2.1 | 已实现 | `level2-hints.json` 已按规则生成。`HintPopupUIManager` 的 `levelHintJsons` 按关卡顺序引用两份 JSON，`Begin` 按 `GameProgress.Level` 取那份；关卡号变了再调就换成新关卡的列表，从存着的提示开始，新关卡就是第一条。`Level2HintFlowManager` 在播 M2.1 的同一帧调 `Begin`，提示框照旧等独白播完才出 |
| 第二关开始后，第一关的逐帧逻辑不影响第二关 | 已实现 | `Level1HintFlowManager` 只在关卡号为 1 时把“当前没有提示”按 T1.1 处理，也只在关卡号为 1 时于主菜单关闭后播 M1.1、调 `Begin`；第二关存档由 `Level2HintFlowManager` 先播 M2.1 再调 `Begin`。其余逐帧检查找不到 T1. 开头的编号，页签保持全开，Group Photo 回响和泛光圆圈保持关闭，门卫室和铁链两个使用点保持锁着，它们本来就用过了 |
| Continue 读档回到正确的关卡和提示 | 已实现 | `Progress/Progress.json` 加了关卡号 `level`，默认 1，没有这个字段的旧存档按第一关读。进门时 `GameProgress.StartLevel(2)` 写入关卡号，`hintIndex` 回到 -1，清空已勾任务并存档。读档在第二关时不显示标题，M2.1 没播过就先补播，再出存着的第二关提示 |
| Start New Game 清档后回到第一关 | 已实现 | 关卡号跟着 `Progress/Progress.json` 一起删，`GameSaveManager.ClearSaves` 清掉 `GameProgress` 的缓存后读回 1。`Level2HintFlowManager` 和 `HintPopupUIManager` 都在主菜单关闭后才记下状态（是否已开始、读的是哪一关），清档只在主菜单里发生，所以它们不用订阅 `SavesCleared` |

## 第二关流程

步骤号对应设计文档的详细流程表。打勾、播独白和开放使用点都写在 `Level2HintFlowManager` 里，照第一关每帧按状态检查的写法；使用点照第一关，提示出现前 `Allowed` 为 false。

| 步骤 | 设计要求 | 状态 | 做法 |
| --- | --- | --- | --- |
| 1 | 走进厂区，显示第二关标题，播 M2.1 | 已实现 | 见上面开头衔接 |
| 2 | 走到宿舍入口播 M2.2；T2.1 出现后用 Lock Pick 撬开入口，出 Dormitory unlocked，勾 T2.1，接着放进入宿舍的过场 | 已实现 | 宿舍楼正面中央的双扇门和楼体合在一个网格里，没有单独的门扇，也不做看得见的锁，只靠 F 提示；撬开后马上过场传送，门不用转开，所以不加 `SwingDoor`。`WorkersDormitory` 下加了 `Dorm Entrance Lock`：贴着双扇门外面一块薄 BoxCollider，盖住整个门洞，物体原点在门把手高度，F 提示就出在那里；挂 `ItemUsePoint`，要求 `LockPick_BackpackItem`，F 提示 Use Lock Pick to pry open the dormitory door，useNotice 写 Dormitory unlocked，saveId `dorm_entrance`，onUsed 先 `HintPopupUIManager.CompleteTask("T2.1_open_dormitory")`，再调 `Level2HintFlowManager.EnterDormitory` 开始过场。`Level2HintFlowManager` 每帧把它的 `Allowed` 设成 `HasReached("T2.1")`。M2.2 用 `WorkersDormitory/Dorm Entrance Zone`，勾了 Is Trigger 的 BoxCollider，从门口盖住台阶，往外到台阶前约 3.5 米，左右各 4 米；和进门判定一样每帧看玩家位置，第二关开始、M2.1 播过以后才查 |
| 3 | 读进入宿舍的过场，读完传送到 207 室 | 已实现 | 过场 id `enter_dormitory`，画面全黑时传送到 `207 Spawn`，见下面传送和过场 |
| 4 | 在 207 室里看看，播 M2.3 | 已实现 | `EnterDormitory` 把 `Play("M2.3")` 作为过场淡入完的回调。T2.1 勾完后提示框照常等 1.2 秒换下一条，但过场期间不出新提示，过场一结束 M2.3 就在同一帧排进独白，提示框再等它播完才出 T2.3；房间见下面 207 室 |
| 5 | T2.3 出现，翻到日记第二页勾 `T2.3_check_diary` | 已实现 | `Level2HintFlowManager` 每帧看 `PlayerPanelUIManager`：面板开着、在日记页签、`CurrentGrandpaDiaryPageIndex` 为 1（第二页 Room 207）时勾。不看日记直接看了山路图时，山路图的 onInspectEnd 先勾这一项，否则 T2.3 永远完不成 |
| 6 | 在半开的抽屉里按 E 看山路图，放到桌面上后勾 `T2.3_inspect_map`，播 M2.4 | 已实现 | 见下面山路图。`Level2HintFlowManager` 每帧把山路图 `InspectableItem` 的 `Allowed` 设成 T2.3 是否已出现，之前不出 E 提示，也拿不起来 |
| 7 | T2.4 出现后调焦距和对焦，拍下清楚的山路图，依次勾 `T2.4_frame_map`、`T2.4_focus_map`、`T2.4_take_photo`，总任务 `T2.4_photo_map` 随之勾上，播 M2.5 | 已实现 | 见下面山路图 |
| 8 | T2.5 出现后在 207 门口按 F 离开，放离开宿舍的过场 | 已实现 | `ItemUsePoint` 的 `requiredItem` 留空时只看 `Allowed`，第一关两个使用点和宿舍入口都要道具，行为不变；`useNotice` 留空时不发 `OnToolUsed`，否则底部会空占 2.4 秒，过场跟着干等。`207/Door/Door Leaf` 挂使用点：F 提示 Leave Room 207，`promptAnchor` 是 `Door Handle`，没有使用提示，saveId `room207_door`，onUsed 先 `HintPopupUIManager.CompleteTask("T2.5_leave_room")`，再调 `Level2HintFlowManager.LeaveDormitory`。`Level2HintFlowManager` 每帧把它的 `Allowed` 设成当前提示是不是 T2.5。从 `207 Spawn` 转身、往屋里退到 2.2 米都对得上门扇 |
| 9 | 读离开宿舍的过场，第二关结束；不淡入，接着读第三关开头的过场 | 已实现 | `room207_door` 用过就算第二关完成，照 `gate_chain` 的做法不另存，`Level2HintFlowManager.LevelComplete` 给第三关查。`LeaveDormitory` 一次播 `leave_dormitory` 和第三关的 `follow_map`，全黑时传送到 `Main Road End Spawn`，五段读完才淡入。提示框不另做收尾：T2.5 勾完后等过场结束，下一条超出列表，提示框关掉，`hintIndex` 存为 4，和第一关结束一样。连读的做法和淡入时写出第三关标题都归第三关，见[第三关 Spec](level-three-spec.md)开头一节 |

### 传送和过场

- 过场组件是 `CutsceneTextUI`，挂在 `In-game UI Root/CutsceneUIRoot` 上，第二关的两段和[第三关 Spec](level-three-spec.md) 开头、到山顶的两段共用。`CutsceneUIRoot` 自带 Canvas，排序 50，在游戏中所有 UI 之上、主菜单（100）之下；子物体 `Backdrop` 是全屏纯黑图，`TextArea` 从开场的 `OpeningUIRoot/TextArea` 复制，字体、字号、颜色和继续箭头都相同。两个子物体平时关着，免得黑图挡住别的 UI 的点击。
- `Play(id, onBlack, onFinished)`：一调用就锁输入，照 `MainMenuUIManager` 关掉 Gameplay 动作表和移动，记下进入前的状态，结束时照原样恢复。先等底部中间的字走完，也就是使用提示（如 Dormitory unlocked）和独白，再用 1 秒淡成全黑，调 `onBlack`，然后逐段显示；读法和开场文字相同，逐字显示、在文字区点击加速、点击换段，不能跳过。只有黑屏读字时放出光标，读完收回再用 1 秒淡入，调 `onFinished`。
- `IsPlaying` 从调用起到淡入完都为真。`HintPopupUIManager` 换下一条提示前除了等独白，也等它变回假，所以过场里不会出新提示。
- 文字从 `Assets/Narrative/cutscene-text.json` 读，第二关的 id 是 `enter_dormitory` 和 `leave_dormitory`，离开时接着读第三关的 `follow_map`，都已按[过场文字](../../json-cutscene-text.md)同步。背景先用黑屏。
- 传送在 `onBlack` 里做。`PlayerMovementManager` 自己记着 yaw 和 pitch，每帧按它们设朝向，直接改 Transform 会被转回去；玩家又带 `CharacterController`，开着时改位置会被它顶回。所以加了 `PlayerMovementManager.TeleportTo(Transform)`：关掉 `CharacterController`，把脚底放到目标位置，yaw 取目标朝向，pitch 归零，清掉速度，再打开。
- 落点放一个空物体，用它的位置和朝向：`207 Spawn` 在 207 室门里，面朝屋里，已接在 `Level2HintFlowManager` 的 `room207Spawn` 上。离开宿舍后的落点用[第三关 Spec](level-three-spec.md)开头一节定的 `Main Road End Spawn`，已放好，接在 `mainRoadEndSpawn` 上：`In-Scene Objects/工厂道路向内` 的子物体，在主路上小路 `支路 至索道站 LineB` 的路口前约 3.5 米、出厂方向那条车道上，面朝两栋厂房之间的空当，看得到路口和空当里的站棚，头顶是上山的缆线。1 号塔架在主路尽头那栋厂房背后，从主路上看不到，走进空当才看得到。要调就在 Scene 视图里拖它，传送只用它的位置和水平朝向。

### 207 室

已搭好，场景对象 `In-Scene Objects/室内空间/207`。宿舍楼模型的室内只是遮光的浅层，进不去，所以 207 是单独一间，放在宿舍楼正下方的地下，传送进去。搭建脚本、对象层级和注意事项见 `ArtSource/Room207/CLAUDE.md`。

- **位置**。宿舍楼正下方约 90 米的地下，朝向是宿舍楼转 180 度，屋里朝窗就是宿舍楼背面。`室内空间` 下原有的三块测试室内 `卧室`、`大厅`、`卧室/白膜场景搭建` 都不动，207 只从 `卧室/room` 复制了床和柜子。
- **房间壳**。ProBuilder 围的约 4 × 6 米四人间，净高 2.9 米。窗墙中间一扇窗，和宿舍楼的窗同尺寸，窗框借宿舍楼的绿漆窗框；对面门墙中间是门，门套和门扇借门卫室门的蓝灰漆木。墙面是门卫室的水泥扫描贴图提亮成发旧的白灰，下面 1.2 米刷绿墙裙；地面和窗台用门卫室的旧水泥地。外侧不做。
- **家具**。两张上下铺 `Bunk Bed Left`、`Bunk Bed Right` 和两组铁皮柜 `Locker Left`（四门）、`Locker Right` 从 `卧室/room` 复制、缩回原大：床靠两面侧墙面对面，上铺约 1.9 米高，眼高看得到上铺铺面，不挡视线；柜子高 1.8 米，靠侧墙放在门边。原来那套 `SM_Table_Chair_Set_02a` 是没有抽屉的军用折叠桌，配不上抽屉，换成新做的宿舍两屉桌 `Table`，背靠窗墙放在两张床之间，桌面 0.78 米，在腰下；红棕漆和门卫室的值班桌成套，见 `ArtSource/Room207/DormDesk/`。
- **新加的东西**。`Stove` 是一只铁皮炉子，在屋子中间偏右，炉门朝门口；`Stove Pipe` 从炉顶竖起，拐弯后平着通到窗墙，从窗户上方穿出去，从门口看是一条引向窗户的线。`Table Drawer` 是桌子左边那只抽屉（面向窗户时的左手边），半拉开，不能开合；里面的空物体 `Map Spot` 标着山路图背面朝上放的位置。
- **窗外背板**。在宿舍楼背面二楼一扇窗外拍了一张宽角照片，往索道方向偏转、略抬头，贴在 207 窗外远处的平面上。窗外是背后的巷子、围墙、砖房和山；从屋子中间能看到山脊上的 3 号塔架和爬上去的索道，走近窗户还能看到 2 号塔架，4 号偏在一边看不到。宿舍楼背后的摆件改了要重拍。
- **光照**。`Lighting` 下：窗外一盏聚光灯当斜进来的阳光，落在桌面左半、抽屉旁的地面和左边下铺上，窗框投下影子，从门口看亮处正在抽屉那一侧；窗口一盏面光当天光；一盏暖色补光；一个反射探针。屋里另有局部 Volume `207 Interior Volume`，压低天空环境光、提高曝光、开 SSAO，不加它整间屋会被天光照平、发灰。
- **雾和沙尘**。全局雾本来会透进屋里，屋里有一层粉灰色的雾，局部 Volume 里关掉了；工厂沙尘的局部雾体积在地面上方，碰不到 207。Play Mode 里再确认一次。
- `207 Spawn` 在门里一步，面朝窗户。
- 门扇 `Door Leaf` 带一个 BoxCollider，挂着按 F 离开的使用点，见上面第 8 步。
- 家具、炉子、门、窗和抽屉都是单独、名字清楚的 GameObject。作品集 Level design 展示页已按这个排布和窗口阳光的真实落点重画，见 `ArtSource/Portfolio/LevelDesign/LevelDesign_207.png`，Goals 两条待用户确认。

### 山路图

- 纸片：`207/Mountain Road Map`，和 `Map Spot` 同位置同朝向，不挂在 `Table` 下，因为重跑桌子的摆放脚本会重建 `Table`。两个子物体是背靠背的 Unity 自带 Quad，18 × 12.5 厘米：`Back` 朝上贴 `MountainRoadMap_Back.mat`，`Front` 朝下贴 `MountainRoadMap_Front.mat`，并绕竖轴转了 180 度，等于把纸左右翻面，和贴图的画法对上。两个材质从相框照片的材质复制，在 `Assets/Art Resources/Props/MountainRoadMap/`，底色压到 0.8，不然纸在屋里的曝光下发白、铅笔线看不清。不投影。根物体带一块 2 毫米厚的 BoxCollider 给查看和拍照用；抽屉本身没有碰撞体。路线图的上方朝窗户，站在桌前看是正的。
- 放到桌面：`InspectableItem` 的 `putBackSpot` 是 `207/Desk Spot`，在桌面正中略偏抽屉一侧的窗口阳光里，避开了窗框斜投的影子，背面朝上、上方朝窗户、略斜。第一次看完放下时纸片飞到桌面上，之后一直留在那里，方便拍照。读档时 `Level2HintFlowManager` 在提示到了 T2.4 以后（或关卡号大于 2）每帧把它摆回 `Desk Spot`；新游戏时 `InspectableItem` 订阅清档事件放回抽屉。
- 查看：根物体挂 `InspectableItem`，名字 The Way Up，不填 readText，右侧不出英文转写，改用 T 键悬停翻译，`inspectRotationOffset` 为 (-80, 0, 0)，拿起来先看到背面的路线图，字是正的，翻过来是工票。最近检视距离设得很近，滚轮拉近后整张纸铺满画面，能看清铅笔字。onInspectEnd 依次 `CompleteTask("T2.3_check_diary")`、`CompleteTask("T2.3_inspect_map")`、`Play("M2.4")`。从桌前 0.2 到 0.9 米的几个站位核对过，抽屉和桌子都不挡 E 提示。
- 悬停翻译：根物体挂 `PaperTranslation`，读 `MountainRoadMap_Translations.json`，里面是正反两面每处字在各自 Quad uv 里的四个角和英文，由画贴图的脚本一起导出，英文见[游戏文本](../../game-text.md)山路图一节。查看时底部多一行 T Translate（`InspectItemUIRoot/KeyHints/Row_Translate`，只在拿着带 `PaperTranslation` 的物品时显示）。按 T 由 `InspectItemUIRoot` 上的 `InspectTranslationUI` 处理：纸片转到正对镜头、眼前这一面的字摆正，停止跟随左键旋转（Rotate 那行藏起），滚轮仍能缩放；鼠标落在哪处字上，`TranslationTip` 就在鼠标旁边显示英文，样式照抄报纸菜单的悬浮翻译。再按 T 或右键放下就退出。输入动作是 `PlayerControls` 的 `InspectTranslate`。
- 相册翻译：拍照那一刻的回响目标带 `PaperTranslation` 时，`CameraSystemManager` 把当前这一面每处字投到 Main Camera 的视口坐标，随回响记录存进 `Echo/EchoPhotos.json` 的 `translations`。在相册里点开这张照片，`CameraPhotoPreviewUIManager` 按鼠标在照片里的位置查这些框，用 `PhotoPreviewUIRoot/TranslationTip` 显示英文。只有拍成回响的那张有翻译，回响拍成后关掉，之后再拍山路图的普通照片没有翻译。
- 拍照：同一物体挂 `EchoTrigger`，编号 `mountain_road_map`，手账文字照游戏文本，便签位置用默认的照片下方。场景里默认关着，`Level2HintFlowManager` 照 Group Photo 重拍的做法，只在当前提示是 T2.4 且还没拍成时打开。这一步是调相机参数的教学，要拍清楚纸上的字才算：`minScreenSizeOverride` 设 0.3，默认焦距下纸只占画面短边约 0.06，得拉长焦距；勾了 `requireFocus`，对焦距离要对上纸的距离，判定规则见[回响取景提示 Spec](../Systems/echo-targeting-spec.md)对焦一节。最大距离 1.6 米。纸已经在桌面上，站在桌前低头时镜头到纸约 1 米（桌面离地 0.78 米，眼高约 1.68 米），对焦滑条默认 10 米，要拖到 1 米左右。`Level2HintFlowManager` 每帧看 `EchoTargetDetector`：纸框好了（`OutOfFocusTarget` 或 `CurrentTarget` 是山路图）勾 `T2.4_frame_map`，框变黄勾 `T2.4_focus_map`。onCaptured 依次勾 `T2.4_frame_map`、`T2.4_focus_map`、`T2.4_take_photo`，再播 M2.5，前两项补勾是防止框刚变黄那一帧就按快门、每帧检查没赶上，总任务卡住。

### 读档

- 现在 Continue 不存玩家位置，玩家总在厂门外的默认位置出生。第二关里读档时，`Level2HintFlowManager` 在第二关开始的那一帧按使用点记录把人放回去：`dorm_entrance` 用过、`room207_door` 没用过就传送到 `207 Spawn`，M2.3 没播过就补播（它只在进 207 的过场淡入后播，在过场里退出会漏掉）；`room207_door` 用过就传送到 `Main Road End Spawn`。
- `room207_door` 用过以后的这一条只管关卡号还是 2 的读档，也就是在离开 207 的黑屏里退出的情况；关卡号已经是 3 时，开局那段整个不跑，由 `Level3HintFlowManager` 摆玩家、开提示，见[第三关 Spec](level-three-spec.md)的读档。
- 只用这两个使用点的记录，不另存位置。宿舍入口之前读档照常在厂门外出生，走回来就行。点 Continue 时 EventSystem（执行顺序 -1000）先关掉主菜单，传送在同一帧里，不会先闪一帧厂门。

## 美术资源

按[美术资源列表](../../art-resources.md)第二关核对。

| 资源 | 状态 | 位置与现状 |
| --- | --- | --- |
| 宿舍楼前的旧旗杆 | 已实现 | `In-Scene Objects/工厂建筑群/01 建筑与构筑物/旗杆`，在宿舍楼正门轴线上、离门约 12 米，高约 10 米，不挂旗；基座和杆子有碰撞，离大门到宿舍入口的直线路线约 7.7 米，不碰 `Dorm Entrance Zone`。制作见 `ArtSource/Flagpole/CLAUDE.md`。Play Mode 观感待确认 |
| 207 室 | 已实现 | `In-Scene Objects/室内空间/207`，宿舍楼正下方的地下单独一间。床和柜子从 `室内空间/卧室/room` 复制后缩回原大，房间壳、炉子和烟筒、窗外背板和光照新做，见上面 207 室和 `ArtSource/Room207/CLAUDE.md`。Play Mode 观感待确认 |
| 桌子抽屉 | 已实现 | 新做的宿舍两屉桌 `207/Furniture/Table` 左边那只抽屉 `Table Drawer`，单独成件、半拉开、不能开合，里面的 `Map Spot` 标着山路图的位置，见 `ArtSource/Room207/DormDesk/CLAUDE.md` |
| 山路图 | 已实现 | 正反两面贴图：`Assets/Art Resources/Props/MountainRoadMap/MountainRoadMap_Front.png`（印好并填过的旧工票）和 `_Back.png`（铅笔路线图），2048 × 1424，导入时关掉了 2 的幂缩放。路线按场景实测布局画，从宿舍 207 沿主路到底、转小路到索道下站、坐吊斗经 8 座塔架到上站，走上站背后的栈道到山脊，观景平台上画叉。场景里的纸片是 `207/Mountain Road Map`，背面朝上躺在抽屉里，看过一次后留在桌面上，查看、悬停翻译和回响都已接上，见上面山路图。制作见 `ArtSource/Props/MountainRoadMap/CLAUDE.md`。Play Mode 观感待确认 |

## 待确认

1. 进门触发盒的位置和大小，现在从门内约 5 米开始，在 Play Mode 里走一遍再定。
2. 宿舍正门台阶最下一级的碰撞面比门前地形高约 1 米，玩家的 Step Offset 是 0.3，可能走不上台阶。站在台阶前的地面上离门约 2 米，照样能对着门按 F，流程不受影响；要不要补一级台阶或垫高地面，在 Play Mode 里看过再定。
3. `Dorm Entrance Zone` 的大小和 M2.2 的时机，在 Play Mode 里走一遍再定。
4. 山路图回响的最小入镜大小 0.3、最大距离 1.6 米、对焦阈值 `maxFocusBlur` 0.003，和纸片底色 0.8，在 Play Mode 里拍一次看过再定。纸片挪到桌面上以后离镜头更近，`Desk Spot` 的位置和朝向、悬停翻译的字框大小（塔架编号的框很小）也一起看。对焦滑条是 0.1 到 1000 米的对数刻度，1 米附近只占很短一段，先不改，拖不准再说。
5. `Main Road End Spawn` 的位置和朝向，在 Play Mode 里看过再定。从主路上看不到 1 号塔架，要看到就得把落点挪进两栋厂房之间的空当，离开主路。

## 测试入口

- 完整走一遍：Start New Game 后照第一关流程剪链开门，再走进大门。
- 跳过第一关：退出 Play 后改 `Progress/Progress.json`，`usedPoints` 里加上 `gate_chain`，`hintIndex` 改成 8（T1.10），`level` 是 1，点 Continue，大门已经开着，右上角出 T1.10；走进大门，T1.10 勾上，提示框消失后出标题。背包、日记这些可以不管，开头衔接不用它们。
- 测读档：进门看完标题和 M2.1、出 T2.1 后退出 Play，点 Continue，应直接显示 T2.1，不重播标题和 M2.1，Player's Panel 的页签全开；在标题还没消失时退出，Continue 后应补播 M2.1 再出 T2.1。
- 测进宿舍：在上面跳过第一关的改法上，再确认 `Backpack/BackpackItems.json` 的 `itemIds` 里有 `lock_pick`，没有就加上。进门出 T2.1 后走到宿舍楼正门，台阶前播 M2.2；对着门按 F，底部出 Dormitory unlocked，T2.1 勾上，字消失后画面淡黑，逐段读三段文字，期间走动、拍照、F、面板都不响应；读完淡入时人在 207 门里、面朝窗户、视线水平，接着播 M2.3，播完才出 T2.3，操作恢复。也可以直接把 `level` 改成 2、`hintIndex` 改成 0，Continue 后补播 M2.1 再出 T2.1。
- 测 207 里的第 5 到 7 步：照下面直接测 207 室的改法读档进，或者从宿舍入口进。从入口进就照上面测进宿舍的改法，`level` 2、`hintIndex` 0，`usedPoints` 只留 `gate_chain`、去掉 `dorm_entrance`，背包里有 `lock_pick`，`GrandpaDiary/Acquisition.json` 的 `acquired` 为 true（日记面板和回响都要它）。撬门进 207，出 T2.3 后：打开面板翻到日记第二页 Room 207，T2.3 的第一项勾上；到桌前看左边半开的抽屉，对着纸片按 E，先看到的是正着的铅笔路线图，右边是四行英文，右键放回后第二项勾上、播 M2.4，出 T2.4。要测没看日记的情况，就跳过翻日记直接看图，放回后两项应一起勾上。T2.4 时举起相机按 V 全屏，站在桌前低头对准纸片，默认焦距下框是白的、没有小字；拉长焦距到纸够大，第一项勾上，框下出 Out of focus；把对焦拖到 1.2 米左右，小字消失、框变黄，第二项勾上；按快门应出 New Echo Discovered，第三项和 T2.4 勾上、播 M2.5，播完出 T2.5。出 Out of focus 时按快门只存普通照片，不勾。T2.4 之前对着纸片框不变黄。
- 测清档：在第二关里退出 Play，重新 Play 后在主菜单点 Start New Game，应回到第一关开场，第二关的标题和提示都不出现。
- 直接测 207 室：在上面跳过第一关的改法上，再往 `usedPoints` 里加 `dorm_entrance`，`level` 改成 2，`hintIndex` 改成 1（T2.3），`GrandpaDiary/Acquisition.json` 的 `acquired` 为 true，点 Continue，应直接出生在 207 室门里、面朝窗户；`playedMonologues` 里没有 M2.3 时先补播 M2.3 再出 T2.3。
- 测离开 207（第 8、9 步）：在直接测 207 室的改法上把 `hintIndex` 改成 3（T2.5），Continue 后出 T2.5。转身对着门，门把手处出 F 提示 Leave Room 207；按 F 底部不出字，T2.5 勾上，接着淡黑，逐段读三段离开宿舍的文字，不淡入，接着读两段跟着山路图的文字，期间走动、拍照、F、面板都不响应；淡入时人在主路上小路路口前，面朝两栋厂房之间的空当，视线水平，提示框随后消失，操作恢复。退出后 `Progress/Progress.json` 的 `hintIndex` 是 4，`usedPoints` 多了 `room207_door`。`hintIndex` 是 1 或 2 时对着门没有 F 提示。
- 测结尾读档：在上面的改法上再加 `room207_door`、`hintIndex` 改成 4，Continue 后应出生在同一个落点，没有提示框；第三关还没做，不出标题。
- 测第一关不受影响：背包里没有 Lock Pick、Bolt Cutter 时，对着门卫室门锁和大门铁链仍没有 F 提示。
- 关卡号是 `Progress/Progress.json` 里的 `level`，第一关是 1，进门后是 2；`hintIndex` 是当前关卡提示列表里的位置。
