# ArtSource/FactoryBoundary/

## 用途

厂区围墙模块的源文件和脚本：9 米墙段的六个变体、独立砖柱、3 米标准墙段，以及门卫室旁补缺口的短墙。大门本体在 `../FactoryGate/`。

## 内容

- `FactoryBoundary.blend`：大门、柱子、3 米墙三个基础模块的源文件，贴图已打包。`FactoryWall_9m_Variants.blend`：9 米墙段源文件。
- `build_boundary.py`：后台 Blender 执行，读当前大门源文件，覆盖大门导出，重建立柱和围墙，输出源文件和两张预览，不建备份。
- `build_wall_variants.py`：输出 9 米源文件和六个 GLB，六版几何、砖块排列、UV 和材质完全相同，只改逐砖顶点颜色分布，没有白灰；`wall_variants_report.json` 核对几何和颜色哈希。
- `crop_guardhouse_wall.py`：从 `FactoryWall_3m.glb` 裁出 0.82 米的 `FactoryWall_GuardhouseShort.glb`，不含白灰。
- `fix_concrete_top_uv.py`：把大门源文件、两个围墙源文件和 `Models/`、门卫室 `Guardhouse_WallReturns.glb` 里混凝土水平面的 UV 移到贴图干净的中段，大门 GLB 和 FBX 从源文件重导，其余 GLB 只改 UV 字节；已执行，重跑不会再改。
- `refresh_brick_color.py`：模块拆分前的历史调色脚本，读旧备份。
- `boundary_report.json`、`unity_prefab_report.json`、`unity_import_verification.json`：导出尺寸、预制体和 Unity 导入检查。
- `Previews/`：三个基础模块的渲染、直线和直角拼接演示（演示排列没进场景），以及门卫室后墙在 Unity 里的实景。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/Gate Area/FactoryBoundary/`：`Models/` 是柱子、3 米墙、9 米六版、门卫室短墙和大门旁短墙（`FactoryWall_GateShort.glb`，由 `../FactoryRoads/crop-entrance-wall.py` 裁出）的 GLB，`Prefabs/` 是 `FactoryGate`、`FactoryPillar`、`FactoryWall_3m`、`FactoryWall_9m_01` 至 `_06`，同目录的 CLAUDE.md 是使用说明。GLB 不嵌贴图，预制体复用大门的材质和贴图，带简单碰撞体和拼接定位点。
- 预制体由 `../FactoryGate/BuildBoundaryPrefabs.cs` 在 Unity 里生成（Build 重新保存三个基础预制体，Validate 只检查）。
- 主场景 `In-Scene Objects/工厂围墙` 沿路线混排 88 段墙和 89 根柱，相邻墙段不重复，转角之间按实际距离收口，坡地用混凝土基座；门卫室短墙是 `Rear and East Wall/Wall_030_V06/FactoryWall_GuardhouseShort`，补 `Pillar_030` 与门卫室之间的缺口，完整的一端插入墙柱，裁开的一端藏在门卫室外墙里。场景里的大门直接引用 `../FactoryGate/` 的 GLB，不用 `Prefabs/FactoryGate`。
- 摆放和接缝记录在本机 `Library/FactoryBoundaryPlacement/`。

## 注意

- 规格：3 米模块柱心间距 3 米、墙高约 3.68 米、墙身厚 0.50 米；9 米模块定位点间隔 9 米、墙身长 8.25 米。
- 逐砖色差是固定的顶点颜色，没有实例随机材质。
- 不要直接把未配置共享材质的 9 米 GLB 拖进场景。
- `refresh_brick_color.py` 不要对当前模块运行，它会恢复已删除的短墙；后续调色以当前源文件为输入。
- `.gitignore` 忽略 `*.log`、`*.blend1` 和 `__pycache__/`。实际观感和通行待 Play Mode 验收。
