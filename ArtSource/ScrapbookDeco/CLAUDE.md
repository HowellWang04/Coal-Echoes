# ArtSource/ScrapbookDeco/

## 用途

剪贴簿文本框框图、五张贴纸和三枚油墨印章的绘图脚本。贴纸按剧情画：索道吊斗、矿灯安全帽、山顶夜景徽章、搪瓷缸、洗煤厂邮票。印章是外公进厂那天的石炭井日期戳（1980.10.03）、中间画厂房烟囱剪影的石炭井洗煤厂公章、挂着吊斗的索道塔架章。

## 内容

- `Scripts/draw_scrapbook_art.py`：用 Pillow 程序化绘制，4 倍超采样后缩小。`python -I Scripts/draw_scrapbook_art.py <输出目录>` 输出 `TextArea.png`（1632 × 904），`Sticker_CablewayBucket.png`、`Sticker_MinerHelmet.png`、`Sticker_RidgeNight.png`、`Sticker_EnamelMug.png`、`Sticker_PlantStamp.png`，以及 `Stamp_Postmark1980.png`、`Stamp_PlantSeal.png`、`Stamp_CablewayTower.png`（各 1024 × 1024）。印章文字用 `Assets/Art Resources/UI/Fonts/Imprima 400.ttf`。

## 与其他部分的关系

- 文本框放在 `Assets/Art Resources/UI/Player Panel/Scrapbook/Scrapbook Elements/TextArea.png`，贴纸放在同目录 `Stickers/`，印章放在同目录 `Stamps/`。
- `TextArea.png` 是 Multiple 模式的 Sprite，按原来 204 × 113 的框图 8 倍重画，裁切区域同步放大为 (72, 56, 1512, 792)，Sprite ID 不变。
- 每张贴纸和印章对应 `Assets/Prefabs/DecoPreset_<名字>.asset`，`presetName` 由 `ScrapBookCanvas` 写进存档作为剪贴簿里的元素 ID。贴纸预设的 `decoType` 是 Sticker，印章是 Stamp，决定它在剪贴簿顶栏 Decos 下拉菜单里归哪一类；预设要加进场景 `ScarpbookTab` 上 `ScrapBookUIManager` 的 Deco Presets 才会出现在顶栏。

## 注意

- 重新导出后若改了画布大小，要同步改 `TextArea.png.meta` 里的 rect。
- `presetName` 是存档键，定下后不要改。`DecoPreset_Sticker1.asset` 是旧测试贴纸的预设，文件保留，旧存档里的测试贴纸读档后会消失。
- 所有图开 mipmap 和三线性过滤；剪贴簿元素最大放大 3 倍，这个分辨率在 4K 下放到最大不会糊。
- 贴纸用 Player Panel 的暖棕色系，带白色模切边和淡投影；邮票保留齿孔，不加模切边。
- 印章照回响照片 ECHO 印章的画法，单色半透明、带磨损斑和细噪点，没有白边和投影；墨色分蓝黑、印泥红、深棕三种。放在画布上和贴纸一样，存档类型也记作 sticker。
- 图案避开锤镐这类政治意味重的符号，公章中间画厂房，不画五角星；日期戳底部的小星是用户确认保留的邮戳装饰。
