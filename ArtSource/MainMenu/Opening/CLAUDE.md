# ArtSource/MainMenu/Opening/

## 用途

开场演出的记录：报纸菜单转场结束后，镜头抬向天空，逐段显示五段英文开场文字，再降到玩家视角，显示第一关章节标题后交出操作。

## 内容

- `checks.txt`：资源和场景配置检查。
- `opening-1920.png`、`opening-4x3.png`：两种画幅的编辑器预览。

## 与其他部分的关系

- 文字在 `Assets/Narrative/opening-intro.json` 的 `paragraphs` 里。
- `In-game UI Root/MainMenuUIRoot/MenuPresentation/Newspaper/Spread/StartGameButton` 调用 `MainMenuUIManager.StartGame`，报纸放大和纸墨消退结束后 `In-game UI Root/MainMenuUIRoot/OpeningUIRoot` 上的 `OpeningSequenceUI` 接管镜头和文字，主菜单继续维持输入锁定。两个脚本都在 `Assets/Scripts/MainMenu/`，转场见 `../../NewspaperMenu/Spread/Runtime/`。
- 文字淡入后逐字显示；框内左键把当前段加速，显示完后出现上下浮动的箭头，再点一次换段；最后一段确认后文字淡出，镜头降到玩家视角，角度和 FOV 对齐后才恢复操作。
- 菜单镜头和玩家测试点不在同一位置，全画面为天空时把演出镜头移到玩家相机位置再降下视角。同一刻调 `MainMenuUIRoot` 上 `OpeningSceneState.Restore`：摩托车出现、两扇门关回、锁链挂锁显示，所以镜头降下时玩家第一次看到的就是锁着的大门；存档里铁链已经剪断时，大门保持开着、不挂锁链。菜单显示时由 `MainMenuUIManager.Start` 调 `Prepare` 进入 1980 年状态；Continue 或跳过开场时菜单根物体关闭，`OnDisable` 还原。
- 镜头抬向天空时 OpeningUIRoot 下的 `LetterboxTop`、`LetterboxBottom` 两条黑边同时滑入，把画面收成 2.39:1；最后一段文字淡出后，黑边随镜头降到玩家视角一起收回。黑边高度按 OpeningUIRoot 当时的尺寸算，其他画幅也保持 2.39:1。黑边在 Backdrop 之上、TextArea 之下。
- 镜头降下、黑边收回时，OpeningUIRoot 下的 `ChapterTitle`（`ChapterLabel` 写 Chapter One，`TitleText` 写 The Factory Gate）同步渐显，停 2 秒后 1 秒渐隐，然后才恢复操作。两行文字直接写在 Inspector 里，用 Imprima 的 Hint Underlay 材质，压在亮地面上也看得清。
- Canvas 为 Screen Space Overlay，用 Imprima 字体。

## 注意

- 只接受框内左键，过渡期间的点击无效，重复点击不叠加加速。
- 有存档时点 Start New Game 先由 `GameSaveManager.ClearSaves` 原地清档，再开始报纸转场和开场；Continue 不播开场，直接交出操作。
- 要测开场结束后重新领取日记，Play 前选 `Tools > Coal Echoes > Diary > Preview Pickup Next Play`，只在本次 Play 临时重置日记持有状态。
