# 过场文字的同步规则

给 AI 看。人改的是 [过场文字](../json-cutscene-text.md)，这份写怎么把它变成 JSON。

- 所有关卡共用一份 `Assets/Narrative/cutscene-text.json`。由 `In-game UI Root/CutsceneUIRoot` 上的 `CutsceneTextUI` 按 `id` 读，做法见[第二关 Spec](../Specs/Levels/level-two-spec.md)的传送和过场。不直接手改 JSON。
- 用户说“同步过场”时按文档重新生成，结构是 `cutscenes` 列表，每项是 `id` 和 `paragraphs`，`paragraphs` 是字符串数组，一项一段。
- 列和字段的对应：`id` 写进 `id`；同一个 `id` 的几行是同一段过场，按行序写进 `paragraphs`；“触发时机”只看第一行，和“中文意思”一样只给人看，不进 JSON。
- `id` 用英文小写短名加下划线，说明是哪一段过场，比如 `enter_dormitory`。过场组件按 `id` 播放，定下后不要改，改了要同步调用方。
- 一行是一段，段内不换行。一段在屏幕中央排得太长时拆成两行，像开场文字的第四段那样。
- 游戏里的表现：画面淡出后在屏幕中央逐段显示，读法和开场文字相同，逐字显示、点击加速、点击换段、不能跳过；读完画面淡入，玩家已经在下一处。调用方可以把几个 `id` 放进同一次黑屏连着读，比如 `leave_dormitory` 接着 `follow_map`，中间不淡入。
- 开场文字不在这份文档里，它在 `Assets/Narrative/opening-intro.json`，由 `OpeningSequenceUI` 读。
- 改完同步 [游戏文本](../game-text.md)。
