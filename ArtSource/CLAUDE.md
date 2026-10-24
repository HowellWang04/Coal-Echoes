# ArtSource/

## 用途

游戏美术资源的制作源文件、生成脚本、检查记录和场景装配记录，放在 Assets 外，不被 Unity 导入。Unity 用的导出结果在 `Assets/Art Resources/`。

## 内容

- `Cableway/`：运煤索道全套，塔架、站房、缆线、吊斗运动。
- `FactoryGate/`：洗煤厂大门。`FactoryBoundary/`：围墙模块。`Guardhouse/`：门卫室模型和摆放脚本，没有说明；其中 `PolyHaven/` 是墙面和水泥件的扫描贴图，有说明。`FactoryGateGrass/`：厂门内外的枯草摆放。
- `FactoryRoads/`：厂内土路、连接路和厂外公路。`FactoryRoadTunnel/`：厂外公路隧道的建模和地形脚本，没有说明。`G87DirtyRoads/`：G87 土路贴图包的原始文件和评估，没有说明。
- `FactoryHeightReview/`：厂区对象按地形落地。`FactoryGrounding/`：另一轮厂区对象贴地脚本和检查，没有说明。`FactoryYardLayout/`：杂物成组摆放。`CoalPile/`：煤堆。`SandDune/`：封路的三堆沙丘。`WorkersDormitory/`：宿舍楼干净材质和开场老化。`Signs/`：限高牌贴图生成记录（两份 txt）。
- `NewspaperMenu/`：双页报纸主菜单。`MainMenu/`：品牌 Logo、开场演出和初版菜单。`InnerMonologue/`：内心独白字幕效果图。`PickupNotice/`：右下角拾取提示的手 icon 和效果图。
- `Props/`：撬锁工具、断线钳、旧相框、门卫室桌椅、台灯和室内陈设，以及 Group Photo 合照的制作。`DustyMotorcycle/`：开场的红色摩托车。`ScrapbookDeco/`：剪贴簿贴纸绘图脚本。`EchoPhotoMark/`：回响照片的印章、便签纸和胶带。
- `DesktopAssetImport/`、`NatureAssetSetup/`、`Lands/`、`RocksGobi/`、`RockyDesert/`：第三方素材包的导入、材质转换、LOD 生成和配色记录，没有说明。`Cleanup/`：旧索道和铁轨移除的记录，没有说明。
- 有 CLAUDE.md 的目录以它为准。

## 与其他部分的关系

- 主场景是 `Assets/Scenes/Main Game Scene.unity`，对象名和层级以场景为准；地形操作记录在 `TerrainReferences/`。
- 项目规范和文档入口见仓库根目录 `AGENTS.md`，历史记录在 `Docs/development-log.md`。
- 改写前的旧 README 原文备份在仓库根目录 `ReadmeBackup/`。

## 注意

- 这里的 `.cs` 文件都是 Unity CLI `eval_file` 或 MCP `execute_code` 的一次性脚本，不参与游戏编译；大多针对当时的场景，重跑前先核对对象名和路径。
- 各目录的 `Before/` 和 `Previous/` 是操作前备份和弃用版本，不是现状。
- RocksGobi、Lands、RockyDesert 三个素材包不进 git，相关记录目录在新克隆的仓库里会缺文件。
