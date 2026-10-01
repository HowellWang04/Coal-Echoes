# 代码结构

本文档是项目代码文档门户，用于快速定位系统、源码和详细说明。

- [查看全部代码详解](code-details.md)
- [查看开发说明](dev-guidelines.md)

## 技术栈

- 引擎：Unity HDRP（2026年9月由 URP 迁移，详见 [HDRP 迁移记录](hdrp-migration.md)）
- 地形系统：MapMagic
- 摄像机系统：Unity 原生 Camera，第一人称视角由 PlayeMovementManager 直接控制（Cinemachine 包已安装但场景未使用）
- 输入系统：Unity Input System，项目自定义运行时代码已完成迁移

## 源码结构

项目自定义源码全部位于 `Assets/Scripts/`。每个文件的详细说明见 [代码详解](code-details.md)，以文件名为标题检索。标注“详解待补”的文件尚未写入代码详解。

```
Assets/Scripts/
├── In-Game Systems/
│   ├── Camera System/                     拍照系统
│   │   ├── CameraSystemManager.cs         拍照系统总入口
│   │   ├── CameraStateMachine.cs          相机状态转换规则
│   │   ├── CameraModeState.cs             相机模式状态定义
│   │   ├── CameraViewMode.cs              手持与全屏取景模式定义
│   │   ├── CameraViewTransitionHandler.cs 双向取景过渡动画
│   │   ├── CameraRaiseAnimationHandler.cs 相机举起与放下动画，手持景深
│   │   ├── CameraModelInertia.cs          手持相机模型惯性与自然晃动
│   │   ├── CameraPreviewSync.cs           实时预览 Camera 镜头同步
│   │   ├── CameraTweakUIManager.cs        相机参数与 HDRP 后处理 UI
│   │   ├── CameraCapture.cs               固定尺寸照片截图
│   │   ├── ScreenReadBackCaptureProvider.cs 原始屏幕尺寸截图实现
│   │   ├── CameraHighlightObject.cs       可拍摄物体数据组件
│   │   ├── CameraHighlightObjectDetector.cs 可见物体检测与高亮
│   │   ├── HighlightShader.shader         高亮扫光 Shader（HDRP ForwardOnly）
│   │   ├── CameraPhotoStorage.cs          照片和清单持久化
│   │   ├── CameraPhotoAlbumUIManager.cs   相册分页、缓存、预览和批量删除
│   │   ├── CameraPhotoThumbItem.cs        相册缩略图条目
│   │   ├── CameraPhotoPreviewUIManager.cs 照片大图预览
│   │   └── AlbumPhotoContextMenu.cs       相册右键菜单
│   ├── Character Movement System/
│   │   ├── Scripts/
│   │   │   ├── PlayeMovementManager.cs    角色移动、重力和第一人称视角
│   │   │   └── PlayerFootstepAudio.cs     脚步音效控制
│   │   └── Audio/                         蹲起、跳跃、落地、脚步音效
│   ├── Input System/
│   │   ├── PlayerControls.inputactions    输入动作源文件
│   │   ├── PlayerControls.cs              自动生成的输入包装类，不要手改
│   │   ├── PlayerInputProvider.cs         统一输入实例持有者
│   │   ├── PhotoModeInputRouter.cs        拍照输入路由
│   │   └── PlayerPanelInputRouter.cs      玩家面板输入路由
│   ├── Inspect Object System/             F 键检视物品
│   │   ├── InspectSystemManager.cs        目标检测和交互提示
│   │   ├── InspectUIManager.cs            检视界面与物体克隆
│   │   ├── InspectRotator.cs              检视物体旋转与缩放
│   │   └── InspectableItem.cs             可检视物品数据
│   ├── Echo System/                       回响系统
│   │   ├── EchoSystemManager.cs           Echo 拍照事件监听
│   │   ├── EchoTrigger.cs                 Echo 场景数据组件，挂在 B 类物品上
│   │   ├── EchoPhotoStorage.cs            照片与 Echo 文本映射
│   │   ├── EchoPopupUI.cs                 Echo 发现提示
│   │   └── ItemUseTrigger.cs              背包道具使用触发区
│   ├── Scrap Book System/                 手账
│   │   ├── ScrapBookUIManager.cs          手账工具栏和布局协调
│   │   ├── ScrapBookCanvas.cs             手账元素创建与重建
│   │   ├── ScrapBookDataManager.cs        手账数据与布局持久化
│   │   ├── ScrapBookDraggable.cs          工具栏拖入画布
│   │   ├── ScrapBookElement.cs            画布元素交互
│   │   ├── ScrapBookElementMeta.cs        元素存档标识
│   │   ├── ScrapBookTextBox.cs            手账文本框编辑
│   │   ├── ScrapBookDecoPreset.cs         手账装饰预设
│   │   └── ScrapBookPhotoVisual.cs        复用相册缩略图 Prefab 显示手账照片（详解待补）
│   └── Game System/
│       └── GameFlowManager.cs             初始光标状态控制
├── Player Panel/
│   ├── PlayerPanelUIManager.cs            玩家面板和页签总控
│   ├── GrandpaDiary/                      爷爷日记
│   │   ├── GrandpaDiaryJsonParser.cs      日记 JSON 解析
│   │   ├── GrandpaDiaryPageData.cs        日记数据模型
│   │   ├── GrandpaDiaryPageView.cs        日记页面数据绑定
│   │   ├── GrandpaDiaryTextAreaBinding.cs 文字标记与 TMP 绑定
│   │   ├── GrandpaDiaryStickyNoteSlot.cs  日记便签显示槽
│   │   ├── GrandpaDiaryStickyNoteStorage.cs 便签解锁持久化
│   │   ├── GrandpaDiaryStickyNoteStyleSet.cs 便签样式集 ScriptableObject（详解待补）
│   │   ├── GrandpaDiaryPageTurnTransition.cs 翻页过渡动画（详解待补）
│   │   └── GrandpaDiaryPageReferenceLayout.cs 日记页固定 1224×756 参考尺寸（详解待补）
│   ├── ItemBackpack/                      背包
│   │   ├── ItemBackpackManager.cs         背包数据管理
│   │   ├── ItemBackpackSlot.cs            背包槽位与三维预览
│   │   └── ItemBackpackUIManager.cs       背包列表 UI
│   └── Animation/                         照片显示区淡入动画
├── UI Animation/                          通用 UI 动效（详解待补）
│   ├── UIAnimationProfile.cs              动效参数 ScriptableObject
│   ├── UIAnimationScope.cs                为范围内按钮、页签、面板注册动效
│   ├── UIButtonTweenFeedback.cs           按钮悬停、按下、选中反馈
│   ├── UITabTransition.cs                 页签显示与隐藏过渡
│   ├── UIPanelSlideTransition.cs          面板滑入过渡
│   ├── ScrapbookFlyFeedback.cs            照片飞入手账的反馈动画
│   └── Editor/
│       └── GrandpaDiaryD2Setup.cs         编辑器一次性脚本，配置日记 p02 页左右背景
└── Terrain/
    └── MapMagicTerrainSeamWeld.cs         MapMagic 地形接缝焊接（详解待补）
```
