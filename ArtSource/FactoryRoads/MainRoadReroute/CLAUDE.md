# ArtSource/FactoryRoads/MainRoadReroute/

## 用途

厂区西端主路改线的规划脚本和记录。原来的发卡形环形路改成一条主路从两栋砖砌厂房之间继续向西、S 形弯过去停在西端厂房 `SM_ConcreteHouse_Pack_2_C_10` 门前，再分出五条支路到各厂房门前和料仓群。

## 内容

- `route.py`：设计主路中线、车道偏移、支路和枯树路灯的新位置，输出 `route.json` 和 `route-plan.png`。需要 numpy、scipy、matplotlib，读 `../../FactoryYardLayout/Survey/` 的采集数据。
- `apply.cs`：通过 Unity MCP 的 execute_code 运行，第一次运行写 `before.json`，之后可重复执行，不保存场景。
- `before.json`：旧环形路的控制点和被移动对象的原变换。
- `Previews/`：同机位改线前后俯视、支路近景和两张眼高视角的编辑器截图。

## 与其他部分的关系

- 主路是 `In-Scene Objects/工厂道路向内` 和 `工厂道路向外` 两条 8.5 米车道，中心相距 6.25 米，沿同一条中线左右偏移；厂门到两栋砖房之间的控制点保留，以西换新控制点。
- 改线的支路在 `In-Scene Objects/工厂支路` 下五条（同组另两条通索道站的见 `../StationRoad/`），宽 5.5 米，从主路中线起步，在门前两三米处渐隐：`支路 至 ConcreteHouse_Pack_2_C_9`（西南厂房北面的门）、`至 ConcreteHouse_Pack_2_C_0`（南面厂房门前水泥坪）、`至 ConcreteHouse_Pack_1_C_2`（北面小厂房南面的门）、`至 BrickHouse_1_C`（北面砖房南墙下）、`至料仓`（西端料仓群和西端厂房北侧）。
- 支路材质 `Assets/Art Resources/Environment/FactoryRoads/DirtPath05/DirtPathSpline.mat`，和主路同一个 shader，贴图是 G87 Path Dirt 05，比主路略深偏棕、没有车辙；Render Queue 比主路低，交叉处主路在上。
- `Assets/Scripts/Environment/FactoryDirtRoadSpline.cs` 的 `endFade` 是首尾渐隐长度，主路 7 米、支路 4 米。
- 同次移动的对象：`In-Scene Objects/环路内枯树` 下 7 棵枯树移到路边，路灯 `SM_StreetLight13`、`15`、`16` 移到主路中线上，`17`、`20`、`22` 移到支路尽头旁，路牌 `SM_StreetSign21`、`22` 改到新路边并转向。
- `Tools > Coal Echoes > Paint Factory Ground` 读场景里所有 `FactoryDirtRoadSpline`，改线后要重刷，路边煤灰才跟着新路走。杂物按新路重排见 `../../FactoryYardLayout/`。

## 注意

- 没有改地形高度，热浪不用刷新。路障、锥桶、钢梁堆没有动。
- G87 Path Dirt 02 只有中间三成不透明，叠上路肩渐隐后几乎只剩碎边，不要拿它做支路。
- G87 贴图的授权情况和主路一样尚未确认。
- Play Mode 观感待确认。
