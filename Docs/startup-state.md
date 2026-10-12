# 各系统启动时间轴

2026 年 10 月 11 日。按系统整理进入 Play、主菜单等待开始，以及各系统何时开放使用。

各系统并行经历 Awake、OnEnable、Start，以下章节顺序不代表系统逐个启动。零秒指 MainMenuUIManager.Start 开始显示菜单，前面的加载耗时不固定。执行顺序数字表示回调先后，不是毫秒。

最近一次静态核对共 29 项通过，主场景缺失脚本为零。以下为代码规定的状态，实际 Play 审计仍待完成。

## 主菜单与开场

| 阶段 | 脚本或对象 | 执行内容与结果 |
| --- | --- | --- |
| Start，顺序一百，主菜单零秒 | MainMenuUIManager | 记录游戏状态，关闭 Gameplay 输入与玩家移动，隐藏游戏 HUD，启用菜单相机，显示并解锁鼠标 |
| 零至 0.6 秒 | MenuPresentation、DOTween | 菜单透明度由零到一，标题、按钮和渐变遮罩淡入；淡入期间也可以点击 |
| 菜单等待 | MainMenuUIManager | isShowing 为真，openingStarted 为假；Start Game 与 Quit 可用，Continue 禁用 |
| 菜单等待 | OpeningSequenceUI | Idle，OpeningUIRoot 隐藏，文案引用已配置，没有文字或镜头动画 |
| 点击 Start Game 后 | MainMenuUIManager、OpeningSequenceUI | 菜单淡出 0.3 秒，再抬头、播放四段文字、低头；演出完成才恢复玩家操作 |

菜单由独立的 MainMenuUIRoot 控制，使用 Screen Space Overlay。MenuContent 与 BackdropShade 缩放均为一。菜单期间 MainMenuCamera 开启，Main Camera 与 CameraPreview 的 Camera 组件关闭；玩家对象和唯一 AudioListener 保持激活。

## 输入与角色移动

| 阶段 | 脚本或对象 | 执行内容与结果 |
| --- | --- | --- |
| Awake 与 OnEnable，顺序负三 | PlayerInputProvider | 创建 Controls 并启用动作，供后续脚本绑定；尚未开始 Update |
| Awake 与 OnEnable，顺序负一 | PlayerMovementManager | 缓存玩家与相机引用，绑定移动和视角动作 |
| Awake，顺序零 | GameFlowController | 设置默认锁鼠标状态；该脚本目前不负责关卡进度 |
| OnEnable，顺序零 | PhotoModeInputRouter、PlayerPanelInputRouter | 绑定拍照模式和玩家面板输入回调 |
| Start，顺序一百 | MainMenuUIManager | 关闭 Gameplay 全部动作，AllowControl 设为假，在首次 Update 前完成锁定 |
| 菜单等待 | EventSystem、InputSystemUIInputModule | 唯一 EventSystem 接收菜单 UI 输入；鼠标可见且未锁定，移动、拍照、F 和 Tab 不响应 |
| 开场结束 | MainMenuUIManager | 恢复进入菜单前的移动、Gameplay 输入与光标状态 |

## 玩家面板与通用 UI

| 阶段 | 脚本或对象 | 执行内容与结果 |
| --- | --- | --- |
| Awake，顺序零 | UIAnimationScope、UIButtonTweenFeedback | 为按钮配置动效和引用 |
| Awake，顺序十 | PlayerPanelUIManager | 准备日记页面和面板动效，IsOpen 设为假，关闭 PanelContainer；子 UI 已先完成初始化 |
| Start，顺序十 | PlayerPanelUIManager、UITabTransition | 绑定侧栏与日记分页，默认选择相册，其他页签隐藏 |
| Start，顺序一百 | MainMenuUIManager | Game UI Root 保持激活，CanvasGroup alpha 为零，interactable 与 blocksRaycasts 为假 |
| 菜单等待 | PlayerPanelUIManager | 面板关闭，所有游戏 UI 不接收点击，主菜单按钮保留反馈 |
| 开场结束并按 Tab | PlayerPanelUIManager、UIPanelSlideTransition | 打开玩家面板，显示当前页签 |

## 拍照系统

| 阶段 | 脚本或对象 | 执行内容与结果 |
| --- | --- | --- |
| Awake，顺序零 | CameraTweakUIManager | 缓存取景器和全屏拍照的默认参数，关闭网格，准备相机参数页 |
| Awake，顺序零 | CameraRaiseAnimationHandler | 隐藏手持相机模型，关闭 CameraPreview，手持对焦 Volume 权重设为零 |
| Awake 与 OnEnable | CameraPreviewSync、CameraModelInertia | 缓存同步和模型惯性所需引用，模型保持隐藏 |
| Awake，顺序十 | CameraSystemManager、CameraViewTransitionHandler | 创建状态机，设为 Disabled，默认视图 Handheld，隐藏全屏拍照 UI，关闭全屏拍照 Volume 权重 |
| Start，顺序零 | CameraPhotoCapture | 准备照片缓冲区，尚未拍摄或写入照片 |
| 菜单等待 | CameraSystemManager | 无拍摄、预览或视图过渡任务，相机调节 UI 隐藏 |
| 开场结束并进入拍照模式 | PhotoModeInputRouter、CameraSystemManager | 举起相机并启用取景，之后才允许拍摄和视图切换 |

## 相册与照片文件

| 阶段 | 脚本或调用关系 | 执行内容与结果 |
| --- | --- | --- |
| Awake，顺序零 | CameraPhotoAlbumUIManager 调用 CameraPhotoStorage.LoadManifest | 读取 CapturedPhotosManifest.json 中全部照片的文件名，绑定分页和删除按钮，默认第一页、删除模式关闭 |
| Awake 与 OnEnable 中的 Refresh | CameraPhotoAlbumUIManager 调用 CameraPhotoStorage.LoadTexture | 读取当前页 PNG，生成并缓存缩略图，再释放用于生成缩略图的原图纹理；命中缓存时不重复读取文件 |
| Awake，顺序零 | CameraPhotoPreviewUIManager | 绑定点击关闭回调，模式设为 None，隐藏照片预览 |
| Awake，顺序零 | AlbumPhotoContextMenu | 绑定加入手账按钮，隐藏右键菜单，清空当前照片选择 |
| Start，顺序十发起，随后跨帧 | CameraSystemManager 启动 CameraPhotoAlbumUIManager.PrewarmThumbnails | 遍历照片列表，跳过已缓存项，通过 CameraPhotoStorage.LoadTexture 读取其余照片并生成缩略图 |
| 菜单等待 | CameraPhotoAlbumUIManager | 相册界面隐藏，第一页已准备，剩余缩略图继续预热 |
| 游戏中打开相册并点击照片 | CameraPhotoAlbumUIManager 调用 CameraPhotoStorage.LoadTexture | 重新读取对应大图，交给 CameraPhotoPreviewUIManager 显示 |

CameraPhotoStorage 是静态文件读写工具，不挂 GameObject，没有独立的 Awake 或 Start。文件读取发生在调用它的当下，相册和手账共用这份照片文件。

菜单期间由保持激活的 CameraSystemManager 承载预热协程。相册自身的 Start 也有同一预热入口，会在相册首次实际启用时执行。预热在主线程跨帧进行，不保证菜单淡入的 0.6 秒内全部完成。

## 手账系统

| 阶段 | 脚本或调用关系 | 执行内容与结果 |
| --- | --- | --- |
| Awake，顺序负二 | ScrapBookDataManager | 建立单例，读取 RegisteredPhotos.json 中已加入手账的照片文件名，此时不读取图片像素 |
| Awake，顺序负一 | ScrapBookCanvas | 缓存画布 RectTransform，供后续恢复元素使用 |
| 首次 OnEnable，顺序零 | ScrapBookUIManager | 配置照片元素预制体，绑定分类按钮和数据事件 |
| 同一次 OnEnable，刷新素材栏 | ScrapBookUIManager 调用 CameraPhotoStorage.LoadTexture | 按手账登记列表读取照片，生成并缓存手账素材栏缩略图 |
| 素材栏刷新后 | ScrapBookUIManager 调用 ScrapBookDataManager.LoadCanvas | 读取 CanvasLayout.json 中照片、贴纸和文字的布局 |
| 随后的元素重建 | ScrapBookCanvas.RebuildElement | 恢复位置、旋转和缩放；照片元素通过 CameraPhotoStorage.LoadTexture 读取图片，完成后 canvasRestored 为真 |
| 菜单等待 | ScrapBookUIManager | 随玩家面板隐藏，已恢复的元素保留 |
| 游戏中打开手账页签 | ScrapBookUIManager | 重新订阅数据并刷新素材栏，沿用缓存，不重复恢复同一布局 |

手账管理器负责已加入手账的照片和排版。它与相册共用照片文件，各自持有缩略图缓存。同为顺序零的相册与手账 UI 不保证谁先运行，数据管理器和画布已先于手账 UI 就绪。

## 物品交互与背包

| 阶段 | 脚本或对象 | 执行内容与结果 |
| --- | --- | --- |
| Awake，顺序负二 | ItemBackpackManager | 建立单例和空物品列表；当前尚未实现背包进度读档 |
| OnEnable，顺序零 | ItemBackpackUIManager | 订阅背包变化和检查结束事件，刷新列表 |
| Awake，顺序零 | InspectSystemManager | 缓存提示引用，隐藏 F 提示与远处圆点 |
| Start，顺序零 | InspectSystemManager | 建立场景中可拾取物品的缓存 |
| 菜单等待，每次 Update | InspectSystemManager | Gameplay 关闭时提前返回，不做物品检测，提示保持隐藏 |
| 菜单等待 | InspectUIManager、ItemBackpackUIManager | 检查面板与背包页签隐藏，IsInspecting 为假，没有检查模型 |
| 游戏中交互或打开背包 | InspectSystemManager、InspectUIManager、ItemBackpackUIManager | 按操作显示提示、检查物品或刷新背包列表 |

InspectUIManager 没有必须提前执行的 Awake 或 Start，进入检查时直接设置当前物品、相机和模型。隐藏待用不代表初始化失败。

## 爷爷日记

| 阶段 | 脚本或对象 | 执行内容与结果 |
| --- | --- | --- |
| 进入 Awake 前 | GrandpaDiaryProgress | 重置本次运行的持有状态缓存，处理日记拾取预览标记 |
| PlayerPanelUIManager.Awake，顺序十 | GrandpaDiaryProgress | 首次查询持有状态时读取存档；新存档默认未持有，旧存档按既定规则迁移 |
| 同一次 Awake | PlayerPanelUIManager、日记页面组件 | 读取 grandpa-diary-content.json，准备页面视图和翻页组件；按持有状态设置日记按钮可见性 |
| Start，顺序十 | PlayerPanelUIManager | 绑定上一页和下一页按钮 |
| 菜单等待 | 日记页签 | 随玩家面板隐藏；不在主菜单领取日记 |
| 游戏中对摩托车按 F | GrandpaDiaryPickup、InspectSystemManager | 获得日记，刷新页签可用性，显示获得通知，同时满足回响系统的日记前置条件 |

grandpaDiaryText 是旧版单文本显示的可选回退入口，当前使用 JSON、页面预制体与页面视图。日记页签隐藏时，已准备的数据仍保留。

## 回响与通知

| 阶段 | 脚本或对象 | 执行内容与结果 |
| --- | --- | --- |
| 进入 Awake 前 | EchoDiscoveryStorage、EchoTrigger | 重置发现记录的内存缓存和活动回响物品注册表 |
| OnEnable | EchoTrigger、EchoSystemManager | 注册场景回响物品，订阅拍摄和首次发现事件 |
| Awake | EchoTargetingUI、EchoPopupUI 两个实例 | 隐藏手持与全屏取景框、回响文字、回响通知和获得日记通知 |
| 菜单等待，每次 LateUpdate | EchoTargetDetector | 拍照状态为 Disabled，IsTakingAim 为假，CurrentTarget 为空，不检测回响 |
| 同期 LateUpdate | EchoTargetingUI | 保持取景框和文字隐藏，没有发现提示动画 |
| 游戏中持有日记并进入拍照模式 | EchoTargetDetector、EchoTargetingUI | 按视角、距离和遮挡条件识别物品，显示取景提示 |
| 有效拍摄并成功保存照片后 | CameraSystemManager、EchoSystemManager | 按既定规则记录发现、解锁便签并显示首次发现通知 |

## 跨系统先后关系

| 必须先就绪 | 随后才执行 |
| --- | --- |
| PlayerInputProvider 创建 Controls | 移动和输入路由绑定动作 |
| ScrapBookDataManager、ItemBackpackManager 建立数据实例 | 手账与背包 UI 读取数据和订阅事件 |
| ScrapBookCanvas 缓存画布 | ScrapBookUIManager 恢复手账布局 |
| 相册、相机调节、照片预览等子 UI 完成初始化 | PlayerPanelUIManager、CameraSystemManager 隐藏面板 |
| 游戏系统完成启动配置 | MainMenuUIManager 接管输入、相机和 HUD |

完整新游戏初始化、Continue、探索记录 Tab 和正式关卡进度仍待开发。

已有修正包括固定上述执行顺序、菜单期间停止 F 检测，以及清理旧 WallClimbTrigger 缺失组件。本次仅按系统重排文档。只读检查脚本保留在本机 Library/OpeningImplementation/AuditMenuStartup.cs，真实 Play 状态仍待核对。
