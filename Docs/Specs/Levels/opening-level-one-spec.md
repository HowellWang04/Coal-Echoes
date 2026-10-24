# 开场与第一关 Spec

对照[设计文档](../../design.md)的“开场：时间流逝中的沙丘”和“关卡1”两节，按当前代码、场景和美术资源列出已实现和未实现的部分。设计以设计文档为准，提示和独白文字以 [json-hints.md](../../json-hints.md)、[json-inner-monologue.md](../../json-inner-monologue.md) 为准，回响规则见[回响系统 Spec](../Systems/echo-system-spec.md)和[回响取景提示 Spec](../Systems/echo-targeting-spec.md)。状态分三种：已实现、部分、未实现。

## 范围与完成条件

- 起点是主菜单点击 Start New Game，终点是剪断铁链、打开 Factory Gate，对应设计文档详细流程第 11 步。进厂后转入第二关。
- 街道照片和值班记录本是两个 B 类可选回响，不是完成条件。
- 玩家出生在大门外约 14 米的路边，站在摩托车旁面朝大门，已经下车，没有下车动画。
- 按键分工：F 是交互，拿 Lock Pick、拿 Bolt Cutter、撬门卫室的门、剪链开大门都按 F；E 是查看，值班记录本按 E 看内容，不能拿走，留在原处。值班记录本同时是回响物品，被拍到时照常走回响照片的逻辑。

## 开场

| 设计要求 | 状态 | 实现位置与现状 |
| --- | --- | --- |
| 双页报纸主菜单，1980 年 10 月 3 日的报纸，右页是从索道塔架俯拍厂区的实时照片 | 已实现 | `NewspaperMenuUI`、`NewspaperPhotoPass`，场景对象 `MainMenuUIRoot/MenuPresentation/Newspaper`，文案 `Assets/Narrative/newspaper-menu.json`；机位是 `Gameplay Root/MainMenuCamera`，图注写明从索道塔架上俯瞰，挪机位要同步改图注 |
| 点击开始后照片放大、纸墨消退，年份从 1980 翻到 2026 | 已实现 | `NewspaperMenuUI` 的 Zoom Delay、Zoom Duration、Reveal Duration，年份是 `YearTitle` |
| 三堆沙丘从地下升起，留在场景里挡住主路 | 已实现 | `FactorySandAccumulation`，沙丘在 `In-Scene Objects/沙丘`；碰撞和隐形挡墙始终在终点位置 |
| 天空快速掠过 | 已实现 | `OpeningSkyMotion`，只转 HDRI，没有体积云 |
| 开场文字期间镜头对着天空，从塔架机位换到厂门外；镜头降下时玩家才第一次看到摩托车和锁着的大门 | 已实现 | `OpeningSequenceUI` 在镜头抬到天空时把演出相机移到玩家相机位置。另有 `OpeningSceneState`（挂在 `MainMenuUIRoot`）在菜单期间打开两扇门、隐藏 `Chain_And_Padlock` 和摩托车，镜头换位的同一刻还原；塔架机位看不到大门，这段目前没有可见效果，保留给以后可能看得到大门的机位。Continue 和跳过开场在菜单关闭时还原，存档里铁链已剪断时还原成开门、不挂链；编辑器里保存的是关门挂链的姿态 |
| 宿舍楼由新变旧 | 已实现 | `DormitoryMaterialAging`，干净材质在 `Assets/Art Resources/Environment/WorkersDormitory/Clean/`；其他建筑没有干净版，设计只要求宿舍楼 |
| 镜头抬向天空，黑边收成 2.39:1，五段开场文字逐字显示，点击加速和换段，不能跳过 | 已实现 | `OpeningSequenceUI`，文字 `Assets/Narrative/opening-intro.json` |
| 镜头降到玩家视角，黑边收回 16:9，同时渐显 Chapter One / The Factory Gate，停一会儿再渐隐 | 已实现 | `OpeningUIRoot/ChapterTitle`，两行文字写在 Inspector 里 |
| 演出期间不响应移动、拍照、F 交互和面板 | 已实现 | `MainMenuUIManager` 锁定输入，结束后交出操作 |
| Continue 继续游戏，不重播开场 | 已实现 | `MainMenuUIManager.ContinueGame`，有存档时可点，没有存档置灰；直接交出操作，任务提示、已播独白、门卫室的门和剪链后的大门按进度存档恢复 |
| 新游戏初始化，清空照片、回响、日记、背包和剪贴簿 | 已实现 | Start New Game 有存档时调 `GameSaveManager.ClearSaves` 原地清档，不重载场景：删掉全部存档目录（含进度存档），清空静态缓存，再发 `SavesCleared` 事件，相册、剪贴簿、背包、独白、工具使用点、门卫室的门、大门和可拾取道具各自回到新游戏状态，然后直接播开场 |

## 第一关流程

步骤号对应设计文档的详细流程表。

| 步骤 | 设计要求 | 状态 | 实现位置与现状 |
| --- | --- | --- | --- |
| 1 | 报纸转场和开场文字 | 已实现 | 见上表 |
| 2 | 镜头到玩家视角，播 M1 | 已实现 | `Level1HintFlowManager` 在主菜单关闭后播 M1，再开始提示 T1 |
| 3 | 从摩托车储物箱拿到 Grandpa's Diary、Group Photo、Lock Pick，T1 勾选，播 M2，Player's Panel 可以打开 | 已实现 | 摩托车上的 `GrandpaDiaryPickup`，一次 F 三样到手，F 提示 `Get Grandpa's things`，拿到后右下角依次出三行拾取提示；Lock Pick 不摆实物，直接进背包；Group Photo 见美术资源 |
| 4 | Tab 打开 Player's Panel，看到四个页签，T2 | 已实现 | `PlayerPanelUIManager`；页签按教学顺序逐个开放，见系统现状 |
| 5 | 读日记第一页，把 Group Photo 加进 Scrapbook 并摆放，T3，播 M3；可看日记全部页面，支持输入页码跳转 | 部分 | T3 三个勾选和 M3 已接，日记目前只有一页；页码跳转未实现 |
| 6 | 关面板、举相机、走进泛光圆圈重拍 Group Photo，T4，播 M4 | 已实现 | 站位机位是 `工厂大门/Group Photo Retake View`，按用户在 Scene 视图里对着合照校好的位置和朝向摆放，离大门约 10 米；泛光圆圈 `工厂大门/Group Photo Retake Circle` 是 HDRP 贴花，在它脚下。`FactoryGate` 挂 `EchoTrigger`（`group_photo_retake`），站在机位 1.2 米内、相机朝向和机位相差 10 度以内才变黄框，不再要求大门在框中央；拍成后它的 onCaptured 勾 `T4_match_photo`、播 M4。圆圈和这个回响由 `Level1HintFlowManager` 只在 T4 显示且未拍成时打开，拍成即消失，读档按提示进度恢复，不另存 |
| 7 | 在 Backpack 页签查看 Lock Pick，T5 | 已实现 | 切到 Backpack 页签即勾选，设计里完成条件标待定；点物品可放大看模型，初始角度没调 |
| 8 | 用 Lock Pick 撬开门卫室，T6、T7 | 已实现 | 门铰链 `Guard Post/Guardhouse_Rebuilt/Door_Hinge_Game` 上挂 `ItemUsePoint` 和 `SwingDoor`，F 提示在 `LockPromptAnchor`，门向外开 100 度；没带 Lock Pick 或 T6 出现前不显示提示，也撬不开；开门状态存档，读档时门直接是开的 |
| 9 | 进门卫室，发现值班记录本和 Bolt Cutter | 已实现 | 值班记录本在值班桌上，街道照片挂在后墙；桌面被记录本和台灯占满，Bolt Cutter `Guard Post/室内陈设/BoltCutter` 钳头着地，斜靠在观察窗下的侧墙上 |
| 10 | 按 E 查看值班记录本，播 M5 | 已实现 | 照生化危机的方式，不打开单独页面：记录本 `Logbook` 挂 BoxCollider 和只查看的 `InspectableItem`，走近出 E 图标，按 E 后本子从桌上飞到镜头前，封面朝玩家，可旋转缩放；屏幕上方显示 Guard Post Duty Log，右侧 `InspectItemUIRoot/ReadTextPanel` 显示本子内容（`InspectableItem` 的 readText），不加底色，暖白字带煤灰黑柔影，和右下角拾取提示同一材质。再按 E、Esc 或右键放回桌上，这时 onInspectEnd 播 M5，独白每份存档只播一次。本步不勾任务提示，完成条件见待确认 |
| 11 | 拿起 Bolt Cutter，剪断铁链，打开 Factory Gate，T8，播 M6、M7，第一关结束 | 部分 | Bolt Cutter 挂 `InspectableItem` 填 `bolt_cutter`，按 F 拾取进背包，右下角出拾取提示，`Level1HintFlowManager` 见到背包里有它就播 M6；铁链 `FactoryGate/Chain_And_Padlock` 挂 BoxCollider 和 `ItemUsePoint`，F 提示在 `CutPromptAnchor`，没带 Bolt Cutter 或 T8 出现前不显示，也剪不了；用后两扇门扇上的 `SwingDoor` 向厂内转开 90 度，勾 T8，播 M7，最后隐藏锁链挂锁。Bolt Cutter 用完留在背包里。铁链使用点 saveId 为 `gate_chain`，读档时两扇门直接是开的、锁链挂锁隐藏；本关完成没有单独记录，T8 勾完后提示进度存为全部完成 |

## 本关物品

| 物品 | 设计 | 模型与资源 | 场景与交互 | 文字 |
| --- | --- | --- | --- | --- |
| Lock Pick | 流程道具，储物箱拿到 | 预制体、图标、背包资源 `lock_pick` 都有 | 摩托车 F 进背包，门卫室使用点已接 | 背包用途说明已写 |
| Group Photo 重拍 | A 类回响，大门前泛光圆圈 | 泛光圆圈贴花和正式 Group Photo 已做 | Factory Gate 回响和站位圆圈已接，拍成勾 T4、播 M4 | M4 已写；手账文字暂用 M4 原句，待定 |
| Bolt Cutter | 流程道具，门卫室拿到 | 预制体、图标、背包资源 `bolt_cutter` 都有，已在背包 catalog 里；钳口不能开合 | 门卫室 F 拾取进背包，大门铁链使用点已接 | 背包用途说明、M6 和剪链的 F 提示已写 |
| 值班记录本 | B 类回响，按 E 查看内容，不能拿走 | 第三方皮面笔记本模型 | 场景对象 `Guard Post/室内陈设/Logbook`，回响编号 `fb2f4677570c4148a6914782a778a943`，可识别可记录；按 E 拿到眼前查看，旁边显示本子内容，放回时播 M5；轴心在本子中心 | 本子内容和手账文字在[游戏文本](../../game-text.md)里，都已填进场景，需按当前故事复核 |
| 街道照片 | B 类回响 | 旧木相框加街市照片 | 场景对象 `Guard Post/室内陈设/OldPhotoFrame_StreetMarket`，回响编号 `street_market_photo`，只认正面，可识别可记录 | 手账文字空着，待定 |

## 系统现状

| 系统 | 状态 | 说明 |
| --- | --- | --- |
| 右上角任务提示 | 已实现 | `HintPopupUIManager` 读 `Assets/Narrative/level1-hints.json`，按顺序显示，子任务打勾，全部完成后等 1.2 秒再出下一条，独白播放时不出；`Level1HintFlowManager` 每帧按状态勾选，T4_match_photo、T6、T8 靠物体上的 UnityEvent 调 `CompleteTask` |
| 内心独白 | 已实现 | `InnerMonologueUIManager` 读 `Assets/Narrative/inner-monologue.json`，屏幕下方手写体字幕，每句每份存档只播一次；M1 到 M7 都已接 |
| Player's Panel 页签解锁 | 已实现 | 按教学顺序逐个开放，不该开的页签变淡点不开，见下面“Player's Panel 页签解锁”一节 |
| 背包 | 已实现 | 相册同款纸张底图，一行一件物品，点一行放大看模型；场景道具挂 `InspectableItem` 并填 `backpackItem` 即可按 F 拾取进背包，使用点挂 `ItemUsePoint` 按 F 使用；第一关门卫室和大门两个使用点由 `Level1HintFlowManager` 等 T6、T8 出现后才开放，提早用了提示就勾不上 |
| 场景物品查看 | 已实现 | 被墙、玻璃或关着的门挡住的物品不出提示，也拿不到；查看时原物隐藏，副本从原位飞到镜头前，退出时只查看的物品飞回原位再显示，道具进背包不飞回。`InspectableItem` 填了 `backpackItem` 按 F（Interact）拾取，不填时只查看、按 E（Inspect），提示图标随之换成 F 或 E；场景里有 Bolt Cutter 和值班记录本两个。填了 readText 的物品查看时右侧显示这段文字，没有底色，靠柔影和场景分开。初始角度 inspectRotationOffset 相对相机，物品总以同一面朝向玩家。拿到眼前时会检查墙和家具，挡住或穿插就往镜头拉近，最近到相机近裁剪面 0.3 米，滚轮拉远也停在这个位置；查看时不能走动，只在拿起时算一次，玩家自己转动物品后仍可能碰到旁边的东西。查看界面 `InspectItemUIRoot`：上方是名字和说明，暖白字加煤灰黑柔影；底部一排按键提示 `KeyHints`，E 和 Esc 图标配 Close、鼠标左键配 Rotate、滚轮配 Zoom，行样式复制自 `CameraKeyHintUIRoot`。E、Esc、右键都能退出，拿起那一帧的 E 不算，放下那一帧也不会再拿起别的东西。查看期间举相机（1）和开关 Player's Panel（Tab）两个输入暂时关掉，放下后恢复 |
| 回响系统 | 部分 | 识别、黄框、发现记录、手账描边与文字、首次发现通知已实现，获得日记前整个系统不开启；站位圆圈和每次拍成的 onCaptured 已实现，Group Photo 重拍靠它推进主线；类别和关卡字段未实现 |
| 回响收集进度和探索记录页签 | 未实现 | 见待实现细则 |
| 右下角拾取提示 | 已实现 | `PickupNoticeUI`，场景对象 `In-game UI Root/PickupNoticeUIRoot`，见下面“右下角拾取提示”一节 |
| 右上角通知 | 已实现 | 只有 `New Echo Discovered`，显示 2 秒 |
| 存档与进度 | 部分 | 日记持有、背包、照片、回响发现、剪贴簿各自存档；当前提示和已勾任务、已播独白、用过的工具使用点存在 `Progress/Progress.json`（`GameProgress`），使用点填 saveId 才存，读档时走 onRestored 直接呈现结果；Start New Game 清档、Continue 读档已接，门卫室的门和大门铁链都会存；本关完成还没存，独立测试存档没有 |

### Player's Panel 页签解锁

四个页签按钮始终都在侧边栏里，第一关按提示的教学顺序逐个开放。不是当前该开的页签不能点开，并且要让玩家一眼看出来：

- 没开放的按钮变淡，悬停没有反馈；点了图标左右抖一下，不出文字提示。
- 下一步该开的页签图标轻轻脉动，点开后停止。
- 打开面板时如果停在没开放的页签上，直接切到该开的页签。
- 已开放的页签不会再锁回去。

| 当前提示 | 能打开的页签 | 引导的页签 |
| --- | --- | --- |
| 拿到日记前 | 面板打不开 | 无 |
| T1、T2 | 日记 | 日记 |
| T3，读完日记第一页前 | 日记 | 日记 |
| T3，读完第一页后 | 日记、相册 | 相册 |
| T3，合照加进剪贴簿后 | 日记、相册、剪贴簿 | 剪贴簿 |
| T4 | 日记、相册、剪贴簿 | 无 |
| T5 | 全部 | 背包 |
| T6 及以后 | 全部 | 无 |

顺序由 `Level1HintFlowManager` 每帧按当前提示算出，交给 `PlayerPanelUIManager.SetTabAccess`；锁着时的点击由 `SidebarLockedClick` 接。

## 美术资源

按设计文档第一关美术资源列表核对。

| 资源 | 状态 | 位置与现状 |
| --- | --- | --- |
| Group Photo 合照画面 | 已完成 | `Assets/Art Resources/Props/GroupPhoto/GroupPhoto_1980.png`，摩托车 `GrandpaDiaryPickup` 的 `groupPhoto` 引用它。背景是在大门正中往外 16 米处渲染的 1980 年大门（重拍机位另外手动校过，不在这里）（大门敞开并换上新漆、没有杂草和天线、宿舍楼干净材质、两根门柱间挂毛笔字红横幅），十二名新职工由 Codex 生图加入，只把人物和脚下阴影贴回原渲染，再由脚本取中间 80% 做成黑白旧照；已拿过旧占位图的存档要清档才会换图。制作说明见 `ArtSource/Props/GroupPhoto/CLAUDE.md` |
| Lock Pick 模型与 Backpack 缩略图 | 已完成 | `Assets/Art Resources/Props/LockPick/` |
| Bolt Cutter 模型与 Backpack 缩略图 | 已完成 | `Assets/Art Resources/Props/BoltCutter/`；预制体没有碰撞体，场景实例上加了 BoxCollider；钳口没有开合动画 |
| 泛光圆圈 | 已完成 | `Assets/Art Resources/Props/GroupPhoto/RetakeCircle.png` 和 `RetakeCircle.mat`，HDRP 贴花投在地面上的暖金色光环，半径和回响站位半径一致；实机亮度待确认 |
| 门卫室门锁 | 已完成 | 插芯锁和钥匙孔做在门卫室的门模型里，没有单独的锁物体 |
| 右上角提示弹窗样式 | 已完成 | `HintPopupUIRoot`，行预制体 `Assets/Prefabs/HintTaskRow.prefab` |
| 值班日记本 | 已完成 | 用第三方皮面笔记本模型，查看时直接看模型，内容用 UI 文字显示，不需要单独的本子页面 |
| 按键图标 1、E、F、Tab | 部分 | 图标在 `Assets/Art Resources/UI/Inspect Icons/Default/`；任务提示文字里现在用引号写按键名，没有换成图标 |
| 拾取提示的手 icon | 已完成 | `Assets/Art Resources/UI/Pickup Notice/PickupIcon_Hand.png`，一只手从上往下抓住一件实心小物件，画法照内心独白的云朵思考 icon；生成脚本见 `ArtSource/PickupNotice/CLAUDE.md` |
| 右下角拾取提示行样式 | 已完成 | 暖纸白 Imprima 加煤灰黑柔影，背后一条淡暗带，字号和 icon 比独白字幕小；效果图在 `ArtSource/PickupNotice/Previews/`，实机观感待确认 |

场景环境已完成：工厂大门（两扇门独立、带铁链挂锁和碰撞）、门卫室（扫描贴图墙面和地面、门窗、竖牌）、门卫室陈设（值班桌椅、摔坏的台灯、值班床、铁皮柜、木箱、旧相框）、围墙和短墙、门口枯草、摩托车（无 LOD）、三堆沙丘、宿舍楼干净材质、内心独白字幕样式、报纸菜单。各项制作记录见 `ArtSource/` 对应目录的 CLAUDE.md。

## 待实现

按流程顺序。

1. **日记页码跳转**。
2. **探索记录页签和收集进度**。`EchoTrigger` 还要加 A、B 类别和所属关卡。
3. **剩余进度存档**。记录本关完成。
4. **任务提示里的按键图标**。

### Player Panel 探索记录页签

按关卡显示本关目标和完成情况，一个 `n/x` 的小型收集进度（n 为已发现数，x 为本关 B 类回响总数，第一关为 2），已发现回响以卡片分页展示并关联手账文字，未发现的只体现在总数里。主线完成和回响收集分开记录。分页外观和交互复用相册页签 `AlbumTab` 的网格、上一页、下一页和页码，收集进度和翻页页码分开显示。重复拍摄不增加条目。后续关卡何时显示、点卡片是在页签内读文字还是跳转手账，待确认。

### 提示界面

文字配色分两种：直接压在场景上的字（拾取提示、内心独白、物品查看的名字说明正文和底部按键提示、手持相机按键提示、F 交互提示）一律 Imprima 暖白字加煤灰黑柔影，材质 `Imprima 400 SDF - Pickup Underlay`，独白换手写体但配色相同；纸张底上的字（右上角任务提示、Player's Panel）用深棕色。

| 提示类型 | 位置与行为 |
| --- | --- |
| 当前任务 | 右上角持续显示，随进度勾选和更新 |
| 获得物品 | 右下角，手 icon 加物品名，多条先进先出排成一列，见下面“右下角拾取提示” |
| 首次发现回响 | 右上角临时显示 2 秒，结束后恢复当前任务 |
| 按 F 拿取或使用，按 E 查看 | 跟随对应物品，写明这次交互的对象，图标显示对应按键 |
| 回响识别文字 | 取景画面顶部 |
| 内心独白 | 屏幕下方字幕，播放期间不出新任务提示 |

### 右下角拾取提示

拿到物品时屏幕右下角出一行提示，左边是手拿起东西的 icon，右边是物品名。整列贴右下角，字号和 icon 比独白字幕小，和下方中间的独白字幕同时出现也不抢。

- **哪些物品出**：所有按 F 拾取的物品（第一关是 Bolt Cutter），以及摩托车储物箱给的 Grandpa's Diary、Group Photo、Lock Pick，每件一行。只有这次新拿到的才出；读档和 Continue 不出；旧存档已有日记、再到摩托车补拿时只出补拿到的那几件。
- **滑入**：新的一行从屏幕右边外滑进右下角最下面一格，同时淡入。
- **排布**：先进先出，最新的在最下面，已有的每来一条就平滑往上挪一格。
- **停留**：每行显示固定时间后淘汰。
- **淘汰**：最早进来的最先走，最上面一行淡出，下面的不动。同时最多 3 行，第 4 行进来时最上面那行立刻淘汰，不等时间到。
- **摩托车三样**：按 Grandpa's Diary、Group Photo、Lock Pick 的顺序，间隔很短依次滑入，每行都看得到滑入；排好后日记在最上，Lock Pick 在最下，也按这个顺序淘汰。
- 和右上角任务提示一样排在 Player's Panel 下面，打开面板时被盖住。
- 停留时间、滑入和上移时长、最多行数是 `PickupNoticeUI` 的 Inspector 参数，实机调。
- 背包物品接 `ItemBackpackManager.OnItemAdded`；摩托车三样由 `GrandpaDiaryPickup` 按上面的顺序推送，Lock Pick 最后进背包，靠 OnItemAdded 出，不会出两次。日记和合照的名字写在 `GrandpaDiaryPickup` 的 Inspector 里。

## 待确认

1. T5 和第 10 步的完成条件，设计里标待定。
2. Group Photo 重拍是否计入本关 `n/x`，拍成后解锁什么。
3. 街道照片的手账文字，值班记录本的旧文字是否按当前故事改。
4. 回响手账文字现在写在场景 Inspector 里，是否像日记和独白一样移到 JSON。
5. 本关情绪基调，设计里标待定。

## 测试入口

- Play 前关掉 `MainMenuUIRoot` 的 Show On Start，跳过菜单和开场直接测游戏功能。
- Play 前取消勾选场景根物体 `Opening Scene Toggle`，保留菜单，点 Start New Game 后跳过转场、镜头和文字。
- `Tools > Coal Echoes > Diary > Preview Pickup Next Play`，下一次 Play 临时当作没拿过日记；同一次 F 拿到的 Lock Pick 和 Group Photo 仍写进真实存档。
- 要从头完整走一遍，在主菜单点 Start New Game，有存档时会原地清空全部存档再播开场。要保留现有存档，先把 `Application.persistentDataPath` 下的存档目录备份出去。
- 要接着上次的进度测，点 Continue。
- 重拍合照要站进泛光圆圈、按合照的角度对着大门拍；圆圈只在 T4 出现。要跳过前面直接测 T6、T8，退出 Play 后把存档 `Progress/Progress.json` 的 `hintIndex` 改成 4（T5），点 Continue，切到背包页签完成 T5 后就出 T6；背包里要已经有 Lock Pick。
- 独立测试存档和各节点起点（开场结束未拿日记、已有日记未拿断线钳、已拿断线钳在大门前、大门已开）未实现，等进度存档一起做。
