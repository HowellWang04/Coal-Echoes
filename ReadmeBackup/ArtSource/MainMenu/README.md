# 开始菜单

当前游戏名为 Coal Echoes，中文名为《煤火回声》。现已改为全屏双页报纸菜单，点击开始后放大实时厂区照片，再恢复实机画面并接原开场，当前实现见 [报纸菜单](../NewspaperMenu/README.md)。本目录保留此前使用 Bitter Regular 透明英文 Logo 和 Imprima 按钮的菜单历史，独立 Logo 源文件、字体许可与中英组合见 [Logo 记录](../Branding/README.md)。

`coal-echoes-1920.png` 和 `coal-echoes-1280.png` 保留刚改名时的 Imprima 文字标题预览。下文为初版菜单记录，当前报纸菜单的 Play Mode 验收单独记录。

2026 年 10 月 11 日。这里保留开始菜单初版的画面与检查记录。后续已接通四段开场演出，当前记录见 `ArtSource/Opening/`。

主场景对象为 `MainMenuUIRoot`，控制脚本为 `MainMenuUIManager`，背景镜头为 `Gameplay Root/MainMenuCamera`。UI 使用项目 Imprima 字体、现有 UIAnimationScope 和 UIButtonTweenFeedback，画面按高度缩放，参考尺寸为 1920 × 1080。渐变遮罩为编辑器生成的 UI 贴图。

Play 时显示菜单并锁定 Gameplay 输入与移动，隐藏原有游戏内 UI。开始按钮已接开场演出，结束后恢复操作；继续按钮暂时置灰；退出按钮在编辑器中停止 Play，在构建中退出，用户已确认退出不再报错。没有重置或删除任何玩家存档。

在 Play 前关闭 MainMenuUIRoot 组件中的 Show On Start，可继续测试原有游戏功能。菜单使用独立镜头，未移动玩家、摩托车或回响测试桌。

`menu-1920.png`、`menu-16x10.png`、`menu-4x3.png` 是编辑器中的相机渲染预览，使用临时 Screen Space Camera Canvas 展示同一菜单层级。正式运行使用 Screen Space Overlay，文字不受场景后期影响。临时预览对象已清理。

`checks.txt` 记录 21 项资源和场景配置检查。Unity 编辑器脚本编译通过，未进入 Play Mode，鼠标反馈、淡入、输入锁定及最终观感由用户试玩确认。编辑前场景快照位于本机 `Library/MainMenuImplementation/Before.unity`。
