# 代码详解

[返回代码结构门户](code-structure.md)

本文档记录 `Assets/Scripts/` 下每个项目源码文件的职责、主要流程和协作关系。文件名链接直接打开源码。自动生成文件只说明用途，不应手动修改。

## CameraSystem

### AlbumPhotoContextMenu.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/AlbumPhotoContextMenu.cs)
- **职责**：管理相册缩略图的右键菜单，并把当前照片登记到手账照片列表。
- **主要流程**：`Show()` 保存照片文件名并把菜单移动到点击位置，按钮调用 `ScrapBookDataManager.RegisterPhoto()`，点击菜单外区域时隐藏菜单。
- **协作对象**：`PhotoThumbItem`、`ScrapBookDataManager`。
- **输入现状**：菜单外点击检测通过新版 Input System 的 `Mouse.current` 读取左右键和鼠标位置。

### CameraCapture.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraCapture.cs)
- **类型**：文件中的实际类名是 `CameraPhotoCapture`。
- **职责**：在帧渲染结束后读取当前屏幕，并输出指定尺寸的照片纹理。
- **主要流程**：`Capture()` 等待 `WaitForEndOfFrame`，通过 `ReadPixels` 读取屏幕，再借助临时 `RenderTexture` 缩放到配置尺寸，最后通过回调返回新的 `Texture2D`。
- **性能策略**：可复用屏幕读取缓冲区，默认输出 1920×1080，但每次拍照仍会创建返回用纹理。
- **协作对象**：由 `CameraSystemManager` 启动协程调用。

### CameraHighlightObject.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraHighlightObject.cs)
- **职责**：标记可被相机识别和高亮的场景物体。
- **保存数据**：在 `Awake()` 中收集所有子级 `Renderer`，并保存每个 Renderer 的原始材质数组，供高亮移除时恢复。
- **配置数据**：`HighlightObjectType` 当前包含 `Book` 和 `Helmet`。
- **协作对象**：`CameraHighlightObjectDetector`、`CameraSystemManager`、`EchoTrigger`。

### CameraHighlightObjectDetector.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraHighlightObjectDetector.cs)
- **职责**：在拍照模式中寻找进入相机视野的可拍摄物体，并添加高亮材质。
- **实时检测**：仅在相机状态为 `Active` 时扫描 `CameraHighlightObject`，根据 Renderer 可见性、视口位置和最大距离维护高亮集合，其他状态清除全部高亮。
- **拍照确认**：`FilterConfirmedObjects()` 对候选对象执行完整入镜和射线遮挡检查，返回真正计入照片的对象。
- **协作对象**：读取 `CameraSystemManager.CurrentState`，向 `CameraSystemManager` 提供 `HighlightedObjects`。

### CameraModelInertia.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraModelInertia.cs)
- **职责**：为手持相机模型提供基于视角变化的俯仰、偏航和滚转惯性，以及待机和移动时的自然手臂晃动。
- **旋转惯性**：读取 Main Camera 每帧旋转增量，生成反向拖拽并限制最大俯仰和偏航，停止转动后通过 `SmoothDamp` 平滑回正。
- **自然晃动**：通过 Main Camera 的水平位移估算移动强度，在低幅度待机正弦漂移和移动步伐位置、旋转晃动之间平滑混合。
- **职责边界**：只修改独立 `CameraInertiaPivot` 的局部位置和旋转；`CameraRaiseAnimationHandler` 与 `CameraViewTransitionHandler` 继续修改其子级 CameraModel，避免覆盖同一 Transform。

### CameraModeState.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraModeState.cs)
- **职责**：定义拍照系统的四个状态。
- **状态**：`Disabled` 表示未进入相机，`Active` 表示可拍照，`Capturing` 表示正在截图，`Reviewing` 表示正在查看刚拍摄的照片。
- **协作对象**：`CameraStateMachine`、`CameraSystemManager` 和输入路由共同使用该枚举。

### CameraPhotoAlbumUIManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraPhotoAlbumUIManager.cs)
- **职责**：管理相册照片清单、分页、缩略图缓存、预览入口和批量删除。
- **初始化**：从 `CameraPhotoStorage` 读取清单，注册翻页和批量删除按钮，并可逐帧预热缩略图缓存。
- **新增与展示**：保存新照片后将文件名插到清单首位；`Refresh()` 按当前页生成 `PhotoThumbItem`。缩略图的十六比九边框由 Prefab 提供。
- **删除流程**：删除模式下支持选择删除和全部删除，同步清理 PNG 文件、缩略图缓存和照片清单。
- **协作对象**：`CameraPhotoStorage`、`CameraPhotoPreviewUIManager`、`PhotoThumbItem`、`AlbumPhotoContextMenu`。

### CameraPhotoPreviewUIManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraPhotoPreviewUIManager.cs)
- **职责**：显示刚拍摄照片或相册缩略图对应的大图预览。
- **两种模式**：拍摄预览会播放闪光和淡入动画，相册预览允许点击照片关闭，不再使用独立关闭按钮。
- **纹理显示**：把传入的 `Texture2D` 转换为 Sprite 后赋给 UI Image。
- **协作对象**：由 `CameraSystemManager` 和 `CameraPhotoAlbumUIManager` 调用。

### CameraPhotoStorage.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraPhotoStorage.cs)
- **职责**：持久化照片 PNG 和照片文件名清单。
- **保存位置**：使用 `Application.persistentDataPath/CapturedPhotos`，清单文件为 `CapturedPhotosManifest.json`。
- **主要流程**：`SavePhotoPng()` 生成带时间戳的文件名并异步写入 PNG，`SaveManifest()` 同步写入 JSON，`LoadTexture()` 从磁盘恢复纹理。
- **协作对象**：`CameraPhotoAlbumUIManager`、`ScrapBookUIManager`。

### CameraPhotoThumbItem.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraPhotoThumbItem.cs)
- **类型**：文件中的实际类名是 `PhotoThumbItem`。
- **职责**：表示一个相册缩略图条目，连接预览点击、右键菜单和删除选择状态。
- **主要流程**：`Bind()` 设置纹理与点击回调，右键通过 EventSystem 打开 `AlbumPhotoContextMenu`，删除模式下启用 Toggle 并关闭普通按钮。再次左键同一缩略图会关闭当前大图预览。
- **删除反馈**：使用 DOTween 驱动缩略图子级 `UIEffectBase` 的 Transition Rate，由 Prefab 上配置的 UIEffect 预设决定撕碎、溶解等视觉。
- **资源清理**：销毁时移除按钮和 Toggle 的监听器。

### CameraPreviewSync.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraPreviewSync.cs)
- **职责**：让输出到相机实体屏幕 RenderTexture 的 CameraPreview 跟随 Main Camera 镜头。
- **同步内容**：在 `LateUpdate()` 中同步世界位置、旋转、视野角、近裁剪面和远裁剪面。
- **渲染边界**：CameraPreview 使用独立 Volume Layer 并排除 CameraModel Layer，避免实时屏幕拍到实体相机和自身。
- **协作对象**：由 `CameraRaiseAnimationHandler` 随相机模型显隐启用或停用 CameraPreview。

### CameraRaiseAnimationHandler.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraRaiseAnimationHandler.cs)
- **职责**：播放相机模型举起和放下动画，并统一控制实体模型、CameraPreview 和 Handheld Focus Volume 的可见状态。
- **主要流程**：协程在起点与终点的局部位置、旋转和缩放之间插值；举起结束后保留模型，放下结束后隐藏模型并执行完成回调。
- **手持聚焦**：模型可见时启用 Handheld Focus Volume，使用 Main Camera 前向深度把焦点同步到 `focusTarget`，未绑定目标时回退到相机模型轴心。
- **视觉处理**：放下前关闭相机模型的投射和接收阴影，避免近距离模型阴影干扰场景。
- **协作对象**：`PhotoModeInputRouter` 调用举起和放下流程，`CameraViewTransitionHandler` 调用模型显隐接口。

### CameraStateMachine.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraStateMachine.cs)
- **职责**：约束相机模式的合法状态切换，不直接操作场景对象或 UI。
- **主要流程**：进入模式、退出模式、请求拍照、完成拍照、打开预览和关闭预览都返回是否转换成功。
- **事件**：状态变化后触发 `OnStateChanged`，同时传出旧状态和新状态。
- **协作对象**：由 `CameraSystemManager` 创建并持有。

### CameraSystemManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraSystemManager.cs)
- **职责**：拍照系统总入口，协调状态机、手持与全屏取景、UI、Volume、截图、相册、Echo 和日记便签解锁。
- **取景模式**：进入相机时重置为 `Handheld`；`ToggleViewMode()` 在 `Handheld` 与 `Fullscreen` 之间切换，并在动画完成后才设置 Fullscreen Photo Volume 权重。
- **进入与退出**：状态变化时控制取景显示、照片预览和对应 Volume，退出时关闭全屏 Volume 并重置相机参数。
- **拍照逻辑链**：`Capture()` 请求进入 `Capturing` 并保存高亮候选快照；手持拍照会在帧末读取前临时隐藏 CameraModel、停用 CameraPreview 和 Handheld Focus Volume、启用 Fullscreen Photo Volume，读取完成后恢复手持状态。
- **照片处理**：截图完成后显示预览并保存照片，再把确认的 Echo 文本写入 `EchoPhotoStorage`；过渡期间拒绝拍照。
- **叙事联动**：照片中的 `EchoTrigger` 可解锁 `GrandpaDiaryStickyNoteStorage` 对应便签，流程结束后触发 `OnPhotoCapturedWithEchoes`。
- **协作对象**：`CameraStateMachine`、`CameraViewTransitionHandler`、`CameraPhotoCapture`、相册与预览管理器、`CameraHighlightObjectDetector`、Echo 系统。

### CameraTweakUIManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraTweakUIManager.cs)
- **职责**：管理拍照界面的相机参数、后处理参数、页签和构图网格。
- **可调参数**：焦距、景深光圈、暗角、曝光、Bloom 和饱和度。
- **主要流程**：启用时绑定 Slider、按钮和 Toggle，关闭时解除监听；焦距与视野角通过 35mm 全画幅传感器高度换算，其他后期参数同步写入 Preview 与 Fullscreen 两份独立 Volume Profile。
- **Volume 边界**：不修改 ProjectCoal Global Volume，避免退出相机模式后继续影响普通探索画面。
- **重置行为**：首次初始化时从可用相机 Profile 保存一组共享默认值，`ResetCameraTweaks()` 将默认参数恢复到 Preview 与 Fullscreen 两份 Profile 并同步 UI。
- **协作对象**：Unity Camera、Preview Volume、Fullscreen Volume、`CameraSystemManager`。

### CameraViewMode.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraViewMode.cs)
- **职责**：定义相机启用后的两种取景显示方式。
- **模式**：`Handheld` 显示实体相机及其实时屏幕，`Fullscreen` 隐藏实体相机并显示全屏相机 UI。
- **职责边界**：该枚举不替代 `CameraModeState`；前者表示取景显示方式，后者表示拍照、截图和照片预览流程状态。

### CameraViewTransitionHandler.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/CameraViewTransitionHandler.cs)
- **职责**：播放手持与全屏取景之间的双向过渡，并集中控制全屏 UI CanvasGroup 和相机模型显隐。
- **进入全屏**：保存手持姿态，将相机模型按可调局部位移、旋转和缩放向镜头推进，同时淡入全屏 UI，动画结束后隐藏模型。
- **返回手持**：从全屏过渡姿态重新显示模型，反向插值回保存的手持姿态并淡出 UI。
- **运行限制**：同一时间只允许一个过渡协程，向 `CameraSystemManager` 暴露配置状态和过渡状态；全屏 Volume 的实际切换由管理器在完成回调中执行。
- **拍照协作**：提供即时模型显隐接口，让手持拍照可以在帧末读取期间排除实体相机而不改变当前取景模式。

### ScreenReadBackCaptureProvider.cs

- [打开源码](../../Scripts/In-Game%20Systems/Camera%20System/ScreenReadBackCaptureProvider.cs)
- **类型**：文件中的实际类名是 `ScreenReadbackCaptureProvider`，位于 `PhotoSystem.Runtime.Capture` 命名空间。
- **职责**：提供保持当前屏幕分辨率的基础截图实现。
- **主要流程**：等待帧结束后读取屏幕缓冲，再复制出独立纹理交给回调。
- **与主流程的区别**：它不负责缩放到固定照片尺寸，当前拍照总控引用的类型是 `CameraPhotoCapture`。

## CharacterMovementSystem

### PlayeMovementManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Character%20Movement%20System/Scripts/PlayeMovementManager.cs)
- **类型**：文件名保留了 `Playe` 拼写，实际类名是 `PlayerMovementManager`，位于 `Controller.FinalCharacterController` 命名空间。
- **职责**：消费新版输入系统的移动和视角值，并驱动角色与相机。
- **移动逻辑链**：`Gameplay.Movement` 更新二维输入，脚本将相机前方和右方投影到地面，计算目标速度并应用加减速与重力，最终调用 `CharacterController.Move()`。
- **视角逻辑链**：`Gameplay.Look` 更新鼠标增量，`LateUpdate()` 修改角色水平旋转和相机垂直俯仰。
- **控制开关**：面板、拍照光标模式和检视系统通过 `SetPlayerMovementControl()` 暂停或恢复控制。

### PlayerFootstepAudio.cs

- [打开源码](../../Scripts/In-Game%20Systems/Character%20Movement%20System/Scripts/PlayerFootstepAudio.cs)
- **职责**：根据角色移动和着地状态控制循环脚步声。
- **主要流程**：确保 AudioSource 使用配置的脚步音频并循环播放；角色着地且正在移动时播放，停止移动或离地时暂停。
- **协作对象**：读取 `PlayerMovementManager.IsGrounded` 和 `IsMoving`。

## InputSystem

### PlayerControls.inputactions

- [打开输入动作源文件](../../Scripts/In-Game%20Systems/Input%20System/PlayerControls.inputactions)
- **职责**：新版 Input System 的可编辑输入动作源文件，Action Map 名为 `Gameplay`。
- **当前绑定**：`Movement` 使用 WASD，`Look` 使用鼠标位移，`CameraModeToggle` 使用数字 1，`PhotoShutter` 使用鼠标左键，`PlayerPanelToggle` 使用 Tab，`CursorToggle` 使用鼠标右键。
- **检视与道具交互绑定**：`Interact` 使用 F，已经接入 `InspectSystemManager` 和 `ItemUseTrigger`；`Cancel` 使用 Escape 和鼠标右键，已经接入 `InspectUIManager`；`InspectRotate` 使用鼠标左键，`InspectZoom` 使用滚轮，已经接入 `InspectRotator`。
- **生成关系**：保存或导入该资源后生成 `PlayerControls.cs`。

### PlayerControls.cs

- [打开源码](../../Scripts/In-Game%20Systems/Input%20System/PlayerControls.cs)
- **职责**：Unity 根据 `PlayerControls.inputactions` 自动生成的强类型 C# 包装类。
- **提供接口**：公开 `GameplayActions` 和每个 `InputAction` 属性，使消费者可以通过 `Controls.Gameplay.Movement` 等路径取得动作。
- **生命周期**：实现 `IInputActionCollection2` 和 `IDisposable`，支持整体启用、停用和释放。
- **维护规则**：禁止手动修改，任何输入变更都应编辑 `.inputactions` 源文件后重新生成。

### PlayerInputProvider.cs

- [打开源码](../../Scripts/In-Game%20Systems/Input%20System/PlayerInputProvider.cs)
- **挂载位置**：主场景的 `Player` 对象。
- **职责**：创建并持有唯一的 `PlayerControls` 实例，统一管理新版输入的生命周期，不解释具体按键功能。
- **生命周期**：`Awake()` 创建实例，`OnEnable()` 启用全部 Action Map，`OnDisable()` 停用，`OnDestroy()` 释放。
- **公开接口**：只读属性 `Controls` 允许移动和输入路由取得同一份输入实例。
- **执行顺序**：`DefaultExecutionOrder` 为负 3，早于依赖它的移动脚本和路由组件初始化。

### PhotoModeInputRouter.cs

- [打开源码](../../Scripts/In-Game%20Systems/Input%20System/PhotoModeInputRouter.cs)
- **职责**：订阅拍照相关 Action，并把输入转换为相机系统调用。
- **数字 1**：在玩家面板关闭时举起或放下相机，举起动画完成后进入拍照模式并启用快门 Action。
- **V 键**：在 `Active` 状态调用 `ToggleViewMode()` 切换手持与全屏取景；过渡期间不允许再次切换或放下相机。
- **鼠标左键**：在 `Active` 状态拍照，在 `Reviewing` 状态关闭本次预览。
- **鼠标右键**：只在 `Fullscreen` 的 `Active` 状态切换可见光标，同时暂停或恢复角色移动与视角控制；切回手持时自动退出光标模式。
- **协作对象**：`PlayerInputProvider`、`CameraSystemManager`、`PlayerPanelUIManager`、`PlayerMovementManager`、`CameraRaiseAnimationHandler`。

### PlayerPanelInputRouter.cs

- [打开源码](../../Scripts/In-Game%20Systems/Input%20System/PlayerPanelInputRouter.cs)
- **职责**：订阅 `Gameplay.PlayerPanelToggle` 并切换玩家面板。
- **限制条件**：只有相机系统处于 `Disabled` 状态时才允许打开或关闭面板，避免拍照和预览期间发生界面冲突。
- **协作对象**：`PlayerInputProvider`、`CameraSystemManager`、`PlayerPanelUIManager`。

## InspectObjectSystem

### InspectableItem.cs

- [打开源码](../../Scripts/In-Game%20Systems/Inspect%20Object%20System/InspectableItem.cs)
- **职责**：保存一个可检视物品的名称、描述和展示参数。
- **展示参数**：包括物体与相机的检视距离、预览缩放和初始旋转偏移。
- **协作对象**：`InspectSystemManager`、`InspectUIManager` 和背包系统读取这些数据。

### InspectRotator.cs

- [打开源码](../../Scripts/In-Game%20Systems/Inspect%20Object%20System/InspectRotator.cs)
- **职责**：控制检视克隆的旋转和前后缩放。
- **主要流程**：激活时接收相机、初始距离和输入持有者，按住鼠标左键时读取 `Gameplay.Look` 旋转物体，滚轮修改物体沿相机前方向的距离。
- **输入现状**：通过 `Gameplay.InspectRotate`、`Gameplay.Look` 和 `Gameplay.InspectZoom` 使用新版 Input System，并按旧 Input Manager 的灵敏度换算输入值。
- **协作对象**：由 `InspectUIManager` 动态添加到检视克隆，并由后者传入 `PlayerInputProvider`。

### InspectSystemManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Inspect%20Object%20System/InspectSystemManager.cs)
- **职责**：寻找玩家视野内最近的可检视物品，并显示远距离圆点或近距离检视提示。
- **检测流程**：启动时缓存场景中的 `InspectableItem`，运行时检查激活状态、距离和视口位置，选择最近目标并把提示投影到屏幕。
- **交互流程**：近距离按 F 后启动检视，并通过 `ItemBackpackManager` 把原物品加入背包。
- **限制条件**：拍照模式、玩家面板或检视界面正在开启时停止检测。
- **输入现状**：通过 `PlayerInputProvider.Controls.Gameplay.Interact` 读取新版 Input System 的 F 键动作。

### InspectUIManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Inspect%20Object%20System/InspectUIManager.cs)
- **职责**：管理检视界面的开启、关闭和检视物体克隆。
- **开启流程**：保存光标状态，停止玩家控制，显示名称和描述，在相机前克隆物体并递归设置检视层，移除克隆的碰撞体、刚体和数据组件。
- **关闭流程**：销毁克隆、恢复玩家控制与光标状态，并触发 `OnInspectStopped`。
- **输入现状**：通过 `PlayerInputProvider.Controls.Gameplay.Cancel` 读取新版 Input System 的 Escape 和鼠标右键动作。
- **协作对象**：`PlayerMovementManager`、`InspectRotator`、背包 UI。

## PlayerPanel

### PlayerPanelUIManager.cs

- [打开源码](../../Scripts/Player%20Panel/PlayerPanelUIManager.cs)
- **职责**：玩家面板总控，管理相册、手账、爷爷日记和背包四个页签。
- **开关流程**：打开时暂停玩家控制并显示光标，关闭时恢复此前光标状态并通知相册关闭预览。
- **页签流程**：按钮先隐藏全部页签，再显示目标页签；启动后默认显示相册。
- **日记流程**：从 JSON 解析页面数据，按页面 ID 或索引选择 `GrandpaDiaryPageView`，并管理翻页按钮和纯文字回退显示。
- **协作对象**：`PlayerPanelInputRouter`、`PlayerMovementManager`、相册管理器和爷爷日记组件。

## GrandpaDiary

### GrandpaDiaryJsonParser.cs

- [打开源码](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryJsonParser.cs)
- **职责**：把爷爷日记 JSON 转换为 `GrandpaDiaryPageData` 数组。
- **主要流程**：使用 `JsonUtility` 读取根对象的 `pages`，再按 `pageIndex` 排序。
- **失败行为**：空内容、缺少 pages 或解析异常时记录警告并返回空数组。
- **协作对象**：由 `PlayerPanelUIManager` 加载日记资源时调用。

### GrandpaDiaryPageData.cs

- [打开源码](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryPageData.cs)
- **职责**：定义可由 JSON 序列化的日记数据模型。
- **页面数据**：包含页面 ID、索引、标题、文字块列表和便签槽列表。
- **文字块数据**：通过 `markerId` 对应页面中的文字区域。
- **便签数据**：包含槽位 ID、触发信息、便签内容和默认解锁状态。

### GrandpaDiaryPageView.cs

- [打开源码](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryPageView.cs)
- **职责**：把一页日记数据绑定到场景中对应的文字区域和便签槽。
- **初始化**：缺少显式引用时自动查找子级 `GrandpaDiaryTextAreaBinding` 和 `GrandpaDiaryStickyNoteSlot`。
- **显示流程**：清空旧内容，按 marker ID 填充文字，再根据默认状态或持久化解锁记录显示便签。
- **协作对象**：`PlayerPanelUIManager`、日记数据类、文字绑定和便签槽组件。

### GrandpaDiaryStickyNoteSlot.cs

- [打开源码](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryStickyNoteSlot.cs)
- **职责**：表示日记页面上的一个可解锁便签显示槽。
- **主要流程**：按 `slotId` 与数据匹配，显示时填写内容，隐藏时清空内容并关闭根对象。
- **悬停效果**：通过 CanvasGroup 在正常透明度和悬停透明度之间切换。
- **协作对象**：由 `GrandpaDiaryPageView` 控制。

### GrandpaDiaryStickyNoteStorage.cs

- [打开源码](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryStickyNoteStorage.cs)
- **职责**：持久化已经解锁的日记便签槽 ID。
- **保存位置**：使用 `Application.persistentDataPath/GrandpaDiary/UnlockedStickyNotes.json`。
- **主要接口**：读取全部 ID、判断指定槽是否解锁，以及幂等地解锁新槽。
- **协作对象**：`CameraSystemManager` 在拍到对应 Echo 时解锁，`GrandpaDiaryPageView` 在显示页面时查询。

### GrandpaDiaryTextAreaBinding.cs

- [打开源码](../../Scripts/Player%20Panel/GrandpaDiary/GrandpaDiaryTextAreaBinding.cs)
- **职责**：把日记文字块的 `markerId` 映射到一个 TMP 文本组件。
- **主要流程**：缺少引用时自动寻找当前对象或子级 TMP_Text，`Matches()` 判断标记，`SetContent()` 更新显示文字。
- **协作对象**：由 `GrandpaDiaryPageView` 查找并写入内容。

## ItemBackpack

### ItemBackpackManager.cs

- [打开源码](../../Scripts/Player%20Panel/ItemBackpack/ItemBackpackManager.cs)
- **职责**：以单例形式保存本次运行中的背包物品数据。
- **加入流程**：从 `InspectableItem` 复制名称、描述和检视参数，保存原场景对象引用，然后禁用原物体并触发 `OnBackpackChanged`。
- **数据范围**：当前列表只保存在内存中，没有磁盘持久化或移除接口。
- **协作对象**：`InspectSystemManager`、`ItemBackpackUIManager`、`ItemUseTrigger`。

### ItemBackpackSlot.cs

- [打开源码](../../Scripts/Player%20Panel/ItemBackpack/ItemBackpackSlot.cs)
- **职责**：显示一个背包物品条目，并生成独立的三维预览。
- **预览流程**：在场景远处克隆物体，递归设置预览层，创建专用 Camera 和 RenderTexture，再把纹理交给 RawImage。
- **点击流程**：点击槽位后使用原物品的 `InspectableItem` 数据启动检视。
- **资源清理**：销毁槽位时销毁预览克隆与相机，并释放 RenderTexture。

### ItemBackpackUIManager.cs

- [打开源码](../../Scripts/Player%20Panel/ItemBackpack/ItemBackpackUIManager.cs)
- **职责**：根据背包数据生成槽位列表。
- **刷新流程**：订阅 `OnBackpackChanged`，刷新时清除旧槽位并为每条记录实例化 `ItemBackpackSlot`。
- **检视联动**：从背包启动检视时禁用 ScrollRect，检视结束事件触发后恢复滚动。
- **协作对象**：`ItemBackpackManager`、`ItemBackpackSlot`、`InspectUIManager`。

## ScrapBookSystem

### ScrapBookCanvas.cs

- [打开源码](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookCanvas.cs)
- **职责**：手账画布的元素工厂、布局提取器和存档重建入口。
- **创建流程**：可创建照片、贴纸和文本框元素，为元素添加 RectTransform、显示组件、`ScrapBookElement` 和 `ScrapBookElementMeta`。
- **Echo 展示**：照片存在 Echo 内容时，在照片下方生成对应的文字标签。
- **存档流程**：`GetAllElementData()` 收集类型、来源 ID、位置、旋转、缩放和文本；`RebuildElement()` 根据保存数据重建元素。
- **协作对象**：`ScrapBookDraggable`、`ScrapBookDataManager`、`EchoPhotoStorage`。

### ScrapBookDataManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookDataManager.cs)
- **职责**：管理已登记照片、装饰列表和画布布局持久化。
- **照片存储**：照片文件名保存到 `Application.persistentDataPath/ScrapBook/RegisteredPhotos.json`，注册时防止重复并触发刷新事件。
- **画布存储**：元素布局保存到同目录下的 `CanvasLayout.json`。
- **装饰数据**：装饰列表当前只保存在内存中，但添加或移除时仍触发 UI 刷新事件。
- **协作对象**：相册右键菜单、`ScrapBookUIManager`、`ScrapBookCanvas`。

### ScrapBookDecoPreset.cs

- [打开源码](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookDecoPreset.cs)
- **职责**：定义一个可在 Project 窗口创建的手账装饰 ScriptableObject。
- **配置数据**：预设名称、装饰类型、图标和默认尺寸。
- **装饰类型**：`DecoType` 用于区分贴纸和文本框。
- **协作对象**：`ScrapBookUIManager` 生成工具栏，`ScrapBookCanvas` 创建或重建元素。

### ScrapBookDraggable.cs

- [打开源码](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookDraggable.cs)
- **职责**：让工具栏中的照片或装饰可以拖入手账画布。
- **开始拖动**：生成半透明且不接收射线的拖动影子，并缓存根 Canvas 与画布引用。
- **结束拖动**：把屏幕坐标转换为画布局部坐标，落点位于画布内时调用 `ScrapBookCanvas` 创建照片或装饰。
- **输入方式**：使用 Unity UI EventSystem 的拖拽接口，不依赖旧 Input Manager 的轴配置。

### ScrapBookElement.cs

- [打开源码](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookElement.cs)
- **职责**：处理已放入手账画布的元素交互。
- **交互**：左键拖动位置，右键拖动旋转，滚轮按步长缩放，点击后移动到同级最上层。
- **边界限制**：根据元素尺寸、缩放和画布范围限制 anchoredPosition，防止元素完全拖出画布。
- **输入方式**：使用 Unity UI EventSystem 的指针与滚轮事件。

### ScrapBookElementMeta.cs

- [打开源码](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookElementMeta.cs)
- **职责**：附着在每个画布元素上，保存存档所需的元素类型和来源 ID。
- **类型值**：当前使用 `photo`、`sticker` 和 `textbox`。
- **来源 ID**：照片保存文件名，贴纸和文本框保存装饰预设名称。
- **协作对象**：由 `ScrapBookCanvas` 创建和读取。

### ScrapBookTextBox.cs

- [打开源码](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookTextBox.cs)
- **职责**：管理手账文本框的显示状态和编辑状态。
- **主要流程**：普通状态显示 TMP_Text，双击后切换到 TMP_InputField 并聚焦，结束编辑后把内容写回展示文本。
- **初始化方式**：既支持 Inspector 引用，也支持 `ScrapBookCanvas` 动态创建后通过 `InitRefs()` 注入。
- **资源清理**：销毁时移除输入结束监听器。

### ScrapBookUIManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Scrap%20Book%20System/ScrapBookUIManager.cs)
- **职责**：管理手账工具栏的照片与装饰分类，并协调画布恢复和保存。
- **照片分类**：读取已登记照片，创建缩略图和拖拽组件，带 Echo 的照片显示描边。
- **装饰分类**：根据 `ScrapBookDecoPreset` 动态生成贴纸或文本框工具栏条目。
- **持久化流程**：首次启用时恢复画布，关闭时从 `ScrapBookCanvas` 收集元素并保存布局。
- **协作对象**：`ScrapBookDataManager`、`ScrapBookCanvas`、`CameraPhotoStorage`、`EchoPhotoStorage`。

## EchoSystem

### EchoPhotoStorage.cs

- [打开源码](../../Scripts/In-Game%20Systems/Echo%20System/EchoPhotoStorage.cs)
- **职责**：保存照片文件名与该照片捕获到的手账 Echo 文本之间的映射。
- **保存位置**：使用 `Application.persistentDataPath/Echo/EchoPhotos.json`。
- **主要接口**：`Save()` 新增或覆盖指定照片的文本列表，`Load()` 返回照片对应文本，不存在时返回空列表。
- **协作对象**：`CameraSystemManager` 写入，相册和手账系统读取。

### EchoPopupUI.cs

- [打开源码](../../Scripts/In-Game%20Systems/Echo%20System/EchoPopupUI.cs)
- **职责**：显示短暂的发现 Echo 提示。
- **主要流程**：`Show()` 设置提示文本并重新启动计时协程，达到显示时长后自动隐藏。
- **协作对象**：由 `EchoSystemManager` 调用。

### EchoSystemManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Echo%20System/EchoSystemManager.cs)
- **职责**：监听成功拍照事件，并判断照片中是否包含 Echo 物体。
- **主要流程**：订阅 `CameraSystemManager.OnPhotoCapturedWithEchoes`，找到第一个带 `EchoTrigger` 的对象后显示一次提示并结束遍历。
- **职责边界**：具体 Echo 文本保存和便签解锁由 `CameraSystemManager` 完成，本组件只负责发现提示。

### EchoTrigger.cs

- [打开源码](../../Scripts/In-Game%20Systems/Echo%20System/EchoTrigger.cs)
- **职责**：附着在可产生 Echo 的场景物体上，保存拍照后进入手账的文本和可选便签槽 ID。
- **数据字段**：`scrapbookContent` 保存 Echo 文本，`stickyNoteSlotId` 指向需要解锁的爷爷日记便签。
- **协作对象**：由 `CameraSystemManager` 在确认拍照对象后读取。

### ItemUseTrigger.cs

- [打开源码](../../Scripts/In-Game%20Systems/Echo%20System/ItemUseTrigger.cs)
- **职责**：为需要指定背包物品才能触发的场景交互提供通用触发区。
- **主要流程**：玩家进入 Trigger 后检查背包物品名称，满足条件时显示提示，按 F 调用 Inspector 配置的 UnityEvent。
- **输入现状**：玩家进入触发区时自动取得其 `PlayerInputProvider`，通过 `Gameplay.Interact` 读取新版 Input System 的 F 键动作。
- **协作对象**：`PlayerInputProvider`、`ItemBackpackManager` 和 Inspector 配置的 `onUse`。

## GameSystem

### GameFlowManager.cs

- [打开源码](../../Scripts/In-Game%20Systems/Game%20System/GameFlowManager.cs)
- **类型**：文件中的实际类名是 `GameFlowController`。
- **当前职责**：在 `Awake()` 中锁定并隐藏光标。
- **当前范围**：尚未实现关卡流程、胜负条件或场景切换管理。
