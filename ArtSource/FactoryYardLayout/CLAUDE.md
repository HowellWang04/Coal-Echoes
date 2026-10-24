# ArtSource/FactoryYardLayout/

## 用途

厂区空地杂物按种类成组靠墙靠设备摆放的规划脚本和记录。对象是 `工厂建筑群` 下 04 桶罐与容器、05 堆放物与杂物（煤堆除外）、06 搬运与检修设施里散在空地上的杂物，只改位置和朝向，层级和对象不变。

## 内容

- `export.cs`：在编辑器里采集场景，写入 `Survey/`（体积大，不进 git，可重新生成）。
- `yard_common.py`、`plan_layout.py`：规划位置，输出 `plan.json` 和 `apply_plan.json`，需要 numpy、scipy、matplotlib。
- `apply.cs`：先按 `transforms-before.json` 把三组杂物复位，再按 `apply_plan.json` 摆放、落地、离墙微调，可重复执行，不保存场景。`applied.json` 是结果。
- `verify.cs`：检查穿插、落地、悬空和压路，写入 `verification.json`。
- `transforms-before.json`、`transforms-after.json`：调整前后所有杂物的本地变换，按实例 ID 记录。
- `Previews/`：调整前后同机位截图，拍于道路改线之前。
- 脚本都通过 Unity MCP 的 execute_code 运行，坐标系 u 沿厂区主路，v 垂直主路。

## 与其他部分的关系

- 道路包括主路两条车道和 `工厂支路` 下的支路，改线见 `../FactoryRoads/MainRoadReroute/`，`route.py` 也读这里的 `Survey/`。
- 建筑、设备、路障、路灯、标识不动；煤堆单独摆放，见 `../CoalPile/`。

## 注意

- 摆放规则：叠在一起的东西整体移动；落地门前 4.5 米、左右各 0.8 米不放，厂门内侧留空，道路两侧各留 1.2 米；同种杂物同一侧路边 20 米内归一组，沿一面墙排开最长约 16 米、最多两排，组前留 3 米通道；托盘、水泥袋、箱子靠围墙，桶靠储罐和管道，气瓶、推车、梯子靠厂房墙；倒地的梯子和气瓶扶正。
- 落地只认地形和水泥地坪，墙基和料仓底座的碰撞盒比模型宽，会把杂物托到半空。
- 重跑整套流程先复位，再依次跑 `export.cs`、`plan_layout.py`、`apply.cs`、`verify.cs`。重开 Unity 后实例 ID 会变，复位要改用路径匹配。
- 路障、锥桶和水泥隔离墩可能和关卡通行路线有关，没有动。
- 壁挂空调 `SM_AirConditioning10`、`SM_AirConditioning17` 会被报为不接触碰撞体，是那段山墙没有碰撞体，不是悬空。
