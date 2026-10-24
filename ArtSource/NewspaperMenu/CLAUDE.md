# ArtSource/NewspaperMenu/

## 用途

全屏双页报纸主菜单的美术方向、文案来源和制作记录。报纸铺满画面，正文中文、按钮英文，右页厂区照片是 MainMenuCamera 的实时画面经纸墨处理；点击 Start New Game 后照片放大铺满屏幕，纸墨消退，接原有开场。

## 内容

- `ArtDirection.md`：美术基准，参考图观察、纸墨、报头、分栏和布局细节。`References/evening-newspaper-reference.jpg`：用户提供的《羊城晚报》纸面参考。
- `copy.zh-CN.json`：文案草案；运行文案以 `Assets/Narrative/newspaper-menu.json` 为准。
- `concept-v1.png`、`concept-v2.png`、`prompts.json`：早期悬浮报纸方案的 imagegen 概念图和提示词，图内文字不是正式文案。`assets-prompts.json`：`Newsprint.png` 和 `LoadingYard.png` 的生成提示词。
- `Setup.cs`、`Preview.cs`、`AnimationChecks.cs`、`checks.txt`、`menu-1920.png`、`menu-1280.png`：早期悬浮报纸版本的搭建、预览和隔离检查脚本及结果，对应的漂浮、弯曲和飞走动画已不在场景里。
- `Halftone/`：纸墨 shader 试验和来源，见其 CLAUDE.md。`Spread/`：双页排版制作、运行时说明和转场联动，见其 CLAUDE.md。

## 与其他部分的关系

- 场景对象是 `In-game UI Root/MainMenuUIRoot/MenuPresentation/Newspaper`，挂 `NewspaperMenuUI`、CustomPassVolume、`FactorySandAccumulation`、`OpeningSkyMotion`、`DormitoryMaterialAging`，代码都在 `Assets/Scripts/MainMenu/`。`MainMenuUIManager` 控制显示和输入锁（见 `../MainMenu/`），转场结束调 `OpeningSequenceUI`（见 `../MainMenu/Opening/`）。
- 运行资源在 `Assets/Art Resources/UI/NewspaperMenu/`：`Newsprint.png` 纸纹，`Fonts/` 字体和许可，`Halftone/` 材质，`SpreadPreview/` 字库和静态照片。`LoadingYard.png`、`CablewayMark.png` 是旧版配图和标记，场景已不用。
- 品牌 Logo 和字体来源见 `../MainMenu/Branding/`。

## 注意

- 报纸属于故事里的八十年代，以出版当时的视角报道正常运转的工厂和职工生活，不预知废弃，不加政治暗示和口号式旁白；参考图里的人物、口号不采用。
- 当前废弃厂区画面只作菜单进入游戏的视觉入口，不配八十年代在用厂区的说明。
- 改了 `newspaper-menu.json` 后编辑器里调 `ApplyCopy()` 刷新，新汉字要补字库。
- `Setup.cs` 在已有 Newspaper 时会停止，不能用来覆盖现有场景。
- Continue 跳过报纸转场，沙丘、天空和宿舍楼直接是终点状态。
