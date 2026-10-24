# Coal Echoes 煤火回声 Logo

主菜单现已改用中文报头与圆形索道标记的悬浮报纸，当前实现见 `ArtSource/NewspaperMenu/README.md`。本目录保留独立品牌 Logo 及此前菜单的制作记录。

2026 年 10 月 11 日制作。采用用户确认的旧厂印刷方向，英文为 Bitter Regular，中文为 Source Han Serif SC Medium。英文使用两行左对齐的大写字，短索道与吊斗置于 COAL 右侧空白处，绳索线宽为源图的 4.5 单位，局部细小缺损表现旧印刷；中英组合加入一个暗赭红方印。

## 文件

`Exports/` 保存暖纸白与煤灰黑的英文主标、中英组合，以及去掉索道和磨损的缩小版。SVG 中的文字已转换为原字体轮廓，可直接编辑路径，无需在查看设备上安装字体。PNG 均为透明 RGBA，英文主标为 2400 × 1500，中英组合为 2400 × 1760，另有宽 240 像素的缩小样本。

`Previews/identity-board.png` 展示深色、浅纸色与缩小效果。`menu-logo-1920.png` 和 `menu-logo-1280.png` 是 Unity 编辑器的临时相机 Canvas 预览，尺寸分别为 1920 × 1080 和 1280 × 960。正式菜单使用 Screen Space Overlay。

`build_logo.py` 从真实字体生成轮廓与透明图像，使用 Pillow 和 fontTools。可通过 `python build_logo.py` 重新导出。本机 fontTools 安装在忽略的 `Library/BrandingTools/`，其他环境可安装同名依赖。源码中的位置、字距、颜色、索道和缺损随机种子可编辑。

## 字体与配色

英文源文件来自 [Google Fonts 官方仓库](https://github.com/google/fonts/tree/main/ofl/bitter)，中文源文件来自 [Adobe 思源宋体官方仓库](https://github.com/adobe-fonts/source-han-serif/tree/release/OTF/SimplifiedChinese)。Bitter Regular 从官方可变字体按 wght 400 实例化。原始字体、实例化字体和各自的 OFL 许可文本保存在 `Fonts/` 中。后续报纸菜单将 Bitter Regular 与思源宋体 Medium 连同许可复制至 Unity 报纸资源目录，并烘焙静态 TMP 字库。

暖纸白为 `#E8DFCD`，煤灰黑为 `#292724`，暗赭红为 `#9A6146`。主菜单使用单色暖纸白英文主标，按钮与正文继续使用 Imprima。

## Unity

运行图片位于 `Assets/Art Resources/UI/Branding/CoalEchoesLogo.png`，导入为 Single Sprite，最大尺寸 4096，关闭压缩和 Mipmap，启用透明度。

主场景 `MainMenuUIRoot/MenuPresentation/MenuContent/CoalEchoesLogo` 使用 Image 显示，参考宽度 480、高度 300，保持比例且不拦截点击。原 `CoalTitle` 与 `EchoesTitle` 对象停用，按钮位置、事件和菜单控制脚本保持原配置。主场景已保存，修改前快照在本机 `Library/BrandingImplementation/Before.unity`。

标题下方 `TitleRule` 按截图标注延长至 464 单位宽，保留 2 单位粗细与左对齐。最新编辑器预览已包含该改动，历史 Play 截图仍为短分割线。

2026 年 10 月 21 日，主菜单已换成报纸版式，`MenuContent` 连同 `CoalTitle`、`EchoesTitle`、`TitleRule`、`CoalEchoesLogo` 已从主场景删除。上面两段是旧菜单的记录，Logo 图片资源仍保留在原路径。

两种比例的 Bitter 编辑器渲染已核对，文字、中文四字、透明背景及缩小版均已检查。重新进入 Play 后实际画面正常，截图见 `Previews/menu-bitter-play.png`，最终观感待用户确认。

历史 Oswald 版本的旧诊断截图 `menu-oswald-play.png` 未捕获到 Logo，后续重新进入 Play 显示正常，用户也确认可见，记录见 `menu-oswald-play-confirmed.png`。本次替换 Bitter 时再次观察到 Play 中重导入图片后 Logo 消失，重进 Play 恢复；后续在编辑模式导入，按用户要求不添加热导入处理代码。
