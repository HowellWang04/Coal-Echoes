# ArtSource/MinePortal/

## 用途

第四关山顶站东北角下面封死的矿洞口和洞口前的平台，交代运煤索道的煤从山里的矿上来。水泥端墙带拱门，两侧翼墙，拱门钉着旧木板、中间一道锈铁栅门，600 毫米轨距的轨道从门下伸出来，穿过平台到矸石堆边的木车挡，轨道上停着一辆空矿车。洞口朝站房前方（山头方向），从栈道和观景平台往下看得见；平台只看不走。

## 内容

- `layout.json`：洞口和平台的布局，地形脚本和 Blender 脚本都读它。站房坐标 u 朝站房右、v 朝站房前，高度相对基础顶面；洞口坐标 X = u − 原点 u，Z = v − 原点 v，洞口朝 +Z。里面有原点、地面高度、端墙宽深高和东半边的斜顶、拱门、两道翼墙的角度长度高度、墙厚、翼墙伸进端墙的长度、平台多边形、挖填坡度和轨道终点。
- `Scripts/survey.cs`：Unity MCP execute_code 的一次性脚本，导出站房朝向、基础顶面和四角，以及站房东侧 0.5 米间距的地形高度到 `Survey/terrain_heights.json`，选址时用，之后的脚本不读它。
- `Scripts/sculpt_terrain.cs`：按 layout 改地形。平台内压平（向外 1% 排水），平台外按 1.6 的坡向上切、按 1.0 的坡向外填，墙后只填不削；端墙块里和翼墙前 1.2 米压到地面或墙顶以下，起坡藏在墙体里。同时改 Main、Draft 两份地形、MapMagic 边缘缓存和固定高度源的两份矩阵，再对齐地形材质。每次先从 `Before/` 还原原始高度再算，可以反复跑；不保存场景。
- `Before/`：第一次改地形前的原始高度，`Tile_0_-1_Main.json`、`Tile_0_-1_Draft.json` 是地形窗口，`*.source.json` 是高度源矩阵窗口。
- `Scripts/build_portal.py`：后台 Blender 建模、导出 FBX、写 `verification.json`、存 `Blender/MinePortal.blend`，几秒钟。木板和枕木按栈道的旧木板扫描逐块贴 UV，水泥按 3 米、锈铁按 0.5 米一个 UV 单位做平面投影，和复用的材质对上。
- `Exports/MinePortal.fbx`：两个网格。`MinePortal` 是墙、封板、栅门、轨道和车挡，四个材质槽依次是水泥、木板、锈铁、黑；`MineCart` 是矿车，只有锈铁。
- `UnityIntegration/PlacePortal.cs`：设 FBX 导入（不导材质和动画），重建场景里的 `MinePortal`，按槽位换材质，设静态标记；不保存场景。
- `Previews/`：放进场景后的截图，`unity-day-*` 是白天，`unity-night-*` 是第四关的月夜，机位有观景平台往回看、平桥上往下看、平台上对着洞口和俯瞰。

## 与其他部分的关系

- Assets 只有 `Assets/Art Resources/Environment/MinePortal/Models/MinePortal.fbx`。材质全用现成的：水泥是站房基础的 `SummitFoundation_Concrete`，木板是栈道的 `SummitBoardwalk_WeatheredWood`，锈铁是驱动电机的 `Motor_RustySteel`，封板后面的黑是厂外隧道的 `TunnelBlack`。
- 场景对象 `In-Scene Objects/山顶矿洞/MinePortal`，是 FBX 实例，位置是 layout 原点，Y 旋转和 `CablewaySummitStation` 相同；没有碰撞体。
- 端墙背面贴着站房基础后沿（`../Cableway/SummitStation/`），东半边在基础东北角正下方；栈道平桥（`../SummitBoardwalk/`）在西边约 10 米、高出平台约 11.5 米。
- 改的地形是 `Tile_0_-1` 的 Main 和 Draft，高度源是 `SheepRange_Native_Main`、`_Draft`。

## 注意

- 地形格子约 1 米，所以端墙块做成近 3 米深、翼墙 2 米厚、翼墙伸进端墙 1.5 米，地形的起坡都落在墙体里；墙改薄或去掉伸进去的一段，墙面前会戳出地形尖角，墙角会露缝。
- FBX 的子网格按材质第一次出现的顺序排，`build_portal.py` 建网格前按槽位排序面，Unity 里子网格顺序才和槽位一致，`PlacePortal.cs` 按这个顺序给材质。
- 改 layout 后依次跑 `build_portal.py`、把 FBX 拷到 Assets 下覆盖、跑 `PlacePortal.cs`、跑 `sculpt_terrain.cs`，再运行 `Tools > Coal Echoes > Refresh Factory Heat Haze` 并保存场景。
- 地形脚本不动 u < −3.5（栈道柱脚）和基础四周（v ≤ 12.6 只抬不降，基础侧壁停在原地面），所以栈道不用重跑 survey。
- 西侧切坡和端墙西边的山体很陡，白天近看岩石贴图被拉长；第四关只有月夜，玩家只能从栈道和山头往下看，没处理。
- 第四关宿舍楼回响的视线从平台上空约 8 米经过，没有被挡。
- Draft 地形格子粗，窗口里有一个样本变了约 1.8 米，只影响远处的低精度地形。
- 没有挂回响，也没有游戏文字。Play Mode 里还没看过月夜下从栈道和山头看不看得出洞口。
