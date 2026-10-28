# ArtSource/MainMenu/

## 用途

主菜单和开场相关的品牌、开场演出和初版菜单记录。现在的主菜单是全屏双页报纸，制作记录在 `../NewspaperMenu/`，这里放它之外的部分。

## 内容

- `Branding/`：Coal Echoes 独立品牌 Logo 的源脚本、导出、字体和许可，见其 CLAUDE.md。
- `Opening/`：报纸转场之后六段开场文字和镜头演出的记录，见其 CLAUDE.md。
- `Initial/`：初版菜单（透明英文 Logo 加 Imprima 按钮）的截图和检查记录，见其 CLAUDE.md。

## 与其他部分的关系

- 场景对象 `In-game UI Root/MainMenuUIRoot` 和控制脚本 `Assets/Scripts/MainMenu/MainMenuUIManager.cs` 贯穿三者：Play 时显示菜单并锁定输入，Start New Game 经报纸转场接 `OpeningUIRoot` 上的 `OpeningSequenceUI`，结束后交出操作；Continue 跳过转场和开场，直接交出操作。
- 报纸菜单的字体来自 `Branding/`，Logo 图片保留在 `Assets/Art Resources/UI/Branding/`，场景里不再显示。

## 注意

- Play 前取消勾选场景根物体 `Debug Tools` 上的 Show Main Menu 可以跳过菜单直接测游戏功能；Play Opening 控制 Start New Game 后播不播开场。
- Start New Game 按钮、标签和下划线 `StartUnderline` 都以目录栏中线居中；改按钮文字或字号后要同步加宽这三者，保证文字一行显示。
- Start New Game 是新游戏：有存档时调静态的 `GameSaveManager.ClearSaves`，删掉 `Application.persistentDataPath` 下全部存档目录并发出 `SavesCleared`，读过存档的系统各自回到新游戏状态，不重载场景，随后接报纸转场和开场；没有存档时直接接转场。
- Continue 只在有存档时可点（有进度存档，或旧存档已有日记），按钮引用填在 `MainMenuUIManager` 的 continueButton 上。
