# 报纸菜单

## 已确认方向

2026 年 10 月 12 日改为整张报纸覆盖主菜单，参考用户提供的旧报纸纸色、磨损、密集分栏和照片印刷质感，正文中文、菜单按钮英文，文字继续使用 TextMeshPro。参考图中的政治内容、人物、口号不采用。

最新主要参考为用户提供的《羊城晚报》纸面照片，已归档到 `References/evening-newspaper-reference.jpg`。用户已确认按 16 比 9 重排为双页摊开的报纸，保留中央折缝，文字与照片不拉伸；纸墨、报头、横竖栏、分隔线及布局细节见 `ArtDirection.md`。照片已减黄并加深黑灰油墨，当前预览为 `Halftone/neutral-newsprint.png`。

报纸内的厂区照片改为 MainMenuCamera 输出，经 RenderTexture 和半调 shader 实时处理。点击 Start Game 后先放大照片，直到其画面覆盖全屏，再逐渐去掉印刷处理、恢复原相机颜色与细节，之后继续现有抬头看天空、开场对白和玩家操作交接。取消报纸飞走动画。当前废弃厂区画面作为菜单进入游戏的视觉入口，不配上八十年代在用厂区的历史照片说明。

纸墨预览、来源许可及适配说明见 `Halftone/README.md`。双页排版、实时相机照片及新转场已接入并保存主场景，当前截图见 `Spread/Runtime/menu-1920.png`，运行方式和验证见 `Spread/Runtime/CLAUDE.md`。原独立排版 Prefab 和制作记录保留在 `Spread/README.md`。

## 旧版实现记录

以下为此前已接入场景的 `concept-v2.png` 悬浮报纸版本，新方向将替换该布局、静态配图与飞走动画。两张概念图由 imagegen 生成，图内文字不是正式文案。

报纸属于故事中的八十年代，正文使用出版当时的视角，报道正常运转中的工厂与职工生活。纸张在游戏发生时已泛黄磨损，但文章不预知工厂废弃，也不回顾数十年后的遗迹。

删去概念图中的“旧日影像”“岁月不寂”“仍在记忆里回响”等回顾性段落。配图改为当年正常运转的厂区、装煤作业或职工活动，不使用当前废弃厂区截图。概念图中的一九八七年十月十二日、期号与报社名均未定稿，不沿用。

文案采用朴实、具体的厂区消息与生活通知，游戏不加入政治暗示。移除概念图里的“或许沉默，但从未消失”“山仍在，煤仍在，人也在”“劳动光荣，时代前进”及其他口号式、象征性旁白，报头两侧的诗句也不沿用。情感通过饭盒、夜班饭菜、电影放映等生活细节呈现；右下角改为失物招领，页脚只列本期栏目。

标题、正文和英文按钮均已使用 TextMeshPro，纸张纤维、折痕与黑白照片使用独立贴图。文案草案见 `copy.zh-CN.json`，运行文案以 `Assets/Narrative/newspaper-menu.json` 为准，均为游戏内虚构报纸拟稿，不是历史报刊原文。

### 旧版场景状态

第二版构图已获用户同意，现已接入主场景。`menu-1920.png` 与 `menu-1280.png` 是当前 Unity 编辑器预览，概念图保留为构图记录，其中的旧占位文字不再使用。

主场景对象为 `MainMenuUIRoot/MenuPresentation/Newspaper`，参考尺寸为 800 × 960，轻微向左倾斜。原 `MenuContent` 先停用，2026 年 10 月 21 日已从场景删除；开始、继续和退出三个按钮移至报纸，保留原点击事件与缩放反馈。背景继续使用 `Gameplay Root/MainMenuCamera`，正式 Canvas 为 Screen Space Overlay。2026 年 10 月 21 日起场景里 `MainMenuUIRoot` 的 Canvas 保存为启用，编辑时可在 Scene 视图看到并直接调整报纸（位于世界原点附近，放大 2 倍），编辑模式下 Game 视图也会显示报纸。运行时仍由 `MainMenuUIManager` 控制：Start 里启用并从透明淡入，开场结束后在 OnDisable 里关闭；`showOnStart` 关闭时整个 `MainMenuUIRoot` 停用，不会显示。Start 还会把 `MenuPresentation` 和 `Newspaper` 强制激活，编辑时关掉这两个物体不影响游戏；Scene 视图的小眼睛只影响编辑器显示。唯一不能关的是挂着脚本的 `MainMenuUIRoot`，它没激活时脚本不会运行，主菜单和开场都不会出现。

`NewspaperMenuUI` 在等待时轻微漂浮并弯曲纸面，`NewspaperGraphicBend` 让图片、线条与 TMP 字形使用同一变形。点击 Start Game 后立即停止接收菜单点击，纸张在 1.1 秒内卷曲并飞往右上方，完全离场后隐藏菜单内容并调用原有 `OpeningSequenceUI`。Continue 仍待进度恢复功能接通，当前置灰。

### 旧版素材和文字

运行资源放在 `Assets/Art Resources/UI/NewspaperMenu/`。Newsprint 与 LoadingYard 由内置 imagegen 生成，分别为空白旧新闻纸和虚构的在用装煤站照片，图内没有文字。精确提示词与产物对应关系见 `assets-prompts.json`。CablewayMark 是独立的圆形索道标记。

中文使用 Source Han Serif SC Medium（10 月 22 日起正文、副题、头条已换成年代字体，见 `Spread/README.md` 的“年代字体”），英文使用 Bitter Regular，报头和 Start Game 使用 TMP 的粗体样式。字体及 OFL 许可随运行资源保存，原始来源记录见 `ArtSource/Branding/README.md`。两个 SDF 字体资源为静态字库，已覆盖全部当前文字；新增未收录的汉字时需重新烘焙字库。未添加运行中重新导入资源的处理代码。

正文、标题、图片说明和失物招领从 `Assets/Narrative/newspaper-menu.json` 读取。在编辑器修改该文件后，可调用报纸组件的 `ApplyCopy()` 刷新预览，进入 Play 时会自动读取；菜单按钮文字直接保存在 TMP 组件中。

`Setup.cs` 记录首次搭建过程，已有 Newspaper 时会停止，不用于覆盖现有场景。`Preview.cs` 与 `AnimationChecks.cs` 是通过 Unity CLI eval_file 执行的编辑器检查源文件，不编入游戏。

### 旧版验证

两种画幅的编辑器预览均已核对，20 个文字对象没有溢出，也没有缺失字形。17 项隔离检查通过，覆盖按钮绑定、字库、实际 TMP 顶点变形、图片细分、1.1 秒离场时长、离场方向、完全离屏后只调用一次完成回调，以及恢复原位置，结果见 `checks.txt`。

隔离检查在临时菜单副本上显式调用生命周期方法并移动实际 Tween 的时间点，不等同于 Play Mode。编辑模式的 DOTween 不执行正常玩家生命周期，因此未验证运行时取消、连续帧表现或真实输入。漂浮观感、点击反馈和完整开场衔接由用户进入 Play Mode 验收。

预览临时使用相机 Canvas，并关闭预览相机的时间抗锯齿以免单帧截图产生文字重影，正式场景设置未因此改变。修改前场景快照保存在本机 `Library/NewspaperMenu/Before.unity`。
