# 故事走向

记录游戏的叙事框架与具体文字内容。分四层：**爷爷旧日记原文**（预先存在的主线框架）、**Echo 细节**（拍摄 B 类物品触发的场景补充）、**主角便签**（由 Echo 触发后贴在日记固定位置上的客观线索连接）、**手账内容**（照片进入手账后显示的主角个人理解）。

---

## 一、总体叙事框架

主人公整理爷爷遗物 → 发现一张爷爷与工友在大门前的合照、一本旧日记簿 → 前往父亲老家（西北废弃选矿厂）→ 通过拍照触发回响、记录手账、对比爷爷日记 → 一步步走向选矿厂旁的山顶 → 在黎明朝日下拍下那张合照同款照片，完成爷爷遗愿。

**情感主线**：从"好奇爷爷的故事"到"读懂爷爷"。玩家可只靠 F 键走完流程，但只有拍摄 B 类物品才能真正理解爷爷。

---

## 二、叙事载体分工

| 载体 | 内容性质 | 口吻 | 字体/视觉 |
|------|---------|------|-----------|
| 爷爷日记原文（Tab） | 预先存在的主线情感框架，粗线条 | 第一人称，温情、回忆 | 爷爷旧日记字体，偏旧时代手写感 |
| Echo 文字（拍 B 类触发） | 具体场景的细节补充，可能来自物品文字或场景信息 | 视物品而定，多为事务性/客观记录 | 用于右上角提示、照片高亮依据、日记便签解锁依据 |
| 主角便签（爷爷日记 Tab） | Echo 触发后贴在爷爷旧日记固定位置上的纸质便签，负责把物品线索和旧日记段落连接起来 | 主角视角，短句、克制、偏客观线索 | 独立便签美术资源 + 主角便签字体；不直接写在爷爷旧日记本体上 |
| 手账内容（Scrapbook） | 照片拖入手账后显示的 `EchoTrigger.scrapbookContent`，负责主角更完整的情绪、推理和理解 | 主角视角，更私人、更完整 | 贴近主角笔记风格，不替代爷爷日记便签 |
| 场景本身（C 类对照） | 过去繁荣 vs 现在荒废 | 无文字，纯视觉 | N/A |

原则：**爷爷日记承载过去的声音，便签负责线索连接，手账承载主角更完整的当下理解，物品（值班本等）保持事务性口吻**。四者对照产生张力：旧日记是过去的声音，Echo 是现场证据，便签是连接线，手账是主角真正消化后的表达。

---

## 三、爷爷日记预配置与便签位

爷爷日记 Tab 需要提前配置：

- **日记原文**：爷爷本来写在旧日记里的内容。
- **文本区域标记**：UI 上每一页可以预先放多个 TextArea，每个 TextArea 配一个唯一 markerId，用来控制文本在页面上的位置。
- **内容来源**：日记正文在 JSON 文件里人工编写，并用 markerId 标记每段内容；游戏开始时读取 JSON，把内容分配到对应页的对应 TextArea。
- **便签位**：哪些页、段落旁、留白或页边位置会贴便签；位置固定，需要提前配置，并同样使用唯一 markerId / slotId 对应内容。
- **触发来源**：每个便签位对应哪个 B 类物品或 EchoTrigger。
- **便签内容**：主角写在便签上的文字，不直接写在爷爷旧日记本体上。
- **便签交互**：优先考虑静态贴纸式呈现；如果要做纸片拖拽/掀开，需要另行评估成本。当前方案为鼠标悬浮便签时降低透明度，显示便签下方的老日记内容，移开后恢复。

### JSON 配置格式草案

UI 负责位置，JSON 负责文字内容。示例：

```json
{
  "pages": [
    {
      "pageId": "p01",
      "pageIndex": 0,
      "pageTitle": "开篇",
      "textBlocks": [
        {
          "markerId": "p01_body_01",
          "content": "这里写第一页第一个 TextArea 的爷爷日记原文。"
        },
        {
          "markerId": "p01_body_02",
          "content": "这里写第一页第二个 TextArea 的爷爷日记原文。"
        }
      ],
      "stickyNoteSlots": [
        {
          "slotId": "p01_note_watchlog",
          "markerId": "p01_note_watchlog",
          "triggerId": "watch_log",
          "noteContent": "The north mountain road was not just a shortcut. Someone once ran toward the mountain from here, and Grandpa kept writing about that same ridge years later.",
          "unlockedByDefault": false
        }
      ]
    }
  ]
}
```

约定：

- `pageId` 对应一页日记。
- `textBlocks[].markerId` 对应 UI 上 markerId 为同名值的 TextArea。
- `stickyNoteSlots[].slotId` 对应 UI 上 slotId 为同名值的固定便签位。
- `stickyNoteSlots[].triggerId` 后续可对应场景中 `EchoTrigger` 的触发 ID。

---

## 四、分关卡故事节点

### 第一关：厂区大门（功能演示）

| 节点 | 类型 | 触发内容 | 状态 |
|------|------|---------|------|
| 抵达大门，铁链上锁 | 场景 | 引导玩家寻找其他入口 | 已定 |
| 进入门卫室 | 场景 | — | 已定 |
| 拍摄值班记录本 | B | 触发 Echo → 解锁爷爷日记上的固定便签，暗示山顶主线 | 已定（见下表） |
| 门外拾取梯子 | A | 无叙事，流程道具 | 已定 |
| 翻墙进厂，关卡结束 | 场景 | — | 已定 |

### 第二关：（待定）

- 地点：
- 核心 B 类物品：
- 主线推进：

### 后续关卡：（待定，结构同上）

---

## 五、Echo 文字内容表

拍摄每个 B 类物品触发的内容。`物品` 对应场景里的 `EchoTrigger` 组件。

| 物品 | 所在关卡 | EchoTrigger 配置 | 物品上的文字（事务性） | 解锁的便签位 | 便签内容 | 手账内容 | 状态 |
|------|---------|-------------------|---------------------|-------------|---------|---------|------|
| 值班记录本 | 第一关 | `stickyNoteSlotId = p01_note_watchlog` | **EN:** Guard Post Duty Log<br>Sept. 12, 1973 / Clear<br>06:10 Day shift entered through Main Gate.<br>18:50 Night shift changeover completed.<br>Note: One worker left the factory area by the north mountain road without permission. Reported to the shift lead.<br><br>**CN:** 门卫室值班记录<br>1973年9月12日 / 晴<br>06:10 早班经正门入厂。<br>18:50 晚班交接完成。<br>备注：一名工人未经许可从厂区北侧山路离开，已上报班组长。 | 第一关日记页，靠近爷爷关于厂北山路和山顶灯光的段落 | **EN:** The north mountain road was not just a shortcut. Someone once ran toward the mountain from here, and Grandpa kept writing about that same ridge years later.<br><br>**CN:** 厂北山路不只是近路。曾经有人从这里往山里跑，而多年以后，爷爷还一直在写同一条山脊。 | **EN:** I thought the mountain was only a backdrop. But the duty log makes Grandpa's repeated notes about the ridge feel less like scenery and more like a destination he could never let go of.<br><br>**CN:** 我原本以为那座山只是背景。但这本值班记录让我觉得，爷爷反复写到的那道山脊，不像风景，更像是他一直放不下的目的地。 | **定稿** |
| | | | | | |

Unity 场景中值班记录本的 `EchoTrigger` 建议填写：

```text
Scrapbook Content:
I thought the mountain was only a backdrop. But the duty log makes Grandpa's repeated notes about the ridge feel less like scenery and more like a destination he could never let go of.

Sticky Note Slot Id:
p01_note_watchlog
```

---

## 六、爷爷日记全文（按主线顺序）

玩家在「爷爷日记 Tab」中逐步解锁的内容，随关卡推进展开。

> （待写：开篇——整理遗物时的引子）

> 第一关·爷爷旧日记原文（草案）：那时候下了班，我总爱顺着厂北边那条小路往山上走。班组长嫌不安全，记过我好几回。可只有站到山顶上，才看得见整个厂子的灯一盏一盏亮起来——像谁把星星撒在了地上。我跟你奶奶说过，等老了，我还要再上去看一回。

> 第一关·值班本 Echo 解锁的主角便签：The north mountain road was not just a shortcut. Someone once ran toward the mountain from here, and Grandpa kept writing about that same ridge years later.

> 中文对照：厂北山路不只是近路。曾经有人从这里往山里跑，而多年以后，爷爷还一直在写同一条山脊。

> （待写：结尾——山顶朝日）
