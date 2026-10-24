# ArtSource/ScrapbookDeco/

## 用途

剪贴簿文本框框图和五张贴纸的绘图脚本。贴纸按剧情画：索道煤斗、矿灯安全帽、山顶夜景徽章、搪瓷缸、洗煤厂邮票。

## 内容

- `Scripts/draw_scrapbook_art.py`：用 Pillow 程序化绘制，4 倍超采样后缩小。`python -I Scripts/draw_scrapbook_art.py <输出目录>` 输出 `TextArea.png`（1632 × 904）和 `Sticker_CablewayBucket.png`、`Sticker_MinerHelmet.png`、`Sticker_RidgeNight.png`、`Sticker_EnamelMug.png`、`Sticker_PlantStamp.png`（各 1024 × 1024）。

## 与其他部分的关系

- 文本框放在 `Assets/Art Resources/UI/Player Panel/Scrapbook/Scrapbook Elements/TextArea.png`，贴纸放在同目录 `Stickers/`。
- `TextArea.png` 是 Multiple 模式的 Sprite，按原来 204 × 113 的框图 8 倍重画，裁切区域同步放大为 (72, 56, 1512, 792)，Sprite ID 不变。
- 每张贴纸对应 `Assets/Prefabs/DecoPreset_<名字>.asset`，`presetName` 由 `ScrapBookCanvas` 写进存档作为剪贴簿里的元素 ID。

## 注意

- 重新导出后若改了画布大小，要同步改 `TextArea.png.meta` 里的 rect。
- `presetName` 是存档键，定下后不要改。`DecoPreset_Sticker1.asset` 是旧测试贴纸的预设，文件保留，旧存档里的测试贴纸读档后会消失。
- 所有图开 mipmap 和三线性过滤；剪贴簿元素最大放大 3 倍，这个分辨率在 4K 下放到最大不会糊。
- 贴纸用 Player Panel 的暖棕色系，带白色模切边和淡投影；邮票保留齿孔，不加模切边。
