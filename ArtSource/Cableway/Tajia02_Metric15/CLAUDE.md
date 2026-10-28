# ArtSource/Cableway/Tajia02_Metric15/

## 用途

主场景现在用的米制索道模型：15 米标准塔、8 米进站支架、各塔加长腿、水泥墩、回转站和吊斗。Blender 1 单位等于 1 米，导入 Unity 缩放为 1，改塔高或线距只改脚本参数，不缩放模型。

## 内容

- `Scripts/`：通过 Blender MCP 在空白文件里按顺序执行，除 `build_tower.py` 外都在它的命名空间里跑，复用其中的型材函数和材质。
  1. `build_tower.py` 生成标准塔；先执行 `entry_tower_params.py` 再执行它得到进站支架。横梁半长 `CROSSHEAD_HALF` 决定线距。
  2. `prepare_and_bake.py` 合并网格、展 UV 并烘焙 AO、法线和底色，依次把 `STEP` 设为 prepare、AO、Normal、BaseColor。
  3. `surface.py` 按构件方向取样旧塔锈蚀钢材条带，生成底色和 HDRP Mask。
  4. `export_tower.py` 导出塔身 FBX 并保存源文件。
  5. `build_leg_extensions.py` 读 `leg_extensions.json` 生成各塔加长腿、底板和梯子延长段，按 `EXT_NAMES` 分标准塔与进站支架两次生成。
  6. `build_foundation.py` 生成单个水泥墩。
  7. `build_station_terminal.py` 生成回转轮、支架和 U 形转运轨道。
  8. `build_bucket.py` 生成约 1 立方米的吊斗。
  烘焙和导出用 `BAKE_SCENE`、`BAKE_SOURCE`、`BAKE_OBJECT`、`BAKE_PREFIX`、`BAKE_SIZE` 指定对象和贴图前缀。
- `Blender/`：标准塔、进站支架、两种加长腿、水泥墩、回转站、吊斗各一份源文件，`*_status.txt` 是烘焙状态记录。
- `Exports/`：各部件 FBX。
- `Textures/`：各部件的 BaseColor、Normal、HDRP_Mask 和 AO。
- `leg_extensions.json`：由 Unity 按当前地形算出的每座塔塔身底面高度、四条腿和梯子的加长长度。
- `verification_*.json`：标准塔和进站支架的导出检查。
- `UnityIntegration/`：每轮修改前的场景和两条缆线网格备份，文件名注明对应修改。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/Cableway/CablewayTower_Metric15/`，预制体 `CablewayTower_Metric15`、`CablewayEntryTower`、`CablewayReturnTerminal`。
- 主场景 `In-Scene Objects/索道群/塔架/CablewayTower_01` 是进站支架，`_02` 至 `_08` 是标准塔，实例下各有 `LegExtensions` 和 `Piers` 子对象。原 9 至 11 号塔已拆，山顶终点站在原 9 号塔旁，见 `../SummitStation/`。
- 山脚站内设备在 `索道群/站房/CablewayFoothillStation` 下，见 `../FoothillStationPlacement/`。
- 缆线网格是 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset` 和 `Cable_1.asset`，经两站回转轮连成一圈，垂度约为跨距的 2.2%。吊斗沿缆线运动见 `../Movement/`，塔顶轮组方向的修正见 `../SheaveAlignment/`。
- 墩顶高度取墩中心地面以上 0.4 米与墩内最高点以上 0.05 米中较高者。

## 注意

- 线距由 `CROSSHEAD_HALF` 决定，现在是 2 米即线距 4 米，站房、缆线和终点站都按这个线距做，改它要连带改。
- 塔移动位置后要重新计算 `leg_extensions.json`，再重跑第 5 步。
- FBX 导到 Unity 后，Blender 坐标 (x, y, z) 对应 Unity 本地坐标 (-x, z, -y)。
- 山脚站棚所在地面有坡，局部埋入地面，不整平；山脚站的回转站比建模基准低 1.5 米，见 `../FoothillLowering/`。
- 装卸煤还没做，Play Mode 没有验证过。
