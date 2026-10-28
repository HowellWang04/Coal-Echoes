# 任务提示的同步规则

给 AI 看。人改的是 [任务提示](../json-hints.md)，这份写怎么把它变成 JSON。

- 每关一份 `Assets/Narrative/levelN-hints.json`，第一关是 `level1-hints.json`，由场景里 `HintPopupUIRoot` 上 `HintPopupUIManager` 的 `levelHintJsons` 按关卡顺序引用；新加一关要把它的 JSON 接到列表末尾。不直接手改 JSON。
- 用户说“同步提示”时按文档重新生成，结构是 `hints` → `tasks` → `subtasks`。
- 列和字段的对应：`hintId` 写进 `hintId`；“标题”写进 `heading`，同一条提示只看第一行；`taskId` 写进 `taskId`；“上级任务”留空的是任务，填了的是那项任务的子任务；“文本”写进 `text`；“中文意思”只给人看，不进 JSON。
- 表里的行序就是显示顺序。
- 按键在文档里写成方括号，比如 `[Tab]`，同步时“文本”列换成 TMP 图标标签 `<sprite name="Tab">`，在游戏里显示成按键图标；“中文意思”列照写方括号，不进 JSON。方括号里只能写图标资源 `Assets/Art Resources/UI/Hint Popup/HintKeyIcons.asset` 里有的键：`1`、`E`、`F`、`R`、`Tab`、`V`，大小写照这里。要用别的键，先加进 `HintPopupSetup` 的 `KeyIcons`，再运行 `Tools > Coal Echoes > Hint Popup > Build Key Icon Sprite Asset` 重建。“标题”列不支持按键图标。
- `hintId` 就是设计文档里的提示编号，写成 T 加关卡号、点、本关序号，比如第一关是 T1.1、T1.2……，第二关是 T2.1、T2.2……，每关从 .1 编起，不同关卡的编号不会重复。`taskId` 用所属提示的编号开头，加下划线和英文短名，比如 `T1.5_match_photo`。`hintId` 和 `taskId` 是代码打勾用的键，定下后不要改，改了要同步 `Level1HintFlowManager` 等调用方，以及场景里 UnityEvent 填的字符串（第一关有 `FactoryGate`、门卫室的门和大门铁链三处）。
- 游戏里的表现：右上角弹窗一次显示一条提示；当前提示全部打勾后，等独白播完再换下一条；有子任务的任务只能靠子任务全部完成来打勾。
- F 交互提示（比如第一关的 T1.8）和右上角通知不在这份文档里，写在对应的场景物体或组件上。
- 改完同步 [游戏文本](../game-text.md)。
