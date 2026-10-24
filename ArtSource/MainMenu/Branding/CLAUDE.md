# ArtSource/MainMenu/Branding/

## 用途

Coal Echoes《煤火回声》的独立品牌 Logo：旧厂印刷方向，英文 Bitter Regular 两行大写左对齐，短索道和吊斗放在 COAL 右侧空白处，带局部细小缺损；中英组合加一个暗赭红方印。主菜单已改成报纸版式，Logo 现在不在场景里显示。

## 内容

- `build_logo.py`：用 Pillow 和 fontTools 从真实字体生成轮廓和透明图像，`python build_logo.py` 重新导出；位置、字距、颜色、索道和缺损随机种子都在源码里。
- `Exports/`：暖纸白和煤灰黑两种颜色的英文主标、中英组合，以及去掉索道和磨损的缩小版，SVG 和透明 PNG 各一份。SVG 里的文字已转成轮廓，不需要安装字体。
- `Fonts/`：Bitter 可变字体和按 wght 400 实例化的 Regular、思源宋体 SC Medium，以及早期方案用过的 Oswald 和 Barlow Condensed，各自的 OFL 许可一起放。
- `Previews/`：`identity-board.png` 深色、浅纸色和缩小效果；`menu-logo-*.png`、`menu-bitter-play.png`、`menu-oswald-*.png` 是旧版菜单里 Logo 的编辑器和 Play 截图。
- `verification.json`：导出检查。

## 与其他部分的关系

- 运行图片 `Assets/Art Resources/UI/Branding/CoalEchoesLogo.png` 保留，导入为 Single Sprite；原来显示它的 `MenuContent` 已从主场景删除。
- 报纸菜单把 Bitter Regular 和思源宋体 Medium 连同许可复制到了 `Assets/Art Resources/UI/NewspaperMenu/Fonts/`，见 `../../NewspaperMenu/Spread/`。
- 英文字体来自 Google Fonts 官方仓库，中文来自 Adobe 思源宋体官方仓库。

## 注意

- 品牌色：暖纸白 `#E8DFCD`，煤灰黑 `#292724`，暗赭红 `#9A6146`。
- 本机 fontTools 装在忽略的 `Library/BrandingTools/`，其他环境自行安装同名依赖。
