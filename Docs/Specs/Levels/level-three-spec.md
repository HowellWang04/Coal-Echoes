# 第三关 Spec

对照[设计文档](../../design.md)的“关卡3：索道站始发站”一节和[剧本](../../script.md)的第三关。第三关的流程都定了：开头从第二关离开 207 室接上，跟着山路图来到主路尽头，玩家自己沿小路走到索道下站；中段在下站给驱动电机上油，索道动起来；结尾按 F 上吊斗坐上山，快到站时跳过和不跳过都进同一段黑屏过场，天在路上黑下来，吊斗在夜里开到山顶站门口停下，玩家转身站到站台上。月夜见[月光 Spec](../Systems/moonlight-spec.md)。开头和中段已实现，由 `Level3HintFlowManager` 串起来，从离开 207 一直到索道动起来、出 T3.4；结尾从上吊斗、乘坐、跳过、到站前的黑屏过场到站上站台、写出第四章标题，都由 `CablewayRideManager` 实现。Play Mode 都待用户测。

## 范围

- 开头：从第二关离开宿舍的过场接上，到玩家沿小路走到索道下站为止
- 中段：到了下站以后拿起 Oil Can 给驱动电机上油、让索道动起来，到出现 T3.4 为止，设计见设计文档关卡3第 3 到 5 步
- 结尾：从按 F 上吊斗开始，到玩家站到山顶站站台上、显示第四关标题为止，设计见设计文档关卡3第 6 到 9 步

## 开头：跟着山路图到下站

### 玩家流程

| 步骤 | 玩家经历 | 说明 |
| --- | --- | --- |
| 1 | 在 207 门口按 F 离开，读离开宿舍的过场 | 第二关第 8、9 步，见[第二关 Spec](level-two-spec.md) |
| 2 | 画面不淡入，接着读第三关开头的过场：主人公看着拍下的山路图，沿主路一直走到尽头 | 两段过场在同一次黑屏里连着读，读法相同；第二关在这里结束。沙丘以后约 220 米主路不让玩家走，用这段文字带过 |
| 3 | 淡入时玩家站在主路尽头，面朝通往下站的小路，写出 Chapter Three / The Lower Station | 第三关从这里开始 |
| 4 | 玩家自己沿小路走到索道下站 | 不传送。小路是现成的土路，约 80 米，最后一段上坡到站棚 |
| 5 | 走进下站，播 M3.2，勾 T3.1 | 接中段，见设计文档关卡3第 3 步 |

### 两段过场连读

已实现，Play Mode 待用户测。

- `CutsceneTextUI.Play` 加了收一组 id 的重载，把各自的段落接起来读，中间不淡入；`onBlack` 仍只在开头全黑时调一次，`onFinished` 在最后淡入完调。收一个 id 的写法转给它，进宿舍那段不变。有一个 id 在 JSON 里找不到就整段不播，只打警告。
- 第三关开头的过场 id 是 `follow_map`，单独一条，在[过场文字](../../json-cutscene-text.md)的第三关一节，已同步 JSON 和[游戏文本](../../game-text.md)，不并进 `leave_dormitory`，两关的文字各改各的。
- 第二关第 8 步门口的使用点 `room207_door` 在 onUsed 里调 `Level2HintFlowManager.LeaveDormitory`，一次播 `leave_dormitory` 和 `follow_map`，`onBlack` 里传送到 `Main Road End Spawn`。

### 传送和章节标题

- 落点是空物体 `Main Road End Spawn`，第二关离开 207 时已放好并接上，在小路 `工厂支路/支路 至索道站 LineB` 的路口前，面朝两栋厂房之间通往站棚的空当，位置和朝向见[第二关 Spec](level-two-spec.md)的传送和过场。路口在主路终点往回约 12 米，落点要让玩家一眼看到它；1 号塔架在主路尽头那栋厂房背后，从主路上看不到。传送用现有的 `PlayerMovementManager.TeleportTo`。
- 已实现，Play Mode 待用户测。第三关的开始照第二关开头衔接的写法：`Level3HintFlowManager`（`Assets/Scripts/In-Game Systems/Hint System/`）挂在 `Gameplay Root/Managers/Level3HintFlowManager`，每帧检查。关卡号为 2、第二关已完成（`Level2HintFlowManager.LevelComplete`，即 `room207_door` 用过）、第二关提示已全部显示完（`HintPopupUIManager.Finished`）时，调 `GameProgress.StartLevel(3)` 和 `ChapterTitleUI.Show`，两行字填在 Inspector 里。
- 不能在 `onBlack` 里就 `StartLevel(3)`。T2.5 勾完后提示框要等过场结束才收尾，收尾时写进度，会把第二关的提示位置写进第三关。提示框在过场结束的同一帧收尾，所以等它显示完，标题也是淡入时就出。
- 标题消失后照第二关的顺序，先播 M3.1，再调 `hintPopup.Begin()` 出 T3.1，文字在[内心独白](../../json-inner-monologue.md)和[任务提示](../../json-hints.md)的第三关一节，已同步进 `inner-monologue.json` 和 `level3-hints.json`。`HintPopupUIManager.levelHintJsons` 已接上第三份 `level3-hints.json`，`Begin` 按关卡号取得到。

### 走到下站

- 小路是现成的两条土路，不用新做。`支路 至索道站 LineB` 是干道，约 78 米，穿过两栋厂房之间的空地，贴着煤堆 B 走到坡脚，再上坡到 LineB 站棚北端；`支路 至索道站 LineA` 在煤堆 B 西北角分出去，约 29 米上到 LineA 站棚北端。路线和上坡整平见 `ArtSource/FactoryRoads/StationRoad/CLAUDE.md`，最陡约 22 度，上坡手感没在 Play Mode 里走过。
- 走到哪里算到了下站，用一个勾了 Is Trigger 的 BoxCollider `CablewayFoothillStation/Lower Station Zone` 盖住两座站棚北半段和中间的空地，两条小路从哪条上来都算，`Level3HintFlowManager` 照第二关每帧看玩家位置，不用物理回调。走进去时勾 `T3.1_reach_station`、播 M3.2，再记一条进度 `lower_station`（读档用，见中段的读档），之后是中段。
- 主路尽头往回到沙丘背后这段是开放的，玩家可以走回去，不加空气墙。

### 读档

- 现在 Continue 不存玩家位置。关卡号是 3、还没走到下站时读档，由 `Level3HintFlowManager` 传送到 `Main Road End Spawn`。本次游戏里刚开始的第三关不传送，玩家看标题时可能已经往前走了。
- 在黑屏里退出时关卡号还是 2，读档后放到落点，照常开始第三关、写出标题。关卡号已经是 3 时不重播标题，开头独白没播过就补播，再出存着的提示，和第二关一样。
- `Level2HintFlowManager` 开局那段（补播 M2.1、第二关已完成时传送到落点、调 `hintPopup.Begin()`）已改成只在关卡号为 2 时跑，第三关的读档只由 `Level3HintFlowManager` 摆玩家、开提示。

## 中段：给电机上油

### 玩家流程

| 步骤 | 玩家经历 | 说明 |
| --- | --- | --- |
| 1 | 走进下站，M3.2 点掉后右上角出 T3.2，下面两条子任务 | 接开头第 5 步 |
| 2 | 在电机旁按 F 拿起 Oil Can，进查看画面转一转，右下角出 Oil Can，播 M3.3，勾第一条子任务 | 和第一关拿 Bolt Cutter 一样 |
| 3 | 对着电机按 F 上油，底部出 Motor started，电机转起来，满山吊斗一起动；接着播 M3.4，勾第二条子任务，T3.2 完成 | |
| 4 | M3.4 点掉后出 T3.4，站门口那只吊斗正往上车段开 | 接结尾的上吊斗 |

### 场景对象

- 电机已放好：`In-Scene Objects/索道群/站房/CablewayFoothillStation/Drive Motor`，在上山一侧的 LineB 站棚里、滚轮架南边，靠着站棚后墙（不朝缆线的那条长边），坐在走道格栅上。站棚从后墙到站台边约 3.5 米。上车段在同一侧滚轮架北边（见结尾的上吊斗），从电机前走过去约 4 米。上油时 LineA 进站口那只吊斗正沿两座站棚之间往南开，隔着缆线看得到，绕过回转轮后沿 LineB 站台往北，穿过滚轮架开进上车段
- 模型和预制体见 `ArtSource/Cableway/DriveMotor/`。看得出在转的部件是电机和减速箱之间带两道黄漆的联轴器鼓，子物体 `Model/Rotor`，绕自己的本地 X 轴转。实例上已加一个包住全部网格的 BoxCollider
- `Drive Motor` 上挂 `ItemUsePoint`，`requiredItem` 填 Oil Can，F 提示 Use Oil Can to oil the motor（T3.3），useNotice 填 Motor started，saveId 填 `drive_motor`，`promptAnchor` 填预制体里现成的 `OilFillerAnchor`，在减速箱加油口上方。F 提示靠射线找，射线忽略触发器，上面那个 BoxCollider 不勾 Is Trigger 正好用。已挂好
- Oil Can 已做好并放好：模型见 `ArtSource/Props/OilCan/`，背包物品 `Assets/Art Resources/Props/OilCan/OilCan_BackpackItem.asset`，itemId `oil_can`，displayName Oil Can，usage 是[游戏文本](../../game-text.md)里的背包说明，缩略图和查看用的 prefab 都已填。场景实例 `CablewayFoothillStation/Oil Can` 在减速箱一端前面的沙地上，站在电机前就看得到，已加 BoxCollider 和 `InspectableItem`，`backpackItem` 填上面的资源，查看距离照背包物品资源
- 读档用的出生点 `CablewayFoothillStation/Lower Station Spawn` 已放好，在 LineB 站棚里电机和站台边之间，脚踩站棚地板，面朝站台边和缆线，见下面的读档

### 地面和碰撞

- LineB 站棚一带地面是平的，走道格栅高出地面约 0.3 米；电机坐在格栅上，Oil Can 放在电机旁的木托盘 `SM_Pallet38` 上。LineA 站棚的格栅被积沙埋着，那边玩家踩的是积沙
- 山脚站的站棚、滚轮架和回转站模型本身都没有碰撞体。`CablewayFoothillStation/LineB Station Colliders` 上加了三个 BoxCollider：后墙、滚轮架，和盖住走道格栅的地板，地板顶面比格栅略低，从地面迈上去不超过角色的台阶高度。LineA 一侧在 `LineA Station Colliders` 上也有后墙和滚轮架两个；回转站没加

### 流程

已实现，Play Mode 待用户测。中段的检查都写在 `Level3HintFlowManager` 里，照第一关每帧检查。

- `oilCan.Allowed` 和 `driveMotor.Allowed` 都等 `hintPopup.HasReached("T3.2")`。T3.2 出现前就拿起或用掉，子任务会勾不上，和第一关 Bolt Cutter 等 T1.9 一样。电机的 F 提示还要背包里有 Oil Can，没拿油罐时看着电机不出提示
- 背包里有 `oil_can` 时勾 `T3.2_pick_up_oil`、播 M3.3，和第一关拿到 Bolt Cutter 播 M1.7 一样。拿起时自动进查看画面，右下角的 Oil Can 是背包加物品时自己发的
- 电机的 onUsed 在 Inspector 里接两件事：`HintPopupUIManager.CompleteTask("T3.2_oil_motor")` 和 `InnerMonologueUIManager.Play("M3.4")`。`ItemUsePoint` 在 onUsed 之前先发 Motor started，M3.4 等它淡出才出，提示框等 M3.4 点掉才出 T3.4
- 索道启动不放在 onUsed 或 onRestored 里，由 `Level3HintFlowManager` 每帧看：电机用过、`CablewayMovementManager` 没在动时调 `StartMovement()`。onRestored 在场景开局的 Start 里调，那时主菜单还在，在那里启动的话索道会在主菜单背后一直走，玩家拿到操作时站门口那只早开走了。每帧的检查放在主菜单关掉之后，第一次上油和读档走同一句
- 索道在动时 `Level3HintFlowManager` 每帧转电机的转动件，转不转只跟着 `IsMoving`，读档和新游戏不用另外处理
- 索道一直走到玩家那只吊斗开到山顶站门口，那时整条索道停下，之后不再开，见结尾的到站
- 音效“索道启动”见[音效列表](../../sound-effects.md)，接法等音效做好再定
- 上完油 Oil Can 留在背包里，和 Lock Pick、Bolt Cutter 一样，背包没有移除物品的接口

### 新游戏

Start New Game 不重载场景，电机的 `ItemUsePoint` 回到没用过，`InspectableItem` 把 Oil Can 放回原处，背包清空。索道不用另外复位：清档只在开局主菜单里发生，那时还没玩过，而索道只在主菜单关掉以后由 `Level3HintFlowManager` 启动，清档时一定还停在开局位置，所以 `CablewayMovementManager` 没有订阅 `SavesCleared`。

### 读档

- 走进 `Lower Station Zone` 时记的 `lower_station` 和落到山顶站台时记的 `summit_arrival` 一样用 `GameProgress.MarkUsed`
- Continue 时 `Level3HintFlowManager` 看这条：记过就传送到 `Lower Station Spawn`，没记过照开头的读档放到 `Main Road End Spawn`
- 上完油、还没上吊斗时读档，玩家出生在下站，索道在拿到操作时从开局位置重新动起来，站门口那只照样约 11 秒开到上车段。M3.4 已经播过，提示框直接出 T3.4，不再被独白定住
- Oil Can 靠背包存档回来，`InspectableItem` 开局时看到背包里有，就把场景里那只藏起来

## 结尾：坐吊斗上山

### 玩家流程

| 步骤 | 玩家经历 | 说明 |
| --- | --- | --- |
| 1 | 给电机上完油，站门口那只吊斗慢慢开向上山一侧的站台；吊斗出了滚轮架、经过上车段时按 F，玩家直接到了吊斗里，沿缆线往山上走，和当年煤运下来的方向相反 | 见下面的上吊斗 |
| 2 | 乘坐途中不能走动，能转视角、举相机拍照；没举相机时画面角落显示长按 Space 跳过的提示 | 见下面的乘坐和跳过 |
| 3a | 一直坐着，坐到离山顶站门口的停车点还差三秒的地方 | 自动进入步骤 4，第三关一直是白天，乘坐途中天色不变 |
| 3b | 中途长按 Space 跳过 | 立刻进入步骤 4 |
| 4 | 画面淡出成黑屏，中央逐段显示到站前的过场文字，读法和开场文字相同 | 两条路读同一段，见下面的黑屏过场文字；黑屏时切成月夜，吊斗放到停车点前三秒的位置 |
| 5 | 文字读完画面淡入，已是月夜，视角朝着吊斗前进的方向；吊斗走完最后三秒，在山顶站门口停下，整条索道跟着停住 | 见下面的到站 |
| 6 | 视角向右转 90 度，朝向站台 | |
| 7 | 瞬移到站台上，显示 Chapter Four / The Upper Station，交还走动 | 第三关结束 |

单程沿缆线约 1.06 公里，爬升约 210 米。按现在每秒 2.5 米的速度要坐七分钟左右，所以给跳过。

### 上吊斗

已实现，Play Mode 待用户测。`CablewayMovementManager`（场景对象 `In-Scene Objects/索道群/索道移动管理`）带着 36 个吊斗沿一条闭合路线循环，上吊斗和乘坐由 `CablewayRideManager`（`Assets/Scripts/Environment/`，挂在 `Gameplay Root/Managers/CablewayRideManager`）做，按 F 后直接瞬移进吊斗，不做爬进去的动作。

- 线路方向：站在山脚站朝山上看是右上左下。行程往上增加时吊斗从山脚站沿右边的 LineB 上山，再沿左边的 LineA 下山回站，所以山脚站 LineB 站棚是上山一侧，LineA 是下山进站一侧。两条缆线都在两座站棚之间，LineB 那条离 LineB 站棚朝缆线敞开的长边约 1.6 米
- 上车段：在 LineB 滚轮架北边，从滚轮架北端到站棚北口外约 3 米，不在滚轮架南边的站台上。滚轮架顶上的托辊正在吊斗里人眼的高度，上车段放在南边的话，刚上车吊斗就带着镜头从托辊里穿过去；放在北边，吊斗已经过了滚轮架。按站位沿整条上山线查过，镜头只会穿过山脚站这个滚轮架和山顶站进站的滚轮架，塔架都不碰，检查脚本和结果是 `ArtSource/Cableway/Movement/ride_clearance.cs` 和 `ride_clearance.json`
- 高度：站里的缆线已放低 1.5 米，见 `ArtSource/Cableway/FoothillLowering/`。LineB 这一侧站棚外的地面是平的，上车段斗沿比玩家脚下的地面高约 1.7 米，比 LineA 一侧高；瞬移上车，不用真的爬
- 开局摆位：`塔架/CablewayTower_02/CoalBucket (1)` 摆在山脚站北端、两座站棚之间的 LineA 进站口，是一只刚下山、正要进站的吊斗，`CablewayMovementManager` 准备运动时按每只吊斗的当前位置求起始行程（`FindNearestDistance`）。索道一动，它先往南走约 11 米到回转轮，绕过去，再沿 LineB 站棚往北，穿过滚轮架，从进站口到上车段开头约 27 米。截图在 `ArtSource/Cableway/Movement/Previews/`，是反转方向以前在另一侧拍的
- 启动：上完油索道就动，从这时开始一直运行，见中段的流程
- 反应时间：按 F 上油后，底部的 Motor started 显示约 2.4 秒，这段时间玩家能走动；接着 M3.4 把玩家定住，点掉才能动，T3.4 和上车的 F 也是这时才有，多在上油后 5 到 7 秒。场景里的速度是每秒 2.5 米，站门口那只约 11 秒开到上车段开头，从电机走到上车段约 4 米、一两秒，第一只赶得上。吊斗过上车段约 3.8 米，F 只有约 1.5 秒，要站在那里看着它从滚轮架里出来再按；错过了等下一只。够不够在 Play Mode 里看
- 上车范围：一个勾了 Is Trigger 的 BoxCollider `CablewayFoothillStation/Boarding Zone` 盖住上车段那截缆线，`CablewayRideManager` 每帧看有没有吊斗的轴心（在抓绳器上）在里面，和第二关判断玩家位置的写法一样
- 按 F：`CablewayFoothillStation/Boarding Point` 是一块不勾 Is Trigger 的薄 BoxCollider，沿上车段立在缆线靠 LineB 站棚的一侧，挂 `ItemUsePoint`，`requiredItem` 留空，F 提示 Climb into the bucket（T3.5），不填 useNotice。场景里的射线忽略触发器，所以 F 提示要靠这块实体碰撞体，它也顺便挡住玩家走到吊斗底下。`CablewayRideManager` 每帧把它的 `Allowed` 设成“已到 T3.4 且上车范围里有吊斗”，并把 F 提示的锚点 `Prompt Anchor` 挪到那只吊斗的斗沿上。用“已到 T3.4”而不是“当前提示是 T3.4”，因为上了吊斗 T3.4 就勾完、提示框收尾，读档后还要能再上。不填 saveId
- 按下后：onUsed 依次调 `HintPopupUIManager.CompleteTask("T3.4_board_bucket")` 和 `CablewayRideManager.Board()`，把玩家放进范围里那只吊斗。错过了这一只，下一只转过来进了范围还能上。全线 36 只吊斗等距排开，间距约 59.5 米，约 24 秒过一只，见 `ArtSource/Cableway/Movement/`

### 乘坐

- 上车时 `Board` 先把 `PlayerMovementManager.AllowMovement` 设成 false，只锁走动，同时关掉 `CharacterController`，转视角照常；再用 `TeleportTo` 的位置加朝向重载把玩家放到站位点，面朝吊斗前进方向、视线水平。之后 `CablewayRideManager` 每帧在 LateUpdate 把玩家放回站位点，吊斗在 Update 里已经走过。不把玩家挂到吊斗下面，层级不变，也不会带上吊斗的缩放
- 转视角写的是世界朝向，吊斗在站里拐弯时视角不会跟着甩。`SetPlayerMovementControl` 仍同时管走动和转视角，独白、面板和查看照旧用它，和 `AllowMovement` 互不覆盖
- 坐的就是场景里现有的吊斗 `CablewayCoalBucket`，不改模型。斗身是开口朝上、上宽下窄的翻斗，斗口上方约 0.3 米有一根顺着长边的吊架横梁，两头各一根斜撑往上收到抓绳器，中间一根竖管
- 站位点是 `CablewayRideManager` 上吊斗本地坐标里的固定偏移 `standOffset`，36 只吊斗模型相同，不用每只都加空物体。它在斗底前半、偏向不挨着另一条线的一侧（上山时的行进方向右侧）；原地转一圈，镜头离吊斗自己的竖管和斜撑最近约 0.13 米，近裁剪面 0.05 米切不到。眼高截图是 `ArtSource/Cableway/Movement/Previews/ride-*.png`，是反转方向以前在另一侧拍的
- 乘坐时能举相机拍照，可以拍到和主菜单报纸照片相近的从索道上俯拍厂区的角度。相机的输入不看 `AllowMovement`，乘坐中照常可用，不用另开
- 坐到停车点前三秒时相机可能还举着。`CablewayRideManager` 先等相机停在稳定状态，不在举起放下、切换视图或拍照中（`PhotoModeInputRouter.CameraBusy`），再调 `PhotoModeInputRouter.LowerCamera()` 收起，和按相机键收起一样，放下的动作在过场淡出时播完。只多等不到一秒，离停车点还有余量

### 跳过

已实现，Play Mode 待用户测。

- 跳过键是长按 Space，`PlayerControls.inputactions` 的 Gameplay 里加了按钮动作 `Skip`，绑 Space，`PlayerControls.cs` 已重新生成。按住多久算数由 `CablewayRideManager` 自己计时（`skipHoldDuration`），松手清零，用长按是为了免得误触
- 只在乘坐中、`PlayerMovementManager.AllowControl` 为真时计时和显示，独白、面板和查看界面开着时不算，在手账里打空格不会跳过
- 举着相机时也不计时、不显示，相机按键提示和跳过提示都在右下角，会叠在一起；要跳过得先放下相机
- 提示是 `In-game UI Root/SkipPromptUIRoot` 上的 `SkipPromptUI`，在右下角，照相机按键提示的样式复制了一行：Space 键图标（`Inspect Icons/Default/keyboard_space_outline.png`，已改成 Sprite 导入）加 Hold to Skip，文字下面的线是底，按住时上面一条 `Fill` 从左往右填满。没有另做圆形进度圈，和游戏里已有的按键提示一个样子
- 跳过时立刻进到站前的黑屏过场，和坐到停车点前三秒自动进的是同一段，后面的流程完全一样

### 黑屏过场文字

已实现，Play Mode 待用户测。过场 id 是 `ride_to_summit`，文字见下面的到站前过场文字，已放进[过场文字](../../json-cutscene-text.md)、JSON 和游戏文本。

- 两个入口：中途长按 Space 跳过；或者玩家那只吊斗离停车点只剩三秒的路，按每秒 2.5 米约 7.5 米。`CablewayRideManager` 每帧用 `CablewayMovementManager.DistanceUntil` 求还差多少，两种都只进一次
- 第二关进出 207 室也各有一段黑屏过场文字，几处共用 `In-game UI Root/CutsceneUIRoot` 上的 `CutsceneTextUI`，第二关进宿舍时已经做好，做法见[第二关 Spec](level-two-spec.md)的传送和过场。读法照开场文字，逐字显示、点击加速、点击换段、不能跳过
- 开场的 `OpeningSequenceUI` 绑着抬镜头、黑边和第一关标题，不直接复用；`CutsceneTextUI` 逐段读文字的部分是照它写的
- 组件在画面全黑时调一次 `onBlack`，在这里做四件事：`CablewayMovementManager.StopMovement()` 让索道在读字时停住；调 `SampleMovement` 把全部吊斗一起挪到玩家那只离停车点还差三秒的位置，跳过的玩家就是从这里接上，不跳过的在淡出那一秒里多走了一点，也拉回这里；把玩家视角转回吊斗前进方向、视线水平；调 `MoonlightController.SetNight(true)` 切成月夜。之后要读完几段文字才淡入，天空和环境光有足够的帧刷新
- 过场期间锁输入的方式照 `MainMenuUIManager`，结束时按进入前的状态恢复，组件里已做好。淡入完调 `onFinished`，`CablewayRideManager` 在这里再关掉 Gameplay 输入、锁住转视角，玩家只看着吊斗进站，直到站上站台

### 到站

已实现，Play Mode 待用户测。

- 停车点是空物体 `CablewaySummitStation/Ride Stop Point`，标出停车时吊斗轴心的位置，就在路线上，在山顶站门口、上山线进站的滚轮架 `CablewaySummitStation/CablewayStationRollerFrame_LineA` 前面约 1 米，正好在站房地基前沿上方，开不进站里。那个滚轮架在吊斗里人眼的高度，从它开始镜头会穿过去，见 `ArtSource/Cableway/Movement/ride_clearance.json`。吊斗前沿离滚轮架约 0.3 米；停下时斗沿比站台地面高约 2.1 米
- 到站判断用新加的 `CablewayMovementManager.DistanceUntil(bucket, position)`：按每只吊斗的起始行程、当前行程和 `FindNearestDistance` 求出这只吊斗还要走多远才到停车点。从站门口那只算起约 1071 米
- 最后三秒不交给索道自己走：`onBlack` 时记下停车点对应的行程，`CablewayRideManager` 每帧按索道速度往前推、调 `SampleMovement`，推到停车点正好停住，不会冲过头。索道从 `onBlack` 起就是停着的，之后不再开；`Level3HintFlowManager` 原来看到电机用过、索道没在动就会启动它，现在多看一条 `CablewayRideManager.ArrivalStarted`（进了黑屏以后，或者记过 `summit_arrival`）就不启动，读档时也一样。索道停了，电机转动件跟着停
- 停稳后停顿一下，视角平滑转向站台落点的朝向，就是向右 90 度。上山线进山顶站时站台在行进方向的右边，玩家的站位点也在吊斗右侧。转视角锁着，`PlayerMovementManager` 不会覆盖这里转的朝向
- 转完再停顿一下，`TeleportTo` 瞬移到落点 `CablewaySummitStation/Summit Platform Spawn`，`AllowMovement` 设回 true，交还视角、走动和 Gameplay 输入。落点在站房地基前沿和 LineA 站棚前端之间那条走道上，离停下的吊斗约 2 米，面朝转身后的方向：前面是栏杆外月夜的远山和月亮，左边贴着站棚前端的立柱。山顶站 LineA 站棚离缆线比 LineB 那边近，转身时左手边的站棚立柱离镜头只有约 0.3 米，但不穿模。站台上之后的视野和路线以第四关为准，栈道在站房背后另一侧，要绕过去
- 落地时第三关结束：记 `summit_arrival`，`GameProgress.StartLevel(4)`，写出 Chapter Four / The Upper Station。第四关还没有流程管理器，这几步先由 `CablewayRideManager` 做，第四关的管理器做出来以后挪过去；在第四关 Continue 时玩家出生在哪里也等它
- 停车、转身、落点的截图在 `ArtSource/Cableway/Movement/Previews/arrive-*.png`，脚本是同目录的 `arrive_preview.cs`

### 存档

- 落到站台上时调 `GameProgress.MarkUsed(MoonlightController.SummitArrivalId)` 记一条进度（id 是 `summit_arrival`），和 `ItemUsePoint` 记 id 的做法相同。月夜组件开局时看这条决定是否直接切成月夜，索道看这条决定不再启动
- 切成月夜到站上站台之间只有几秒，这几秒里退出的话没有这条记录，读档回到白天的下站，T3.4 已勾完，还能再上吊斗坐一趟
- 现在 Continue 不存玩家位置，读档后玩家在哪里出生是所有关卡共同的问题，不在本 Spec 里解决

### 到站前过场文字

文字已定，在[过场文字](../../json-cutscene-text.md)里，id 是 `ride_to_summit`，JSON 和游戏文本已同步。跳过和不跳过的玩家都在快到站时读这一段，读完画面亮起时吊斗还在路上、已是夜里，所以文字写天在路上黑下来，最后一句停在快到上站，接上画面里吊斗进站。山下没有灯这件事留给第四关站上山脊时再看，这里不说破。

第二段借第五座塔架用上外公日记第三页 1983 年山洪那篇。日记一拿到就能翻全，第三关没有专门读它的一步，写在这里所有玩家都会读到。吊斗路过 5 号塔时不另弹独白，跳过的玩家看不到，独白还会把乘坐中的玩家定住。

1. The bucket climbed the line tower by tower, the opposite way the coal used to come down. I counted the towers the way Jianguo had marked them on his map.
   （吊斗沿着缆线一座塔一座塔往上走，和当年煤运下来的方向相反。我照着建国在图上画的，一座一座数着塔架。）
2. Past the fifth tower, I thought of the summer of '83. The flood had washed out its footing, the line had stopped, and Jianguo had put the map back in the drawer.
   （过第五座塔时，我想起八三年夏天。山洪冲垮了它的地基，索道停了，建国又把图收回了抽屉。）
3. Somewhere past the last tower, the sun went down behind the ridge, and the plant below slipped into the dusk.
   （过了最后一座塔，太阳落到山脊后面，山下的厂区沉进了暮色里。）
4. They had planned to come up at night, to see the lights. It was night now, and the upper station was just ahead.
   （他们约的是夜里上山，来看灯。现在天黑了，上站就在前面。）

## 美术资源

| 资源 | 现状 | 说明 |
| --- | --- | --- |
| 主路尽头到下站的小路 | 已有 | `工厂支路` 下通索道站的两条土路，见上面走到下站 |
| 驱动电机 | 已做，已摆进场景 | 上山一侧 LineB 站棚里、靠近上车段的锈电机组，给它上油后启动索道；联轴器鼓单独做成转动件，见中段的场景对象 |
| Oil Can 模型与 Backpack 缩略图 | 已做，已摆进场景 | 电机旁的手压式机油壶，F 拾取后给电机上油 |
| 乘坐的吊斗 | 用现有 `CablewayCoalBucket`，不改模型 | 只挪一只到下站门口，站位点用固定偏移，见上面的上吊斗和乘坐 |
| 山顶站停车点和站台落点 | 已放好 | 两个空物体 `Ride Stop Point` 和 `Summit Platform Spawn`，见上面的到站 |
| 跳过提示 | 已做 | 复制相机按键提示的一行，Space 键图标用现有图标包里的，见上面的跳过 |

## 待确认

目前没有。

## 验收

1. 在 207 门口按 F，离开宿舍的三段读完后不淡入，接着读第三关开头的两段；全程不响应走动、拍照、F 交互和面板
2. 淡入时人在小路路口前，面朝通往站棚的空当，视线水平；随即写出 Chapter Three / The Lower Station，提示框里没有第二关剩下的提示
3. 沿小路走到站棚北端不卡、不掉出路面，上坡走得上去；走进站棚开口能被检测到
4. 在黑屏里退出，Continue 后出生在主路尽头并写出标题；标题出过以后、走到下站以前退出，Continue 后出生在主路尽头，不重播标题
5. 在 LineB 站棚里走动不穿后墙和滚轮架，从地面走上站棚地板不卡，脚不陷进格栅；走进下站以后、T3.2 出现以前，Oil Can 没有 F 提示、拿不起来
6. 按 F 拿起 Oil Can，进查看画面，右下角出 Oil Can，播 M3.3，勾第一条子任务；没拿 Oil Can 时看着电机没有 F 提示，拿了以后出 Use Oil Can to oil the motor
7. 对着电机按 F，底部出 Motor started，电机转动件和全部吊斗一起动起来，接着播 M3.4，T3.2 完成，M3.4 点掉后出 T3.4
8. 走到下站以后退出，Continue 后出生在下站；上完油以后退出，Continue 后索道在拿到操作时从开局位置重新动起来，站门口那只照样开向上车段，Oil Can 在背包里、不在场景里
9. 上完油以后 Start New Game，吊斗回到开局位置、静止，电机不转，Oil Can 回到电机旁
10. 开局时站门口那只吊斗停在左边 LineA 的进站口，看着像刚下山要进站；上完油索道动起来，从山脚站朝山上看，右边的线往上走、左边的往下走，站门口那只吊斗约 11 秒后穿过 LineB 滚轮架进上车范围；出了 T3.4、范围里有吊斗时，在滚轮架北边对着那只吊斗按 F 出 Climb into the bucket，提示跟着吊斗走，按下进到吊斗里；范围里没有吊斗时没有 F 提示，走不到吊斗底下；错过一只，下一只进范围还能上；上了吊斗以后退出，Continue 后在下站还能再上
11. 上吊斗后面朝上山方向，不能走动，能转视角，没有脚步声；能举相机，切全屏能拍照，拍下的是吊斗里看到的画面；吊斗和其他 35 只一起沿缆线上山，玩家跟着吊斗不抖、不掉出来；原地转一圈，竖管和斜撑不被近裁剪面切掉；经过塔架时不擦镜头
12. 坐上吊斗后右下角出 Hold to Skip；长按 Space，下划线填满才跳；松手清零；独白、面板开着时和举着相机时不显示、不计时；进黑屏以后不再有跳过提示
13. 不跳过时坐到离停车点还差三秒自动黑屏；这时举着相机的话，相机先放下再黑屏，全屏取景和回看照片时也一样，淡入后相机不在画面里；跳过和不跳过两条路读同一段文字，淡入后都从同一处接上，看不到吊斗跳变
14. 到站前过场文字的读法和开场文字一致，不能跳过；过场期间不响应走动、拍照、F 交互和面板
15. 淡入时已是月夜，视角朝前；吊斗走约三秒，在山顶站门口滚轮架前停下，镜头不穿滚轮架，所有吊斗一起停住
16. 停稳后视角平滑向右转到站台，随后瞬移到站台上，写出 Chapter Four / The Upper Station，交还走动和视角；之后索道一直不动，电机转动件也停
17. 落到站台以后退出，Continue 后是月夜、索道不动；切成月夜以后、落到站台以前退出，Continue 后回到白天的下站，还能再上吊斗
