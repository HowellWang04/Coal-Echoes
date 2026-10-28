# AGENTS.md

## 文档入口

| 文档                                                             | 文件                                          | 内容                                                                                                                                                                        |
| ---------------------------------------------------------------- | --------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [README](README.md)                                              | `README.md`                                   | 项目介绍、目录结构                                                                                                                                                          |
| [CONTEXT](CONTEXT.md)                                            | `CONTEXT.md`                                  | 项目术语                                                                                                                                                                    |
| [设计文档](Docs/design.md)                                       | `Docs/design.md`                              | 给人阅读，关卡、键位、系统组成、关卡流程                                                                                                                                    |
| [设计文档规则](Docs/ai-rules/design-rules.md)                    | `Docs/ai-rules/design-rules.md`               | 给 AI 的设计文档写法：写什么、不写什么、细节放哪；改设计文档前先读                                                                                                          |
| [美术资源列表](Docs/art-resources.md)                            | `Docs/art-resources.md`                       | 给人阅读，按关卡列要做的美术资源和用途                                                                                                                                      |
| [美术资源列表规则](Docs/ai-rules/art-resources-rules.md)         | `Docs/ai-rules/art-resources-rules.md`        | 给 AI 的美术资源列表写法；改列表前先读                                                                                                                                      |
| [音效列表](Docs/sound-effects.md)                                | `Docs/sound-effects.md`                       | 按通用和各关卡列要做的音效、环境声和用途，给人看；不含音乐                                                                                                                  |
| [开场与第一关 Spec](Docs/Specs/Levels/opening-level-one-spec.md) | `Docs/Specs/Levels/opening-level-one-spec.md` | 对照设计文档的开场和第一关实现清单，已实现、未实现、美术资源、待确认、测试入口                                                                                              |
| [第二关 Spec](Docs/Specs/Levels/level-two-spec.md)               | `Docs/Specs/Levels/level-two-spec.md`         | 对照设计文档的第二关实现清单，精简后的流程：开头衔接、进出 207 室的过场和传送、207 室、山路图、读档、美术资源                                                               |
| [第三关 Spec](Docs/Specs/Levels/level-three-spec.md)             | `Docs/Specs/Levels/level-three-spec.md`       | 第三关开头跟着山路图到主路尽头、沿小路走到下站，中段拿 Oil Can 给电机上油、索道动起来，结尾按 F 上吊斗、坐吊斗上山、长按 Space 跳过、快到站时的黑屏过场文字、吊斗在山顶站门口停下后转身上站台的流程和技术方案 |
| [第四关 Spec](Docs/Specs/Levels/level-four-spec.md)              | `Docs/Specs/Levels/level-four-spec.md`        | 第四关从上站站台走栈道上山头、读日记最后一页、拉近拍宿舍楼、广角拍厂区和星空、放进手账、读结尾后淡回山头的流程，两张照片的判定和读档 |
| [回响系统 Spec](Docs/Specs/Systems/echo-system-spec.md)          | `Docs/Specs/Systems/echo-system-spec.md`      | 回响分类、玩家流程、已实现与待实现                                                                                                                                          |
| [回响取景提示 Spec](Docs/Specs/Systems/echo-targeting-spec.md)   | `Docs/Specs/Systems/echo-targeting-spec.md`   | 回响取景框、判定规则、参考图                                                                                                                                                |
| [月光 Spec](Docs/Specs/Systems/moonlight-spec.md)                | `Docs/Specs/Systems/moonlight-spec.md`        | 第四关和结尾的月夜，意境、何时切换、Volume 与平行光的做法、存档恢复                                                                                                         |
| [游戏文本](Docs/game-text.md)                                    | `Docs/game-text.md`                           | 按出现顺序汇总游戏内全部文字，给人通读；改了来源文件要同步这里                                                                                                              |
| [剧本](Docs/script.md)                                           | `Docs/script.md`                              | 按场次写的游戏剧本，也是 video essay 的流程底稿                                                                                                                             |
| [技术栈](Docs/tech-stack.zh-CN.md)                               | `Docs/tech-stack.zh-CN.md`                    | 引擎、插件、地形与美术工具                                                                                                                                                  |
| [开发日志](Docs/development-log.md)                              | `Docs/development-log.md`                     | 按日期记录做了什么、为什么、还差什么                                                                                                                                        |
| `ArtSource/` 各子目录 CLAUDE.md                                  | `ArtSource/**/CLAUDE.md`                      | 单个模型或模块的现状、生成脚本和注意事项；Claude Code 读到该目录时自动加载，其他工具进入目录时先读它                                                                        |
| `TerrainReferences/` 各子目录 README                             | `TerrainReferences/**/README.md`              | 实测地形导入和材质同步记录                                                                                                                                                  |

### 游戏文本来源

游戏里的日记、独白、任务提示和过场文字都由 `Docs/` 下 `json-` 开头的文档生成 JSON。每份成对：左边给人改文字，里面只有内容；右边给 AI 看，写怎么同步成 JSON。

| 内容     | 给人改的文字                                                 | 给 AI 的同步规则                                                                           | 生成的 JSON                                   |
| -------- | ------------------------------------------------------------ | ------------------------------------------------------------------------------------------ | --------------------------------------------- |
| 外公日记 | [Docs/json-grandpa-diary.md](Docs/json-grandpa-diary.md)     | [Docs/ai-rules/json-grandpa-diary-rules.md](Docs/ai-rules/json-grandpa-diary-rules.md)     | `Assets/Narrative/grandpa-diary-content.json` |
| 内心独白 | [Docs/json-inner-monologue.md](Docs/json-inner-monologue.md) | [Docs/ai-rules/json-inner-monologue-rules.md](Docs/ai-rules/json-inner-monologue-rules.md) | `Assets/Narrative/inner-monologue.json`       |
| 任务提示 | [Docs/json-hints.md](Docs/json-hints.md)                     | [Docs/ai-rules/json-hints-rules.md](Docs/ai-rules/json-hints-rules.md)                     | `Assets/Narrative/levelN-hints.json`          |
| 过场文字 | [Docs/json-cutscene-text.md](Docs/json-cutscene-text.md)     | [Docs/ai-rules/json-cutscene-text-rules.md](Docs/ai-rules/json-cutscene-text-rules.md)     | `Assets/Narrative/cutscene-text.json`         |

## 工作方式

- 用中文回答用户。
- 个人作品集项目，代码以清晰可读为先，不过度工程化。
- 场景、机制、叙事耦合紧，改动时注意影响范围。
- 动手前想清楚，有歧义就问；只写需要的代码；只改必须改的地方，保持现有风格。
- AI 负责代码和文档；需要改场景、Inspector 或资源引用时可以通过 Unity MCP 直接改，改完说明改了什么。
- 不用 Windows GUI 自动化操作 Unity，除非用户在当前对话里明确允许。
- Play Mode 测试和最终效果由用户确认，不为常规验证跑命令行编译。
- 改 git 前先问用户；提交说明用英文，不加任何 AI 署名（如 Co-Authored-By），推送前给用户过目。
- 进度只记在仓库文档里，不依赖某个 AI 工具自己的记忆，换用 Claude 或 GPT 都能接着做。
- 完成一项工作后在[开发日志](Docs/development-log.md)里记一两句：做了什么、为什么、还差什么。

## 文档分工

- 模块说明统一写在该目录的 CLAUDE.md 里，只写现状，标题写目录路径，固定四段：用途（一两句话）、内容（目录里每个或每组文件一行，说明是做什么的）、与其他部分的关系（对应的场景对象、Assets 资源和代码，以及上下游）、注意（不能想当然的约束、改动后必须做的事、未完成的部分）。注意只写结论，不写排查过程。样例见 `ArtSource/NewspaperMenu/Spread/Runtime/CLAUDE.md`。
- 写上层目录的 CLAUDE.md 时，读下层 CLAUDE.md 来概括，不重新读一遍源文件。
- CLAUDE.md 里不写日期和修改经过，不写“某日起”“原值为”，停用的功能删掉或只留一句；参数值以材质、Inspector 和代码为准，不抄进文档。
- 改了某个目录里的文件或它对应的资源后，主动检查该目录的 CLAUDE.md 是否还准确，过时就只改过时的部分，不在末尾追加段落；改了什么、为什么、试过哪些值只记在开发日志里。
- 新模块直接写 CLAUDE.md，不再新建模块 README.md。`TerrainReferences/` 下的 README 是地形操作存档，保持原样。
- `Docs/design.md` 只给人看设计，不写实现和交互细节，具体规矩见 [设计文档规则](Docs/ai-rules/design-rules.md)。
- `Docs/script.md` 按场次写玩家从打开游戏到结尾经历的事情，只写剧情，具体文字全文放[游戏文本](Docs/game-text.md)，关卡规则放设计文档。已定的场次照设计写，没定的标 **待定**，早期构想标 **构想**；文档里不写给 AI 的说明。
- Spec 分两类放：按关卡写的放 `Docs/Specs/Levels/`，跨关卡的系统放 `Docs/Specs/Systems/`。新建 Spec 后在上面的文档入口加一行。

## 注意事项

### 资源与引用

- 脚本文件名必须和类名一致。移动或改名资源时 .meta 要一起移动，否则场景引用会丢。
- UI 管理脚本挂在它控制的 UI 根对象上（依赖面板的开关时机），没有画面的系统放在 `Gameplay Root/Managers` 下。
- 输入改 `PlayerControls.inputactions`，`PlayerControls.cs` 是自动生成的，不要手改。
- 存档都在 `Application.persistentDataPath` 下的 `CapturedPhotos/`、`ScrapBook/`、`Echo/`、`GrandpaDiary/`、`Backpack/`、`Progress/`；新增存档目录要加进 `GameSaveManager` 的清档列表；把存档读进内存的组件要订阅 `GameSaveManager.SavesCleared` 回到新游戏状态，新游戏不重载场景。结尾后回主菜单是重载整个场景，订阅静态事件的组件要在 OnDisable 或 OnDestroy 里退订。
- 构建场景列表只有 `Main Game Scene`。

### 文本配置

- `json-` 文档是对应 JSON 的唯一来源，对照表见上面的“游戏文本来源”。用户说“同步日记”“同步独白”“同步提示”“同步过场”时，先读对应的规则文档再重新生成 JSON，不直接手改 JSON。
- `json-` 文档只放给人改的内容，不写给 AI 的说明；规则改动写进 `Docs/ai-rules/` 里对应的那份。

### 相机与渲染

- 相机后期分三份 Volume：取景器第 10 层，全屏拍照第 11 层，手持对焦第 12 层；Main Camera 的 Volume Mask 为 6145，CameraPreview 为 1025。HDRP 的光圈属于相机，不能按 Volume 混合。
- 相机调节 UI 只写取景器和全屏两份 Profile，不要改全局 Volume。
- 沙尘参数在材质上调，不在雾盒组件上调；Shader Graph 的 Time 节点在雾体积里无效，位移由 `SandstormTimeDriver` 累加。
- HDRP 资源的颜色缓冲和后期缓冲都是 R16G16B16A16，背包查看相机 `BackpackViewCamera` 靠它输出透明背景，改回 R11G11B10 模型会带一块不透明的底。

### 地形与场景

- 改地形只改局部，不要整体重新生成 MapMagic；改完同步 Main 与 Draft 的材质及边缘缓存，刷新热浪并保存场景。
- 改了地形或沙尘范围后，运行 `Tools > Coal Echoes > Refresh Factory Heat Haze` 并保存场景。
- 索道路线点存在场景里，改了绳索形状要重新生成。
- 尺度：DEM 1:1，玩家胶囊 1.8 米，眼高约 1.68 米，Main Camera 竖直 FOV 80，近裁剪面 0.05 米。相机在胶囊中心前 0.25 米，贴墙时离墙面约 0.13 米，近裁剪面调回 0.3 米会穿墙看到屋里。

### Git 与素材

- `Assets/TerrainData/SheepRangeGenerated` 的地形走 Git LFS；其他二进制 .asset 会被 `*.asset text` 规则改坏，新增时要先加 LFS 规则。
- RocksGobi、Lands、RockyDesert 三个素材包（约 10GB）不进 git，只在本机，新克隆的仓库里会显示丢失。
- Industrial Harbour 素材重新导入时 58 个碰撞网格会提示缺法线，可以忽略。
- Blender 后台脚本可能在项目根目录留下乱码名的空文件夹，可以直接删。
