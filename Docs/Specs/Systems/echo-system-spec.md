# 回响系统 Spec

回响（Echo）是场景里还留着外公那个年代痕迹的物品或场景。玩家举起相机对准它，取景框变黄时拍下并保存照片，就算发现这个回响，照片放进手账时带上对应的手账文字。本文写整个系统的组成、已有部分和待做部分；取景框和判定的细节见[回响取景提示 Spec](echo-targeting-spec.md)，每关有哪些回响见各关 Spec 和[设计文档](../../design.md)的“本关物品”表。

## 分类

| 类别 | 是否必需 | 拍到后 | 判定 |
| --- | --- | --- | --- |
| A 类回响 | 主线必经 | 推进关卡，也可以同时解锁叙事内容 | 对准一件物品；或按旧照片、日记重现一处场景，要站在指定位置（例如泛光圆圈）再按黄框对准 |
| B 类回响 | 可选 | 带出手账文字等补充叙事，不影响通关 | 对准一件物品 |

玩家界面里只叫 Echo，不显示 A、B 分类。开门、剪链用的流程道具进背包，不属于回响。

## 玩家流程

1. 在摩托车旁按 F 获得外公日记，回响系统开启。在此之前只能拍普通照片，之后也不补算之前的照片。
2. 举起相机取景，对准符合条件的回响时四角框变黄，取景画面顶部显示 `Undiscovered Echo` 或 `Discovered Echo`。
3. 按 V 切到全屏取景才能拍照，手持时快门没有反应。黄框时按快门，照片保存成功才算拍到；白框时拍照只是普通照片。
4. 首次发现时右上角盖下 ECHO 印章，旁边打出 `New Echo Discovered`；这张照片带上回响文字，在相册、手账顶栏和手账里都有略暖的相框和 ECHO 印章，放进手账后照片上贴着写有回响文字的手写便签。
5. A 类回响拍到后完成当前主线任务，出现下一条提示。

## 已实现

| 部分 | 现状 | 代码 |
| --- | --- | --- |
| 开启条件 | 未获得日记时不识别、不记录、不解锁、不提示 | `GrandpaDiaryProgress.HasDiary`，在 `EchoTargetDetector` 和 `CameraSystemManager` 里判断 |
| 回响数据 | 每个回响挂一个组件，存固定编号、可选最大距离、可选最小入镜大小、可选只认正面、可选站位点和半径及朝向角度、手账文字、便签在照片上的位置（哪一侧、沿边偏移、角度） | `EchoTrigger`、`EchoNoteLayout` |
| 取景判定 | 距离、站位（位置和朝向）、是否在框中央（有站位点时不看）、入镜大小、遮挡、照片正反面；多个目标取最靠近中心的一个；退出比进入略宽，防止闪烁 | `EchoTargetDetector` |
| 推进主线 | 每次保存回响照片都触发该回响的 onCaptured，不只首次发现；场景里在这里接提示勾选和独白 | `EchoTrigger`、`CameraSystemManager` |
| 取景界面 | 手持和全屏两套白黄四角框、回响文字和落下动效 | `EchoTargetingUI`、`EchoViewfinderGraphic` |
| 拍摄锁定 | 按快门那一刻锁定黄框目标，照片保存成功后才记录 | `CameraSystemManager` |
| 发现记录 | 按固定编号存到 `Echo/DiscoveredEchoes.json` | `EchoDiscoveryStorage` |
| 照片与文字 | 每张回响照片记下拍到的手账文字和便签位置，存到 `Echo/EchoPhotos.json`，重复拍也保留；旧记录没有便签位置时按默认的照片下方 | `EchoPhotoStorage` |
| 回响标记 | 照片预制体上的 `EchoStamp` 印章和相框变色，相册、手账顶栏、手账画布三处共用 | `EchoPhotoMark`、`Photo_Thumbnail_Prefab` |
| 手账便签 | 回响文字做成手写便签，是照片的子物体，按保存的便签位置摆放，一条文字一段，点便签等于点照片 | `ScrapBookCanvas` |
| 手账范围 | 顶栏拖拽的预览和放下后一致，回响照片带便签；整体超出手账页面就不放下；画布上拖动、旋转、缩放也按包括便签在内的整体范围限制 | `ScrapBookDraggable`、`ScrapBookElement` |
| 首次发现提示 | 右上角盖印章、打出文字，打完停 2 秒后淡出，重复拍摄不弹 | `EchoSystemManager`、`EchoPopupUI` |

## 场景里的回响

主场景目前配置了六个回响：第一关三个，大门是 A 类，门卫室里两个按 B 类处理；第二关一个，207 抽屉里的山路图是 A 类；第四关两个，从山头拉近拍的宿舍楼和广角带夜空的山脊夜景，都是 A 类。

| 物品 | 场景对象 | 编号 | 手账文字 | 便签位置 | 状态 |
| --- | --- | --- | --- | --- | --- |
| Group Photo 重拍 | `In-Scene Objects/工厂大门/FactoryGate` | `group_photo_retake` | 已填，正文在[游戏文本](../../game-text.md) | 照片下方偏右 | 站位点是 `Group Photo Retake View`，站在它 1.2 米内、相机朝向和它相差 10 度以内、离大门 22 米以内才识别，脚下是泛光圆圈 `Group Photo Retake Circle`；只在 T1.5 未拍成时开启，拍成勾 `T1.5_match_photo`、播 M1.5 |
| 值班日记本 | `In-Scene Objects/工厂大门/Guard Post/室内陈设/Logbook` | `fb2f4677570c4148a6914782a778a943` | 已填，正文在[游戏文本](../../game-text.md) | 照片下方偏右 | 可识别、可记录 |
| 街道照片 | `In-Scene Objects/工厂大门/Guard Post/室内陈设/OldPhotoFrame_StreetMarket` | `street_market_photo` | 已填，正文在[游戏文本](../../game-text.md) | 照片左侧 | 可识别、可记录，只认相框正面 |
| 山路图 | `In-Scene Objects/室内空间/207/Mountain Road Map` | `mountain_road_map` | 已填，正文在[游戏文本](../../game-text.md) | 照片下方 | 只在 T2.4 未拍成时开启，最大距离 1.6 米、最小入镜大小 0.3，要求对焦；拍成勾 T2.4 的三项子任务、播 M2.5；拍成的那张照片记下纸上每处字的位置，在相册里点开能悬停看英文，见[第二关 Spec](../Levels/level-two-spec.md)山路图 |
| 山脚宿舍楼 | `In-Scene Objects/工厂建筑群/01 建筑与构筑物/大型厂房/WorkersDormitory` | `ridge_dormitory` | 已填，正文在[游戏文本](../../game-text.md) | 默认 | 站位点是山头观景平台中央的 `In-Scene Objects/山顶栈道/Ridge Dormitory View`，半径 3.2 米盖住整个平台，相机朝向和它相差 2 度以内；最大距离 950 米、最小入镜大小 0.2（焦距约 220mm 以上），要求对焦（光圈 f/5.6、275mm 时对焦距离约 170 米以外）。站房基础挡在平台中间和西侧的视线上，只有靠东的栏杆边看得见。只在 T4.3 未拍成时开启，拍成勾 T4.3 的三项子任务、播 M4.3，见[第四关 Spec](../Levels/level-four-spec.md) |
| 山脊夜景 | `In-Scene Objects/工厂建筑群/01 建筑与构筑物/旗杆/Flagpole` | `ridge_night_view` | 已填，正文在[游戏文本](../../game-text.md) | 默认 | 宿舍楼已挂拉近那张，一个物体只能挂一个回响，所以挂在宿舍楼门前的旗杆上，旗杆和宿舍楼从平台看只差约 1 度。站位点是平台中央的 `In-Scene Objects/山顶栈道/Ridge Night View`，朝向是望旗杆的方向往上抬 12 度，半径 3.2 米、相差 10 度以内；焦距上限 28mm，最大距离 950 米，最小入镜大小 0.001。这样旗杆和宿舍楼一定在画面里，画面顶部至少高出地平线约 11 度，是夜空；直接低头对着旗杆不算。只在 T4.4 未拍成时开启，拍成勾 T4.4、播 M4.4 |

## 待确认

1. 值班日记本算主线步骤还是 B 类回响。
2. Group Photo 重拍成功后解锁什么，例如手账内容。
3. 回响文字现在写在场景 Inspector 里，是否和外公日记、内心独白一样移到 JSON，保持一份文字来源。
