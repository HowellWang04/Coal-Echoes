# ArtSource/Cableway/SummitStation/FoundationConcrete/

## 用途

山顶终点站水泥基础专用混凝土材质的生成记录和对照截图。基础原来和站棚底座共用 `Station_Concrete`，那张贴图每 3 米重复一次且边缘不无缝，大块深浅斑在 15 米宽的墙面上排成格子，所以给基础单独换了材质。

## 内容

- `request.webp`：用户指出格子感的截图。
- `before-front.png`：改前从 8 号塔方向看站体。`after-side.png`：改后从东侧看受光面。

## 与其他部分的关系

- 贴图由 `../../../Guardhouse/PolyHaven/Scripts/break_tiling.py` 生成，在仓库根目录运行 `python ArtSource/Guardhouse/PolyHaven/Scripts/break_tiling.py concrete_wall_007 2.16 4 "Assets/Art Resources/Environment/Cableway/CablewaySummitStation" SummitFoundation_Concrete`，用 concrete_wall_007 扫描原件，不带门卫室的做旧，重复间隔 8.64 米。
- Unity 资源在 `Assets/Art Resources/Environment/Cableway/CablewaySummitStation/`：`SummitFoundation_Concrete.mat`（HDRP/Lit）和它的颜色、法线、遮罩三张图。材质只挂在场景的 `CablewaySummitStation/StationFoundation` 上，站棚底座和滚轮架的混凝土仍用原来的材质。
- 材质参数从门卫室墙面 `GH_Wall_ConcreteWall007` 复制，改成按模型 UV 映射。

## 注意

- 基础网格的 UV 在每个面上都是 3 米一个单位，贴图 Tiling 取 3 ÷ 8.64。重建基础网格（`../MoveToTower09/move_station.cs`）时如果改了 UV 比例，Tiling 要跟着改。
- 墙面只有扫描本身的细碎纹理，没有大尺度的深浅变化。用户不要灰白色的大块污渍，不要再叠加大尺度明暗。
