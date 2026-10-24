# 厂外四车道公路

柏油材质使用 Poly Haven 的 [Clean Asphalt](https://polyhaven.com/a/clean_asphalt)，作者 Dimitrios Savva，按 [CC0](https://polyhaven.com/license) 提供。项目采用 2K Diffuse、OpenGL Normal 和 Roughness，按原素材约 2.1 米的尺度平铺，粗糙度反相后写入 HDRP Mask 的平滑度通道。

场景对象位于 `In-Scene Objects/厂外公路与连接路`。横向公路长 930 米，路面宽 15.5 米，中心路拱高 0.155 米，包含双黄线、车道虚线及边线。两端各 65 米逐渐混入砂地，避开山体与隧道；约 350 米的厂门连接路沿沟谷延伸，沿用厂内双 spline 的材质和不规则边缘混合。连接路的每条贴地网格均为 8.5 米宽，中心间距约 6.224 米，重叠后路面覆盖约 14.7 米，厂门前后不再收窄，入厂段以缓弯接入主路。

道路碰撞由局部整平后的 Terrain 提供，网格在编辑器和运行时按场景保存的纵断面生成。地形准备与应用脚本位于 `TerrainReferences/FactoryHighway`，原始高度源和受影响地形备份留在其本地 `Before` 目录中。Main、Draft、高度源、边缘缓存及热浪已同步，实际通行和远景效果待 Play Mode 确认。

当前门洞约 15.3 米，门卫室侧门柱保持原位，另一侧接墙已裁短。线形与门洞调整脚本为上级目录的 `align-gate-approach.cs`，局部路基修整为 `grade-widened-approach.cs`；这些是当前场景的一次性编辑记录，不应重新运行旧 `setup.cs` 覆盖现有布局。
