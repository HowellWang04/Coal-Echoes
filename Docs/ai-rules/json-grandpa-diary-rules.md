# 外公日记的同步规则

给 AI 看。人改的是 [外公日记](../json-grandpa-diary.md)，这份写怎么把它变成 JSON。

- 生成 `Assets/Narrative/grandpa-diary-content.json`，由主场景直接引用。不直接手改 JSON。
- 用户说“同步日记”时按文档重新生成，保持 JSON 结构不变。
- 每个 `## pNN 标题` 是一页日记，对应设计文档里的 P1、P2……
- 正文换行写 `<br>`，空一行写 `<br><br>`。
- 改完同步 [游戏文本](../game-text.md)。
