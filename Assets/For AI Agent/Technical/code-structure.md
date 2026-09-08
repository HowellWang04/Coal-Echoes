# 代码结构

本文档是项目代码文档门户，用于快速定位系统、源码和详细说明。

- [查看全部代码详解](code-details.md)
- [查看开发说明](dev-guidelines.md)

## 技术栈

- 引擎：Unity URP
- 地形系统：MapMagic
- 摄像机系统：Cinemachine
- 输入系统：Unity Input System，项目自定义运行时代码已完成迁移

## 源码范围

项目自定义源码主要位于 `Assets/Scripts/In-Game Systems/` 和 `Assets/Scripts/Player Panel/`。以下每个文件都提供源码入口和详细说明入口。

## CameraSystem

- [AlbumPhotoContextMenu.cs](../../Scripts/In-Game%20Systems/Camera%20System/AlbumPhotoContextMenu.cs)　相册右键菜单　[详细说明](code-details.md#albumphotocontextmenucs)
- [CameraCapture.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraCapture.cs)　固定尺寸照片截图　[详细说明](code-details.md#cameracapturecs)
- [CameraHighlightObject.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraHighlightObject.cs)　可拍摄物体数据组件　[详细说明](code-details.md#camerahighlightobjectcs)
- [CameraHighlightObjectDetector.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraHighlightObjectDetector.cs)　可见物体检测与高亮　[详细说明](code-details.md#camerahighlightobjectdetectorcs)
- [CameraModelInertia.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraModelInertia.cs)　手持相机模型惯性与自然晃动　[详细说明](code-details.md#cameramodelinertiacs)
- [CameraModeState.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraModeState.cs)　相机模式状态定义　[详细说明](code-details.md#cameramodestatecs)
- [CameraPhotoAlbumUIManager.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraPhotoAlbumUIManager.cs)　相册分页、缓存、预览和批量删除　[详细说明](code-details.md#cameraphotoalbumuimanagercs)
- [CameraPhotoPreviewUIManager.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraPhotoPreviewUIManager.cs)　照片预览界面　[详细说明](code-details.md#cameraphotopreviewuimanagercs)
- [CameraPhotoStorage.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraPhotoStorage.cs)　照片和清单持久化　[详细说明](code-details.md#cameraphotostoragecs)
- [CameraPhotoThumbItem.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraPhotoThumbItem.cs)　相册缩略图条目　[详细说明](code-details.md#cameraphotothumbitemcs)
- [CameraPreviewSync.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraPreviewSync.cs)　实时预览 Camera 镜头同步　[详细说明](code-details.md#camerapreviewsynccs)
- [CameraRaiseAnimationHandler.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraRaiseAnimationHandler.cs)　相机举起与放下动画　[详细说明](code-details.md#cameraraiseanimationhandlercs)
- [CameraStateMachine.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraStateMachine.cs)　相机状态转换规则　[详细说明](code-details.md#camerastatemachinecs)
- [CameraSystemManager.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraSystemManager.cs)　拍照系统总入口　[详细说明](code-details.md#camerasystemmanagercs)
- [CameraTweakUIManager.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraTweakUIManager.cs)　相机参数与后处理 UI　[详细说明](code-details.md#cameratweakuimanagercs)
- [CameraViewMode.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraViewMode.cs)　手持与全屏取景模式定义　[详细说明](code-details.md#cameraviewmodecs)
- [CameraViewTransitionHandler.cs](../../Scripts/In-Game%20Systems/Camera%20System/CameraViewTransitionHandler.cs)　双向取景过渡动画　[详细说明](code-details.md#cameraviewtransitionhandlercs)
- [ScreenReadBackCaptureProvider.cs](../../Scripts/In-Game%20Systems/Camera%20System/ScreenReadBackCaptureProvider.cs)　原始屏幕尺寸截图实现　[详细说明](code-details.md#screenreadbackcaptureprovidercs)

## CharacterMovementSystem

- [PlayeMovementManager.cs](../../Scripts/In-Game%20Systems/Character%20Movement%20System/Scripts/PlayeMovementManager.cs)　角色移动、重力和视角　[详细说明](code-details.md#playemovementmanagercs)
- [PlayerFootstepAudio.cs](../../Scripts/In-Game%20Systems/Character%20Movement%20System/Scripts/PlayerFootstepAudio.cs)　脚步音效控制　[详细说明](code-details.md#playerfootstepaudiocs)

## InputSystem

- [PlayerControls.inputactions](../../Scripts/In-Game%20Systems/Input%20System/PlayerControls.inputactions)　输入动作源文件　[详细说明](code-details.md#playercontrolsinputactions)
- [PlayerControls.cs](../../Scripts/In-Game%20Systems/Input%20System/PlayerControls.cs)　自动生成的输入包装类　[详细说明](code-details.md#playercontrolscs)
- [PlayerInputProvider.cs](../../Scripts/In-Game%20Systems/Input%20System/PlayerInputProvider.cs)　统一输入实例持有者　[详细说明](code-details.md#playerinputprovidercs)
- [PhotoModeInputRouter.cs](../../Scripts/In-Game%20Systems/Input%20System/PhotoModeInputRouter.cs)　拍照输入路由　[详细说明](code-details.md#photomodeinputroutercs)
- [PlayerPanelInputRouter.cs](../../Scripts/In-Game%20Systems/Input%20System/PlayerPanelInputRouter.cs)　玩家面板输入路由　[详细说明](code-details.md#playerpanelinputroutercs)

## InspectObjectSystem

- [InspectableItem.cs](../../Scripts/In-Game%20Systems/Inspect%20Object%20System/InspectableItem.cs)　可检视物品数据　[详细说明](code-details.md#inspectableitemcs)
- [InspectRotator.cs](../../Scripts/In-Game%20Systems/Inspect%20Object%20System/InspectRotator.cs)　检视物体旋转与缩放　[详细说明](code-details.md#inspectrotatorcs)
- [InspectSystemManager.cs](../../Scripts/In-Game%20Systems/Inspect%20Object%20System/InspectSystemManager.cs)　目标检测和交互提示　[详细说明](code-details.md#inspectsystemmanagercs)
- [InspectUIManager.cs](../../Scripts/In-Game%20Systems/Inspect%20Object%20System/InspectUIManager.cs)　检视界面与物体克隆　[详细说明](code-details.md#inspectuimanagercs)

## PlayerPanel

- [PlayerPanelUIManager.cs](../../Scripts/Player%20Panel/PlayerPanelUIManager.cs)　玩家面板和页签总控　[详细说明](code-details.md#playerpaneluimanagercs)

## GrandpaDiary

- [GrandpaDiaryJsonParser.cs](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryJsonParser.cs)　日记 JSON 解析　[详细说明](code-details.md#grandpadiaryjsonparsercs)
- [GrandpaDiaryPageData.cs](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryPageData.cs)　日记数据模型　[详细说明](code-details.md#grandpadiarypagedatacs)
- [GrandpaDiaryPageView.cs](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryPageView.cs)　日记页面数据绑定　[详细说明](code-details.md#grandpadiarypageviewcs)
- [GrandpaDiaryStickyNoteSlot.cs](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryStickyNoteSlot.cs)　日记便签显示槽　[详细说明](code-details.md#grandpadiarystickynoteslotcs)
- [GrandpaDiaryStickyNoteStorage.cs](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryStickyNoteStorage.cs)　便签解锁持久化　[详细说明](code-details.md#grandpadiarystickynotestoragecs)
- [GrandpaDiaryTextAreaBinding.cs](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryTextAreaBinding.cs)　文字标记与 TMP 绑定　[详细说明](code-details.md#grandpadiarytextareabindingcs)

## ItemBackpack

- [ItemBackpackManager.cs](../../Scripts/Player%20Panel/ItemBackpack/ItemBackpackManager.cs)　背包数据管理　[详细说明](code-details.md#itembackpackmanagercs)
- [ItemBackpackSlot.cs](../../Scripts/Player%20Panel/ItemBackpack/ItemBackpackSlot.cs)　背包槽位与三维预览　[详细说明](code-details.md#itembackpackslotcs)
- [ItemBackpackUIManager.cs](../../Scripts/Player%20Panel/ItemBackpack/ItemBackpackUIManager.cs)　背包列表 UI　[详细说明](code-details.md#itembackpackuimanagercs)

## ScrapBookSystem

- [ScrapBookCanvas.cs](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookCanvas.cs)　手账元素创建与重建　[详细说明](code-details.md#scrapbookcanvascs)
- [ScrapBookDataManager.cs](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookDataManager.cs)　手账数据与布局持久化　[详细说明](code-details.md#scrapbookdatamanagercs)
- [ScrapBookDecoPreset.cs](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookDecoPreset.cs)　手账装饰预设　[详细说明](code-details.md#scrapbookdecopresetcs)
- [ScrapBookDraggable.cs](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookDraggable.cs)　工具栏拖入画布　[详细说明](code-details.md#scrapbookdraggablecs)
- [ScrapBookElement.cs](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookElement.cs)　画布元素交互　[详细说明](code-details.md#scrapbookelementcs)
- [ScrapBookElementMeta.cs](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookElementMeta.cs)　元素存档标识　[详细说明](code-details.md#scrapbookelementmetacs)
- [ScrapBookTextBox.cs](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookTextBox.cs)　手账文本框编辑　[详细说明](code-details.md#scrapbooktextboxcs)
- [ScrapBookUIManager.cs](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookUIManager.cs)　手账工具栏和布局协调　[详细说明](code-details.md#scrapbookuimanagercs)

## EchoSystem

- [EchoPhotoStorage.cs](../../Scripts/In-Game%20Systems/Echo%20System/EchoPhotoStorage.cs)　照片与 Echo 文本映射　[详细说明](code-details.md#echophotostoragecs)
- [EchoPopupUI.cs](../../Scripts/In-Game%20Systems/Echo%20System/EchoPopupUI.cs)　Echo 发现提示　[详细说明](code-details.md#echopopupuics)
- [EchoSystemManager.cs](../../Scripts/In-Game%20Systems/Echo%20System/EchoSystemManager.cs)　Echo 拍照事件监听　[详细说明](code-details.md#echosystemmanagercs)
- [EchoTrigger.cs](../../Scripts/In-Game%20Systems/Echo%20System/EchoTrigger.cs)　Echo 场景数据组件　[详细说明](code-details.md#echotriggercs)
- [ItemUseTrigger.cs](../../Scripts/In-Game%20Systems/Echo%20System/ItemUseTrigger.cs)　背包道具使用触发区　[详细说明](code-details.md#itemusetriggercs)

## GameSystem

- [GameFlowManager.cs](../../Scripts/In-Game%20Systems/Game%20System/GameFlowManager.cs)　初始光标状态控制　[详细说明](code-details.md#gameflowmanagercs)
