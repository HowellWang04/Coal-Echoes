# ArtSource/NewspaperMenu/Spread/

## 用途

双页报纸主菜单的排版制作和检查：一次性排版与接入脚本、字体和字库记录、各阶段截图。运行时说明在 `Runtime/`，转场里同步的沙丘和天空在 `SandAccumulation/` 和 `SkyMotion/`。

## 内容

- `BuildPreview.cs`：编辑模式一次性排版脚本，生成审核用的 prefab 和截图，不改加载的场景。`Install.cs`：把定稿排版接进主场景。`VerifyRuntime.cs`：在临时对象上跑菜单控制器和 HDRP pass，输出 `Runtime/` 下的截图和检查。`BakeMenuPhoto.cs`：按 Play Mode 菜单开场的状态重新烘焙编辑模式显示的静态照片。都通过 Unity CLI `eval_file` 或 MCP 执行。
- `copy.zh-CN.json`：排版预览时的中文文案旧稿，没有和运行文案同步。
- `menu-spread-1920.png`、`camera-original.png`、`effect-zero.png`、`checks.json`：排版审核稿的渲染、同次相机原图、效果归零检查和文字、比例、shader 检查。
- `dated-before.png`、`dated-after.png`：加报头日期前后。`fonts-compare.png`、`fonts-period.png`：换年代字体前后的对照和整版。
- `Runtime/`、`SandAccumulation/`、`SkyMotion/`：见各自 CLAUDE.md。

## 与其他部分的关系

- 场景对象是 `In-game UI Root/MainMenuUIRoot/MenuPresentation/Newspaper/Spread`，全部文字用 TextMeshPro，正文来自 `Assets/Narrative/newspaper-menu.json`，编辑器里改完调 `NewspaperMenuUI.ApplyCopy()` 刷新。
- 审核 prefab `Assets/Art Resources/UI/NewspaperMenu/SpreadPreview/NewspaperSpreadPreview.prefab` 和同目录 `SpreadPaper.png` 不在场景里，只是静态审核稿；`CameraPhoto.png` 是编辑模式下照片位置显示的静态图。
- 字体在 `Assets/Art Resources/UI/NewspaperMenu/Fonts/`，都是 SIL OFL，许可随字体放。字库按用途分：正文、启事、图注、日期、页脚用思源宋体 Regular 烘的 `SpreadPreview/Spread Body SDF`；副题和正文开头的“本报讯”用朱雀仿宋烘的 `Resources/Fonts & Materials/Spread Fangsong SDF`；头条用思源宋体 Heavy 烘的 `Spread Headline SDF`；其他小标题和栏名用思源宋体 Medium 烘的 `Spread Chinese SDF`；报头用 Ma Shan Zheng 烘的 `Spread Masthead SDF`；英文用 Bitter Regular 烘的 `Fonts/Bitter Newspaper SDF`。
- 参考分辨率 1920 × 1080，报纸铺满画面，中央竖折缝保留，右页照片 16 比 9。

## 注意

- 字库都是静态图集，改 `newspaper-menu.json` 加了新汉字要补进对应字库，否则缺字。字库资源里没有存源字体，补字时临时把 `Fonts/` 下对应的源字体挂回字库，切成 Dynamic 用 `TryAddCharacters` 追加，再切回 Static；不要整份重烘，重烘会换掉材质上调过的洇墨参数。
- 仿宋字库必须留在 `Resources/Fonts & Materials/` 下，“本报讯”由 `NewspaperMenuUI.ApplyCopy` 包上 TMP 的 `<font>` 标签换字体，TMP 按名字从那里读；JSON 里不写标签。
- 洇墨靠 TMP 材质的 Face Dilate 和 Softness，头条要调得比正文轻，大字上柔化过重像模糊。
- 文案是本项目虚构的日常消息，不引用参考报纸的文章；厂区配图只标全景，不把废弃厂区说成八十年代作业现场。
- 改了排版后跑 `VerifyRuntime.cs` 复查溢出和缺字；改了菜单相机、菜单里可见的场景或纸墨参数后跑 `BakeMenuPhoto.cs`。
