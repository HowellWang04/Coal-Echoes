# ArtSource/Room207/

## 用途

第二关 207 室的搭建脚本、窗外背板的拍摄和预览截图。207 是在宿舍楼正下方地下单独搭的一间四人间，玩家靠过场文字加传送进出。

## 内容

- `Scripts/place-furniture.cs`：建 `室内空间/207` 根节点，从 `卧室/room` 复制两张上下铺和两组铁皮柜并缩回原大，只跑一次。
- `Scripts/build-shell.cs`：ProBuilder 围地面、顶、四面墙（窗洞和门洞）、绿墙裙、窗台、窗框和玻璃、门套、门扇和拉手，并生成白灰墙、墙裙、玻璃三个材质；重跑会重建这些对象。
- `Scripts/build-props.cs`：炉子、烟筒、床和柜子的碰撞体、`207 Spawn`；重跑会重建。
- `Scripts/capture-window-view.cs`：在宿舍楼背面二楼一扇窗外拍窗外背板的照片 `Room207_WindowView.png`。
- `Scripts/build-lighting.cs`：窗外背板平面、屋内局部 Volume、窗口阳光、窗口天光、补光和反射探针；重跑会重建 `Lighting` 和背板。
- `Scripts/render-previews.cs`：临时相机从眼高拍 `Previews/` 里的截图，不弄脏场景。
- `Previews/`：门口（`207 Spawn` 的视角）、近窗、抽屉、回看门口、炉子五张眼高截图和一张俯视图。
- `DormDesk/`：窗下的宿舍两屉桌和半开的抽屉，Blender 建模，见该目录 CLAUDE.md。

## 与其他部分的关系

- 场景对象 `In-Scene Objects/室内空间/207`，在宿舍楼 `WorkersDormitory` 正下方的地下，朝向是宿舍楼转 180 度，屋里 +z 朝窗，也就是宿舍楼背面。和 `卧室`、`大厅`、`白膜场景搭建` 不重叠，三块原件没动。
- 子对象：`Furniture`（`Bunk Bed Left`、`Bunk Bed Right`、`Locker Left`、`Locker Right`、`Table` 和它下面的 `Table Drawer`、`Map Spot`，`Stove`、`Stove Pipe`），`Room Shell`（`Floor`、`Ceiling`、四面墙、`Dado`），`Window`（`Window Sill`、`Window Frame`、`Window Glass`、`Window View Backdrop`），`Door`（`Door Casing`、`Door Leaf`、`Door Handle`），`Lighting`（`207 Interior Volume`、`Window Sunlight`、`Window Skylight`、`Room Fill Light`、`Room Reflection Probe`），`207 Spawn`，`Mountain Road Map`（抽屉里的山路图纸片，按 `Map Spot` 摆，见 `../Props/MountainRoadMap/`），`Desk Spot`（桌面上的空物体，山路图第一次看完后放到这里）。每样都是单独的 GameObject，作品集 Level design 图从这里导出俯视坐标，见 `../Portfolio/LevelDesign/`。
- Assets 在 `Assets/Art Resources/Environment/Room207/`：白灰墙、墙裙、玻璃材质，窗外背板的照片和材质，屋内 Volume Profile，`DormDesk/`。墙面材质复制自门卫室 `GH_Wall_ConcreteWall007`（扫描贴图见 `../Guardhouse/PolyHaven/`），地面和窗台直接用 `GH_Concrete_FloorWorn001`，窗框借宿舍楼的 `Dorm_FrameGreen`，门借门卫室的 `GH_BlueGrey_PaintedWood`，炉子和烟筒用门卫室的 `GH_OxidizedIron`。
- 流程、传送和山路图交互见 `Docs/Specs/Levels/level-two-spec.md`：`Door Leaf` 挂着按 F 离开的使用点（saveId `room207_door`），F 提示出在 `Door Handle` 处；`207 Spawn` 是进宿舍过场的传送落点，也是第二关读档放回 207 的位置；`Mountain Road Map` 挂着按 E 查看、按 T 悬停翻译和拍照回响，看完一次后留在 `Desk Spot`。

## 注意

- ProBuilder 件的物体原点都在 207 原点、缩放为 1。墙面材质是物体空间三向投影，门卫室贴图里的墙根返潮和檐下积灰按物体高度画，原点挪了纹理会断开、做旧会错位。
- 屋里的光靠 `207 Interior Volume`，优先级高于全局和月夜 Volume。全局是沙漠日光的固定曝光和满天光，不加它整间屋被天光照平、发灰；它压低天空环境光、提高曝光、关雾、开 SSAO。全局雾本来会透进屋里，现在被它关掉；工厂沙尘的局部雾体积在地面上方，碰不到 207。
- 反射探针是实时的，开启时渲染一次；盒子要包住墙面和窗玻璃、过渡距离要小。探针淡出的地方 HDRP 退回天空反射，屋里所有表面会发白发蓝、窗外蒙白。改了家具或墙面要关开一次探针刷新。
- 窗外背板是贴在窗外远处平面上的照片，自发光、不受曝光影响，只在窗口那么大的视角里透视对得上。照片中心往索道方向转过、比真实方向压低了一些，好让山脊上的 3 号塔架从屋中间看得见。宿舍楼背后的摆件、索道或天气改了要重跑 `capture-window-view.cs`，背板会自动换图。
- 窗口阳光是窗外的一盏聚光灯，角度按屋里的构图定，真实太阳几乎贴着宿舍背墙照不进来。它照在桌面左半、半开抽屉旁的地面和左边下铺上，从门口看亮处正在抽屉那一侧；抽屉缩在桌面下、窗又高，直射光进不了抽屉，抽屉里靠窗口天光。作品集 Level design 图按这盏灯的真实落点画，改了角度要重画。玻璃要保持深色薄玻璃，浅色玻璃会被这盏灯照亮、把窗外蒙白。
- 门扇要塞满门洞，留缝会看到门外的虚空。
- 观感、碰撞走位和雾在 Play Mode 里还没验过。
