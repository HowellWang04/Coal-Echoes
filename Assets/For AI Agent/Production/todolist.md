# Todo List

[查看开发日志](development-log.md)

## 2026-08-31

### 爷爷日记动态页面 Prefab 与随机便签样式计划

> 目标：日记页面只配置一个共享 Prefab，运行时按 JSON 自动生成跨页；每个跨页左页和右页各最多一个便签；有便签的页侧首次显示时从四种美术样式中随机选择一种，并按 `slotId` 固定保存；文字位置、尺寸和倾斜角度必须与被选中的便签美术匹配。

#### Step P0 — 锁定页面与便签规则

AI 工作

- [x] 确认一个 JSON `page` 对应一个摊开的左右跨页，而不是一张单页
- [x] 确认每个跨页最多包含一个左页便签和一个右页便签，两侧便签均为可选
- [x] 确认四张便签资源均为 1024×1024、带 Alpha 的正方形画布，但纸张倾斜、阴影和卷角方向不同
- [x] 确认不能只随机替换 `Image.sprite`；每种样式必须同时应用独立的文字位置、尺寸、安全边距和 Z 轴角度
- [x] 确认随机结果以唯一 `slotId` 为键保存；同一便签再次打开不重新抽取，新 `slotId` 第一次出现时才抽取

用户工作

- [x] 确认采用单一动态页面 Prefab，而不是继续逐页复制并配置场景对象
- [x] 确认 Prefab 中同时预留 `StickyNote_Left` 和 `StickyNote_Right`
- [ ] 后续为每个 JSON 便签明确填写 `left` 或 `right`，避免只有一个便签时依赖数组顺序猜测页侧

#### Step P1 — 完成动态页面生成基础代码

AI 工作

- [x] 为 `PlayerPanelUIManager` 增加共享页面 Prefab 和 `DiaryPagesRoot` 引用
- [x] 在存在 Prefab 引用时按 JSON 页面数量生成运行时页面，并为实例写入对应 `pageId`
- [x] 保留现有场景页面作为迁移期回退，Prefab 尚未绑定时不破坏当前日记
- [x] 让共享 Prefab 可以按数组顺序接收两块正文和便签数据，不再要求 Prefab 写死 `p01` 标记
- [x] 为便签数据增加明确的页侧字段，并让页面视图按 `left` 或 `right` 选择对应槽位
- [x] 完成代码静态检查，并由用户确认 Unity Console 无新增编译错误

用户工作

- [x] Unity 完成脚本重新编译，确认没有新增红色 Console 错误
- [ ] 迁移完成前保留原有 `DiaryPage_p01`、`DiaryPage_p02` 和 `DiaryPage_p03`，不提前删除

#### Step P2 — 建立共享页面 Prefab

AI 工作

- [x] 按根节点、左右半页、背景、正文和便签槽的顺序逐个核对 Inspector 截图
- [x] 对每个对象给出唯一一套明确参数，发现旧页面专用字段时指出应保留、清空或替换的内容
- [x] 确认左右半页 Pivot 均朝向书脊，并确认 `RectMask2D`、背景延伸和翻页缩放方向正确

用户工作

- [x] 从已能翻页的 p01 复制并创建 `GrandpaDiaryPageTemplate` Prefab，原场景对象保持不变
- [x] Prefab 根节点横纵全拉伸，四边为 0，Pivot 为 0.5、0.5，Scale 为 1
- [x] `LeftPage` 使用左半区锚点、Pivot X 为 1，并挂载 `RectMask2D`
- [x] `RightPage` 使用右半区锚点、Pivot X 为 0，并挂载 `RectMask2D`
- [x] `PageBG_Left` 和 `PageBG_Right` 使用同一张完整跨页背景，通过遮罩显示左右半页，Scale 保持 1
- [x] `Text_Left` 和 `Text_Right` 分别位于对应半页，并按左后右的顺序绑定到 `Text Areas`
- [x] 在 `LeftPage` 下建立 `StickyNote_Left`，在 `RightPage` 下建立 `StickyNote_Right`
- [x] `Sticky Note Slots` 数量设置为 2，元素 0 绑定左槽，元素 1 绑定右槽
- [x] 完成 Prefab 层级和根组件截图确认后再进入便签样式配置

#### Step P3 — 建立四种共享便签样式配置

AI 工作

- [x] 新增最小的共享便签样式配置资源，每个样式只保存当前需求需要的数据
- [x] 每个样式保存 Sprite、文字 Anchored Position、文字 Width、文字 Height、文字 Z Rotation 和安全边距
- [x] 让 `StickyNote_Left` 与 `StickyNote_Right` 共用同一份样式配置，不在两个槽位重复维护四套数值
- [x] 修改 `GrandpaDiaryStickyNoteSlot`，显示时同时应用图片和该图片对应的文字 Rect Transform 参数
- [x] 保持现有悬浮透明效果，随机图片和文字布局不得改变便签解锁逻辑

用户工作

- [ ] 检查 `StickyNote_01_Flat`、`StickyNote_02_CurlLeft`、`StickyNote_03_CurlRight`、`StickyNote_04_Flat` 的导入设置
- [ ] 四张图片均使用 Sprite 2D and UI、Single、Alpha Is Transparency 开启、Mip Maps 关闭
- [ ] 创建共享样式配置资源并按固定顺序拖入四张 Sprite
- [ ] 把同一个样式配置资源绑定到左右两个便签槽

#### Step P4 — 逐张校准文字安全区域与角度

AI 工作

- [ ] 根据每张便签的纸面上边缘、透明边界和卷角位置给出第一版文字区域与 Z Rotation 参数
- [ ] 为左卷角样式缩小左下文字区域，为右卷角样式缩小右下文字区域
- [ ] 为两张 Flat 样式分别匹配各自的顺时针或逆时针倾斜，不把两者当成同一角度
- [ ] 核对最长现有便签内容，保证自动字号不会低于最终可读下限

用户工作

- [ ] 在 Prefab 或独立预览场景中依次预览四种样式，不一次只看默认 Flat 样式
- [ ] 每种样式使用同一段最长测试文字，检查文字基线与纸面方向一致
- [ ] 确认文字不进入阴影、透明区、左下卷角或右下卷角
- [ ] TMP 开启自动字号并设置可读的最小值与最大值，保留正常换行
- [ ] 分别截图四种样式，由 AI 根据实际 Game View 结果继续微调

#### Step P5 — 实现随机分配与固定保存

AI 工作

- [ ] 扩展现有爷爷日记便签存储，在同一份存档中记录 `slotId` 对应的样式索引
- [ ] 已有样式索引时直接读取，不再次调用随机选择
- [ ] 新 `slotId` 第一次显示时只随机一次，立即写入存档
- [ ] 样式数量变化时只处理超出范围的旧索引，不影响正常已有分配
- [ ] 便签尚未解锁时不必提前生成样式分配，首次实际显示时再决定

用户工作

- [ ] 确保所有 JSON 便签使用永久唯一的 `slotId`，后续不要随意重命名已有 ID
- [ ] 为每条便签填写页侧字段和便签内容
- [ ] 如需重新测试随机结果，先由 AI 指出具体便签样式存档位置，再手动备份或删除测试存档

#### Step P6 — 绑定 Prefab 并完成运行验证

AI 工作

- [ ] 静态核对 Prefab 引用、JSON 字段、页侧映射、随机存档和翻页代码之间的完整链路
- [ ] 根据 Console 或 Play Mode 截图修复明确出现的问题，不顺手重构其他 UI 系统
- [ ] 运行验证通过后移除迁移期回退代码和不再使用的逐页场景引用

用户工作

- [ ] 在 `PlayerPanelUIManager` 中绑定 `GrandpaDiaryPageTemplate` 和 `DiaryPagesRoot`
- [ ] 进入 Play Mode，确认运行时按 JSON 自动生成 p01、p02 和 p03
- [ ] 测试 p01 到 p02、p02 到 p03以及反向翻页，确认正文和便签随纸页一起运动
- [ ] 解锁一个左页便签和一个右页便签，确认各自显示在正确页侧
- [ ] 记录首次分配到的便签样式，关闭并重新进入 Play Mode，确认样式没有改变
- [ ] 临时增加一个使用新 `slotId` 的测试便签，确认它会获得新的随机样式并在再次打开时保持
- [ ] 检查四种样式的悬浮透明效果、文字完整性、遮罩裁切和页边越界
- [ ] 所有验证完成后删除旧的逐页场景对象，并再次完整测试

#### Step P7 — 文档同步与最终收尾

AI 工作

- [ ] 只在用户确认 Unity Console 和 Play Mode 结果后更新开发日志
- [ ] 更新代码结构与代码详解，记录动态 Prefab、页侧字段、样式配置和随机存档职责
- [ ] 清理 Todo 中已经被动态 Prefab 方案取代的逐页手工配置说明

用户工作

- [ ] 确认最终页面和便签视觉效果通过
- [ ] 确认是否保留测试用 p02、p03 便签文字，或等待正式叙事内容后再替换

## 2026-08-30

### 爷爷日记 UIEffect 上一页与下一页翻动及背景重绘计划

- [x] Step D0 — 核对现有技术与美术基线
  - [x] 当前日记背景为 `Assets/Art Resources/UI/Player Panel/Grandpa Diary/Grandpa_Diary_BG.png`，实际尺寸为 612×408，文件具有真实 Alpha 通道，但像素量不足以作为最终 Tab 点对点背景
  - [x] 同目录竖向参考图实际尺寸为 1122×1402，文件没有 Alpha 通道，画面里的棋盘格已经烘焙进 RGB，不能直接作为透明素材
  - [x] UIEffect 5.11.7 可以提供渐变明暗、边缘、阴影和遮罩过渡，但 `Flip` 只会镜像纹理，不会生成真实纸张弯曲
  - [x] 初步技术方向采用 UIEffect 表现翻页表面的光影变化，DOTween 控制半页收拢与展开；不新增第三个动画包
  - [x] 保留 `PlayerPanelUIManager.ShowGrandpaDiaryPage()`、JSON 内容绑定、页码、按钮状态和便签存储，只在页码切换前后插入视觉过渡

- [x] Step D1 — 在绘制前锁定最终像素尺寸
  - [x] 目标 Game View 为 1920×1080；Canvas Scaler 参考分辨率同为 1920×1080并按高度适配，目标画面下一个 UI 单位对应一个实际像素
  - [x] `TabsContainer` 为 1224×756；最终完整背景与容器点对点对应为 1224×756，正式资源替换时将 `PageBG` 缩放统一恢复为 1
  - [x] 每个 `DiaryPage_pXX` 代表摊开的左右两页，也就是一个跨页；完整背景中缝固定在 X 612，左右半页各为 612×756
  - [x] 正文和便签属于跨页内容层，不影响背景对半切分；后续页面只需沿用同尺寸 `DiaryPage` 模板，不需要逐页检查 Inspector
  - [x] 最终 PNG 与确认后的日记本显示区域点对点对应；D1 只完成测量与约束记录，没有开始生成或重绘

- [ ] Step D2 — 先用旧背景验证是否需要拆分
  - [ ] 使用当前低分辨率背景建立临时原型，不修改或覆盖原始 PNG
  - [ ] 将完整跨页背景沿中缝直接分成左右两半；下一页从右向左翻，上一页从左向右翻
  - [ ] 翻页时让对应半页的背景、正文和便签作为同一内容组运动，不为每个 TMP 或便签分别配置动画
  - [ ] 保留完整背景母版，通过 Sprite 切片或 UI Mask 非破坏性取得左右半页，不覆盖原始 PNG
  - [ ] 验收中缝对齐、翻页方向清楚、左右半页没有拉伸或接缝，并确认未来新增跨页可直接复用同一模板

- [ ] Step D3 — 原型确认后再重绘高清美术资源
  - [ ] 完整背景严格延续当前 `Grandpa_Diary_BG.png` 的正面摊开旧日记风格，包括暖象牙纸张、轻微泛黄与污迹、不规则纸边、中央装订缝和两侧页叠
  - [ ] 不添加文字、便签、图标或额外装饰，保留当前正文与便签系统使用的干净纸面和安全区
  - [ ] 透明区域必须写入真实 Alpha 通道，不使用棋盘格或纯色假透明背景
  - [ ] 生成阶段必须从目标尺寸或更高的原生像素量开始，只允许向下缩放或裁切到最终尺寸，不允许把现有 612×408 图片放大冒充高清图
  - [ ] 交付一张 1224×756 的完整高清母版，再从同一母版沿 X 612 统一切出两个 612×756 半页，不分别生成左右页，避免中缝和纸张风格不一致
  - [ ] 导入前检查 PNG 实际像素宽高、Alpha 像素格式、四角透明度和百分之一百缩放下的纸边清晰度

- [ ] Step D4 — 建立最小翻页 UI 层级与视觉参数
  - [ ] 在日记 Tab 内区分静态书本底图、当前页内容、目标页内容和可翻纸页，静态中缝与页叠不参与翻动
  - [ ] 可翻纸页的 Pivot 放在书脊侧，使用 DOTween 做横向收拢、过中点换页、反向展开
  - [ ] 在可翻纸页 Graphic 上使用 UIEffect 的斜向高光或渐变阴影表现纸面转向，不把照片删除使用的 Dissolve 预设直接复用到日记翻页
  - [ ] 原型阶段只保留一套共享时长、Ease、阴影和高光参数，确认手感后再写入 `UIAnimationProfile`

- [ ] Step D5 — 接入现有日记换页逻辑
  - [ ] 新增一个最小的 `GrandpaDiaryPageTurnTransition`，只负责翻页方向、动画进度和中点回调，不接管日记内容或存档
  - [ ] 上一页与下一页按钮先请求对应方向的翻页动画，在动画中点调用现有 `ShowGrandpaDiaryPage()` 刷新内容
  - [ ] 动画期间暂时阻止重复翻页，使用不受 `Time.timeScale` 影响的更新方式，并在 Tab 关闭或组件禁用时恢复稳定页面状态
  - [ ] 保持第一页与最后一页按钮限制、会话内当前页索引、JSON 文本块和已解锁便签行为不变

- [ ] Step D6 — Unity 验证与文档同步
  - [ ] 测试下一页右向左、上一页左向右、第一页、最后一页、快速连点和动画中关闭玩家面板
  - [ ] 确认正文、日期、页码和便签与目标页同步，没有闪帧、重叠、镜像文字或旧页残留
  - [ ] 在目标分辨率和至少一个不同宽高比下检查背景点对点清晰度、Alpha 边缘、中缝对齐和 UIEffect 材质表现
  - [ ] 完成后同步 `code-details.md`、Todo List 和开发日志，并记录最终资源尺寸、层级引用和 Play Mode 结果

## 2026-08-27

### 项目级 DOTween UI 动画系统

- [x] Step U0 — 安装并初始化 DOTween
  - [x] Step U0.1 — 从 Unity Asset Store 获取并导入 Demigiant 官方免费版 `DOTween (HOTween v2)`，不安装 DOTween Pro
  - [x] Step U0.2 — 打开 `Tools/Demigiant/DOTween Utility Panel`，执行 `Setup DOTween...`；启用 `Physics`、`Sprites`、`UI` Modules
  - [x] Step U0.3 — Unity Console 无 DOTween 安装或编译错误；实际导入路径为 `Assets/Plugins/Demigiant/DOTween`，Utility Panel 显示 `DOTween v1.3.030 [Release build]`，Asset Store 包页面显示 `1.2.825`

- [ ] Step U1 — 建立项目级 UI 动画规则与共享参数
  - [x] Step U1.1 — 新建一个最小的 `UIAnimationProfile`，集中保存 Hover、Press、Tab 和飞入动画的时长、缩放与 Ease；统一入口采用 Canvas 层级范围内的 `UIAnimationScope`，不建立全局单例
  - [ ] Step U1.2 — 所有 UI Tween 使用不受 `Time.timeScale` 影响的更新方式，保证玩家面板暂停或减速时动画仍能播放
  - [ ] Step U1.3 — 统一处理重复输入与生命周期；新 Tween 开始前终止同目标旧 Tween，组件禁用或销毁时清理并恢复稳定状态
  - [ ] Step U1.4 — 视觉方向保持克制、快速和柔和；避免夸张弹跳、长时间循环或影响 Project Coal 纸张与手账气质的高饱和反馈

- [ ] Step U2 — 实现可复用按钮 Hover、Press 与 Select 反馈
  - [x] Step U2.1 — 新增 `UIButtonTweenFeedback`，默认动画 Button 自身 RectTransform，并允许特殊按钮覆盖可选 Visual Root
  - [x] Step U2.2 — Hover 使用轻微放大或抬升，Press 使用快速压缩，释放后根据 Hover、Selected 和 Normal 状态恢复正确尺寸
  - [x] Step U2.3 — 支持鼠标 Pointer Enter、Exit、Down、Up，并兼容键盘或手柄 Select、Deselect 状态
  - [ ] Step U2.4 — 在 UI 根节点配置一次 `UIAnimationScope`，自动覆盖包含隐藏对象在内的子级 Button；先验证 Album，再验证 Scrapbook、Grandpa Diary 和 Backpack
  - [ ] Step U2.5 — 检查快速连点、指针移出时释放、按钮 Disabled、Tab 已选中状态和面板关闭后重新打开

- [x] Step U3 — 实现玩家面板 Tab 过渡
  - [x] Step U3.1 — 新增独立的 `UITabTransition`，使用 CanvasGroup 与 RectTransform 组合短距离位移和淡入淡出
  - [x] Step U3.2 — 离场 Tab 在过渡期间立即停止交互，动画完成后再隐藏；入场 Tab 在可见后才恢复交互
  - [x] Step U3.3 — 在不改变现有 Tab 内容逻辑的前提下，把 `PlayerPanelUIManager` 的即时 `SetActive` 切换接入过渡组件
  - [x] Step U3.4 — 逐一验证 Album、Scrapbook、Grandpa Diary 和 Backpack 双向切换，避免内容重叠、闪帧和连续点击状态错乱
  - [x] Step U3.5 — 玩家面板稳定后，再评估是否把同一过渡模式应用到 Camera Tweak UI 的 Camera 与 Post Processing Tab

- [ ] Step U3A — 实现 Player Panel 整体切入动画
  - [x] Step U3A.1 — 新增独立的 `UIPanelSlideTransition`，每次打开时让 Sidebar Button 组从屏幕左侧滑入，同时让 Tab 内容组从屏幕右侧滑入
  - [x] Step U3A.2 — 在共享 `UIAnimationProfile` 中集中配置滑入时长、初始透明度与 Ease，不给四个 Sidebar Button 分别添加组件
  - [ ] Step U3A.3 — Unity 验证首次和重复打开、快速关闭重开、四个 Sidebar Button 层级及动画期间交互状态

- [ ] Step U4 — 实现照片 Add to Scrapbook 飞入动画
  - [x] Step U4.1 — 让 `ScrapBookDataManager.RegisterPhoto` 返回是否首次登记成功，数据登记与保存先完成，动画只作为成功反馈
  - [x] Step U4.2 — `PhotoThumbItem` 打开右键菜单时传递照片纹理与缩略图 RectTransform，不使用名称查找源对象
  - [x] Step U4.3 — 在 Player Panel 顶层 Canvas 建立不受相册遮罩裁切的动画层，并显式绑定 Scrapbook Tab 按钮 RectTransform 作为终点
  - [x] Step U4.4 — 新增 `ScrapbookFlyFeedback`，复用一个 RawImage 视觉替身，组合曲线路径、缩小、末段淡出和 Scrapbook 图标轻微回弹
  - [x] Step U4.5 — 菜单立即关闭且登记不等待动画完成；重复照片不播放成功动画，动画中断或面板关闭不能影响已保存数据
  - [ ] Step U4.6 — 测试不同缩略图位置、不同分页、不同分辨率、快速重复操作和相册关闭过程

- [ ] Step U5 — 集成验证、性能检查与文档同步
  - [ ] Step U5.1 — 检查场景中没有悬挂 Tween、隐藏对象仍在拦截射线、动画结束后缩放未恢复或重复创建临时 UI 对象
  - [ ] Step U5.2 — 验证相册点击、右键菜单、删除模式、再次左键关闭预览、手账登记和玩家控制恢复未受影响
  - [ ] Step U5.3 — 在目标分辨率下统一微调动画时长、Ease、缩放幅度和位移距离，避免每个按钮单独形成不同手感
  - [ ] Step U5.4 — 更新 `code-structure.md`、`code-details.md`、Todo List 和开发日志，记录 DOTween 版本、公共组件入口、Inspector 绑定与运行测试结果

## 2026-08-22

### 手持取景与全屏取景切换

- [x] Step C0 — 视图切换键确认为 `V`
- [x] Step C1 — 检查相机模型层级，确认模型只有一个 Mesh Renderer 和一个共用材质，没有独立屏幕 Mesh
- [x] Step C2 — 已在相机模型下建立 `CameraScreen` Quad，完成屏幕覆盖、正面方向和独立渲染面确认
- [x] Step C3 — 新增 `CameraViewMode`，只定义 `Handheld` 和 `Fullscreen`，不改动现有拍照状态机职责
- [x] Step C4 — 在 `PlayerControls.inputactions` 新增 `CameraViewToggle`，绑定 `V` 并重新生成包装类
- [x] Step C5 — 修改举起流程，数字 1 举起相机后默认进入 `Handheld`
- [x] Step C6 — 修改 `CameraRaiseAnimationHandler`，举起动画结束后在手持取景中保留相机模型
- [x] Step C7 — 实现视图切换；手持取景显示相机模型并隐藏全屏相机 UI，全屏取景隐藏相机模型并显示现有相机 UI
- [x] Step C8 — 限制鼠标右键光标模式只在 `Fullscreen` 中生效
- [x] Step C9 — 创建低分辨率 `RenderTexture` 和实时预览材质
- [x] Step C10 — 创建或配置预览 Camera，使其位置、旋转、视野角和拍照镜头保持一致
- [x] Step C11 — 把实时预览材质绑定到相机模型屏幕，并排除相机模型所在 Layer，避免屏幕拍到自身
- [x] Step C12 — 只在相机模型可见时启用实时预览 Camera，模型隐藏后停止渲染；相机后期已拆分为 Preview、Fullscreen 和 Handheld Focus 三个独立 Volume
  - [x] Step C12.1 — 创建 `CameraPreviewPostProcessing`、`FullscreenPhotoPostProcessing` 和 `HandheldFocusPostProcessing` 三个 Layer
  - [x] Step C12.2 — 创建三个 Global Volume 和三个互不共享的 Profile；Preview 与 Fullscreen Profile 包含相同的 Depth Of Field、Vignette、Color Adjustments 和 Bloom，Handheld Focus Profile 只包含 Bokeh Depth Of Field
  - [x] Step C12.3 — Main Camera 的 Volume Mask 包含 Default、FullscreenPhotoPostProcessing 和 HandheldFocusPostProcessing；CameraPreview 的 Volume Mask 只包含 CameraPreviewPostProcessing，避免 ProjectCoal Global Volume 对 RenderTexture 和 CameraScreen 连续处理两次
  - [x] Step C12.4 — `CameraTweakUIManager` 绑定 Preview Volume 和 Fullscreen Volume，`CameraSystemManager` 绑定 Fullscreen Photo Volume
  - [x] Step C12.5 — `CameraRaiseAnimationHandler` 绑定 Handheld Focus Volume、CameraPreview 和 `focusTarget`
  - [x] Step C12.6 — Unity 验证普通探索不受相机调节影响，手持屏幕与全屏取景效果一致，手持外部画面只增加实体相机聚焦
- [x] Step C13 — 确认鼠标左键在 `Handheld` 和 `Fullscreen` 中都能拍照
- [x] Step C14 — 手持拍照帧临时隐藏相机模型、停止实时预览与手持近焦并启用 Fullscreen Photo Volume；Unity 已确认最终照片不包含相机模型、实时屏幕和相机 UI
- [x] Step C15 — 确认从两种视图按数字 1 都能正确播放放下动画并返回普通探索
- [x] Step C16 — 测试连续切换、拍照预览、退出预览、玩家面板限制和光标状态恢复
- [x] Step C17 — 测试实时预览性能，并确认关闭相机后没有继续渲染
- [x] Step C18 — 已同步更新 `code-structure.md`、`code-details.md` 和开发日志

### 取景过渡与手持惯性

- [x] Step T1 — 新增 `CameraViewTransitionHandler`，手持切全屏时相机模型向镜头推进并放大、全屏 UI 淡入，返回手持时反向播放；全屏相机 Volume 在动画完成后才切换
- [x] Step T2 — 过渡期间阻止重复切换、拍照、放下相机和进入光标模式
- [x] Step T3 — 新增 `CameraModelInertia`，根据 Main Camera 旋转增量产生俯仰、偏航和滚转惯性，并根据相机水平位移在待机轻微晃动与移动步伐晃动之间平滑切换
- [x] Step T4 — 在 Camera UI Root 添加 CanvasGroup，创建并绑定 `CameraViewTransitionHandler`，微调过渡位移、旋转、缩放、时长和曲线
- [x] Step T5 — 在 `CameraRaiseAnim` 与 `CameraModel` 之间创建独立 `CameraInertiaPivot`，绑定 Main Camera 和惯性目标并完成基础旋转惯性参数配置
- [x] Step T6 — Unity 测试双向过渡、全屏 Volume 只在动画完成后切换、连续按 V、过渡期间拍照与退出限制、快速转动视角时的模型惯性、待机与移动晃动以及放下相机后的姿态恢复

## 2026-05-24

- [ ] 修复相机相册 1:1 裁剪问题：`thumbPrefab` UI 框改为 16:9，消除检测范围与显示范围不一致
  - [x] Step A1 — 已根据实际相册画面确定最终视觉基准为横向 240×135，使用 4 列和 10×10 间距，后续不再缩小单张照片相对相册整体的尺寸
  - [ ] Step A2 — 缩略图 Prefab 根节点和 RawImage 统一为横向 240×135。移除相册内所有 Echo 高亮代码和引用。已生成 `Photo_Album_Thumbnail_Frame_16x9_Overlay_Rounded.png`，待在 Prefab 中作为透明圆角相纸边框绑定
  - [ ] Step A3 — Grid Layout Group 已设置 240×135、10×10 间距和 4 列；把 `CameraPhotoAlbumUIManager.Page Size` 从 40 改为 16，确保每页 4 列×4 行且不溢出
  - [ ] Step A4 — 静态确认缩略图生成继续保持原始照片宽高比，点击、右键菜单和批量选择删除不受比例修改影响；已移除大图预览关闭按钮引用
  - [ ] Step A5 — Unity 测试新旧照片的缩略图显示、点击检测、分页、删除模式、再次左键关闭大图预览和右键菜单
- [x] 手账画布持久化：为 `ScrapBookDataManager` 补充 JSON 存档逻辑，保存元素位置、角度、缩放（待 Unity 实测）
  - [x] Step 1 — 新建 `ScrapBookElementMeta.cs`，存 type + id
  - [x] Step 2 — `ScrapBookTextBox.cs` 加 `Text` 只读属性
  - [x] Step 3 — `ScrapBookCanvas.cs` 三个创建方法各自挂 Meta，AddPhotoElement 加 fileName 参数
  - [x] Step 4 — `ScrapBookDraggable.cs` OnEndDrag 传 fileName 给 AddPhotoElement
  - [x] Step 5 — `ScrapBookDataManager.cs` 加数据模型 + SavePhotos / LoadPhotos / SaveCanvas / LoadCanvas
  - [x] Step 6 — `ScrapBookUIManager.cs` OnDisable 存盘，OnEnable 读盘重建

- [x] 回响系统第 0 步（共享基础）：拍照时把触发的 Echo 数据绑定到照片 fileName
  - [x] Step 0.1 — 新建 Echo 存储（fileName → List\<string\> scrapbookContent），JSON 存盘，参考 `CameraPhotoStorage`
  - [x] Step 0.2 — `AddPhotoFromTexture` 返回生成的 fileName，让拍照流程能拿到它
  - [x] Step 0.3 — `CameraSystemManager.CaptureFlow` 收集 captureSnapshot 各 `EchoTrigger.scrapbookContent`，按 fileName 写入存储

- [x] 回响系统第 2 层（照片标识 / 手账文字呈现）：带 Echo 的照片在手账 topbar 中高亮
  - [x] Step 2.1 — 定方案：只读文本块、作为照片子节点、从 EchoPhotoStorage 派生、一侧竖排选边避越界
  - [x] Step 2.2 — `ScrapBookCanvas.AddPhotoElement` 生成只读 Echo 文本块（拖入/重建两条路径自动覆盖）
  - [x] Step 2.3 — 派生方案下无需单独存档：标签是照片子节点，重建照片时自动重生成
  - [x] Step 2.4 — 工具栏照片条目：有 Echo 则加发光描边提示（Outline 组件）
  - [x] Step 2.5 — 相册照片条目不显示 Echo 高亮，避免与相册统一照片边框冲突
  - [x] Step 2.6 — 明确手账 Echo 文本定位：照片拖入手账后显示 `EchoTrigger.scrapbookContent`，作为主角手账内容；爷爷日记便签只承载客观线索连接，不替代手账内容

- [ ] 回响系统第 3 层（日记便签）：爷爷日记 Tab 支持固定便签位
  - [x] Step 3.1 — 设计日记页/便签数据结构：日记页ID、爷爷原文、固定便签位ID、触发来源 EchoTrigger、便签内容、解锁状态
  - [x] Step 3.2 — 新建「已解锁便签」存储，JSON 存盘，记录哪些便签位已出现
  - [x] Step 3.3 — 拍照时登记 Echo 对应的便签位，而不是只汇总 scrapbookContent 文本
  - [x] Step 3.4 — `PlayerPanelUIManager.ShowGrandpaDiary` 渲染爷爷原文 + 已解锁便签；主角文字只显示在便签上，不写进爷爷旧日记本体
  - [x] Step 3.5 — 便签交互方案：优先静态贴纸；鼠标悬浮便签时降低透明度，移开恢复，显示便签下方老日记内容
  - [x] Step 3.6 — 配置第一关值班记录本：拍摄后解锁靠近“厂北山路 / 山顶灯光”段落的固定便签

- [ ] 日记字体配置
  - [ ] 爷爷旧日记字体：使用参考图方向的旧时代手写感字体（需确认授权后导入/替换）
  - [ ] 主角便签字体：选择稍现代、简洁、可读的字体，用于便签内容
  - [ ] 在爷爷日记 Tab 中分别绑定两套 TMP Font Asset

- [ ] 爷爷日记翻页功能
  - [x] Step N1 — 设计日记页数据结构：pageId、pageIndex、爷爷原文、页标题/日期（可选）、该页包含的固定便签位列表
  - [x] Step N2 — 新建 JSON 内容配置与解析：UI 上预设多个带 markerId 的 TextArea，JSON 中按 markerId 写内容，游戏开始时分发到对应 TextArea
  - [x] Step N3 — 在 `PlayerPanelUIManager` 中加入当前页索引、上一页/下一页按钮引用、页码文本引用
  - [x] Step N4 — 实现 `ShowGrandpaDiaryPage(index)`：刷新当前页爷爷原文、页码、翻页按钮可用状态
  - [x] Step N5 — 打开爷爷日记 Tab 时默认显示第一页，或显示最近一次停留的页（当前实现：本次会话内保留上次页，初始为第一页）
  - [ ] Step N6 — UI 配置：在 `GrandpaDiaryTab` 下放置日记正文 TMP、上一页按钮、下一页按钮、页码 TMP
  - [x] Step N7 — 与日记便签系统预留接口：每页可以查询并显示该页已解锁的便签位；未做便签前先只显示爷爷原文
  - [ ] Step N8 — 测试：第一页时上一页不可用，最后一页时下一页不可用；切换 Tab 后页内容不串；关闭/打开玩家面板后页状态符合 Step N5 选择

- [ ] 回响收集进度提示
  - [ ] 为每个关卡配置回响物品总数
  - [ ] 统计当前关卡已收集 / 未收集回响数量
  - [ ] 在合适 UI 中清晰提示“当前关卡还有多少个回响物品未收集”

- [ ] 回响系统第 1 层（即时反馈）小修
  - [ ] `HandleConfirmedHighlights` 的 `return` 改为遍历全部 Echo
  - [x] `EchoPopupUI` 补自动隐藏
  - [x] 右上角提示改为统一 `Popup Message`，不直接显示 `EchoTrigger.scrapbookContent`

- [x] 内容：把值班记录本触发的便签文字定稿，确认便签位，填进 story-flow.md，并给出场景 `EchoTrigger.scrapbookContent` 配置文本

---

## Unity 配置检查清单（测试当前已完成步骤）

> 用途：确认 todolist 中已完成的手账持久化、Echo 绑定照片、手账 Echo 呈现、工具栏 Echo 描边是否在 Unity 场景里配置到位。

### 1. 场景基础对象

- [ ] 场景中有且只有一个 `ScrapBookDataManager` 组件对象
  - 作用：管理手账照片列表、装饰列表、画布布局 JSON 存档
  - Inspector：无需要拖拽的字段

- [ ] 场景中有 `CameraSystemManager`
  - [ ] `Camera UI Root` → 拖相机 UI 根节点
  - [ ] `Camera Tweaking UI Manager` → 拖 `CameraTweakUIManager`
  - [ ] `Camera Photo Preview UI Manager` → 拖 `CameraPhotoPreviewUIManager`
  - [ ] `Camera Photo Album UI Manager` → 拖 `CameraPhotoAlbumUIManager`
  - [ ] `Camera Photo Capture` → 拖 `CameraPhotoCapture`
  - [ ] `Highlight Object Detector` → 拖 `CameraHighlightObjectDetector`

- [ ] 场景中有 `EchoSystemManager`
  - [ ] `Camera System Manager` → 拖同场景的 `CameraSystemManager`
  - [ ] `Echo Popup UI` → 拖 `EchoPopupUI`

- [ ] 场景中有 `EchoPopupUI`
  - [ ] `Popup Root` → 拖回响弹窗根节点
  - [ ] `Content Text` → 拖弹窗里显示 Echo 内容的 TMP 文本
  - [ ] `Popup Message` → 统一右上角提示文案，例如“发现新的回响”；不显示 `EchoTrigger.scrapbookContent` 正文

### 2. 手账 UI

- [ ] 手账 UI 根节点上有 `ScrapBookUIManager`
  - [ ] `Photo Category Btn` → 拖照片分类按钮
  - [ ] `Deco Category Btn` → 拖装饰分类按钮
  - [ ] `Element Content` → 拖手账顶部工具栏 / Scroll Content 节点
  - [ ] `Scrap Book Canvas` → 拖放置照片/贴纸/文本框的 `ScrapBookCanvas`
  - [ ] `Deco Presets` → 拖入所有贴纸和文本框的 `ScrapBookDecoPreset`
  - [ ] `Photo Element Size` → 确认工具栏照片条目尺寸
  - [ ] `Echo Glow Color` / `Echo Glow Thickness` → 确认有 Echo 的工具栏照片描边颜色和粗细

- [ ] 手账画布区域上有 `ScrapBookCanvas`
  - [ ] `Text Box Prefab` → 如使用文本框 prefab，拖对应 prefab；为空时会走代码生成的临时文本框
  - [ ] `Default Photo Size` → 确认照片拖入画布后的默认尺寸
  - [ ] `Echo Label Gap` / `Echo Label Width` / `Echo Label Font Size` / `Echo Label Color` → 确认照片周围 Echo 文字样式

- [ ] 如果使用 `Text Box Prefab`
  - [ ] prefab 根节点有 `ScrapBookTextBox`
  - [ ] `Input Field` → 拖 prefab 内的 `TMP_InputField`
  - [ ] `Display Text` → 拖 prefab 内常态显示文字的 TMP 文本

- [ ] 每个 `ScrapBookDecoPreset` 配置完整
  - [ ] `Preset Name` 唯一，不能空
  - [ ] `Deco Type` 正确选择 `Sticker` 或 `TextBox`
  - [ ] `Icon` 已拖入工具栏显示用图
  - [ ] `Default Size` 合理

### 3. Echo 物品

- [ ] 需要触发回响的 B 类物品上有 `CameraHighlightObject`
  - 作用：让相机高亮检测系统能识别该物体

- [ ] 同一个 B 类物品上有 `EchoTrigger`
  - [ ] `Diary Content` 填入当前测试用 Echo 文字

- [ ] 相机取景/拍照时该物品能进入 `CameraHighlightObjectDetector.HighlightedObjects`
  - 作用：`CameraSystemManager` 会用检测结果把 Echo 写入 `EchoPhotoStorage`

### 4. 当前已完成步骤测试流程

- [ ] 测试 Step 0：拍摄带 `EchoTrigger` 的物品后，照片能保存，并在 `Application.persistentDataPath/Echo/EchoPhotos.json` 里写入该照片 fileName 对应的 Echo 文本

- [ ] 测试 Step 2.2：把这张有 Echo 的照片从手账工具栏拖入画布后，照片周围自动出现只读 `scrapbookContent` 文本块；它代表主角手账内容，不是爷爷日记便签内容

- [ ] 测试 Step 2.3：关闭/重新打开手账面板后，已放置照片能从存档重建，Echo 文本块随照片自动重新生成

- [ ] 测试 Step 2.4：手账工具栏照片列表中，有 Echo 的照片条目出现 `Outline` 发光描边；无 Echo 的照片没有描边

- [ ] 测试 Step 2.5：相册照片列表中，带 Echo 与普通照片使用相同边框，不出现额外高亮

- [ ] 测试手账持久化：移动、旋转、缩放照片/贴纸/文本框后关闭手账，再打开时位置、角度、缩放恢复

### 5. 爷爷日记翻页 UI 配置

- [ ] `PlayerPanelUIManager` 的 Grandpa's Diary 区域
  - [ ] `Grandpa Diary Json` → 拖 `Assets/For AI Agent/Narrative/grandpa-diary-content.json` 测试内容；拖了它以后会优先使用文件内容生成页数据
  - [ ] `Grandpa Diary Pages` → 可留空；只有不使用 `Grandpa Diary Json` 时才需要手动配置每页 `pageId`、`pageIndex`、`pageTitle`、`textBlocks`
  - [ ] `Grandpa Diary Page Views` → 拖入每个日记页根节点上的 `GrandpaDiaryPageView`
  - [ ] `Grandpa Diary Prev Page Button` → 拖上一页按钮
  - [ ] `Grandpa Diary Next Page Button` → 拖下一页按钮
  - [ ] `Grandpa Diary Page Text` → 拖页码 TMP 文本

- [ ] 每个日记页根节点
  - [ ] 挂 `GrandpaDiaryPageView`
  - [ ] `Page Id` 与 JSON 中的 `pageId` 一致，例如 `p01`
  - [ ] `Page Root` 可拖本页根节点；不拖则默认用当前 GameObject
  - [ ] `Text Areas` 可手动拖，也可留空让代码自动抓子物体里的 `GrandpaDiaryTextAreaBinding`

- [ ] 每个日记正文 TextArea
  - [ ] 挂 `GrandpaDiaryTextAreaBinding`
  - [ ] `Marker Id` 与该页 `textBlocks.markerId` 一致，例如 `p01_body_01`
  - [ ] `Target Text` 拖该 TextArea 的 TMP 文本；不拖则默认用当前 GameObject 上的 TMP_Text

- [ ] 测试爷爷日记翻页
  - [ ] 打开爷爷日记 Tab 默认显示第一页
  - [ ] 点下一页后显示下一页，并按 marker 填入正确文本
  - [ ] 第一页上一页按钮不可点，最后一页下一页按钮不可点
  - [ ] 页码显示正确，例如 `1 / 3`
