# ArtSource/FactoryRoads/

## 用途

厂内土路、厂门连接路和厂外公路的素材来源、导入记录和一次性编辑脚本。厂内道路是沿地形生成网格贴花的样条路，厂外公路是按纵断面生成的柏油路面。

## 内容

- `SOURCES.txt`、`import-manifest.json`、`validation.json`：道路素材的来源、许可和导入结果。Poly Haven Asphalt 02 为 CC0；G87 Road Repair 和 Sully Cracks and Damage 两个 mod 包没有许可文件，跨游戏使用权限未确认，只做本地评估。
- `PolyHavenAsphalt02/`：Asphalt 02 的 4K 原图。`G87RoadRepair/`、`SullyCracksDamage/`：两个 mod 包的原始贴图。`splines-package-search.json`、`splines-package-install.json`：Unity Splines 包的安装记录。
- `interior-spline-notes.md`、`interior-spline-*.png`、`interior-spline-route.json`、`interior-spline-validation.json`：第一条厂内样条土路的说明、渲染和检查。
- `build-road-sample.cs`、`refine-road-sample.cs`、`render-road-sample.cs`、`road48m-*`：48 米柏油路样段的制作和渲染；`build-dirt-sample.cs`、`render-dirt-sample.cs`、`dirt48m-*`：48 米土路样段。
- `place-gate-road.cs`、`gate-road-*`：厂门连接路的摆放和检查。`widen-gate.cs`、`gate-wide-*`、`apply-entrance-wall.cs`、`crop-entrance-wall.py`：拓宽门洞并裁短另一侧接墙。`align-gate-approach.cs`、`check-gate-alignment.cs`、`clear-approach-shoulders.cs`、`gate-aligned-*`：连接路线形和门洞对齐。`grade-widened-approach.cs`、`smooth-gate-road-bump.cs`、`smooth-gate-road-crest.cs`：连接路的局部路基整平。
- `spline-survey.png`、`unity-preview.png`：早期样条和预览截图。
- `Highway/`：厂外公路和连接路的说明。`MainRoadReroute/`：厂区西端主路改线和五条支路。各见其 CLAUDE.md。

## 与其他部分的关系

- 运行代码是 `Assets/Scripts/Environment/FactoryDirtRoadSpline.cs`（样条土路，Width、Texture Length、`endFade`，地形改动后用组件菜单 Rebuild Road 重新贴地）和 `FactoryHighway.cs`（公路路面）。网格是临时生成数据，不生成 Mesh 资源。
- 材质和贴图在 `Assets/Art Resources/Environment/FactoryRoads/`：`Asphalt02/`、`Highway/`、`DirtPath05/`、`G87RoadRepair/`、`SullyCracksDamage/` 和两个 48 米样段目录。
- 场景对象：厂内主路 `In-Scene Objects/工厂道路向内`、`工厂道路向外`，支路 `工厂支路`，厂外 `厂外公路与连接路`。
- 公路的地形准备在 `TerrainReferences/FactoryHighway/`；路边煤灰由 `Tools > Coal Echoes > Paint Factory Ground` 按场景里所有样条路刷。

## 注意

- 这里的 C# 都是对当时场景的一次性编辑，通过 Unity MCP 或 CLI 运行，不要重跑旧的摆放脚本覆盖现有布局；带 `apply` 开关的脚本先用 false 干跑。
- 样条路材质用 HDRP 网格贴花路径并读顶点透明度做渐隐，升级 HDRP 时要复查自定义 DirtRoadSpline shader。
- G87 贴图授权未确认，发布前要解决。
