# ArtSource/EchoPhotoMark/

## 用途

回响照片标记的贴图源：ECHO 油墨印章、手账便签纸和纸胶带，以及定稿时的效果图。相册、手账顶栏和手账画布用同一套标记。

## 内容

- `Scripts/draw_echo_mark_art.py`：用 Pillow 程序化绘制，4 倍超采样后缩小。`python -I Scripts/draw_echo_mark_art.py <输出目录>` 输出 `Echo_Stamp.png`（512 × 240）、`Echo_Note_Paper.png`（256 × 256）、`Echo_Note_Tape.png`（256 × 72）。印章文字用 `Assets/Art Resources/UI/Fonts/Imprima 400.ttf`。
- `mockup.png`：定稿效果图，上中下依次是相册、手账顶栏、手账画布。图里的照片和便签文字是当时的测试内容，画布便签的位置以 `EchoTrigger` 的配置为准。

## 与其他部分的关系

- `Echo_Stamp.png` 放在 `Assets/Art Resources/UI/Player Panel/Photo Album/`，是 `Assets/Prefabs/Photo_Thumbnail_Prefab.prefab` 里 `EchoStamp` 的图，默认隐藏，由 `EchoPhotoMark.Show` 打开并把相框改成略暖的颜色。
- `Echo_Note_Paper.png`、`Echo_Note_Tape.png` 放在 `Assets/Art Resources/UI/Player Panel/Scrapbook/`，由场景里 `ScrapbookCanvas` 上 `ScrapBookCanvas` 的 Echo Note 字段引用，便签字体是 `NothingYouCouldDo-Regular SDF`。
- 便签贴在照片哪一侧由每个回响的 `EchoTrigger.noteLayout` 决定，拍照时随文字存进 `Echo/EchoPhotos.json`。

## 注意

- 便签纸是 Sliced Sprite，边框 (12, 22, 12, 12)，底边 22 像素留给撕边；重画后改了尺寸要同步边框。Pixels Per Unit 是 200，边框在画布上显示为一半大小。
- 三张图都开 mipmap 和三线性过滤；手账元素最大放大 3 倍。
- `EchoStamp` 带一份 `UIEffectReplica`，跟随 `PhotoImage` 上的 UIEffect，相册删除照片时印章和照片一起溶解；重做印章对象时要保留它。
