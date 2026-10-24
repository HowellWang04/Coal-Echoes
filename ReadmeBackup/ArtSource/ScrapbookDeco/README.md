# 剪贴簿装饰素材

`Scripts/draw_scrapbook_art.py` 用 Pillow 程序化绘制剪贴簿文本框框图和五张贴纸，4 倍超采样后缩小，边缘平滑。

```
python -I Scripts/draw_scrapbook_art.py <输出目录>
```

## 输出

| 文件 | 尺寸 | 放到 |
| --- | --- | --- |
| `TextArea.png` | 1632×904 | `Assets/Art Resources/UI/Player Panel/Scrapbook/Scrapbook Elements/` |
| `Sticker_CablewayBucket.png` 索道煤斗 | 1024×1024 | 同上 `/Stickers/` |
| `Sticker_MinerHelmet.png` 矿灯安全帽 | 1024×1024 | 同上 |
| `Sticker_RidgeNight.png` 山顶夜景徽章 | 1024×1024 | 同上 |
| `Sticker_EnamelMug.png` 搪瓷缸 | 1024×1024 | 同上 |
| `Sticker_PlantStamp.png` 洗煤厂邮票 | 1024×1024 | 同上 |

## 说明

- `TextArea.png` 按原来 204×113 的框图 8 倍重画，形状和颜色照原图。它是 Multiple 模式的 Sprite，裁切区域同步放大为 (72, 56, 1512, 792)，Sprite ID 不变，场景引用不受影响。重新导出后如果改了画布大小，要同步改 `.meta` 里的 rect。
- 贴纸用 Player Panel 的暖棕色系，带白色模切边和淡投影；邮票保留齿孔，不加模切边。
- 所有图都开启 mipmap 和三线性过滤。剪贴簿元素最大放大 3 倍，4K 下文本框约 1200 像素宽、贴纸约 660 像素宽，这个分辨率放到最大也不会糊。
- 每张贴纸对应 `Assets/Prefabs/DecoPreset_<名字>.asset`，`presetName` 是剪贴簿存档键，定下后不要改。
