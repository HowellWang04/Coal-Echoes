# ArtSource/Props/DeskLamp/

## 用途

第一关门卫室桌上摔坏的军绿色铁皮台灯，场景陈设，没有交互。圆形配重底座带拨动开关，灯杆弯了，关节和短灯臂还在；灯罩铰链断了，灯臂末端只剩露着的灯座和半截碎灯泡，钟形灯罩掉在桌上侧躺着，上侧压出一个凹坑。军绿漆边角掉漆露出黑钢和锈，灯罩内的奶白搪瓷发黄，朝上的面积灰，布线拖在桌面上。

## 内容

- `Scripts/build_desklamp.py`：在后台 Blender 进程里建模、烘焙和导出，约半分钟。底座、灯罩、灯座是旋转体，灯杆、灯臂和电线是沿路径的圆管，合并成一个网格；军绿漆、搪瓷、铁件、碎玻璃和布线五种程序化表面用 Cycles 烘焙到一张 1024 贴图上，最后只用一个材质。`build.log`、`verification.json` 是运行输出。
- `Blender/DeskLamp.blend`：源文件，贴图已打包，PreviewStudio 集合是预览用的桌面、灯和相机，不导出。
- `Exports/`：FBX（内嵌贴图）和 GLB。
- `Textures/`：BaseColor、Roughness、Metallic、Normal 和 HDRP_Mask。
- `Previews/`：整体、侧面、灯罩内部三张 Cycles 渲染。
- `UnityIntegration/ImportDeskLamp.cs`：设置贴图导入，生成 HDRP/Lit 材质和预制体，不动场景。

## 与其他部分的关系

- 预制体 `Assets/Art Resources/Props/DeskLamp/DeskLamp.prefab`，轴心在底座底面中心，掉落的灯罩在 +Z 一侧，电线朝 −X、−Z 拖出去。
- 场景实例在 `In-Scene Objects/工厂大门/Guard Post/室内陈设/DeskLamp`，放在值班桌（`../GuardhouseFurniture/`）离椅子较远的后角，灯罩落在值班记录本旁边。摆放数值在 `../GuardhouseFurniture/UnityIntegration/PlaceInGuardhouse.cs`。

## 注意

- 整盏灯连电线和灯罩约 0.40 × 0.57 米，放到桌上时电线和灯罩很容易伸出桌沿或压到记录本，挪动后要检查全部顶点都在桌面范围内。
- 没有碰撞体，也没有灯光组件。
- 改了脚本要重跑，再把 FBX 和三张贴图（BaseColor、Normal、HDRP_Mask）复制到 Assets 下，然后跑 `ImportDeskLamp.cs`。
