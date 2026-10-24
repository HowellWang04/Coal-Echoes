# 山顶终点站

2026年10月4日完成静态装配，未运行 Play Mode。

主场景的 `In-scene Objects/索道群/站房/CablewaySummitStation` 替换最高处的 `CablewayTower_11`。复用当前山脚始发站的两座站棚、两组滚轮架与一套回转轮和转运轨道，入口朝向10号塔。站口位于原11号塔沿下山方向4米处。原11号塔的吊斗保留为终点站下的 `CoalBucket_Approach`。

## 场地

设备占地内地面高差约5.91米。新增15米乘15.5米的混凝土基础，顶面水平，基础侧壁按现有 Main Terrain 高度延伸并埋入地面0.4米，采用站棚混凝土材质并配置静态 MeshCollider。基础网格保存为 `Assets/Art Resources/Environment/Cableway/CablewaySummitStation/StationFoundation.asset`。未改变地形、MapMagic 图或其余塔架位置。

## 缆线

保留两条缆线从山脚到10号塔的原有顶点，仅重做10号塔之后的最后一跨和山顶进站段。最后一跨沿用跨距约2.2%的抛物线垂度，缆线中心距站口设备基准面4.85米，线距4米。两条线经过三只滚轮后延伸到回转轮，`Cable_0` 承载回转轮外侧半圆，连接到 `Cable_1`，组成闭合线路。

10号塔与终点站前的两只吊斗已重新贴合绳段。保留场景原有全部12只吊斗，其中2号塔在本次修改前已有两个吊斗。

## 验证

`verification.json` 为保存后的编辑器静态检查。剩余10座塔架，终点站37个 Renderer，无丢失脚本或无效网格材质。与操作前现场基线比较，改动范围外的对象变换和层级未变，58份地形资源哈希相同。

两条缆线保留段分别有3408和3216个顶点，位移均为零。山顶闭合接缝的浮点误差约0.061毫米，滚轮处误差小于0.31毫米。最后一跨缆线相对原地形的最小净高约7.10米，减去吊斗原点至斗底2.617米后约4.48米。该净高只覆盖最后一跨，不代表全线或站内运动碰撞验证。

`after.png` 和 `return-wheel.png` 为 Unity Scene 视图检查截图。未实现移动、脱索上轨、装卸煤或玩家乘坐功能，Play Mode 效果由用户确认。

## 备份与脚本

`Before/Main Game Scene.disk.unity` 保存操作前磁盘版本，`Before/Main Game Scene.live.unity` 保存包括原有未保存修改在内的现场版本。`Before` 中还有两条原缆线网格和场景变换、材质与地形哈希基线。

本目录的 C# 文件为 Unity CLI `eval_file` 使用的一次性装配与检查脚本，不参与游戏编译。`place_station.cs` 创建站体与基础，`connect_cables.cs` 修改末端线路并替换11号塔，`verify.cs` 执行静态对照，`save.cs` 保存场景。重跑装配脚本前需核对现场条件，不能将其当作运行时组件。

## 下一步

后续已完成独立移动管理对象、自动启动与关卡事件接口，以及非 Play Mode 的 Scene 移动预览，见 [移动说明](../Movement/README.md)。具体关卡操作绑定和 Play Mode 效果确认仍待完成。
