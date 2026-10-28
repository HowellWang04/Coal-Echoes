# ArtSource/NewspaperMenu/Spread/Runtime/

## 用途

双页报纸主菜单的运行时说明和验收截图。菜单右页的厂区照片是 MainMenuCamera 的实时画面，经纸墨处理后贴在报纸上；点击 Start New Game 后报纸放大到照片铺满屏幕，纸墨效果消退，再接回原有开场。运行代码在 `Assets/Scripts/MainMenu/`。

## 内容

- `menu-*.png`、`photo-*.png`、`zoomed-*.png`、`revealed-*.png`：首次接入时 `../VerifyRuntime.cs` 在 1920 × 1080 和 1280 × 960 下拍的菜单、照片、放大和消退截图。相机位置和纸墨参数都已改过，只能用来看排版和转场节点，不代表现在的画面。
- `original-*.png`、`bypass-*.png`：同次的相机原图，以及纸墨强度为 0 时的还原检查。
- `checks.json`、`scene-checks.json`：同次的转场数值检查和场景按钮绑定检查。

## 与其他部分的关系

- 报纸根对象是主场景的 `In-game UI Root/MainMenuUIRoot/MenuPresentation/Newspaper`，上面挂着 `NewspaperMenuUI` 和 CustomPassVolume，子对象 `Spread` 负责排版，正文来自 `Assets/Narrative/newspaper-menu.json`。
- `MainMenuUIManager.Start` 锁定玩家输入后调用 `NewspaperMenuUI.Show`。之后 `NewspaperPhotoPass` 在 HDRP 的 AfterPostProcess 阶段复制 MainMenuCamera 的画面，用 `NewspaperHalftone.shader` 和运行材质 `Assets/Art Resources/UI/NewspaperMenu/Halftone/NewspaperSpread.mat` 处理后交给照片 RawImage。MainMenuCamera 照常输出到屏幕。
- 点击 Start New Game 后依次是停顿、放大、消退，时长由 `NewspaperMenuUI` 的 zoomDelay、zoomDuration、revealDuration 控制，结束后调用 `OpeningSequenceUI.Play`。沙丘、天空、宿舍楼的变化和年份标题共用同一进度，从点击开始到纸墨完全消退结束，配置分别见 `../SandAccumulation/`、`../SkyMotion/`、`../../../WorkersDormitory/CleanAging/`。菜单期间大门敞开、没有锁链、没有摩托车，由 `MainMenuUIRoot` 上的 `OpeningSceneState` 负责，不随进度变化，等开场镜头对着天空时才还原，见 `../../../MainMenu/Opening/`。
- 年份标题是 Newspaper 下的 `YearTitle`，默认隐藏，它在场景里的位置和字号就是落点。转场开始时它缩到报头日期里年份的大小、压在原位，报纸上的年份用 TMP 透明标签藏起，随后边逐年翻到 presentYear 边放大移到落点，纸墨消退完时正好落定，再用 yearFadeDuration 淡出后接开场。起止年份是 `NewspaperMenuUI` 的 issueYear 和 presentYear。
- 悬浮翻译是 Newspaper 下的 `TranslationTip`，挂 `NewspaperTranslationTip`，子对象 `Box` 是煤灰黑矩形底，`Label` 用 Imprima 400 SDF 和 Pickup Underlay 材质。`ApplyCopy` 填每块中文时把 JSON 里对应的 En 字段登记给它；它每帧按鼠标位置判断落在哪块 TMP 的文字范围里，不走射线，所以文字仍不接收射线，按钮点击不受影响。正文一栏有两段时两段一起显示，段间距在 `Label` 的 Paragraph Spacing 上。`EnterPhoto` 开始时关掉它，`OnDisable` 再打开。
- 编辑模式下照片位置显示静态图 `Assets/Art Resources/UI/NewspaperMenu/SpreadPreview/CameraPhoto.png`，由 `../BakeMenuPhoto.cs` 按 Play Mode 菜单开场的状态烘焙。
- 参考分辨率 1920 × 1080。其他画幅时整张报纸等比缩放、完整显示，照片按相机画幅居中放进照片栏，放大终点按照片实际矩形计算。

## 注意

- 改了菜单相机、菜单里可见的场景、`OpeningSceneState` 的开门角度或纸墨参数后，要在编辑模式下运行 `../BakeMenuPhoto.cs`，否则编辑模式和 Play Mode 看到的照片会不一样。
- MainMenuCamera 放在厂区南角索道塔架旁的高处往山下拍，图注写的是“从索道塔架上俯瞰”；挪机位后要同步改 `newspaper-menu.json` 的 photoCaption。
- 检查颗粒和网点要把 Game 视图的 Scale 设为 1x。4K 缩到 0.3 倍左右显示时，细纹理会变成硬像素块和格子。
- 颗粒噪点不能沿横竖格点对齐，否则缩小后会出现细格子。shader 的 `Grain` 先把格点扭弯再取样，换噪点写法时要保持这一点。
- 45 度网点层只在放大转场中淡淡出现，菜单里看不到。淡入按网点在屏幕上的间距计算，`NewspaperMenuUI` 每次排版都会写入 `_DisplayScale`。改网点密度时要一起改材质上的两个淡出阈值，否则网点会显示不全或在菜单里露出来；阈值折算后 1080p 下低于 4 像素左右会出摩尔纹。网点是规则图案，强度稍高就会被认成一张网。
- 在 Play Mode 里改 `NewspaperSpread.mat` 不会马上生效，菜单启动时复制了一份材质，要重新进入 Play Mode。
- `NewspaperPhotoPass` 不在 Scene 视图执行，Scene 视图里看到的照片始终是静态图。
- 资源清理统一在 `NewspaperMenuUI.OnDisable`，中途关闭和正常结束走同一条路径。新增转场联动时要在这里一起还原。
- 年份标题按报头日期开头的汉字年份找起点，`newspaper-menu.json` 的 issueDate 必须以 issueYear 对应的“一九八〇年”这种写法开头。
- Continue 不走报纸转场，菜单关闭时 `NewspaperMenuUI.OnDisable` 照常还原沙丘、天空和宿舍楼。
- 悬浮翻译的英文只能用 ASCII，Imprima 字库是静态的，没有弯引号和长破折号。`columnsEn` 按换行分段，和 `columns` 的段落一一对应；`labels` 的顺序要和 `NewspaperMenuUI` 的 labels 数组一致。
