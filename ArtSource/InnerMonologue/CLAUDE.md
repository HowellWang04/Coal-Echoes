# ArtSource/InnerMonologue/

## 用途

内心独白字幕的效果图和运行用图：在第一关大门玩家视角截图上画屏幕下方字幕的几种方案，选定的 G 版（Elegant Bloom 手写体、柔影加横向暗带、首行左侧云朵思考 icon）已在场景里搭好。

## 内容

- `build_mockups.py`：用 Pillow 和项目里的 Imprima 字体生成效果图，`python build_mockups.py` 重新导出；字号、行距、阴影、暗带和颜色都在源码里。
- `Previews/Background_GateView.png`：编辑器里 Main Camera 在玩家起点看向工厂大门的 1920×1080 渲染，不含 UI。
- `Previews/Monologue_A_ShadowOnly.png`：只有文字和煤灰黑柔影。
- `Previews/Monologue_B_ShadowBand.png`：柔影加两端渐隐的横向暗带。
- `Previews/Monologue_C_BandRule.png`：暗带加字幕上方暗赭红短线，字号略小。
- `Previews/Monologue_Compare.png`：A 到 C 缩小后的对照。
- `Previews/Monologue_D_PlayerHand.png`：改用手写字体 Elegant Bloom，不斜切，带 B 的暗带。
- `Previews/Monologue_E_HandNote.png`：D 加首行左侧一张微斜的黄色小便签记号。
- `Previews/Monologue_F_HandVignette.png`：D 加独白期间画面四周压暗、褪色。
- `Previews/Monologue_Cues_Compare.png`：D 到 F 缩小后的对照。
- `Icons/ThoughtIcon_Cloud.png`、`ThoughtIcon_Dots.png`、`ThoughtIcon_Trail.png`：三种精简思考 icon，256 像素白色透明底，Unity 里用 Image 颜色着色。依次是云朵气泡加两颗圆点、圆角气泡加三个点、只有三颗由小到大的圆点。
- `Icons/MonologueBand.png`：字幕暗带用的白色四边渐隐图，Unity 里染成煤灰黑。
- `Previews/ThoughtIcon_Sheet.png`：三个 icon 在深浅底上的大图和 32 像素小图。
- `Previews/Unity_Subtitle_Edit.png`：场景里实际字幕的编辑器渲染。画布临时改成相机空间才渲染得到，经过了 HDRP 曝光，比 Play Mode 暗。
- `Previews/Monologue_G_IconCloud.png`、`Monologue_H_IconDots.png`、`Monologue_I_IconTrail.png`：D 的首行左侧分别加上三个 icon；`Monologue_Icons_Compare.png` 是三版对照。

## 与其他部分的关系

- `ThoughtIcon_Cloud.png` 和 `MonologueBand.png` 复制到 `Assets/Art Resources/UI/Inner Monologue/`，导入为 Single Sprite、无 mipmap；改了图要重新运行脚本并再复制一次。
- 场景对象是 `In-game UI Root/MonologueUIRoot`，自带 Canvas，排序 30，画在面板、背包检视和任务提示之上、黑屏过场之下。`Subtitle/ContinueArrow` 是从过场文字复制的两笔小箭头，染成字幕的暖白；`ClickBlocker` 是全屏透明图，只在独白占住玩家时打开，挡住对面板的点击。控制脚本 `Assets/Scripts/In-Game Systems/Inner Monologue System/InnerMonologueUIManager.cs` 读 `Assets/Narrative/inner-monologue.json`，字幕阴影用 `Assets/Art Resources/UI/Fonts/Elegant Bloom SDF - Monologue Underlay.mat`。
- `MonologueBand.png` 也是右下角拾取提示每行底下的暗带，改图会一起变，见 `../PickupNotice/CLAUDE.md`。
- 第一关什么时候播哪句由 `Level1HintFlowManager` 决定，第二关由 `Level2HintFlowManager` 决定，右上角提示 `HintPopupUIManager` 等独白点完才出下一条，两个流程管理器再等这条提示出现才开放这一步的 F/E 交互点。
- 工具用成功时的中下方使用提示 `In-game UI Root/UseNoticeUIRoot`（`UseNoticeUI`）和字幕同一位置，两者轮流显示：提示等正在显示的独白点完才出，独白等提示淡出才播，字幕的 useNotice 引用它。
- 独白文字和触发时机见 `Docs/json-inner-monologue.md`，第一关出现的步骤见 `Docs/design.md` 详细流程表。
- A 到 C 用 `Assets/Art Resources/UI/Fonts/Imprima 400.ttf`，与开场文字和右上角提示弹窗一致；D 到 F 用同目录的 `Elegant Bloom.otf`，外公日记正文用的是另一款 `handwriting`。颜色取品牌色，见 `../MainMenu/Branding/CLAUDE.md`。

## 注意

- Imprima 没有斜体字形，效果图的斜体是斜切，和 TMP 的 Italic 做法一样。
- 截图里大门前地面偏亮，只靠柔影时字幕对比度最低，实机要在亮地面和天空两种背景下各看一次。
- 独白不会自动消失：逐字打出时左键加速，打完出箭头，再点才换下一句或收起。从一句出现到最后一句点掉，`Gameplay` 里开着的动作全部关掉、之后只恢复这些，移动也锁住；点击读的是单独的 `Monologue/Advance` 动作，不受这个锁影响。
- 每句在一份存档里只播一次，已播放的 id 存在 `Progress/Progress.json`（`GameProgress`），Continue 后不重播，Start New Game 清档后重新播。
- 独白由物体的 UnityEvent 调 `InnerMonologueUIManager.Play`，参数填 id。值班记录本那句接在记录本 `InspectableItem` 的 onInspectEnd 上，看完放回桌上时播；重拍合照那句接在大门 `EchoTrigger` 的 onCaptured 上；Bolt Cutter 那句由 `Level1HintFlowManager` 见到背包里有它时播，大门打开那句接在大门铁链的 `ItemUsePoint` 上，等 Chain cut 使用提示淡出后才播。第二关 M2.1 由 `Level2HintFlowManager` 在进门的章节标题消失后播，M2.2 到 M2.5 待接。