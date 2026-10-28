# ArtSource/FactoryGateGrass/

## 用途

厂门内外的枯草摆放脚本和前后截图。主场景 `In-Scene Objects/工厂大门/` 下三组：`门口干草` 110 丛围着厂门；`门口补草` 57 丛补在大门正前方；`厂内干草` 433 丛铺满进门两侧的空地。都用 RockyDesert 包的 `GrassDry_Desert_01` 到 `03`，保持预制体链接和原有 LOD，没有碰撞。

## 内容

- `place-gate-grass.cs`：通过 Unity MCP 执行的 Editor 方法体，固定随机种子，重跑会删掉旧的 `门口干草` 并生成同样的布局。
- `add-gate-front-grass.cs`：同样方式执行，只重建 `门口补草`，读 `门口干草` 的位置保持间距，不改动它。
- `add-yard-grass.cs`：同样方式执行，只重建 `厂内干草`，读前两组的位置保持间距。
- `before-eye.png`、`after-eye.png`、`after-side.png`、`lod-normals-after-close.png`：改前改后的玩家视角、侧面和路中线草丛近景。
- `front-before-eye.png`、`front-after-eye.png`、`front-after-close.png`：补草前后的玩家视角和门脚近景。
- `yard-request-top.webp`：用户圈出的厂内种草范围。`yard-after-top.png`、`yard-after-east.png`、`yard-after-alley.png`、`yard-after-strip.png`：种完后的俯视、宿舍楼前大院、前墙与车间之间的夹道、车间临路一侧。

## 与其他部分的关系

- `Assets/Scripts/Environment/Editor/DeadGrassLodNormals.cs` 在导入 `SM_DeadGrass_*_LOD*` 时把法线全改成朝上并重算切线，和 LOD0 一致；包里 LOD1 到 LOD3 本来是真实几何法线，约一半朝下，切换后草丛会从黄色变棕黑。
- 摆放避开的对象：门洞、摩托车（`../DustyMotorcycle/`）和玩家出生点、路面、厂内道路两侧。厂内那组靠碰撞体避开车间、宿舍楼、沙丘和宿舍楼前院的沙堆（`../SandDune/`）、围墙和车间旁的托盘水泥袋，这些对象挪动或改了碰撞后要重跑 `add-yard-grass.cs`。

## 注意

- 摆放规则：墙根两侧离门 4.5 到 32 米，墙外 0.6 到 1.8 米最密，门柱和墙垛附近更密；门卫室墙脚一圈 0.3 到 1.4 米，前后门 2.5 米内不放；门外土路两侧离中线 6.8 到 9.5 米稀疏成簇，两道车辙之间从离门 6 米起零星矮草；门洞前后 7 米、摩托车和出生点 3 米内、路面上、厂内道路两侧 5.5 米内不放；每簇一到五丛，相互至少隔 0.45 米。
- 补草规则：只放门外，大门向厂内开，门扇扫过的地方不放草。门洞前门脚一排矮草（门槛外沿 1.1 米起），门柱两侧成簇；门前到摩托车之间的路面零星矮草，离门越远越稀，离路中线 1.6 到 4.6 米的车辙带不放；门两侧 9 到 18 米墙根加密；离门 13 到 24 米的路肩补在离中线 5.2 到 6.6 米。摩托车、出生点和门卫室门口的留空沿用上一条。
- 厂内规则：前墙内侧到约 23 米深（宿舍楼和沙丘前沿），东到东墙，西到西墙，中间让出厂内道路（离车道中线 3.2 米内不放，草略微长进路沿）和门扇向内扫过的 6.2 米。1.6 米抖动网格撒种，低频噪声让疏密成片；离围墙 2.2 米内或 1.8 米内有碰撞体的地方更密、草更大。头顶有房檐或灯头、脚下 0.3 米内有碰撞体的点不放。车间和路之间只有约 3.5 米宽，网格撒不满，另外沿墙脚每隔约一米在空位里补种。
- 三个脚本按 `place-gate-grass.cs`、`add-gate-front-grass.cs`、`add-yard-grass.cs` 的顺序跑；只重跑后面的不影响前面的，重跑前面的要把后面的也重跑一遍，否则间距对不上。
- 脚本把每丛 LODGroup Size 设为 1.34 除以自身缩放，让大小不一的草在同样距离切换（约 5、16、40 米换低模，80 米剔除）。
- `GrassDry_Desert_01` 远处两级 LOD 统一换成 `MI_DeadGrass_A`，原来的 `MI_DeadGrass_A1` 裁剪阈值高，切换时突然变稀。
- `T_DeadGrass_A1.TGA` 打开了 Mip Maps Preserve Coverage，参考值与材质裁剪阈值一致。这项设置只存在本机的包里。
- RockyDesert 包不进 git，新克隆的仓库里这些草会显示丢失。
