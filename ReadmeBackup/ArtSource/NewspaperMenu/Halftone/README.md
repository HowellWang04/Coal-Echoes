# 半调照片试验

2026 年 10 月 12 日先验证照片处理效果，供用户看图后微调。已加入 shader、可调材质及编辑器渲染脚本，尚未连接正式菜单的实时相机输出、全屏报纸布局或放大转场。

用户反馈首版规则圆点过于数码化，随后按提供的报纸照片改为灰黑油墨、柔和边缘、纸纤维与不均匀着墨主导的第二版。参考图只用于纸墨质感，不沿用其中的人物或政治内容。第二版效果图为 `ink-newsprint.png`，同次相机原图为 `ink-camera-original.png`。

随后用户指定《羊城晚报》照片为美术基准，并要求稍微更接近黑白。第三版仅调整纸墨色，纸底为 `(0.865, 0.85, 0.82)`，墨色为 `(0.16, 0.16, 0.155)`，当前预览改为 `neutral-newsprint.png`。双页重排要求与参考图细节见 `../ArtDirection.md`。

## 来源与适配

算法来自 [Daniel Ilett 的 shaders-halftone](https://github.com/daniel-ilett/shaders-halftone)，使用 MIT 许可。项目 shader 位于 `Assets/Scripts/MainMenu/NewspaperHalftone.shader`，与主菜单脚本放在一起；许可原文保存在同目录的 `DanielIlett-LICENSE.txt`。材质与贴图保留在 `Assets/Art Resources/UI/NewspaperMenu/`。

读取的原始文件为 [Halftone.shadergraph](https://github.com/daniel-ilett/shaders-halftone/blob/main/Assets/Shaders/Halftone.shadergraph)，Git blob 为 `9e01d3ff07355d0d7783e3d8957842b7f1894c35`。本次原文快照保存在 `Upstream/Halftone.shadergraph.txt`，放在 Assets 外，不向 HDRP 项目导入 URP Shader Graph。

保留原图的屏幕比例修正、Circle Density 缩放、旋转、Voronoi 距离与 Smoothstep 网点生成。原图 Voronoi 的 Angle Offset 为 0、Cell Density 为 5，因此种子固定在格点加上 `(0.5, 1)`，这里将九格搜索等价简化为周期距离。原图受光值来自法线与主光方向的点积，改为相机画面显示亮度映射到负一至一，之后沿用 Negate 与 Remap 对网点半径的控制。

上述网点算法现在只作为细弱的底层纹理。第二版以连续灰阶控制着墨量，增加五点软化模拟轻微扩墨，用现有 Newsprint 纹理及固定在照片坐标中的细粒、纤维和低频变化模拟纸张与缺墨，并抬高墨色黑位。纸墨混合在显示色空间完成，再转换回项目线性色空间；缩小时衰减过细网点，减轻规则花纹。它是接受 RenderTexture 的 GPU Blit shader，不生成或缓存一张固定厂区照片。

## 当前预览

全部照片由编辑器内 MainMenuCamera 的临时副本渲染，尺寸为 1920 × 1080。临时副本关闭时间抗锯齿以避免单帧历史残影，不改原相机或场景。单帧预览没有覆盖连续帧的闪烁和缩放时的摩尔纹，正式接入后需在 Play Mode 验收。

| 文件 | 内容 |
| --- | --- |
| neutral-newsprint.png | 当前第三版，暖灰纸底与更深的黑灰油墨 |
| neutral-camera-original.png | 第三版同次相机原图 |
| neutral-effect-zero.png | 第三版效果归零检查 |
| ink-newsprint.png | 第二版偏暖油墨与纸纤维效果，留作对照 |
| ink-camera-original.png | 第二版同次相机原图 |
| ink-effect-zero.png | 第二版效果归零的还原检查 |
| camera-original.png | 首版相机原图，留作记录 |
| halftone-newsprint.png | 首版粗网点，用户已否定其质感方向 |
| halftone-fine.png | 首版细网点对照，留作记录 |
| halftone-monochrome.png | 首版纯黑白对照，留作记录 |
| effect-zero.png | 首版效果归零检查 |

材质为 `Assets/Art Resources/UI/NewspaperMenu/Halftone/NewspaperHalftone.mat`。第二版 Circle Density 为 150、Visible Dot Strength 为 0.12、Ink Density 为 0.94、Ink Spread 为 1.15、Uneven Ink Coverage 为 0.22、Contrast 为 2.2。纸色为 `(0.87, 0.81, 0.64)`，墨色为 `(0.28, 0.285, 0.26)`。细网点密度与强度分开控制，纸色、墨色、扩墨和缺墨均可单独调整。

`ConfigureInk.cs` 记录当前材质参数设置。`Preview.cs` 通过 Unity CLI 的 `eval_file` 执行，重新读取当前相机并输出 `neutral` 开头的照片，只在材质不存在时创建材质，后续保留用户在 Inspector 中的调整；早期版本图片不再覆盖。

## 检查

第二版 shader 导入与实际 GPU 渲染无错误。Effect Strength 为 0 时，1920 × 1080 全图 RGB 与同次相机原图逐像素一致，最大通道差为 0。第二版结果见 `ink-checks.json`，首版结果保留在 `checks.json`。两次均未进入 Play Mode。

第三版重复实际渲染与还原检查，shader 无错误，归零后最大 RGB 通道差仍为 0，结果见 `neutral-checks.json`。本次未进入 Play Mode，也未修改正式菜单的场景布局。

首版最终复核确认临时相机数量为 0，正式 MainMenuCamera 仍输出屏幕；当时 Console 另有一条 `MapMagic/TerrainPreviewURP` 的 `_ALPHATEST_ON` 重复声明错误，未改第三方地形预览 shader。第二版沿用临时相机和渲染资源的 finally 清理路径，未改变正式场景。

双页排版预览新增整页纸纹映射参数 `_PaperUV`、`_PrintRect`、`_PaperSize`，让照片和报纸共享折痕与纤维坐标。预览副本将缺墨程度减至 0.09，原保存材质保持 0.22；结果及还原检查见 `../Spread/README.md` 与 `../Spread/checks.json`。

实时照片、照片放大与印刷效果消退已接入主场景，完成后调用原有抬头看天空和对白流程。运行材质为 `NewspaperSpread.mat`，HDRP Custom Pass 与两种画幅验证见 `../Spread/Runtime/CLAUDE.md`，连续操作仍待 Play Mode 验收。
