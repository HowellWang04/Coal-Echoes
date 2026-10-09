# Tajia02 真实尺寸索道

塔架、加长腿、水泥墩、回转站和吊斗全部按米制建模，Blender 1单位等于1米，导入Unity缩放为1。所有零件使用真实尺寸，改塔高或线距只改参数，不缩放模型。

## 脚本

脚本在 `Scripts`，通过 Blender MCP 在空白文件中按顺序执行。除 `build_tower.py` 外，其余建模脚本都在它的命名空间里执行，复用其中的型材函数和材质。

1. `build_tower.py` 生成15米标准塔。先执行 `entry_tower_params.py` 再执行它，得到8米进站支架。横梁半长 `CROSSHEAD_HALF` 决定线距，当前为2米，即线距4米。
2. `prepare_and_bake.py` 合并网格、展UV并烘焙 AO、法线和底色，依次设置 `STEP` 为 prepare、AO、Normal、BaseColor。
3. `surface.py` 按构件方向取样旧塔锈蚀钢材条带，生成底色和 HDRP Mask。
4. `export_tower.py` 导出塔身FBX并保存源文件。
5. `build_leg_extensions.py` 读取 `leg_extensions.json` 生成各塔加长腿、底板和梯子延长段，按 `EXT_NAMES` 分标准塔与进站支架两次生成。
6. `build_foundation.py` 生成单个水泥墩。
7. `build_station_terminal.py` 生成回转轮、支架和U形转运轨道。
8. `build_bucket.py` 生成约1立方米吊斗。

烘焙和导出用 `BAKE_SCENE`、`BAKE_SOURCE`、`BAKE_OBJECT`、`BAKE_PREFIX`、`BAKE_SIZE` 指定对象与贴图前缀。

## 加长腿数据

`leg_extensions.json` 由Unity按当前地形计算，记录每座塔的塔身底面高度、四条腿的加长长度和梯子加长长度。墩顶取墩中心地面以上0.4米与墩内最高点以上0.05米中较高者。塔移动位置后需要重新计算此文件，再重跑第5步。

## Unity

资源在 `Assets/Art Resources/Environment/Cableway/CablewayTower_Metric15`。标准塔与进站支架各有预制体，场景实例下另有 `LegExtensions` 和 `Piers` 子对象。站内设备在 `CablewayFoothillStation` 下，包括两组 `CablewayStationRollerFrame` 和 `CablewayReturnTerminal`。缆线网格为 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset` 与 `Cable_1.asset`，在站内经回转轮连成一圈，垂度约为跨距的2.2%。

FBX导出到Unity后，Blender坐标 (x, y, z) 对应Unity本地坐标 (-x, z, -y)。

2026年10月4日修正两种塔架顶部轮组方向，轮轴横跨绳索，两个轮心沿绳索前后排列。完整轮组绕各自中心转90度，模型、源文件及建模脚本已同步，见[修正记录](../SheaveAlignment/README.md)。

## 备份

`UnityIntegration` 保存每一轮修改前的场景和缆线网格，文件名注明对应修改。地形恢复前的数据在 `TerrainReferences/SheepRange_USGS_1m/RestoreTowerPads_Before`。

## 未完成

山脚站棚新位置地面有坡，局部埋入地面，待整平。2026年10月4日已将11号塔替换为与始发站相同配置的山顶终点站，并完成末端绳索回转连接，见[终点站装配记录](../SummitStation/README.md)。装卸煤与吊斗移动尚未实现，未进行Play Mode验证。
