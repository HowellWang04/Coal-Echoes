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
3. 黄框时按快门，照片保存成功才算拍到；白框时拍照只是普通照片。
4. 首次发现时右上角显示 `New Echo Discovered`；这张照片带上回响文字，在相册、手账顶栏和手账里都有略暖的相框和 ECHO 印章，放进手账后照片上贴着写有回响文字的手写便签。
5. A 类回响拍到后完成当前主线任务，出现下一条提示。
6. Player's Panel 的探索记录页签按关卡显示收集进度和已发现的回响。（待实现）

## 已实现

| 部分 | 现状 | 代码 |
| --- | --- | --- |
| 开启条件 | 未获得日记时不识别、不记录、不解锁、不提示 | `GrandpaDiaryProgress.HasDiary`，在 `EchoTargetDetector` 和 `CameraSystemManager` 里判断 |
| 回响数据 | 每个回响挂一个组件，存固定编号、可选最大距离、可选只认正面、可选站位点和半径及朝向角度、手账文字、便签在照片上的位置（哪一侧、沿边偏移、角度） | `EchoTrigger`、`EchoNoteLayout` |
| 取景判定 | 距离、站位（位置和朝向）、是否在框中央（有站位点时不看）、入镜大小、遮挡、照片正反面；多个目标取最靠近中心的一个；退出比进入略宽，防止闪烁 | `EchoTargetDetector` |
| 推进主线 | 每次保存回响照片都触发该回响的 onCaptured，不只首次发现；场景里在这里接提示勾选和独白 | `EchoTrigger`、`CameraSystemManager` |
| 取景界面 | 手持和全屏两套白黄四角框、回响文字和落下动效 | `EchoTargetingUI`、`EchoViewfinderGraphic` |
| 拍摄锁定 | 按快门那一刻锁定黄框目标，照片保存成功后才记录 | `CameraSystemManager` |
| 发现记录 | 按固定编号存到 `Echo/DiscoveredEchoes.json` | `EchoDiscoveryStorage` |
| 照片与文字 | 每张回响照片记下拍到的手账文字和便签位置，存到 `Echo/EchoPhotos.json`，重复拍也保留；旧记录没有便签位置时按默认的照片下方 | `EchoPhotoStorage` |
| 回响标记 | 照片预制体上的 `EchoStamp` 印章和相框变色，相册、手账顶栏、手账画布三处共用 | `EchoPhotoMark`、`Photo_Thumbnail_Prefab` |
| 手账便签 | 回响文字做成手写便签，是照片的子物体，按保存的便签位置摆放，一条文字一段，点便签等于点照片 | `ScrapBookCanvas` |
| 手账范围 | 顶栏拖拽的预览和放下后一致，回响照片带便签；整体超出手账页面就不放下；画布上拖动、旋转、缩放也按包括便签在内的整体范围限制 | `ScrapBookDraggable`、`ScrapBookElement` |
| 首次发现提示 | 右上角显示 2 秒，重复拍摄不弹 | `EchoSystemManager`、`EchoPopupUI` |

## 场景里的回响

主场景目前配置了三个回响，都在第一关：大门是 A 类，门卫室里两个按 B 类处理。

| 物品 | 场景对象 | 编号 | 手账文字 | 便签位置 | 状态 |
| --- | --- | --- | --- | --- | --- |
| Group Photo 重拍 | `In-Scene Objects/工厂大门/FactoryGate` | `group_photo_retake` | 暂用 M4 原句，待定 | 照片下方偏右 | 站位点是 `Group Photo Retake View`，站在它 1.2 米内、相机朝向和它相差 10 度以内、离大门 22 米以内才识别，脚下是泛光圆圈 `Group Photo Retake Circle`；只在 T4 未拍成时开启，拍成勾 `T4_match_photo`、播 M4 |
| 值班日记本 | `In-Scene Objects/工厂大门/Guard Post/室内陈设/Logbook` | `fb2f4677570c4148a6914782a778a943` | 有旧文字，需按当前故事重写 | 照片下方偏右 | 可识别、可记录 |
| 街道照片 | `In-Scene Objects/工厂大门/Guard Post/室内陈设/OldPhotoFrame_StreetMarket` | `street_market_photo` | 空 | 照片左侧 | 可识别、可记录，只认相框正面 |

## 待实现

1. **类别和所属关卡**。`EchoTrigger` 还没有 A、B 类别和关卡字段，探索记录的 `n/x` 要靠它区分。推进主线不靠类别，靠各回响自己的 onCaptured。
2. **探索记录页签**。按关卡显示 `n/x` 和已发现回响的卡片，要求见[第一关 Spec](../Levels/opening-level-one-spec.md)的“Player Panel 探索记录页签”一节。代码和界面都还没有。
3. **回响文字**。街道照片的手账文字、值班日记本的旧文字要按当前故事重写，Group Photo 重拍的手账文字待定，改完同步[游戏文本](../../game-text.md)。

## 待确认

1. 值班日记本算主线步骤还是 B 类回响。
2. Group Photo 重拍是否计入本关回响进度 `n/x`。
3. Group Photo 重拍成功后解锁什么，例如手账内容。
4. 回响文字现在写在场景 Inspector 里，是否和外公日记、内心独白一样移到 JSON，保持一份文字来源。
