# 厂门干草

主场景 `In-Scene Objects/工厂大门/门口干草` 下有 110 丛枯草，用的是 RockyDesert 包里的 `GrassDry_Desert_01` 到 `03`，保持预制体链接，带原有 LOD，没有碰撞。

`place-gate-grass.cs` 是通过 Unity MCP 执行的 Editor 方法体，固定随机种子，重跑会删掉旧的 `门口干草` 并生成同样的布局。

## 摆放规则

1. 墙根两侧离门 4.5 到 32 米，墙外 0.6 到 1.8 米最密，墙内稍稀；门柱和墙垛附近概率更高，草丛缩放 0.9 到 1.4。
2. 门卫室墙脚一圈 0.3 到 1.4 米，前后门 2.5 米内不放。
3. 门外土路两侧离中线 6.8 到 9.5 米稀疏成簇；路中线两道车辙之间从离门 6 米开始零星长矮草，缩放 0.45 到 0.7。
4. 门洞前后 7 米、摩托车和玩家出生点 3 米内、路面上都不放，厂内道路两侧 5.5 米内也不放。
5. 每簇一到五丛，位置按高斯分布散开，相互至少隔 0.45 米；朝向随机，倾斜 6 度以内并一半贴合地面法线，按缩放下沉 5 厘米。

## 远近一致

1. 预制体 LOD 按屏幕占比切换，草丛大小不一时小的先消失。脚本把每丛的 LODGroup Size 设为 1.34 除以自身缩放，所有草在同样距离切换：约 5 米、16 米、40 米换低模，80 米剔除（竖直 FOV 80）。
2. `GrassDry_Desert_01` 远处两级 LOD 原用裁剪阈值 0.5 的 `MI_DeadGrass_A1`，切换时会突然变稀，脚本统一换成 `MI_DeadGrass_A`。
3. `T_DeadGrass_A1.TGA` 打开了 Mip Maps Preserve Coverage，参考值 0.191 与材质裁剪阈值一致，远处细枝不再被 mip 平均掉。
4. 包里 LOD0 网格的法线全部朝上，LOD1 到 LOD3 却是真实几何法线，约一半朝下，约 5 米第一次切换后草丛就从黄色变成棕黑色。`Assets/Scripts/Environment/Editor/DeadGrassLodNormals.cs` 在导入 `SM_DeadGrass_*_LOD*` 时把法线全改成朝上并重算切线，与 LOD0 一致。脚本在 git 里，素材包重新导入也会生效。

![修正后路中线的草](lod-normals-after-close.png)

## 注意

RockyDesert 包不进 git，新克隆的仓库里这些草会显示丢失，上面第 3 条贴图导入设置也只存在本机的包里。

![玩家视角](after-eye.png)

![侧面](after-side.png)

改之前的玩家视角见 `before-eye.png`。
