# 煤火回声 Coal Echoes

[English](README.md) | 中文

USC 游戏设计硕士申请作品集。第一人称叙事探索游戏。主人公带着外公的旧日记和一张进厂那天的合照，回到西北一座废弃的洗煤厂。1980 年，外公和一位工友约好等两人都歇班的晚上，扒上回山的空吊斗，到山脊上看全厂的灯火。女儿周岁、山洪冲垮塔架、两人倒班错开，一句“下回”一直拖到 1988 年停产。主人公照着日记在厂里找线索，一路拍照、做手账，找到工友画在旧工票背面的上山路线，让停了几十年的索道重新转起来，坐吊斗上山，在月夜的山脊上拍下厂区和星空，和外公的旧合照收进同一本手账。

## 打开项目

用 Unity 6000.4.6f1 打开，主场景是 `Assets/Scenes/Main Game Scene.unity`，构建列表里只有这一个场景。

## 项目结构

```diff
 ./
 ├── Assets/
 │   ├── Scenes/                    主场景
 │   ├── Scripts/
 │   │   ├── MainMenu/              报纸主菜单、照片转场、开场演出
 │   │   ├── In-Game Systems/       相机拍照、角色移动、输入、物品检视、回响、手账、提示、独白、过场文字
 │   │   ├── Player Panel/          玩家面板：相册、手账、外公日记、背包
 │   │   ├── UI Animation/          DOTween 按钮、页签、面板动效
 │   │   ├── Environment/           索道运行、沙尘、热浪、月光
 │   │   └── Terrain/               MapMagic 地形高度源与接缝处理
 │   ├── Narrative/                 游戏文字 JSON：外公日记、内心独白、任务提示、过场文字、报纸、开场文字
 │   ├── Art Resources/             模型、材质、UI、第三方素材包
 │   ├── Prefabs/                   UI 预制体和手账装饰预设
 │   ├── Settings/                  HDRP Volume、热浪与沙尘资源
 │   ├── TerrainData/               地形数据（Git LFS）
 │   └── Plugins/                   DOTween、MapMagic、TextMesh Pro
 ├── ArtSource/                     美术源文件，主要是 Blender 工程（.blend）和 Python 建模、贴图脚本
 ├── TerrainReferences/             真实地形高度图和导入记录
 ├── Docs/                          设计文档、剧本、游戏文本、关卡和系统 Spec、技术栈、开发日志
 ├── ReadmeBackup/                  改写前的旧 README 备份
 ├── AGENTS.md                      给 AI 助手的工作规则（Codex 读取）
 ├── CLAUDE.md                      引用 AGENTS.md（供 Claude Code 读取）
+├── README.md                      中英文项目介绍，文档门户入口，供人类阅读了解本项目
 └── CONTEXT.md                     项目术语
```

## 文档

- [设计文档](Docs/design.md)：关卡、键位、系统组成、关卡流程
- [剧本](Docs/script.md)：按场次写的剧情
- [游戏文本](Docs/game-text.md)：按出现顺序汇总游戏里的全部文字
- [美术资源列表](Docs/art-resources.md)：按关卡列要做的美术资源和用途
- [音效列表](Docs/sound-effects.md)：按关卡列要做的音效和环境声
- 关卡 Spec：[开场与第一关](Docs/Specs/Levels/opening-level-one-spec.md)、[第二关](Docs/Specs/Levels/level-two-spec.md)、[第三关](Docs/Specs/Levels/level-three-spec.md)、[第四关](Docs/Specs/Levels/level-four-spec.md)
- 系统 Spec：[回响系统](Docs/Specs/Systems/echo-system-spec.md)、[回响取景提示](Docs/Specs/Systems/echo-targeting-spec.md)、[月光](Docs/Specs/Systems/moonlight-spec.md)
- [外公日记](Docs/json-grandpa-diary.md)：日记正文，日记 JSON 的来源
- [技术栈](Docs/tech-stack.zh-CN.md)
- [开发日志](Docs/development-log.md)
