# 任务提示的同步规则

给 AI 看。人改的是 [任务提示](../json-hints.md)，这份写怎么把它变成 JSON。

- 每关一份 `Assets/Narrative/levelN-hints.json`，第一关是 `level1-hints.json`，由场景里 `HintPopupUIRoot` 的提示弹窗引用。不直接手改 JSON。
- 用户说“同步提示”时按文档重新生成，结构是 `hints` → `tasks` → `subtasks`。
- 列和字段的对应：`hintId` 写进 `hintId`；“标题”写进 `heading`，同一条提示只看第一行；`taskId` 写进 `taskId`；“上级任务”留空的是任务，填了的是那项任务的子任务；“文本”写进 `text`；“中文意思”只给人看，不进 JSON。
- 表里的行序就是显示顺序。
- `hintId` 就是设计文档里的提示编号（T1、T2……），每关从 T1 编起。`hintId` 和 `taskId` 是代码打勾用的键，定下后不要改，改了要同步 `Level1HintFlowManager` 等调用方。
- 游戏里的表现：右上角弹窗一次显示一条提示；当前提示全部打勾后，等独白播完再换下一条；有子任务的任务只能靠子任务全部完成来打勾。
- F 交互提示（比如第一关的 T7）和右上角通知不在这份文档里，写在对应的场景物体或组件上。
- 改完同步 [游戏文本](../game-text.md)。
