# 主路改线

厂区西端原来是一圈发卡形的环形路，主路开到头折回再绕一圈，不像真实厂区。现在改成一条主路从两栋砖砌厂房之间的直路继续向西，在原环形路中间缓缓 S 形弯过去，停在西端厂房 `SM_ConcreteHouse_Pack_2_C_10` 门前，再分出五条支路到各厂房门前。

![改线后](Previews/top-after.png)

## 主路

仍是 `工厂道路向内` 和 `工厂道路向外` 两条 8.5 米车道，中心相距 6.25 米，材质 G87 Road Dirt 02 不变。u 大于 -30 米（厂门到两栋砖房之间）的控制点原样保留，以西换成新控制点，两条车道沿同一条中线左右偏移，尽头 7 米渐隐。旧环形路的控制点记在 `before.json`。

## 支路

`In-Scene Objects/工厂支路` 下五条，宽 5.5 米，从主路中线起步（起点被主路盖住），在门前两到三米处渐隐，首尾渐隐 4 米。

| 支路 | 去向 |
| --- | --- |
| 支路 至 ConcreteHouse_Pack_2_C_9 | 西南厂房北面的门 |
| 支路 至 ConcreteHouse_Pack_2_C_0 | 南面厂房门前的水泥坪，从钢梁堆和临时围挡之间穿过 |
| 支路 至 ConcreteHouse_Pack_1_C_2 | 北面小厂房南面的门 |
| 支路 至 BrickHouse_1_C | 北面砖房南墙下的垃圾箱和桶 |
| 支路 至料仓 | 西端料仓群和西端厂房北侧 |

材质 `Assets/Art Resources/Environment/FactoryRoads/DirtPath05/DirtPathSpline.mat`，和主路同一个 Shader，贴图换成 G87 Path Dirt 05：同一套 G87 土路，覆盖是实的，颜色比主路略深偏棕，没有车辙，读起来是走得少的支路。先试过 G87 Path Dirt 02，它只有中间三成不透明，叠上路肩渐隐后几乎只剩碎边，所以换掉。支路材质排序比主路低（Render Queue 1999），交叉处主路在上。授权情况和 G87 主路贴图一样，尚未确认。

`FactoryDirtRoadSpline` 新增 `endFade`（首尾渐隐长度，默认 7 米，原来写死），主路沿用 7 米，支路 4 米。

## 一并调整

- 7 棵 `环路内枯树` 原来都在环形路中间，正好压在新主路上，移到路边 2.5 到 6 米处，彼此相隔 5 米以上，高度跟随地面。
- 原来立在环形路车道中间的路灯：`SM_StreetLight13`、`SM_StreetLight15` 移到新主路中线上（与直路段约 42 米的间距一致），`SM_StreetLight16` 移到主路尽头的中线上，`SM_StreetLight17`、`SM_StreetLight20`、`SM_StreetLight22` 分别移到三条支路尽头旁边。朝向都与所在道路垂直。
- 路牌 `SM_StreetSign21` 从旧车道边移到新主路北侧路边，`SM_StreetSign22` 原地转向，朝着北支路的行进方向。
- `Tools > Coal Echoes > Paint Factory Ground` 改为读取场景里所有 `FactoryDirtRoadSpline`（原来按四个名字找），重刷后路边煤灰跟着新主路和支路走，旧环形路的痕迹消失。
- 杂物按新路重新摆放，见 [厂区杂物摆放](../../FactoryYardLayout/README.md)。

没有改地形高度，所以热浪不用刷新。路障、锥桶、钢梁堆没有动。

## 文件

- `route.py` 设计主路中线、车道偏移、支路和枯树路灯的新位置，输出 `route.json` 和 `route-plan.png`。需要 numpy、scipy、matplotlib，并用到 `../../FactoryYardLayout/Survey/` 的采集数据。
- `apply.cs` 通过 Unity MCP 的 execute_code 运行，第一次运行写 `before.json`，之后可重复执行，不保存场景。
- `Previews/` 为同机位改线前后、支路近景和两张眼高视角截图，Unity 编辑器渲染。
