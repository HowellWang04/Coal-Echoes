# ArtSource/SummitBoardwalk/

## 用途

第四关上站背后的木栈道和山头观景平台，连带站房基础顶面一圈同样式的栏杆。平桥从站房基础顶面后沿出发，沿站房中轴线架空跨过站后的低洼，到头接一小段平台，往左转 10 度，再分三段木台阶顺山脊上到山头，山头是四面有栏杆的观景平台。在后台 Blender 里按 Unity 导出的地形高度生成，木料用 Poly Haven 的旧木板扫描。

## 内容

- `Scripts/survey.cs`：Unity MCP execute_code 的一次性脚本，记录栈道起点（基础顶面后沿、LineB 站棚正后方）、站房朝向、基础顶面范围和两条缆线穿过基础前沿的位置，导出路线一带 0.5 米间距的地形高度，写到 `Survey/`。
- `Survey/site.json`、`Survey/terrain_heights.json`：上面脚本的结果，Blender 脚本读它们。
- `Scripts/build_boardwalk.py`：后台 Blender 建模、导出 FBX、渲染三张预览，约一分钟。开头的常量是布局（平桥长度、转角、平台高度、台阶级数、踏步深、歇脚平台长、观景平台尺寸）和构件截面。每块木料取扫描里的一条木板，按实际尺寸贴、木纹顺长边；柱脚是水泥墩，按柱下地形最高点露出地面 0.12 米、最低点往下埋 0.45 米；高的木架加剪刀撑。同时生成一份只给碰撞用的简化网格。`build.log`、`verification.json` 是运行输出，后者记着台阶梁离地的最小和最大距离、最高的柱子、观景平台离地高度。
- `PolyHaven/weathered_planks/`：Weathered Planks（polyhaven.com/a/weathered_planks），CC0，2k 的颜色、OpenGL 法线、粗糙度三张 JPG。一张对应实物 2 米，竖排九条木板，脚本里记着每条缝的像素位置。
- `Blender/SummitBoardwalk.blend`：源文件，PreviewStudio 集合是预览用的地形、站房方块、灯和相机，不导出。
- `Exports/SummitBoardwalk.fbx`：两个网格。`SummitBoardwalk` 是看得见的模型（含基础栏杆），木料和水泥两个材质槽；`SummitBoardwalk_Collision` 是碰撞网格，只有桥面、台阶斜坡、平台和栏杆挡墙。
- `Previews/`：`SummitBoardwalk_*.png` 是 Blender 渲染（侧面、站房后沿眼高往上看、观景平台往回看）；`unity-*.png` 是放进场景后的截图，带 `-night` 的是切成月夜后拍的，`unity-foundation-*` 和 `unity-walk-junction` 是基础栏杆、前沿吊斗缺口和栈道接口；`unity-dormitory-zoom-night` 是月夜从平台靠东栏杆边用 275mm 拍宿舍楼，`unity-dormitory-zoom-center-day` 是白天从平台中央拍，宿舍楼被站房基础挡住。
- `UnityIntegration/ImportBoardwalk.cs`：设贴图导入（重复平铺），生成 HDRP/Lit 木料材质，FBX 不导入材质；不动场景。
- `UnityIntegration/PlaceBoardwalk.cs`：按 `site.json` 把模型放进场景，换材质，关掉碰撞网格的渲染并加 MeshCollider；会重建 `SummitBoardwalk`，连带删掉挂在它下面的两个第四关触发盒，重跑前先把它们挪出来。不保存场景。
- `UnityIntegration/RidgeDormitoryEcho.cs`：第四关从平台拉近拍宿舍楼的回响，放站位点、给宿舍楼加 `EchoTrigger`、把 Ignore Raycast 层从回响遮挡里去掉；开头注释记着实测的焦距、对焦和看得见宿舍楼的位置。不保存场景。

## 与其他部分的关系

- Assets 在 `Assets/Art Resources/Environment/SummitBoardwalk/`：`Models/SummitBoardwalk.fbx`；`Textures/WeatheredPlanks_BaseColor.jpg`、`_Normal.jpg` 是扫描原件改名，`_HDRP_Mask.png` 由粗糙度换算（R 金属 0，G AO 1，B 细节 1，A 光滑度 = 1 − 粗糙度）；`Materials/SummitBoardwalk_WeatheredWood.mat`。
- 场景对象 `In-Scene Objects/山顶栈道/SummitBoardwalk`，是 FBX 模型实例，位置是 `site.json` 的起点，Y 旋转和站房根节点 `CablewaySummitStation` 相同。子对象 `SummitBoardwalk` 用木料材质和站房基础的 `SummitFoundation_Concrete`，水泥墩的 UV 和基础一样按 3 米一个单位；`SummitBoardwalk_Collision` 渲染关掉，挂 MeshCollider。
- 起点在站房基础（`../Cableway/SummitStation/`）顶面后沿，桥面和基础顶面齐平，玩家从站台绕过 LineB 站棚走上去。基础栏杆的立柱挂在基础侧壁外面，柱脚外侧绕基础一圈钉着一根横木条；后沿的栏杆和横木条到栈道头一对立柱为止，和栈道扶手接成直角。
- 第四关流程见 `Docs/design.md` 关卡4：第 2、3 步沿栈道上山头，第 4 到 7 步在观景平台上。月夜和平台上拍照的取景见 `Docs/Specs/Systems/moonlight-spec.md`。
- 第二关的山路图上画着这段栈道和观景平台，坐标由 `../Props/MountainRoadMap/Scripts/export_layout.cs` 导出，见 `../Props/MountainRoadMap/`。
- 平台中央的空物体 `In-Scene Objects/山顶栈道/Ridge Dormitory View` 和 `Ridge Night View` 是第四关两个回响的站位点，回响挂在 `WorkersDormitory` 和门前的 `Flagpole` 上，参数见 `Docs/Specs/Systems/echo-system-spec.md` 场景里的回响；同一层的 `Summit Lookout Spawn` 是读档时放到平台上的点。
- 根节点下的两个触发盒 `Boardwalk Zone`（平桥起头）、`Summit Lookout Zone`（观景平台）给 `Level4HintFlowManager` 判断走上栈道和到了平台，见 `Docs/Specs/Levels/level-four-spec.md`。

## 注意

- 坐标：栈道局部 u 朝站房右、v 朝站房前、y 朝上，原点在起点。Blender (x, y, z) = (−u, −v, y)；FBX 用 bake space transform 导出，导进 Unity 根节点没有旋转和缩放，网格坐标就是 (u, y, v)。
- 台阶每级约 0.184 米、踏步 0.29 米、坡度约 32 度，三段各 17 级，中间两个 1.2 米的歇脚平台。碰撞网格在台阶上是一块穿过踏步前沿的斜坡，比玩家角色能走的 45 度缓。观景平台在山路图画叉的山头南缘，桥面高出站台 9.4 米，平台下地面高低差约一米。
- 柱长、台阶离地和平台高度都按地形算。改了这一带地形要先重跑 `survey.cs`，再跑 `build_boardwalk.py`，把 FBX 拷到 Assets 下覆盖，看 `verification.json` 里台阶梁离地最小距离还在 0.3 米以上；只改材质或贴图就跑 `ImportBoardwalk.cs`。
- 改了平桥长度、转角、台阶级数或平台尺寸，要同步山路图导出脚本里抄的几个常量，重新导出布局、重画山路图。
- 吊斗挂在缆绳下约 2.6 米，经过基础前沿时斗底离顶面不到 1 米，所以前沿栏杆在两条缆线处各留 2 米宽的缺口，缺口处只有碰撞墙挡玩家，吊斗照常穿过。站房挪位、改线距或改吊斗都要重跑 `survey.cs` 和 `build_boardwalk.py`，缺口跟着缆线走。后沿的吊斗绕回转轮时离顶面 2 米多，不受栏杆影响。
- 基础四边的碰撞墙只在栈道入口断开；站棚、滚轮架和回转轮本身没有碰撞体。
- 白天的沙尘雾和月夜的雾都浓，广角从观景平台看不清 900 米外厂门一带，拉到 275mm 才看得出宿舍楼和厂房轮廓。站房基础挡在正南偏东，平台中间和西侧看不见宿舍楼，只有靠东的栏杆边看得见；平台改尺寸或挪位后要重新核对这条视线。厂区和星空同框那张的取景还要和月亮方位、雾一起调。
- Play Mode 里还没走过：桥面和基础的衔接、台阶的手感、转向小平台处的栏杆接缝、吊斗穿过前沿缺口时会不会擦到立柱，要实际走一遍、看一趟确认。
