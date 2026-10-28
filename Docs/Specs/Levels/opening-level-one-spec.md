# 开场与第一关 Spec

对照[设计文档](../../design.md)的“开场：时间流逝”和“关卡1”两节，按当前代码、场景和美术资源列出已实现和未实现的部分。设计以设计文档为准，提示和独白文字以 [json-hints.md](../../json-hints.md)、[json-inner-monologue.md](../../json-inner-monologue.md) 为准，回响规则见[回响系统 Spec](../Systems/echo-system-spec.md)和[回响取景提示 Spec](../Systems/echo-targeting-spec.md)。状态分三种：已实现、部分、未实现。

## 范围与完成条件

- 起点是主菜单点击 Start New Game，终点是剪断铁链、打开 Factory Gate，对应设计文档详细流程第 12 步。进厂后转入第二关，衔接见[第二关 Spec](level-two-spec.md#开头衔接)。
- 街道照片和值班记录本是两个 B 类可选回响，不是完成条件。
- 玩家出生在大门外约 14 米的路边，站在摩托车旁面朝大门，已经下车，没有下车动画。
- 按键分工：F 是交互，拿 Lock Pick、拿 Bolt Cutter、撬门卫室的门、剪链开大门都按 F；E 是查看，值班记录本按 E 看内容，不能拿走，留在原处。值班记录本同时是回响物品，被拍到时照常走回响照片的逻辑。

## 开场

| 设计要求 | 状态 | 实现位置与现状 |
| --- | --- | --- |
| 双页报纸主菜单，1980 年 10 月 3 日的报纸，右页是从索道塔架俯拍厂区的实时照片 | 已实现 | `NewspaperMenuUI`、`NewspaperPhotoPass`，场景对象 `MainMenuUIRoot/MenuPresentation/Newspaper`，文案 `Assets/Narrative/newspaper-menu.json`；机位是 `Gameplay Root/MainMenuCamera`，图注写明从索道塔架上俯瞰，挪机位要同步改图注 |
| 点击开始后照片放大、纸墨消退，年份从 1980 翻到 2026 | 已实现 | `NewspaperMenuUI` 的 Zoom Delay、Zoom Duration、Reveal Duration，年份是 `YearTitle` |
| 三堆沙丘挡住主路，开场看不到，玩家走进大门才发现 | 已实现 | 沙丘在 `In-Scene Objects/沙丘`，碰撞和隐形挡墙始终在终点位置。`FactorySandAccumulation` 仍在照片转场时让沙丘从地下升起，但菜单相机离得远，主路沙丘夹在宿舍楼和厂房之间，画面里看不出来，这段保留不作为设计要求 |
| 天空快速掠过 | 已实现 | `OpeningSkyMotion`，只转 HDRI，没有体积云 |
| 开场文字期间镜头对着天空，从塔架机位换到厂门外；镜头降下时玩家才第一次看到摩托车和锁着的大门 | 已实现 | `OpeningSequenceUI` 在镜头抬到天空时把演出相机移到玩家相机位置。另有 `OpeningSceneState`（挂在 `MainMenuUIRoot`）在菜单期间打开两扇门、隐藏 `Chain_And_Padlock` 和摩托车，镜头换位的同一刻还原；塔架机位看不到大门，这段目前没有可见效果，保留给以后可能看得到大门的机位。Continue 和跳过开场在菜单关闭时还原，存档里铁链已剪断时还原成开门、不挂链；编辑器里保存的是关门挂链的姿态 |
| 宿舍楼由新变旧 | 已实现 | `DormitoryMaterialAging`，干净材质在 `Assets/Art Resources/Environment/WorkersDormitory/Clean/`；其他建筑没有干净版，设计只要求宿舍楼 |
| 镜头抬向天空，黑边收成 2.39:1，六段开场文字逐字显示，点击加速和换段，不能跳过 | 已实现 | `OpeningSequenceUI`，文字 `Assets/Narrative/opening-intro.json` |
| 镜头降到玩家视角，黑边收回 16:9，同时从左到右写出 Chapter One / The Factory Gate，停一会儿再渐隐 | 已实现 | `OpeningUIRoot/ChapterTitle`，两行文字写在 Inspector 里，书写由其上的 `ChapterTitleWriter` 做 |
| 演出期间不响应移动、拍照、F 交互和面板 | 已实现 | `MainMenuUIManager` 锁定输入，结束后交出操作 |
| Continue 继续游戏，不重播开场 | 已实现 | `MainMenuUIManager.ContinueGame`，有存档时可点，没有存档置灰；直接交出操作，任务提示、已播独白、门卫室的门和剪链后的大门按进度存档恢复 |
| 新游戏初始化，清空照片、回响、日记、背包和剪贴簿 | 已实现 | Start New Game 有存档时调 `GameSaveManager.ClearSaves` 原地清档，不重载场景：删掉全部存档目录（含进度存档），清空静态缓存，再发 `SavesCleared` 事件，相册、剪贴簿、背包、独白、工具使用点、门卫室的门、大门和可拾取道具各自回到新游戏状态，然后直接播开场 |

## 第一关流程

步骤号对应设计文档的详细流程表。

| 步骤 | 设计要求 | 状态 | 实现位置与现状 |
| --- | --- | --- | --- |
| 1 | 报纸转场和开场文字 | 已实现 | 见上表 |
| 2 | 镜头到玩家视角，播 M1.1 | 已实现 | `Level1HintFlowManager` 在主菜单关闭后播 M1.1，再开始提示 T1.1 |
| 3 | 从摩托车储物箱拿到 Grandpa's Diary、Group Photo、Lock Pick，T1.1 勾选，播 M1.2，Player's Panel 可以打开 | 已实现 | 摩托车上的 `GrandpaDiaryPickup`，一次 F 三样到手，F 提示 `Get Grandpa's things`，`Level1HintFlowManager` 等 T1.1 出现（M1.1 点完之后）才把它的 `Allowed` 打开，拿到后右下角依次出三行拾取提示；Lock Pick 不摆实物，直接进背包；Group Photo 见美术资源 |
| 4 | Tab 打开 Player's Panel，看到四个页签，T1.2，播 M1.3 | 已实现 | `PlayerPanelUIManager`；页签按教学顺序逐个开放，见系统现状；`Level1HintFlowManager` 在 T1.2 显示时见到面板打开就勾 T1.2 并播 M1.3，T1.3 等这句点掉才出 |
| 5 | 读日记第一页，T1.3，播 M1.4；可看日记全部页面，支持输入页码跳转 | 部分 | 只在 T1.3 显示时才算：面板开着停在日记第一页累计满 `Level1HintFlowManager` 的 Diary Read Seconds（8 秒），或看过第一页后翻页、关面板，就勾 `T1.3_diary_first_page` 并播 M1.4，日记开着时独白出现在面板下方，T1.4 等这句点掉才出；T1.3 出现前翻的页不算，读档回到 T1.3 要重新看一次第一页；拿到日记就能翻全部页面，不按进度开放；日记共四页，每页左右半页各一篇；页码跳转未实现 |
| 6 | 把 Group Photo 加进 Scrapbook 并摆放，T1.4 | 已实现 | T1.4 出现时开放相册页签，合照加进剪贴簿勾 `T1.4_add_to_scrapbook`，再开放剪贴簿页签，摆进剪贴簿勾 `T1.4_place_in_scrapbook` |
| 7 | 关面板、举相机、按 V 切全屏、走进泛光圆圈重拍 Group Photo，T1.5，播 M1.5 | 已实现 | 手持时不能拍照，相机处于全屏取景就勾 `T1.5_fullscreen`；站位机位是 `工厂大门/Group Photo Retake View`，按用户在 Scene 视图里对着合照校好的位置和朝向摆放，离大门约 10 米；泛光圆圈 `工厂大门/Group Photo Retake Circle` 是 HDRP 贴花，在它脚下。`FactoryGate` 挂 `EchoTrigger`（`group_photo_retake`），站在机位 1.2 米内、相机朝向和机位相差 10 度以内才变黄框，不再要求大门在框中央；拍成后它的 onCaptured 勾 `T1.5_match_photo`、播 M1.5。圆圈和这个回响由 `Level1HintFlowManager` 只在 T1.5 显示且未拍成时打开，拍成即消失，读档按提示进度恢复，不另存 |
| 8 | 在 Backpack 页签查看 Lock Pick，T1.6 | 已实现 | 在 Backpack 页签点 Lock Pick 那一行、放大看模型时勾选，只切到页签不算；`ItemBackpackSlot.OnItemInspected` 发出，`Level1HintFlowManager` 只认 `lock_pick`，T1.6 出现前点开不算；Lock Pick 初始朝向绕 X 转 90 度、距离 0.2 米，手柄和撬钩正对玩家 |
| 9 | 用 Lock Pick 撬开门卫室，T1.7、T1.8 | 已实现 | 门铰链 `Guard Post/Guardhouse_Rebuilt/Door_Hinge_Game` 上挂 `ItemUsePoint` 和 `SwingDoor`，F 提示在 `LockPromptAnchor`，撬开时中下方出使用提示 Guardhouse unlocked，门同时向外开 100 度；没带 Lock Pick 或 T1.7 出现前不显示提示，也撬不开；开门状态存档，读档时门直接是开的 |
| 10 | 进门卫室，发现值班记录本和 Bolt Cutter | 已实现 | 值班记录本在值班桌上，街道照片挂在后墙；桌面被记录本和台灯占满，Bolt Cutter `Guard Post/室内陈设/BoltCutter` 钳头着地，斜靠在观察窗下的侧墙上 |
| 11 | 按 E 查看值班记录本，播 M1.6 | 已实现 | 照生化危机的方式，不打开单独页面：记录本 `Logbook` 挂 BoxCollider 和只查看的 `InspectableItem`，走近出 E 图标，按 E 后本子从桌上飞到镜头前，封面朝玩家，可旋转缩放；屏幕上方显示 Guard Post Duty Log，右侧 `InspectItemUIRoot/ReadTextPanel` 显示本子内容（`InspectableItem` 的 readText），不加底色，暖白字带煤灰黑柔影，和右下角拾取提示同一材质。按鼠标右键放回桌上，这时 onInspectEnd 播 M1.6，独白每份存档只播一次。放回桌上就算这一步完成；本步可选，不勾任务提示，不看也能通关 |
| 12 | 拿起 Bolt Cutter，剪断铁链，打开 Factory Gate，T1.9，播 M1.7、M1.8，第一关结束 | 已实现 | Bolt Cutter 挂 `InspectableItem` 填 `bolt_cutter`，`Level1HintFlowManager` 等 T1.9 出现才把它的 `Allowed` 打开，之前不出 F 提示，按 F 拾取进背包，右下角出拾取提示，`Level1HintFlowManager` 见到背包里有它就播 M1.7；铁链 `FactoryGate/Chain_And_Padlock` 挂 BoxCollider 和 `ItemUsePoint`，F 提示在 `CutPromptAnchor`，没带 Bolt Cutter 或 T1.9 出现前不显示，也剪不了；用后中下方出使用提示 Chain cut，两扇门扇上的 `SwingDoor` 同时向厂内转开 90 度，勾 T1.9，隐藏锁链挂锁；M1.8 也接在 onUsed 上，等使用提示淡出后才播。Bolt Cutter 用完留在背包里。铁链使用点 saveId 为 `gate_chain`，读档时两扇门直接是开的、锁链挂锁隐藏；`gate_chain` 用过就算本关完成，不另存，第二关查 `Level1HintFlowManager.LevelComplete`，见[第二关 Spec](level-two-spec.md#开头衔接)；T1.9 勾完后出本关最后一条提示 T1.10，叫玩家穿过大门走进厂区，由 `Level2HintFlowManager` 在玩家走进大门时勾上；勾完后提示进度存为全部完成，关卡号仍是 1，接着才换到第二关 |

## 本关物品

| 物品 | 设计 | 模型与资源 | 场景与交互 | 文字 |
| --- | --- | --- | --- | --- |
| Lock Pick | 流程道具，储物箱拿到 | 预制体、图标、背包资源 `lock_pick` 都有 | 摩托车 F 进背包，门卫室使用点已接 | 背包用途说明已写 |
| Group Photo 重拍 | A 类回响，大门前泛光圆圈 | 泛光圆圈贴花和正式 Group Photo 已做 | Factory Gate 回响和站位圆圈已接，拍成勾 T1.5、播 M1.5 | M1.5 和手账文字已写 |
| Bolt Cutter | 流程道具，门卫室拿到 | 预制体、图标、背包资源 `bolt_cutter` 都有，已在背包 catalog 里；钳口不能开合 | 门卫室 F 拾取进背包，大门铁链使用点已接 | 背包用途说明、M1.7 和剪链的 F 提示已写 |
| 值班记录本 | B 类回响，按 E 查看内容，不能拿走 | 第三方皮面笔记本模型 | 场景对象 `Guard Post/室内陈设/Logbook`，回响编号 `fb2f4677570c4148a6914782a778a943`，可识别可记录；按 E 拿到眼前查看，旁边显示本子内容，放回时播 M1.6；轴心在本子中心 | 本子内容和手账文字在[游戏文本](../../game-text.md)里，都已填进场景；本子那一页是 1987 年 11 月 21 日，停厂前不久 |
| 街道照片 | B 类回响 | 旧木相框加街市照片 | 场景对象 `Guard Post/室内陈设/OldPhotoFrame_StreetMarket`，回响编号 `street_market_photo`，只认正面，可识别可记录 | 手账文字在[游戏文本](../../game-text.md)里，已填进场景 |

## 系统现状

| 系统 | 状态 | 说明 |
| --- | --- | --- |
| 右上角任务提示 | 已实现 | `HintPopupUIManager` 按 `GameProgress.Level` 读当前关卡的提示 JSON，第一关是 `Assets/Narrative/level1-hints.json`，按顺序显示，完成的任务用铅笔把框从左上往右下一笔一笔涂满（约 0.45 秒，读档恢复的直接显示涂满），同时完成几项时一个框涂完再涂下一个，最后一个子任务涂完才涂它的父任务；最后一个框涂完后等 1.2 秒再出下一条，独白播放时不出；`Level1HintFlowManager` 每帧按状态勾选，T1.5_match_photo、T1.7、T1.9 靠物体上的 UnityEvent 调 `CompleteTask` |
| 内心独白 | 已实现 | `InnerMonologueUIManager` 读 `Assets/Narrative/inner-monologue.json`，屏幕下方手写体字幕，每句每份存档只播一次；M1.1 到 M1.8 都已接 |
| Player's Panel 页签解锁 | 已实现 | 按教学顺序逐个开放，不该开的页签变淡点不开，见下面“Player's Panel 页签解锁”一节 |
| Player's Panel 翻页 | 已实现 | 日记、相册、手账三个页签纸面下方各有一条按键提示 `KeyHints`，样式同物品查看那条，A 配 Previous Page、D 配 Next Page，← → 也能翻（Gameplay 的 `PanelPreviousPage`、`PanelNextPage`）；提示行本身是按钮，能点，悬停照样缩放，翻到头的那一项整行变淡（`UIButtonGroupFade`），日记翻页动画进行中两项都不可用，键盘也不打断动画；键盘由 `PlayerPanelInputRouter` 交给 `PlayerPanelUIManager.TurnPage`，再转给当前页签，背包没有翻页，剪贴簿文本框输入时不翻。页码用日记的手写体写在纸上，格式 1 of 4，因为这个字体的 1 像斜杠；日记页码在右页右下角，避开书脊 |
| 背包 | 已实现 | 相册同款纸张底图，一行一件物品，点一行放大看模型：面板不收起，`InspectItemUIRoot` 自带 Canvas，Override Sorting 排序 25，画在面板、左侧页签栏和右上角任务提示（20）之上，内心独白（30）之下（页签栏和各页签按钮自带 Canvas，排序 0 到 1），GraphicRaycaster 挡住检视时点页签；其中 `BackpackView` 先用接近不透明的黑色把面板、页签栏、任务提示和场景一起压暗（HDRP 在线性空间混合 UI，透明度要给得比看上去高），再把 `Main Camera/BackpackViewCamera` 单独渲染的模型贴在上面；这台相机只拍 InspectItem 层，背景透明，近裁剪面 0.01 米，关掉雾、体积光、Custom Pass 和大部分后期，曝光和调色跟全局 Volume；只在背包查看时开启，贴图放下就释放；场景道具挂 `InspectableItem` 并填 `backpackItem` 即可按 F 拾取进背包，使用点挂 `ItemUsePoint` 按 F 使用；第一关门卫室和大门两个使用点由 `Level1HintFlowManager` 等 T1.7、T1.9 出现后才开放，提早用了提示就勾不上 |
| 场景物品查看 | 已实现 | 摩托车储物箱和工具使用点不必让准星正好压在碰撞体上，3 米内、偏开不超过 `InspectSystemManager` 的 Look Angle（15 度）且中间没挡着也会出 F 提示，几个同时满足时取离准星最近的；拾取和查看类物品只要在画面里、离得够近就算。被墙、玻璃或关着的门挡住的物品不出提示，也拿不到；查看时原物隐藏，副本从原位飞到镜头前，退出时只查看的物品飞回原位再显示，道具进背包不飞回。`InspectableItem` 填了 `backpackItem` 按 F（Interact）拾取，不填时只查看、按 E（Inspect），提示图标随之换成 F 或 E；场景里有 Bolt Cutter 和值班记录本两个。填了 readText 的物品查看时右侧显示这段文字，没有底色，靠柔影和场景分开。初始角度 inspectRotationOffset 相对相机，物品总以同一面朝向玩家。拿到眼前时会检查墙和家具，挡住或穿插就往镜头拉近，最近到相机近裁剪面 0.05 米，滚轮拉远也停在这个位置；滚轮能拉多近、推多远由每个物品自己定（`InspectableItem` 和 `BackpackItemDefinition` 的最近、最远检视距离），小物品推不远，带字的物品能拉到看清字，每格按当前距离的比例走，近处缩放更细；拉得很近时转动物品，边角可能被近裁剪面切掉；查看时不能走动，只在拿起时算一次，玩家自己转动物品后仍可能碰到旁边的东西。查看界面 `InspectItemUIRoot`：上方是名字和说明，暖白字加煤灰黑柔影；底部一排按键提示 `KeyHints`，鼠标右键配 Close、鼠标左键配 Rotate、滚轮配 Zoom，行样式复制自 `CameraKeyHintUIRoot`。只有鼠标右键（Cancel 动作）能退出，E 和 Esc 不再退出，放下那一帧也不会再拿起别的东西。退出时玩家能不能动按拿起前的状态恢复，从背包里查看时面板还开着，放下后照样不能走动和转视角。查看期间举相机（1）和开关 Player's Panel（Tab）两个输入暂时关掉，放下后恢复 |
| 回响系统 | 已实现 | 识别、黄框、发现记录、手账描边与文字、首次发现通知已实现，获得日记前整个系统不开启；站位圆圈和每次拍成的 onCaptured 已实现，Group Photo 重拍靠它推进主线；不统计收集进度 |
| 右下角拾取提示 | 已实现 | `PickupNoticeUI`，场景对象 `In-game UI Root/PickupNoticeUIRoot`，见下面“右下角拾取提示”一节 |
| 中下方使用提示 | 已实现 | `UseNoticeUI`，场景对象 `In-game UI Root/UseNoticeUIRoot`，排在 PlayerPanelUIRoot 前面；每个 `ItemUsePoint` 的 useNotice 填一句，使用成功时经静态事件 `ItemUsePoint.OnToolUsed` 显示，先于 onUsed，`InnerMonologueUIManager` 等它淡出才播下一句，它也等正在显示的独白播完才出。见下面“中下方使用提示”一节 |
| 右上角通知 | 已实现 | 只有 `New Echo Discovered`：ECHO 印章盖下后逐字打出，打完停 2 秒淡出；盖章音效还没做，做好后放进 `EchoPopupUI` 的 Stamp Sound |
| 存档与进度 | 部分 | 日记持有、背包、照片、回响发现、剪贴簿各自存档；当前关卡号、当前提示和已勾任务、已播独白、用过的工具使用点存在 `Progress/Progress.json`（`GameProgress`），使用点填 saveId 才存，读档时走 onRestored 直接呈现结果；Start New Game 清档、Continue 读档已接，门卫室的门和大门铁链都会存；本关完成由 `Level1HintFlowManager.LevelComplete` 按 `gate_chain` 是否用过判断；独立测试存档没有 |

### Player's Panel 页签解锁

四个页签按钮始终都在侧边栏里，从上到下按教学顺序排：日记、相册、剪贴簿、背包，第一关按提示的这个顺序逐个开放。不是当前该开的页签不能点开，并且要让玩家一眼看出来：

- 没开放的按钮变淡，悬停没有反馈；点了图标左右抖一下，不出文字提示。
- 下一步该开的页签图标轻轻脉动，点开后停止。
- 面板默认停在日记页签，之后记住上次看的页签。打开面板时如果停在没开放的页签上，直接切到该开的页签。
- 已开放的页签不会再锁回去。

| 当前提示 | 能打开的页签 | 引导的页签 |
| --- | --- | --- |
| 拿到日记前 | 面板打不开 | 无 |
| T1.1、T1.2、T1.3 | 日记 | 日记 |
| T1.4，合照加进剪贴簿前 | 日记、相册 | 相册 |
| T1.4，合照加进剪贴簿后 | 日记、相册、剪贴簿 | 剪贴簿 |
| T1.5 | 日记、相册、剪贴簿 | 无 |
| T1.6 | 全部 | 背包 |
| T1.7 及以后 | 全部 | 无 |

顺序由 `Level1HintFlowManager` 每帧按当前提示算出，交给 `PlayerPanelUIManager.SetTabAccess`；锁着时的点击由 `SidebarLockedClick` 接。

## 美术资源

按[美术资源列表](../../art-resources.md)第一关核对。

| 资源 | 状态 | 位置与现状 |
| --- | --- | --- |
| Group Photo 合照画面 | 已完成 | `Assets/Art Resources/Props/GroupPhoto/GroupPhoto_1980.png`，摩托车 `GrandpaDiaryPickup` 的 `groupPhoto` 引用它。背景是在大门正中往外 16 米处渲染的 1980 年大门（重拍机位另外手动校过，不在这里）（大门敞开并换上新漆、没有杂草和天线、宿舍楼干净材质、两根门柱间挂毛笔字红横幅），十二名新职工由 Codex 生图加入，只把人物和脚下阴影贴回原渲染，再由脚本取中间 80% 做成黑白旧照；已拿过旧占位图的存档要清档才会换图。制作说明见 `ArtSource/Props/GroupPhoto/CLAUDE.md` |
| Lock Pick 模型与 Backpack 缩略图 | 已完成 | `Assets/Art Resources/Props/LockPick/` |
| Bolt Cutter 模型与 Backpack 缩略图 | 已完成 | `Assets/Art Resources/Props/BoltCutter/`；预制体没有碰撞体，场景实例上加了 BoxCollider；钳口没有开合动画 |
| 泛光圆圈 | 已完成 | `Assets/Art Resources/Props/GroupPhoto/RetakeCircle.png` 和 `RetakeCircle.mat`，HDRP 贴花投在地面上的暖金色光环，半径和回响站位半径一致；实机亮度待确认 |
| 门卫室门锁 | 已完成 | 插芯锁和钥匙孔做在门卫室的门模型里，没有单独的锁物体 |
| 大门旁围墙墙画 | 已完成 | `Assets/Art Resources/Environment/WallMurals/` 的男检修工和女煤质化验员两张褪色墙画贴花，场景对象 `In-Scene Objects/工厂围墙/Gate Wall Murals`，都在从厂外看大门左边的围墙上、朝厂外的一面，高度一致：男检修工在左边那段长墙靠柱子处，女化验员在这根柱子和门柱之间的短墙上，都不居中；调低了不透明度、压成暖灰色调，让砖透出来；只是场景装饰，不是回响 |
| 右上角提示弹窗样式 | 已完成 | `HintPopupUIRoot`，没有底板，只有暖白文字和勾选框；弹窗贴屏幕右上，宽度收窄到 16:9 下正好落在 Player's Panel 右边的空白里，打开面板时不压日记；每行文字两端对齐，最后一行靠左；自带 Canvas，Override Sorting 排序 20，画在面板（最上层 ScrapbookFlyLayer 为 10）之上，背包检视（25，会把它一起压暗）和黑屏过场（50）之下；拍照时 `CameraSystemManager` 关掉 `In-game UI Root` 下所有开着的 Canvas，拍完恢复，带排序的子画布也不会进照片；行预制体 `Assets/Prefabs/HintTaskRow.prefab`，空框 `HintBox_Unfinished.png` 上叠一层铅笔涂抹 `HintBox_Scribble.png`，材质 `HintMarkScribble.mat` 按笔顺显示做涂满动画 |
| 值班日记本 | 已完成 | 用第三方皮面笔记本模型，查看时直接看模型，内容用 UI 文字显示，不需要单独的本子页面 |
| 按键图标 1、E、F、R、Tab、V | 已完成 | 交互提示、物品查看和手持相机按键提示直接用 `Assets/Art Resources/UI/Inspect Icons/Default/` 的 outline 图标；任务提示文字里的按键用 TMP 精灵资源 `Assets/Art Resources/UI/Hint Popup/HintKeyIcons.asset`，由 `Tools > Coal Echoes > Hint Popup > Build Key Icon Sprite Asset` 从 128 像素图标缩到同一高度（Tab 横向更宽），烘进暖白和煤灰黑柔影，在文字里和字号一样高；实机观感待确认 |
| 拾取提示的手 icon | 已完成 | `Assets/Art Resources/UI/Pickup Notice/PickupIcon_Hand.png`，一只手从上往下抓住一件实心小物件，画法照内心独白的云朵思考 icon；生成脚本见 `ArtSource/PickupNotice/CLAUDE.md` |
| 右下角拾取提示行样式 | 已完成 | 暖纸白 Imprima 加煤灰黑柔影，背后一条淡暗带，字号和 icon 比独白字幕小；效果图在 `ArtSource/PickupNotice/Previews/`，实机观感待确认 |

场景环境已完成：工厂大门（两扇门独立、带铁链挂锁和碰撞）、门卫室（扫描贴图墙面和地面、门窗、竖牌）、门卫室陈设（值班桌椅、摔坏的台灯、值班床、铁皮柜、木箱、旧相框）、围墙和短墙、门口枯草、摩托车（无 LOD）、三堆沙丘、宿舍楼前院沙堆、宿舍楼干净材质、内心独白字幕样式、报纸菜单。各项制作记录见 `ArtSource/` 对应目录的 CLAUDE.md。

## 待实现

按流程顺序。

1. **日记页码跳转**。四页日记已经补齐，可以做了。

### 提示界面

文字配色分两种：直接压在场景上的字（拾取提示、中下方使用提示、右上角任务提示、内心独白、物品查看的名字说明正文和底部按键提示、手持相机按键提示、F 交互提示）一律 Imprima 暖白字加煤灰黑柔影，材质 `Imprima 400 SDF - Pickup Underlay`，独白换手写体但配色相同；纸张底上的字（Player's Panel）用深棕色。任务提示的勾选框是白图，在 Image 上染成同样的暖白，加 UI Shadow 做柔影，和拾取提示的手 icon 一样；没有暗带。

| 提示类型 | 位置与行为 |
| --- | --- |
| 当前任务 | 右上角持续显示，随进度勾选和更新 |
| 获得物品 | 右下角，手 icon 加物品名，多条先进先出排成一列，见下面“右下角拾取提示” |
| 工具用成功 | 屏幕中下方一行文字，和独白字幕同一位置，先于同时触发的独白，见下面“中下方使用提示” |
| 首次发现回响 | 右上角盖印章、打出文字，停 2 秒后淡出，结束后恢复当前任务 |
| 按 F 拿取或使用，按 E 查看 | 跟随对应物品，写明这次交互的对象，图标显示对应按键 |
| 回响识别文字 | 取景画面顶部 |
| 内心独白 | 屏幕下方字幕，读法照过场文字：逐字打出，左键点击加速，打完后句末出现上下浮动的小箭头，再点才换下一句或收起，不会自动消失。从一句出现到最后一句点掉，玩家不能走动、转视角、拍照、开关面板和按 F/E，面板开着时点击也只推进独白。点完才出对应的任务提示，这一步的 F/E 交互点要等这条提示出现才出现。`MonologueUIRoot` 自带 Canvas，排序 30，画在背包检视和日记页之上、黑屏过场之下；点击走输入里单独的 `Monologue/Advance`（鼠标左键），全屏透明的 `ClickBlocker` 只在独白占住玩家时打开，挡住对面板的点击 |

### 右下角拾取提示

拿到物品时屏幕右下角出一行提示，左边是手拿起东西的 icon，右边是物品名。整列贴右下角，字号和 icon 比独白字幕小，和下方中间的独白字幕同时出现也不抢。

- **哪些物品出**：所有按 F 拾取的物品（第一关是 Bolt Cutter），以及摩托车储物箱给的 Grandpa's Diary、Group Photo、Lock Pick，每件一行。只有这次新拿到的才出；读档和 Continue 不出；旧存档已有日记、再到摩托车补拿时只出补拿到的那几件。
- **滑入**：新的一行从屏幕右边外滑进右下角最下面一格，同时淡入。
- **排布**：先进先出，最新的在最下面，已有的每来一条就平滑往上挪一格。
- **停留**：每行显示固定时间后淘汰。
- **淘汰**：最早进来的最先走，最上面一行淡出，下面的不动。同时最多 3 行，第 4 行进来时最上面那行立刻淘汰，不等时间到。
- **摩托车三样**：按 Grandpa's Diary、Group Photo、Lock Pick 的顺序，间隔很短依次滑入，每行都看得到滑入；排好后日记在最上，Lock Pick 在最下，也按这个顺序淘汰。
- 排在 Player's Panel 下面，打开面板时被盖住。
- 停留时间、滑入和上移时长、最多行数是 `PickupNoticeUI` 的 Inspector 参数，实机调。
- 背包物品接 `ItemBackpackManager.OnItemAdded`；摩托车三样由 `GrandpaDiaryPickup` 按上面的顺序推送，Lock Pick 最后进背包，靠 OnItemAdded 出，不会出两次。日记和合照的名字写在 `GrandpaDiaryPickup` 的 Inspector 里。

### 中下方使用提示

在使用点按 F 用背包工具成功时，屏幕中下方出一行文字，告诉玩家这次用成了什么。拾取物品照旧只出右下角拾取提示，这里不出。

- **哪些时候出**：`ItemUsePoint` 使用成功的那一刻。第一关两处，Lock Pick 撬开门卫室，Bolt Cutter 剪断大门铁链；第二关撬宿舍入口照样出。读档和 Continue 恢复的使用点走 onRestored，不出。
- **文字**：每个使用点一句，写这次使用的结果，不写按键。填在各使用点 `ItemUsePoint` 的 useNotice 上，汇总在[游戏文本](../../game-text.md)。
- **位置和样式**：和内心独白字幕同一位置，一行，Imprima 暖白字加煤灰黑柔影，和其他压在场景上的字一致；淡入，停留固定时间后淡出。
- **和独白的先后**：提示先出，淡出后再播同一刻触发的独白，第一关是剪链开门时的 M1.8。按 F 时如果已有独白在播，提示等这句播完再出。门照常在按 F 时开始转开，任务提示照常在按 F 时勾选，都不跟着等。
- 排在 Player's Panel 下面，打开面板时被盖住。
- 停留时间和淡入淡出时长是 `UseNoticeUI` 的 Inspector 参数，实机调。

## 待确认

1. Group Photo 重拍拍成后解锁什么。
2. 回响手账文字现在写在场景 Inspector 里，是否像日记和独白一样移到 JSON。
3. 外公日记第一页下面记着 Coal Two，含义待确认。

## 测试入口

- 测试开关集中在场景根物体 `Debug Tools` 的 `DebugTools` 组件上，正常游玩前保持默认（Show Main Menu 和 Play Opening 勾选，Unlocked Panel Tabs 为 Nothing）。
- Play 前取消勾选 `Debug Tools` 的 Show Main Menu，跳过菜单和开场直接测游戏功能。
- Play 前取消勾选 `Debug Tools` 的 Play Opening，保留菜单，点 Start New Game 后跳过转场、镜头和文字。
- 要直接测 Player's Panel 的内容，在 `Debug Tools` 的 Unlocked Panel Tabs 下拉里勾要开的页签，Play 后按 Tab 就能打开，不用先拿日记、也不按提示顺序；Play 中也能改。勾了日记就当作已拿到日记，只对面板生效，不写存档，回响系统和任务提示仍按真实进度。正常游玩前改回 Nothing。日记第一页只在 T1.3 显示时才算读过，提前翻页不会播 M1.4。
- `Tools > Coal Echoes > Diary > Preview Pickup Next Play`，下一次 Play 临时当作没拿过日记；同一次 F 拿到的 Lock Pick 和 Group Photo 仍写进真实存档。
- 要从头完整走一遍，在主菜单点 Start New Game，有存档时会原地清空全部存档再播开场。要保留现有存档，先把 `Application.persistentDataPath` 下的存档目录备份出去。
- 要接着上次的进度测，点 Continue。
- 重拍合照要站进泛光圆圈、按合照的角度对着大门拍；圆圈只在 T1.5 出现。要跳过前面直接测 T1.7、T1.9，退出 Play 后把存档 `Progress/Progress.json` 的 `hintIndex` 改成 5（T1.6），点 Continue，在背包页签点开 Lock Pick 完成 T1.6 后就出 T1.7；背包里要已经有 Lock Pick。
- 剪链开门后 `Progress/Progress.json` 的 `usedPoints` 里有 `gate_chain`，就是本关完成；Start New Game 清档后回到未完成。
- 独立测试存档和各节点起点（开场结束未拿日记、已有日记未拿断线钳、已拿断线钳在大门前、大门已开）未实现。
