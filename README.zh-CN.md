# Project Coal

[English](README.md) | 中文

USC 游戏设计硕士申请作品集。第一人称叙事探索游戏。主人公带着爷爷的旧日记和一张工友合照，回到西北一座废弃洗煤厂。爷爷和一位工友曾约着上山看厂区夜景，却因工作排班和夜间上山限制一再推迟，直到停产也未成行。主人公一路拍照、做手账，沿旧运煤索道上山，最后在夜晚拍下灯火大多已经熄灭的厂区，将新照片收进自己的手账，回应两人未完成的约定。

用 Unity 6000.4.6f1 打开，主场景是 `Assets/Scenes/Main Game Scene.unity`。

## 项目结构

```
Project Coal/
├── Assets/
│   ├── Scenes/                    主场景
│   ├── Scripts/
│   │   ├── In-Game Systems/       相机拍照、角色移动、输入、物品检视、回响、手账
│   │   ├── Player Panel/          玩家面板：相册、手账、爷爷日记、背包
│   │   ├── UI Animation/          DOTween 按钮、页签、面板动效
│   │   ├── Environment/           索道运行、沙尘、热浪
│   │   └── Terrain/               MapMagic 地形接缝处理
│   ├── Narrative/                 爷爷日记 JSON（主场景直接读取）
│   ├── Art Resources/             模型、材质、UI、第三方素材包
│   ├── Prefabs/                   UI 预制体和手账装饰预设
│   ├── Settings/                  HDRP Volume、热浪材质
│   ├── TerrainData/               地形数据（Git LFS）
│   ├── SmokePortalTest/           沙尘效果资源和测试场景
│   └── Plugins/                   DOTween、MapMagic、TextMesh Pro
├── ArtSource/                     美术源文件，主要是 Blender 工程（.blend）和 Python 建模脚本，另有导出备份和装配记录；Unity 不导入
├── TerrainReferences/             真实地形高度图和导入记录
├── Docs/                          项目文档
├── AGENTS.md                      给 AI 助手的工作规则
├── README.md                      英文说明
└── CONTEXT.md                     项目术语
```

## 文档

- [游戏设计](Docs/game-design.md)：玩法、回响系统、关卡与物品
- [故事](Docs/story-flow.md)：叙事分层、已定稿文字
- [技术栈](Docs/tech-stack.md)
- [美术资源](Docs/art-assets.md)
- [待办](Docs/todolist.md)
- [开发日志](Docs/development-log.md)
