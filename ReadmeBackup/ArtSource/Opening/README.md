# 开场演出

五段英文开场文字放在 `Assets/Narrative/opening-intro.json`。

主场景通过 `MainMenuUIRoot/MenuPresentation/Newspaper/Spread/StartGameButton` 调用 MainMenuUIManager.StartGame。菜单在 0.3 秒内淡出，OpeningUIRoot 上的 OpeningSequenceUI 接管镜头与文字，主菜单继续维持输入锁定。两个脚本均位于 `Assets/Scripts/MainMenu/`。

镜头用 1.5 秒抬向天空，文字以 0.35 秒淡入、每秒 32 个字符显示。框内左键将当前段加速到每秒 128 个字符，重复点击不叠加；显示完后出现上下浮动的箭头，再次点击才换段。只接受框内左键，过渡期间的点击无效。最后一段确认后文字淡出，镜头用 1.5 秒降到玩家视角，角度和 FOV 对齐后才恢复操作。

菜单镜头和玩家测试点不在同一位置，因此在全画面为天空时把演出镜头移至玩家相机位置，再降下视角。正式运行使用 Screen Space Overlay Canvas，Imprima 字体大小为参考高度 1080 下的 36。

当前沿用现有存档。新游戏初始化与继续读档仍待实现，Continue 暂时置灰。若要测试开场结束后重新领取日记，在 Play 前选择 `Tools > Coal Echoes > Diary > Preview Pickup Next Play`，仅本次 Play 临时重置日记持有状态，其他记录仍沿用原有存档。
