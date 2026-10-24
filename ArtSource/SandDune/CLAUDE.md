# ArtSource/SandDune/

## 用途

开场时间流逝后厂内积起的三道沙丘，从左侧围墙到右侧围墙连成一线，挡住主路和两侧缺口，玩家进厂后只能穿过宿舍楼绕到工厂后方。沙丘是盖在地形上的网格，地形和道路贴花没有改。

## 内容

- `Blender/SandDune_v01.blend`：源文件。`Exports/`：三堆沙丘的 FBX，各带 `_LOD0` 到 `_LOD2`。
- `Ground/`：两侧沙丘范围内按 0.5 米网格采样的地形高度，`build_dunes.py` 据此贴地生成。
- `Scripts/`：按顺序用。`export_ground.cs` 通过 Unity MCP 的 execute_code 采样地形高度写入 `Ground/`；`build_dunes.py` 在 Blender 里按 `DUNES` 参数用 0.2 米网格建高度场，沿建筑墙面切开、删掉建筑内部、叠到地面高度上并补裙边；`export_dunes.py` 导出三级 LOD 的 FBX 并复制到 Unity，后两级用 Decimate 30% 和 8%；`make_albedo.py` 用普通 Python（Pillow、numpy）从地形的 gravelly_sand 贴图生成底色。
- `Previews/`：Unity 游戏相机截图，含俯视、各缺口的平视和墙外视角，以及开场升起进度 0、0.5、1 的对照。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/SandDune/`：FBX、`Materials/SandDune.mat`（HDRP/Lit，三堆共用）、`Textures/SandDune_BaseColor.png` 和三个预制体。预制体是 FBX 的变体，LOD 阈值和煤堆一样，根节点用 LOD2 网格做 MeshCollider，子对象 `Blocker` 是隐形 BoxCollider。
- 场景实例在 `In-Scene Objects/沙丘` 下：SandDune_MainRoad 在宿舍楼靠主路的山墙和两栋混凝土厂房之间，连带两栋厂房之间的夹道；SandDune_Left 在左侧厂房与围墙之间；SandDune_Right 在宿舍楼右端与围墙之间。三堆峰线对齐，两侧的沙漫过围墙顺墙外地形收尽。
- 开场时 `FactorySandAccumulation` 把三堆的 LOD 网格从地下升回原位，碰撞不动，见 `../NewspaperMenu/Spread/SandAccumulation/`。
- 底色单独做而不用地形沙层的染色，亮度按截图调到各机位与厂区地面持平。

## 注意

- 文档和脚本里的厂区坐标以 `FactoryGate` 轴心为原点，u 沿主路指向厂内，v 指向主路右侧，单位米。三堆峰线都在 u 约 30，预制体朝向 46.97 度；主路沙丘约 28.8 × 25.6 × 5.5 米，左侧约 18.6 × 24.8 × 5.9 米，右侧约 19.2 × 22.8 × 7.0 米（沿 v × 沿 u × 高）。
- Blender 里 X 是 v、Y 是 u。FBX 导入 Unity 后转了 180 度，场景里的朝向是大门朝向减 180 度。
- 主路沙丘朝大门一面 42 度、峰线收尖，让玩家一眼觉得爬不上去；两侧沙丘朝大门 32 度、背面 25 度，让背面坡脚不埋左侧厂房后面的梯子和升降台。42 度仍低于角色 45 度的爬坡上限，所以 `Blocker` 盒子不能省，正面要落在沙深约 0.5 米处，两侧沙丘在墙外另按同样沙深加阶梯状的盒子；改了沙丘形状后要重新核对。
- `Blocker` 在 Ignore Raycast 层，对焦和交互射线穿过，玩家被挡。墙外离墙 7 米以外沙深不足 0.5 米，可以绕过沙坡尖。
- 网格在建筑墙面处切开并补竖直裙边藏进墙里，不伸进宿舍室内；围墙不切，沙子直接穿过墙体。
- `build_dunes.py` 的范围要留够，沙子碰到范围边缘会留下切口，脚本跑完会打印每堆的实际范围。地形改动后要重新导出 `Ground/`。
- 新增沙丘要加进 `FactorySandAccumulation` 的 Dunes，比它的 Depth 还高的沙丘要加大 Depth。
- Play Mode 里的走动手感和坡脚与路面交界的观感待确认。
