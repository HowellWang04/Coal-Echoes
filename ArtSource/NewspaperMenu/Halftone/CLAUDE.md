# ArtSource/NewspaperMenu/Halftone/

## 用途

报纸照片纸墨处理 shader 的试验目录：各版效果预览、材质配置脚本和上游算法快照。正式 shader 在 `Assets/Scripts/MainMenu/NewspaperHalftone.shader`。

## 内容

- `Preview.cs`：Unity CLI `eval_file` 在编辑模式下运行，用 MainMenuCamera 的临时副本渲染 `neutral` 开头的照片，只在材质不存在时创建材质，保留 Inspector 里的调整。`ConfigureInk.cs`：写入当前纸墨配色。
- `neutral-newsprint.png`、`neutral-camera-original.png`、`neutral-effect-zero.png`：当前偏黑白版本的效果图、同次相机原图和效果归零检查。
- `ink-*.png`：上一版偏暖油墨，`halftone-*.png`、`camera-original.png`、`effect-zero.png`：首版规则网点，都只作对照。
- `checks.json`、`ink-checks.json`、`neutral-checks.json`：各版的渲染和归零检查结果。
- `Upstream/Halftone.shadergraph.txt`：上游 Shader Graph 原文快照，放在 Assets 外。

## 与其他部分的关系

- shader 的网点算法改自 Daniel Ilett 的 shaders-halftone（MIT），许可原文是同目录的 `DanielIlett-LICENSE.txt`。它是接受 RenderTexture 的 GPU Blit shader，不生成固定照片。
- 材质在 `Assets/Art Resources/UI/NewspaperMenu/Halftone/`：`NewspaperHalftone.mat` 是这里预览用的，`NewspaperSpread.mat` 是主场景的运行材质，运行方式见 `../Spread/Runtime/CLAUDE.md`。纸纹贴图是 `Assets/Art Resources/UI/NewspaperMenu/Newsprint.png`。
- 美术基准见 `../ArtDirection.md`，参考图在 `../References/`。

## 注意

- Effect Strength 为 0 时输出要和相机原图逐像素一致，改 shader 后用 `Preview.cs` 复查归零。
- 纸墨混合在显示色空间完成再转回线性；缩小显示时要衰减过细的网点，否则出规则花纹。
- 预览是关闭时间抗锯齿的单帧编辑器渲染，连续帧闪烁和缩放摩尔纹要在 Play Mode 看。
- 参考报纸只借纸墨质感，不沿用其中的人物和政治内容。
