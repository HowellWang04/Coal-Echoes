# 0101-WGH Unity Project

USC 游戏设计硕士申请作品集项目。叙事探索游戏，主人公前往废弃选矿厂完成爷爷的遗愿。

## 文档索引

### 设计
- [游戏设计](Assets/For%20AI%20Agent/Design/game-design.md) — 故事、玩法机制、回响系统、物品分类、世界观
- [关卡设计](Assets/For%20AI%20Agent/Design/level-design.md) — 第一关及后续关卡设计
- [关卡物品清单](Assets/For%20AI%20Agent/Design/level-items.md) — 三大关卡 × 三类物品 × 叙事帮助 × 美术状态整合表

### 叙事
- [故事走向](Assets/For%20AI%20Agent/Narrative/story-flow.md) — 叙事框架、Echo 文字、爷爷日记内容
- [爷爷日记内容数据](Assets/For%20AI%20Agent/Narrative/grandpa-diary-content.json) — 游戏内爷爷日记 JSON 数据

### 技术
- [代码结构](Assets/For%20AI%20Agent/Technical/code-structure.md) — 系统分类、源码入口和详细文档门户
- [代码详解](Assets/For%20AI%20Agent/Technical/code-details.md) — 每个项目源码文件的职责、主要流程和协作关系
- [开发说明](Assets/For%20AI%20Agent/Technical/dev-guidelines.md) — 项目约束与代码风格

### 制作管理
- [美术资源列表](Assets/For%20AI%20Agent/Production/art-assets.md) — 美术/音频资源清单与制作状态
- [Todo List](Assets/For%20AI%20Agent/Production/todolist.md) — 按日期记录的待办事项
- [开发日志](Assets/For%20AI%20Agent/Production/development-log.md) — 按日期记录完成内容、设计决定、测试结果和下一步

## AI 协作原则

1. **动手前先想清楚**：有歧义时明确说出来，不默默选一种实现。有更简单方案时主动提出。
2. **最小实现**：只写任务要求的代码，不加没要求的功能、抽象或灵活性。
3. **外科手术式修改**：只改必须改的地方，不顺手重构周边代码，保持现有风格。
4. **目标驱动**：开始前明确验收标准，多步任务先列计划再执行。

## Unity 编辑器协作分工

1. AI 负责修改代码和项目文档，并在交付前完成必要的静态检查。
2. 需要 Unity Inspector、Hierarchy、场景对象或资源引用配置时，AI 提供清晰的逐步操作指引。
3. 用户负责 Unity 场景操作、资源绑定、Console 编译确认和 Play Mode 运行测试，并向 AI 反馈结果或截图。
4. AI 不主动控制 Unity 编辑器，也不主动使用 Windows GUI 自动化操作 Unity，除非用户在当前对话中明确授权。
5. Unity 运行验证以用户在编辑器中的 Console 和 Play Mode 结果为准，不为了常规验证运行完整解决方案的命令行编译。
