# ArtSource/SandDune/

## 用途

开场时间流逝后厂内积起的三道沙丘，从左侧围墙到右侧围墙连成一线，挡住主路和两侧缺口，玩家进厂后只能穿过宿舍楼绕到工厂后方。宿舍楼前院另有一组低沙堆，只表现积沙，不挡路。沙丘和沙堆都是盖在地形上的网格，地形和道路贴花没有改。

## 内容

- `Blender/SandDune_v01.blend`：源文件。`Exports/`：三堆沙丘和前院沙堆的 FBX，各带 `_LOD0` 到 `_LOD2`。
- `Ground/`：两侧沙丘范围内按 0.5 米网格采样的地形高度，`build_dunes.py` 据此贴地生成。
- `Scripts/`：按顺序用。`export_ground.cs` 通过 Unity MCP 的 execute_code 采样地形高度写入 `Ground/`；`build_dunes.py` 在 Blender 里按 `DUNES` 参数用 0.2 米网格建高度场，沿建筑墙面切开、删掉建筑内部、叠到地面高度上并补裙边，峰高可用 `arch` 做成两侧不对称的圆拱，每栋建筑可单独设贴墙积沙的高度；`build_yard_piles.py` 用同样的网格和细节噪声建前院沙堆 `SandDune_DormYard`，参数在 `DRIFTS` 和 `MOUNDS`；`export_dunes.py` 导出三级 LOD 的 FBX 并复制到 Unity，后两级用 Decimate 30% 和 8%，命令行 `--` 后写名字时只导出这几个；`make_albedo.py` 用普通 Python（Pillow、numpy）从地形的 gravelly_sand 贴图生成底色。
- `Previews/`：Unity 游戏相机截图，含俯视、各缺口的平视和墙外视角，以及开场升起进度 0、0.5、1 的对照；`dorm-yard-` 开头的是前院沙堆的斜俯视、墙角平视和进门平视。主路沙丘只有 `main-road-gate-eye` 和 `main-road-wall-eye` 是现在的圆拱，其余主路和开场升起的图还是旧形状。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/SandDune/`：FBX、`Materials/SandDune.mat`（HDRP/Lit，四个共用）、`Textures/SandDune_BaseColor.png` 和四个预制体。预制体是 FBX 的变体，LOD 阈值和煤堆一样，根节点用 LOD2 网格做 MeshCollider；三堆沙丘另有子对象 `Blocker`，是隐形 BoxCollider，前院沙堆没有。
- 场景实例在 `In-Scene Objects/沙丘` 下：SandDune_MainRoad 在宿舍楼靠主路的山墙和两栋混凝土厂房之间，连带两栋厂房之间的夹道；SandDune_Left 在左侧厂房与围墙之间；SandDune_Right 在宿舍楼右端与围墙之间。三堆峰线对齐，两侧的沙漫过围墙顺墙外地形收尽。SandDune_DormYard 铺在前门围墙、右侧围墙和宿舍楼正面围成的前院里，在墙角接上 SandDune_Right 的坡脚。
- 开场时 `FactorySandAccumulation` 把四个的 LOD 网格从地下升回原位，碰撞不动，见 `../NewspaperMenu/Spread/SandAccumulation/`。
- 底色单独做而不用地形沙层的染色，亮度按截图调到各机位与厂区地面持平。

## 注意

- 文档和脚本里的厂区坐标以 `FactoryGate` 轴心为原点，u 沿主路指向厂内，v 指向主路右侧，单位米。三堆峰线都在 u 约 30，预制体朝向 46.97 度；主路沙丘约 25.8 × 25.2 × 5.1 米，左侧约 18.6 × 24.8 × 5.9 米，右侧约 19.2 × 22.8 × 7.0 米（沿 v × 沿 u × 高）。
- Blender 里 X 是 v、Y 是 u。FBX 导入 Unity 后转了 180 度，场景里的朝向是大门朝向减 180 度。
- 主路沙丘的峰线按用户画的剪影做成圆拱：主路正中最高约 5.1 米，往左降到贴混凝土厂房处约 3 米，往右缓降到宿舍楼山墙约 4.4 米。左侧厂房 C_2 不加贴墙积沙，免得左端鼓出肩膀；C_4 的积沙加大，让两栋厂房之间的夹道口仍有 1 到 2 米深的沙。
- 主路沙丘朝大门一面 42 度、峰线收尖，让玩家一眼觉得爬不上去；两侧沙丘朝大门 32 度、背面 25 度，让背面坡脚不埋左侧厂房后面的梯子和升降台。42 度仍低于角色 45 度的爬坡上限，所以 `Blocker` 盒子不能省，正面要落在沙深约 0.5 米处，主路这堆按这条线排成八级台阶，夹道里另有一个，两侧沙丘在墙外另按同样沙深加阶梯状的盒子；改了沙丘形状后要重新核对。
- `Blocker` 在 Ignore Raycast 层，对焦和交互射线穿过，玩家被挡。墙外离墙 7 米以外沙深不足 0.5 米，可以绕过沙坡尖。
- 网格在建筑墙面处切开并补竖直裙边藏进墙里，不伸进宿舍室内；围墙不切，沙子直接穿过墙体。
- `build_dunes.py` 的范围要留够，沙子碰到范围边缘会留下切口，脚本跑完会打印每堆的实际范围。地形改动后要重新导出 `Ground/`。
- 新增沙丘要加进 `FactorySandAccumulation` 的 Dunes，比它的 Depth 还高的沙丘要加大 Depth。
- 前院沙堆是三道贴墙积沙（前门围墙、右侧围墙、宿舍楼入口右侧的地基）加四个散落的小沙包，重叠处高度相加，右前墙角最高约 2 米。坡度不超过 33 度，玩家能走上去，所以不加 `Blocker`。宿舍楼入口前（v 19 到 22.5）、门卫室和大门门扇扫过的地方不放沙。
- 前院沙堆原点在 u 9.6、v 18.3，和右侧沙丘差整 20 米，底色（20 米一铺）和法线（1 米一铺）在两者相接处对得上；改原点要保持这个差。网格边缘都收在围墙和宿舍楼地基里，不补裙边。
- 前院的垃圾箱、木箱和纸箱有几个底边埋进沙里；挪动这些杂物或改了沙堆后要重新核对。沙深超过 0.15 米处的枯草已删，规则同 `../FactoryGateGrass/add-yard-grass.cs`。
- Play Mode 里的走动手感（含前院沙堆）和坡脚与路面交界的观感待确认。
