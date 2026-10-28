# ArtSource/Portfolio/EchoNotes/

## 用途

作品集里展示回响便签的图：手账页上一张普通照片和两张回响照片，回响照片带暖色相框、ECHO 印章和用纸胶带贴着的手写便签。不是游戏截图，是用游戏自己的贴图、字体和数值离线拼出来的。

## 内容

- `compose_echo_notes.py`：用 Pillow 合成。`python -I compose_echo_notes.py <仓库根目录> <输出 png>` 写出整页图，同时在旁边写出只含厂门那张回响照片和便签、透明底并裁到边的 `<输出>_gate_element.png`。
- `echo_notes_scrapbook.png`：整页图，2560 × 1320。左边外公 1980 年的合照是普通照片，中间是厂门 Group Photo 重拍，右边是 207 抽屉里的山路图。
- `echo_notes_scrapbook_gate_element.png`：厂门那张回响照片和便签，透明底。
- `Renders/`：两张回响照片，Unity MCP `execute_code` 用临时 HDRP 相机渲染，1600 × 900。`gate_retake.png` 用 `Group Photo Retake View` 的位置和朝向、竖直视角 50 度；`mountain_road_map.png` 从山路图上方 0.2 米斜向下拍，竖直视角 36 度，物理相机对焦到纸面、光圈 f/16，纸基本占满画面。相机的 Volume 层和主相机一致，渲染完就删掉。

## 与其他部分的关系

- 拼法照 `Assets/Scripts/In-Game Systems/Scrap Book System/ScrapBookCanvas.cs` 的 `AddEchoNote` 和 `PlaceEchoNote`、`ScrapBookPhotoVisual`、`EchoPhotoMark`：照片 120 × 67.5 画布单位，相框是 `Photo_Album_Thumbnail_Frame_16x9_Overlay_Rounded.png`，普通和回响各一种颜色；印章 `Echo_Stamp.png` 按预制体里的锚点和 -11 度；便签纸 `Echo_Note_Paper.png` 九宫格、胶带 `Echo_Note_Tape.png`、字体 `NothingYouCouldDo-Regular.ttf`，宽度、内边距、字号、颜色、阴影、重叠量取场景里 `ScrapbookCanvas` 上的值，便签位置取两个 `EchoTrigger` 的 noteLayout。底纸是 `Scrapbook_Tab_BG.png`。贴图源见 `../../EchoPhotoMark/CLAUDE.md`。
- 便签文字和场景里 `EchoTrigger.scrapbookContent` 一致，正文以 `Docs/game-text.md` 为准；那边改了要同步改脚本里的两段文字再重跑。
- 作品集 Figma 文件“王冠豪-总排版”里，`Frame 15`（节点 `641:547`）左半边 Gameplay design 用了透明底那张，放在 Photograph 清单右边，节点名 `Echo photo with note`。

## 注意

- 每张照片的元素放大倍数（`S`）和旋转是为了排版定的，游戏里是玩家自己调的；便签本身的偏移和角度来自场景，不要改。
- 照片在 Play Mode 里渲染过，画面带当时的沙尘雾；重渲染时结果会随场景灯光和体积雾变化。
- 山路图贴图分辨率有限，相机贴得太近会显得软。