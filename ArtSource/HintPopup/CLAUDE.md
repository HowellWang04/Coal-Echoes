# ArtSource/HintPopup/

## 用途

右上角任务提示勾选框里的铅笔涂抹。任务完成时，涂抹从框的左上角往右下角一笔一笔画出来。

## 内容

- `build_scribble_mark.py`：用 numpy 和 Pillow 生成涂抹图和效果图，`python build_scribble_mark.py` 重新导出；笔画条数、线宽、折返点范围和随机种子都在源码开头。
- `Previews/HintBox_Scribble_Frames.png`：涂抹画到不同进度时的样子，上排是大图，下面两排是游戏里 22 像素的实际大小放大 4 倍，分别压在深色和浅色底上。

## 与其他部分的关系

- 脚本直接写出 `Assets/Art Resources/UI/Hint Popup/HintBox_Scribble.png`，256 像素，和 64 像素的空框 `HintBox_Unfinished.png` 同一画框。透明度是涂抹本身，红色通道是铅笔走到这个像素的时刻，左上第一笔为 0，右下最后一笔为 1。
- 材质 `Assets/Art Resources/UI/Hint Popup/HintMarkScribble.mat` 用 `Assets/Scripts/In-Game Systems/Hint System/HintMarkScribble.shader`，只显示红色通道小于 `_Progress` 的部分，颜色只取 Image 的颜色。
- 行预制体 `Assets/Prefabs/HintTaskRow.prefab` 的 `Box/Mark` 用这张图和这个材质，`HintTaskRow` 给每行复制一份材质，完成时把 `_Progress` 从 0 推到 1；`HintPopupUIManager` 让同时完成的几行排队逐个涂，父任务排在它最后一个子任务之后。

## 注意

- 贴图导入要关 sRGB，红色通道才按原值读；同时开 mipmap、不压缩、不勾 Alpha Is Transparency。重新运行脚本只覆盖 png，导入设置保留在 .meta 里。
- `Hint Popup/` 下的 `HintBox_Mark.png`、`HintBox_Finished.png` 和 `HintPopup_BG.png` 已不再使用。
- 效果图按脚本里的公式模拟 shader，最终观感以 Play Mode 为准。
