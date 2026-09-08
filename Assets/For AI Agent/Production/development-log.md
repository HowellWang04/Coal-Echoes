# 开发日志

[查看 Todo List](todolist.md)

[查看代码结构门户](../Technical/code-structure.md)

本日志按日期记录实际完成内容、设计决定、测试结果和下一步。未完成任务仍以 Todo List 为准。

## 2026-08-22

### 截至本日日志建立前的项目基线

#### 角色控制

- 已实现基于 `CharacterController` 的 WASD 移动、加速、减速和重力
- 已实现鼠标水平转向和摄像机垂直俯仰
- 已实现玩家控制开关，供相机、玩家面板和物品检视临时暂停角色控制
- 已实现根据移动与着地状态播放脚步音效

#### 相机拍照

- 已实现 `Disabled`、`Active`、`Capturing` 和 `Reviewing` 四状态相机状态机
- 已实现数字 1 举起和放下相机，以及对应的相机模型动画
- 已实现鼠标左键拍照和关闭拍摄后预览
- 已实现鼠标右键切换相机设置光标模式
- 已实现焦距、景深光圈、暗角、曝光、Bloom、饱和度和构图网格调节
- 已实现帧末屏幕读取、固定 1920×1080 照片输出和 PNG 保存
- 已实现照片文件名 Manifest、照片加载和缩略图缓存
- 已实现拍摄后闪光、淡入和照片预览
- 已实现相册分页、缩略图打开、选择删除、全部删除和右键菜单
- 已实现相机取景范围内物体检测、遮挡确认和高亮材质恢复
- 已实现相机模式、玩家面板和角色控制之间的基本互斥

#### 玩家面板与背包

- 已实现通过 Tab 打开和关闭玩家面板
- 已实现相册、手账、爷爷日记和背包四个页签
- 已实现打开面板时暂停玩家控制并显示光标，关闭时恢复此前状态
- 已实现可检视物品的名称、描述、展示距离、缩放和初始旋转配置
- 已实现远距离圆点提示、近距离 F 键检视提示和最近目标选择
- 已实现检视物体克隆、左键旋转、滚轮缩放和退出检视
- 已实现检视后把场景物品加入背包并禁用原物体
- 已实现背包槽位、独立 RenderTexture 三维预览和从背包重新检视物品
- 已实现持有指定背包物品时按 F 触发场景 UnityEvent

#### 手账系统

- 已实现从相册右键菜单把照片登记到手账
- 已实现照片、贴纸和文本框工具栏分类
- 已实现把照片和装饰从工具栏拖入手账画布
- 已实现画布元素左键移动、右键旋转、滚轮缩放、点击置顶和边界限制
- 已实现文本框双击编辑
- 已实现照片登记列表 JSON 存储
- 已实现画布元素位置、旋转、缩放、类型、来源和文本内容的 JSON 存储与重建
- 已实现带 Echo 照片拖入手账后自动生成只读手账文字

#### Echo 与爷爷日记

- 已实现拍照时确认画面内真正入镜且未被遮挡的 Echo 物体
- 已实现照片文件名与 Echo 手账文字列表的 JSON 映射
- 已实现带 Echo 照片在手账工具栏中的发光提示
- 已实现发现 Echo 后显示统一弹窗并自动隐藏
- 已实现 `EchoTrigger` 的手账文字和日记便签槽 ID 配置
- 已实现拍摄 Echo 后解锁对应爷爷日记便签
- 已实现爷爷日记 JSON 页面数据、文字 marker 和便签槽数据模型
- 已实现日记页面解析、排序、上一页、下一页、页码和本次会话内页面停留
- 已实现按页面 ID 显示爷爷原文和已解锁便签
- 已实现便签悬浮时降低透明度

#### 输入与技术文档

- 已建立 `Gameplay` Action Map 和统一的 `PlayerInputProvider`
- 角色移动、相机、玩家面板、物品检视、道具使用和相册菜单均已迁移到新版 Input System
- 旧版 `PlayerLocomotionInput`、旧生成输入包装类和废弃相机场景已删除
- 已建立代码结构门户和逐文件代码详解文档

### 基线中仍待 Unity 完整验收的部分

- 相册缩略图仍存在正方形框与横向照片比例不一致的问题
- 手账照片登记、布局存档和重建需要完成完整端到端测试
- Echo 照片绑定、手账高亮和手账文字需要完成完整端到端测试
- 爷爷日记页面对象、文字区域、翻页按钮和便签槽仍需检查场景引用并完成翻页测试
- `EchoSystemManager` 当前只提示遍历中遇到的第一个 Echo，多个 Echo 同时入镜的反馈仍待调整
- 回响收集进度、日记字体和后续关卡内容尚未实现

### 本日新增与完成

- 项目自定义运行时代码已完成新版 Input System 迁移
- Active Input Handling 已切换为只使用 Input System Package
- 已测试角色移动、相机输入、玩家面板、物品检视和相册右键菜单
- 代码结构文档已改为门户，并建立逐文件代码详解文档

### 相机功能设计决定

- 照片只保留横向拍摄，不制作横拍与竖拍切换
- 新增手持取景和全屏取景两种显示方式
- 数字 1 举起相机后默认进入手持取景
- 手持取景中相机模型保留在玩家面前，模型屏幕实时显示镜头画面，屏幕外仍能看到真实场景
- 全屏取景复用当前相机模型隐藏和全屏相机 UI 的效果
- 两种取景方式都允许拍照
- 视图切换键确认为 `V`
- 实时屏幕使用低分辨率 `RenderTexture` 和独立预览 Camera，并在相机模型不可见时停止渲染

### 当前状态

- 手持取景与全屏取景的概念和功能边界已经明确
- 视图切换键已经确认为 `V`
- 已确认相机模型只有一个 Mesh Renderer 和一个共用材质，没有可独立赋材质的屏幕 Mesh
- 已在相机模型下建立 `CameraScreen` Quad，并完成屏幕覆盖、方向和独立渲染面确认
- 已新增独立的 `CameraViewMode`，定义 `Handheld` 和 `Fullscreen`，没有改动现有拍照状态机职责
- 已在 Gameplay 新增 `CameraViewToggle`，绑定键盘 `V`，并由 Unity 重新生成 `PlayerControls.cs`
- `CameraSystemManager` 已持有当前取景状态，每次成功进入相机模式前都会重置为 `Handheld`
- 举起动画结束后会保留相机模型，放下动画结束后仍会隐藏模型
- `V` 键已经连接取景切换逻辑，`Handheld` 显示相机模型并隐藏全屏相机 UI，`Fullscreen` 执行相反显示
- 鼠标右键光标模式只会在 `Fullscreen` 中生效，从光标模式返回 `Handheld` 时会自动恢复锁定光标和角色移动
- 已创建 512 × 288 的 `CameraScreenPreview` RenderTexture 和 URP Unlit 的 `CameraScreenPreviewMaterial`，材质 Base Map 已绑定实时预览纹理
- 已创建 `CameraPreview`，输出到 `CameraScreenPreview`，并通过 `CameraPreviewSync` 同步 Main Camera 的位置、旋转、视野角和裁剪距离
- `CameraPreview` 已启用后期处理、移除重复 Audio Listener，并保持 Camera 组件关闭等待运行时控制
- `CameraScreen` 已绑定 `CameraScreenPreviewMaterial`，关闭投射阴影，并随相机模型使用 `CameraModel` Layer
- `CameraPreview` 的 Culling Mask 已排除 `CameraModel`，避免实时屏幕拍到实体相机和自身画面
- Main Camera 保留 `CameraModel` Layer 渲染，`CameraPreview` 继续排除 `CameraModel`
- 实时预览 Camera 现在只在相机模型可见时启用，模型隐藏后停止渲染
- 已停止让相机调节代码修改 `ProjectCoal Global Volume`
- `CameraTweakUIManager` 已拆分为 Preview Volume 和 Fullscreen Volume 两个引用，同一组相机调节会同步写入两份独立 Profile
- `CameraSystemManager` 只在全屏取景时把 Fullscreen Photo Volume 权重设为 1，普通、手持和拍后预览状态设为 0
- `CameraRaiseAnimationHandler` 只在相机模型可见时启用 Handheld Focus Volume，并使用 Main Camera 前向深度把焦点同步到 `focusTarget`，未绑定时回退到实体相机轴心
- CameraPreview 的 Volume Mask 不应包含 Default；否则 `ProjectCoal Global Volume` 会先处理 RenderTexture，再由 Main Camera 对 CameraScreen 进行第二次处理，造成手持屏幕泛灰
- 已新增 `CameraViewTransitionHandler`；手持切全屏时相机模型按可调局部位移、旋转和缩放向镜头推进，同时通过 CanvasGroup 淡入全屏 UI，返回手持时反向播放
- 全屏相机 Volume 的权重切换已延后到取景过渡完成回调；双向动画播放期间维持来源模式的 Volume，避免相机模型尚未退出画面时外部场景提前套用全屏相机效果
- 手持模式拍照时会在帧末读取前临时隐藏 CameraModel、停止 CameraPreview、关闭 Handheld Focus Volume 并启用 Fullscreen Photo Volume；读取结束后立即恢复手持渲染状态，避免最终照片包含实体相机和实时屏幕
- 取景过渡期间会拒绝重复切换、拍照、放下相机和进入光标模式，避免多个流程同时修改相机模型与 UI
- 已新增 `CameraModelInertia`；独立惯性 Pivot 会根据 Main Camera 的旋转变化产生可调俯仰、偏航和滚转拖拽，并按可调时间平滑回正
- `CameraModelInertia` 已扩展待机与移动晃动；使用 Main Camera 的水平位移估算移动强度，在缓慢的手臂待机漂移和随步伐变化的位置、旋转晃动之间平滑切换，不依赖角色控制器接口
- Unity 层级已采用 `CameraRaiseAnim/CameraInertiaPivot/CameraModel`，举起与取景过渡继续修改 CameraModel，惯性和自然晃动只修改独立 Pivot，避免两套动画覆盖同一 Transform

## 2026-08-23

### Unity 验证结果

- 普通探索、手持取景和全屏取景使用各自预期的 Volume，CameraPreview 不再重复叠加 Default Layer 的全局后处理
- 手持与全屏模式均可拍照，最终照片不包含实体相机、实时屏幕、构图网格或相机 UI
- 手持拍照结束后会恢复 CameraModel、CameraPreview 和 Handheld Focus Volume，全屏拍照结束后保持全屏取景状态
- 双向取景过渡、动画结束后切换全屏 Volume、连续按 V、过渡期间输入限制、两种模式放下相机、照片预览、玩家面板和光标恢复均通过运行测试
- 相机模型的旋转惯性、轻微滚转、待机手臂晃动、移动步伐晃动和停止后的平滑回正均通过运行测试
- 关闭相机后 CameraPreview 停止渲染，实时预览性能未发现阻塞后续流程的问题

### 下一步

- 手持与全屏取景功能 C0 至 C18、取景过渡与手持惯性 T1 至 T6 已完成，继续处理 Todo List 中其他未完成项目

## 2026-08-24

### 相册缩略图比例调整

- 根据相册实际画面确认单张缩略图最终显示尺寸采用横向 240×135，Grid Layout Group 使用 4 列和 10×10 间距
- 当前单张照片相对相册整体的视觉大小已经合适，后续边框调整不得缩小该尺寸
- 当前网格高度最多完整容纳 4 行，因此 `CameraPhotoAlbumUIManager.Page Size` 需要从 40 调整为 16
- 已移除相册内的 Echo 高亮代码与引用。回响照片在相册中不再有额外视觉标识
- 缩略图 Prefab 根节点已统一为横向 240×135。后续在 Prefab 中复用拍照预览的十六比九边框资源
- 已生成 `Photo_Album_Thumbnail_Frame_16x9_Overlay_Rounded.png`。它是较干净的暖象牙圆角相纸边框，中心窗口与外部区域均为透明 alpha，窗口比例精确为十六比九
- Unity 编译与运行验证待进行，重点检查边框、右键菜单、删除模式、分页和再次左键关闭预览

## 2026-08-25

### 相册大图预览关闭按钮移除

- 已恢复 `CameraPhotoAlbumUIManager` 的删除模式 Toggle、选择删除和全部删除引用及逻辑
- 已恢复 `PhotoThumbItem` 的缩略图选择 Toggle 引用及逻辑
- 已移除 `CameraPhotoPreviewUIManager` 的独立关闭按钮引用和显示控制
- 相册缩略图继续使用原有左键预览流程，再次左键同一张缩略图会关闭大图预览

## 2026-08-27

### 相机调节 UI 视觉统一

- 已确认 Camera Tab 的 Focal Length 与 Aperture Slider 使用完全一致的三段样式
- Background 使用 `SliderBG_0` 与米灰色，Fill 使用 `InputFieldBackground` 与暖棕色，Knob 使用 `Knob` 与白色
- 已把相同的 Source Image 与颜色复制到 Post Processing Tab 的 Vignette、Exposure、Bloom 和 Saturation Slider，共核验 12 个 Image 组件
- 本次只修改 Background、Fill 和 Knob 的 Sprite 与颜色，没有修改 Slider 数值范围、事件、导航或布局

### 相册缩略图存储与内存策略审计

- 当前照片在拍摄后统一为 1920×1080 RGB24，并使用无损 PNG 保存到 `Application.persistentDataPath/CapturedPhotos`
- 相册读取完整 PNG 后按最长边 256 像素生成 256×144 RGB24 内存缩略图，不单独保存缩略图文件
- 当前实测 16 张照片全部为 1920×1080，总体积约 25.09 MiB，平均每张约 1.57 MiB
- `PrewarmThumbnails()` 会逐帧预热并缓存全部照片，而不是只缓存当前页；照片数量较多时，启动解码和内存占用会随总照片数增长
- 大图预览会重新加载完整 PNG 并创建 Sprite，目前关闭或替换预览时没有主动销毁旧纹理与 Sprite；这是后续比缩略图尺寸更优先的内存优化点
- 本次只完成静态审计和实际照片文件统计，没有修改照片保存、压缩或缓存代码

### 相册大图预览边框统一

- 已确认旧大图预览仍使用 800×880 的竖向 `Photo FrameBG`，内部 `Photo Holder Mask` 为 720×720，会以方形区域裁切 1280×720 的照片
- 已把大图预览根框调整为横向 1280×720，把遮罩、黑色背景和照片显示区域统一为 1152×648
- 已在大图预览最上层添加 `PhotoFrameOverlay`，复用缩略图当前使用的 `Photo_Album_Thumbnail_Frame_16x9_Overlay_Rounded.png`
- 新覆盖边框保持白色、Simple Image 和关闭 Raycast Target；旧根背景改为透明但保留原有点击射线，避免破坏再次点击关闭预览的交互
- 没有修改 `CameraPhotoPreviewUIManager` 的预览逻辑、动画或序列化引用
- 本次创建的 Slider 与大图边框审计和应用脚本、对应 Meta、临时报告及空的 `Assets/Editor` 目录均已清理，项目中没有相关临时代码残留

### 待 Unity 保存与运行验证

- 保存 `Main Game Scene`，确认 Post Processing Tab 四个 Slider 的 Background、Fill 和 Knob 与 Camera Tab 视觉一致
- 从相册点击新旧照片，确认大图显示完整十六比九画面、圆角边框与缩略图一致且没有拉伸或方形裁切
- 再次点击同一缩略图确认可以关闭大图预览，并检查淡入动画、右键菜单、删除模式和分页未受影响

## 2026-08-29

### UIEffect 接入与照片删除反馈替换

- 已在 Package Manifest 中固定 UIEffect 5.11.7
- `PhotoThumbItem` 删除动画改为由 DOTween 驱动子级 UIEffect 的 Transition Rate
- 原有照片文件删除、缩略图缓存清理、批量延迟和删除后重排逻辑保持不变
- 缩略图 Prefab 的 `PhotoImage` 已配置 UIEffect `Transition-Dissolve` 预设，默认 Transition Rate 设为 0
- `PhotoFrameOverlay` 已配置 UIEffectReplica，并以 `PhotoImage` 的 UIEffect 作为 Target，使照片与相框共用消散参数
- Inspector 配置已保存；Play Mode 中实际删除动画、照片与相框同步消散及删除后重排已确认正常
