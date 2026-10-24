# ArtSource/Props/GuardhouseFurniture/

## 用途

第一关门卫室的值班桌和椅子，以及门卫室室内陈设的摆放记录。桌子是七十年代厂里常见的一头沉办公桌，有一个中间抽屉，右侧柜体上是抽屉、下是带锁的柜门；椅子是木板座的横档靠背椅。两件都刷红棕漆，边角掉漆露出木色，朝上的面积灰，缝隙发黑。

## 内容

- `Scripts/build_furniture.py`：在后台 Blender 进程里建模、烘焙和导出，约半分钟。各部件是带倒角的方块，每件合并成一个网格；程序化漆面（木纹、边缘掉漆、桌面椅面磨薄、积灰、角落污垢）用 Cycles 烘焙到一张 2048 贴图上，最后每件只用一个材质。`build.log`、`verification.json` 是运行输出。
- `Blender/GuardhouseFurniture.blend`：源文件，贴图已打包，PreviewStudio 集合是预览用的地面、墙、灯和相机，不导出。
- `Exports/`：GuardDesk、GuardChair 的 FBX（内嵌贴图）和 GLB。
- `Textures/`：每件一套 BaseColor、Roughness、Metallic、Normal 和 HDRP_Mask。
- `Previews/`：整体、桌子正面、椅子三张 Cycles 渲染。
- `UnityIntegration/ImportFurniture.cs`：设置贴图导入，生成 HDRP/Lit 材质和带 BoxCollider 的预制体，不动场景。
- `UnityIntegration/PlaceInGuardhouse.cs`：把桌椅、台灯和现有素材里的杂物摆进门卫室，并把相框和值班记录本移到这里，写着全部位置，不保存场景。

## 与其他部分的关系

- 预制体在 `Assets/Art Resources/Props/GuardhouseFurniture/`，正面朝 +Z，轴心在底面中心。桌子尺寸 1.20 × 0.60 × 0.78 米，深度不含拉手；椅子座高 0.45 米，靠背顶 0.90 米。
- 场景实例在 `In-Scene Objects/工厂大门/Guard Post/室内陈设` 下。桌子靠前墙，在服务窗下面、朝大门那面观察窗的墙角；椅子拉开并斜着放。桌上是值班记录本 `Logbook` 和台灯（`../DeskLamp/`），灯罩掉在记录本旁边。街道照片相框歪挂在后墙中间。断线钳（`../BoltCutter/`）钳头着地，斜靠在朝大门那面侧墙的观察窗下。值班床靠背对大门的侧墙，两个铁皮柜和一个木箱靠后墙。
- 杂物来自 `ThirdPartyPacks/Military_VOL4_Furniture`（床 `SM_Bed_01a`、柜 `SM_Locker_01b`）和 `Industrial_Harbour`（`SM_WoodBox`，缩到 0.6）。床和柜换成本目录 `Materials/` 下压暗的副本 `GuardBed_Worn`、`GuardLocker_GreyGreen`，素材包原材质没改。
- 门卫室本体见 `../../Guardhouse/`，坐标换算是 Blender (x, y, z) 对应 Guard Post 局部 (−x, z, −y)。值班记录本、街道照片相框和断线钳的摆放见 `Docs/Specs/Levels/opening-level-one-spec.md`。

## 注意

- 桌面在 Guard Post 局部高度 0.96 米，桌面范围 x 1.30 到 2.50、z 1.47 到 2.09，后沿离墙 1 厘米；记录本和台灯已占满桌面，放不下 0.77 米长的断线钳。
- 值班记录本 `Logbook` 的轴心在本子中心，按 E 查看时才会绕自己转、停在画面中间；`PlaceInGuardhouse.cs` 里它的位置按这个轴心写。
- 改了 `build_furniture.py` 要重跑，再把 FBX 和三张贴图（BaseColor、Normal、HDRP_Mask）复制到 Assets 下，然后跑 `ImportFurniture.cs`。
- 两件都是同时在原点建模的，烘焙用的 AO 节点只算本物体，否则会互相投影。
- 现有素材里没有合适的小件杂物（暖水瓶、搪瓷缸、煤炉、挂历之类），纸箱 `SM_Cardboard` 贴图显示不正常，水泥袋印英文，都没用。木箱几个侧面都有英文喷字，转不掉，转向还会碰到床。
- Military 家具包的 FBX 里带 Unreal 碰撞壳 `UCX_*`，导入后会当普通网格显示成泛白的板子（床头床尾、柜门前），摆放时要停用，碰撞用根节点上的 BoxCollider。
