# ArtSource/MainMenu/Initial/

## 用途

开始菜单初版（透明英文 Logo 加 Imprima 按钮）的画面和检查记录。现在的主菜单是全屏双页报纸，见 `../../NewspaperMenu/`，但菜单控制脚本和运行规则没变。

## 内容

- `checks.txt`：初版的资源和场景配置检查。
- `menu-1920.png`、`menu-16x10.png`、`menu-4x3.png`：初版菜单在三种画幅下的编辑器预览。
- `coal-echoes-1920.png`、`coal-echoes-1280.png`：游戏改名为 Coal Echoes 时的 Imprima 文字标题预览。

## 与其他部分的关系

- 场景对象是 `In-game UI Root/MainMenuUIRoot`，控制脚本 `Assets/Scripts/MainMenu/MainMenuUIManager.cs`，背景镜头 `Gameplay Root/MainMenuCamera`。
- Play 时显示菜单、锁定玩家输入和移动、隐藏游戏内 UI；开始按钮接开场演出（见 `../Opening/`），结束后恢复操作；继续按钮有存档时可点，跳过开场直接恢复操作，没有存档时置灰；退出按钮在编辑器里停止 Play，在构建里退出游戏。
- UI 用项目的 Imprima 字体和 `UIAnimationScope`、`UIButtonTweenFeedback`，Canvas 为 Screen Space Overlay，参考尺寸 1920 × 1080，按高度缩放。Logo 来源见 `../Branding/`。

## 注意

- 菜单用独立镜头，不移动玩家、摩托车和回响测试桌。有存档时点开始按钮会清空全部存档，见 `../CLAUDE.md`。
