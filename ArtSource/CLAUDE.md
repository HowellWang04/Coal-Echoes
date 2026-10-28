# ArtSource/

## 用途

游戏美术资源的制作源文件、生成脚本、检查记录和场景装配记录，放在 Assets 外，不被 Unity 导入。Unity 用的导出结果在 `Assets/Art Resources/`。

## 内容

- `Cableway/`：运煤索道全套，塔架、站房、缆线、吊斗运动和山脚站的驱动电机。`StationFence/`：山脚始发站西侧的铁丝网。
- `FactoryGate/`：洗煤厂大门。`FactoryBoundary/`：围墙模块。`Guardhouse/`：门卫室模型和摆放脚本，没有说明；其中 `PolyHaven/` 是墙面和水泥件的扫描贴图，`WindowGlass/` 是窗玻璃的贴图、材质、窗后遮挡面片和反射探针，都有说明。`FactoryGateGrass/`：厂门内外的枯草摆放。
- `FactoryRoads/`：厂内土路、连接路和厂外公路。`FactoryRoadTunnel/`：厂外公路隧道的建模和地形脚本，没有说明。`G87DirtyRoads/`：G87 土路贴图包的原始文件和评估，没有说明。
- `FactoryHeightReview/`：厂区对象按地形落地。`FactoryGrounding/`：另一轮厂区对象贴地脚本和检查，没有说明。`FactoryYardLayout/`：杂物成组摆放。`CoalPile/`：煤堆。`SandDune/`：封路的三堆沙丘和宿舍楼前院的沙堆。`WorkersDormitory/`：宿舍楼干净材质和开场老化。`Flagpole/`：宿舍楼正门前不挂旗的老旗杆。`Signs/`：限高牌贴图生成记录（两份 txt）。
- `NewspaperMenu/`：双页报纸主菜单。`MainMenu/`：品牌 Logo、开场演出和初版菜单。`InnerMonologue/`：内心独白字幕效果图。`PickupNotice/`：右下角拾取提示的手 icon 和效果图。`HintPopup/`：右上角任务提示勾选框的铅笔涂抹。
- `Room207/`：第二关 207 室的搭建脚本、窗外背板的拍摄和窗下的宿舍两屉桌。
- `SummitBoardwalk/`：第四关上站背后的木栈道和山头观景平台，以及站房基础顶面的一圈栏杆。`MinePortal/`：上站东北角下面封死的矿洞口、轨道、矿车和洞口前切出来的平台。
- `Props/`：撬锁工具、断线钳、旧相框、门卫室桌椅、台灯和室内陈设，Group Photo 合照的制作，第二关山路图的贴图，以及第三关的机油壶。`DustyMotorcycle/`：开场的红色摩托车。`ScrapbookDeco/`：剪贴簿贴纸和印章绘图脚本。`EchoPhotoMark/`：回响照片的印章、便签纸和胶带。`NightWind/`：月夜风声的合成脚本。`Footsteps/`：按地面分的玩家脚步声，实录素材和切分脚本，其中 Sonniss 的原始录音不进 git。
- `DesktopAssetImport/`、`NatureAssetSetup/`、`Lands/`、`RocksGobi/`、`RockyDesert/`：第三方素材包的导入、材质转换、LOD 生成和配色记录，没有说明。`Cleanup/`：旧索道和铁轨移除的记录，没有说明。
- `Portfolio/`：作品集排版用的图，`LevelDesign/` 是按 207 室重画的 Level design 展示页，`EchoNotes/` 是用游戏贴图拼的回响照片和手账便签展示图，`NightRide/` 是从吊斗里拍的月夜乘索道图。
- 有 CLAUDE.md 的目录以它为准。

## 与其他部分的关系

- 主场景是 `Assets/Scenes/Main Game Scene.unity`，对象名和层级以场景为准；地形操作记录在 `TerrainReferences/`。
- 项目规范和文档入口见仓库根目录 `AGENTS.md`，历史记录在 `Docs/development-log.md`。
- 改写前的旧 README 原文备份在仓库根目录 `ReadmeBackup/`。

## 注意

- 这里的 `.cs` 文件都是 Unity CLI `eval_file` 或 MCP `execute_code` 的一次性脚本，不参与游戏编译；大多针对当时的场景，重跑前先核对对象名和路径。
- 各目录的 `Before/` 和 `Previous/` 是操作前备份和弃用版本，不是现状。
- RocksGobi、Lands、RockyDesert 三个素材包不进 git，相关记录目录在新克隆的仓库里会缺文件。
