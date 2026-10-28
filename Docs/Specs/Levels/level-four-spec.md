# 第四关 Spec

对照[设计文档](../../design.md)的“关卡4：索道终点站”一节和[剧本](../../script.md)的第四关、结尾。玩家在月夜站上上站站台，沿站房背后的木栈道走上山头观景平台，读日记最后一页，拉近拍山脚的宿舍楼，再拉回广角把厂区和星空拍进一张，放进手账，合上面板读结尾文字，读完淡回山头，长按 Esc 可以回到报纸主菜单。流程由 `Level4HintFlowManager` 串起来，已实现，Play Mode 待用户测。月夜见[月光 Spec](../Systems/moonlight-spec.md)，栈道和平台见 `ArtSource/SummitBoardwalk/CLAUDE.md`。站房东北角下面有封死的矿洞口和洞口前的平台，只看不走，不挂回响也没有文字，见 `ArtSource/MinePortal/CLAUDE.md`。

## 范围

- 从站上上站站台、写出第四章标题开始，到读完结尾文字、淡回山头为止，设计见设计文档关卡4第 1 到 8 步
- 到站、切月夜和写出第四章标题是第三关结尾 `CablewayRideManager` 做的，见[第三关 Spec](level-three-spec.md)

## 玩家流程

| 步骤 | 玩家经历 | 提示和独白 | 怎么算完成 |
| --- | --- | --- | --- |
| 1 | 站在上站站台上，第四章标题写完 | 不播独白，标题消失后出 T4.1 | |
| 2 | 绕到站房背后，走上木栈道 | T4.1 | 走进平桥起头的 `Boardwalk Zone`，勾 `T4.1_find_boardwalk` |
| 3 | 沿栈道走上山头观景平台 | T4.1，M4.1 | 走进 `Summit Lookout Zone`，两条子任务一起勾，播 M4.1，记 `summit_lookout` |
| 4 | 打开面板读日记最后一页 | T4.2，M4.2 | 和 T1.3 一样，第四页开着满 8 秒，或者看过一眼再翻页、合上面板 |
| 5 | 在平台靠东的栏杆边拉近、对焦，拍宿舍楼 | T4.3，M4.3 | 宿舍楼的回响拍成；框够大时勾拉近，对上焦时勾对焦，拍成时三条一起勾 |
| 6 | 拉回广角，带上夜空再拍一张 | T4.4，M4.4 | 山脊夜景的回响拍成 |
| 7 | 把照片加进手账并摆好 | T4.5 | 和第一关 T1.4 一样，手账照片变多勾添加，放下一张照片勾摆放，不认是哪一张 |
| 8 | 合上面板，读结尾文字 | | 提示全部走完、面板合上时播 `ending`，读完记 `ending_read`，淡回山头 |
| 9 | 在山头自由待着，想走时长按 Esc | 右下角 Hold for Main Menu | 按满后黑屏、重载场景，回到报纸主菜单 |

## 场景对象

| 对象 | 位置 | 作用 |
| --- | --- | --- |
| `Level4HintFlowManager` | `Gameplay Root/Managers` | 本关流程，引用和第二、三关的流程管理器同一套 UI 和玩家 |
| `Boardwalk Zone` | `In-Scene Objects/山顶栈道/SummitBoardwalk` 下 | 触发盒，盖住平桥离开站房基础后沿 1 到 5 米那一段，站在基础顶面上不算 |
| `Summit Lookout Zone` | 同上 | 触发盒，盖住观景平台，最上一级台阶不算 |
| `Summit Lookout Spawn` | `In-Scene Objects/山顶栈道` 下 | 平台中央，面朝宿舍楼，读档用 |
| `Summit Platform Spawn` | `CablewaySummitStation` 下 | 第三关落到站台的点，没到过平台时读档用 |
| `Ridge Dormitory View`、`Ridge Night View` | `In-Scene Objects/山顶栈道` 下 | 两个回响的站位点，都在平台中央 |
| 宿舍楼回响 `ridge_dormitory` | `WorkersDormitory` 上 | 拉近那张 |
| 山脊夜景回响 `ridge_night_view` | 宿舍楼门前的 `Flagpole` 上 | 广角那张 |
| `MainMenuPromptUIRoot` | `In-game UI Root` 下，紧跟 `SkipPromptUIRoot` | 复制自跳过提示，换成 Esc 图标和 Hold for Main Menu；挂 `MainMenuReturnUI`，子物体 `Fade` 是单独一层 Canvas 的全屏黑图，排序 60，在过场（50）之上、主菜单（100）之下 |

两个触发盒挂在栈道根节点下，跟着栈道的朝向；栈道重建（`PlaceBoardwalk.cs`）会删掉根节点，要先挪出来再放回去。

## 流程

- 开始：`CablewayRideManager` 落到站台时记 `summit_arrival`、开始第四关、写出标题；流程管理器等标题消失后开始出提示。
- 回响开关：宿舍楼的回响只在当前提示是 T4.3、还没拍成时开，山脊夜景只在 T4.4、还没拍成时开，照第二关山路图的写法每帧设。
- 拍成：两个回响的 onCaptured 里填了勾任务和播独白，和山路图一样；宿舍楼勾 T4.3 的三条子任务、播 M4.3，山脊夜景勾 `T4.4_photo_night_view`、播 M4.4。T4.3 的拉近和对焦另外按 `EchoTargetDetector` 的 `OutOfFocusTarget`、`CurrentTarget` 每帧勾，和 T2.4 一样。
- 手账：订阅 `ScrapBookDataManager.OnPhotosChanged` 和 `ScrapBookDraggable.OnPhotoPlaced`，只勾 T4.5 的两条，不在 T4.5 时调用会被提示弹窗忽略。
- 结尾：提示全部走完（`HintPopupUIManager.Finished`）、面板合上、没在播过场、没读过结尾时播 `ending`，`onFinished` 里记 `ending_read`。过场读法和其他过场一样，读完自己淡入，玩家还在原地，之后没有提示。

## 回主菜单

- 输入加了 Gameplay 的 `MainMenu`（Esc），`PlayerControls.cs` 已按 inputactions 重新生成。
- `Level4HintFlowManager` 每帧设 `MainMenuReturnUI.Available`：读过结尾、没在播过场、面板合上、玩家能走动、相机没举着也没在切换时才出。和跳过提示一样，相机按键提示也在右下角，所以举着相机时不出。
- `MainMenuReturnUI` 复用 `SkipPromptUI` 的显示：按住 Esc 时线从左往右填满，按满 1.2 秒后关掉 Gameplay 输入、0.6 秒淡到全黑，再 `SceneManager.LoadScene` 重载当前场景。主菜单本来就在场景启动时出来，所以重载就回到了报纸主菜单，各系统在 Awake、Start 里重新读存档；Continue 按读档规则回到山头，仍然可以再回主菜单。
- 重载能成立靠两点：`Assets/Scripts` 里没有 `DontDestroyOnLoad` 的对象；订阅静态事件（`SavesCleared`、`OnToolUsed`、`OnPhotoPlaced`、`OnItemInspected`）的组件都在 OnDisable 或 OnDestroy 里退订。以后加静态事件也要这样。

## 两张照片的判定

参数见[回响系统 Spec](../Systems/echo-system-spec.md)的场景里的回响，判定规则见[回响取景提示 Spec](../Systems/echo-targeting-spec.md)。

- 站房基础挡在平台和厂区之间，平台中间和西侧看不见宿舍楼，只有靠东的栏杆边看得见；遮挡照常检查，所以玩家要自己挪到看得见的地方。旗杆比宿舍楼好看见，平台东半边大都行。
- 宿舍楼前沙丘的隐形 `Blocker` 在 Ignore Raycast 层，`EchoTargetDetector` 的遮挡层已去掉这一层，不然从山头永远算被挡。
- 拉近那张：最小入镜大小 0.2，宿舍楼约 900 米远，要拉到约 220mm 以上；要求对焦，默认对焦 10 米是糊的，要推到一两百米以外。
- 广角那张：宿舍楼已经挂了拉近那张的回响，一个物体只能挂一个，所以挂在门前的旗杆上。回响加了焦距上限 `maxFocalLength`，填 28mm；站位点朝向是望旗杆的方向往上抬 12 度，允许相差 10 度。28mm 时画面上下各约 23 度，所以旗杆和宿舍楼一定在画面里，画面顶部至少高出地平线约 11 度；直接低头对着旗杆差 12 度，不算。最小入镜大小 0.001，最广的 7mm 也能过。

## 读档

- 第四关读档时不出标题。到过平台（`summit_lookout`）放到 `Summit Lookout Spawn`，否则放到 `Summit Platform Spawn`；月夜由 `MoonlightController` 按 `summit_arrival` 恢复。
- 读过结尾的存档提示已走完，不再播结尾，玩家站在平台上。读结尾中途退出的，读档后面板是合上的，会直接再播一遍。
- 新游戏只在开局主菜单里清档，流程管理器没有要复位的存档状态；手账照片数跟着手账的事件更新。

## 待确认

1. 平台上只有靠东一侧看得见宿舍楼，玩家会不会找不到位置；Play Mode 里觉得难找，可以挪平台或在提示里点一句。
2. 月夜下拉近的画面很暗，宿舍楼只剩轮廓；要不要靠曝光滑块，或调月光和雾。
3. 月夜下从平桥和观景平台往下看，认不认得出站房下面的矿洞口和矿车。

## 验收

1. 坐吊斗到站，标题写完后出 T4.1，不播独白；走上平桥勾第一条，走上平台勾第二条、播 M4.1。
2. 出 T4.2 后翻到日记第四页，开满 8 秒勾掉、播 M4.2；翻到别的页不算。
3. 出 T4.3 后在平台中间拉近，宿舍楼被站房挡住，框不变黄；挪到靠东的栏杆边，拉到 220mm 以上出 Out of focus、勾拉近；把对焦推远变黄、勾对焦；按快门勾第三条、盖 ECHO 章、播 M4.3，照片带手账文字。
4. 出 T4.4 后拉回广角、镜头略抬起对着宿舍楼一带，框变黄，拍下勾掉、播 M4.4；35mm 以上或低头对着宿舍楼不变黄。
5. T4.3 之前和 T4.4 之后对着宿舍楼或旗杆，框都不变黄。
6. 把照片加进手账、摆好，勾掉 T4.5；合上面板播结尾文字，读完淡回山头，没有提示，可以自由走动拍照。
7. 在第四关各步退出再 Continue：没到平台的回到站台，到过的回到平台，月夜还在，提示接着当时的那条；读完结尾的不再播结尾。
8. 读完结尾淡回山头后右下角出 Hold for Main Menu，举起相机或打开面板时收起；按住 Esc 线填满，松开清零；按满后黑屏，回到报纸主菜单，控制台没有报错。主菜单里 Continue 回到山头，提示还在；Start New Game 从头开始。
