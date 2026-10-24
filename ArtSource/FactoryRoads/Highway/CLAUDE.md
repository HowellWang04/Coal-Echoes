# ArtSource/FactoryRoads/Highway/

## 用途

厂外横向四车道柏油公路和它到厂门的连接路的说明。公路带双黄线、车道虚线和边线，两端渐渐混入砂地避开山体和隧道；连接路沿沟谷到厂门，以缓弯接入厂内主路。

## 内容

无，只有本说明。地形准备和应用脚本在 `TerrainReferences/FactoryHighway/`，线形与门洞调整脚本在上级目录。

## 与其他部分的关系

- 场景对象在 `In-Scene Objects/厂外公路与连接路` 下：`双向四车道柏油公路` 挂 `Assets/Scripts/Environment/FactoryHighway.cs`，按场景里保存的纵断面在编辑器和运行时生成路面网格；`厂门连接路向内`、`厂门连接路向外` 是两条贴地样条路，沿用厂内 `FactoryDirtRoadSpline` 的材质和不规则边缘混合。
- 柏油材质 `Assets/Art Resources/Environment/FactoryRoads/Highway/CleanAsphalt.mat`，贴图是 Poly Haven 的 Clean Asphalt（Dimitrios Savva，CC0），2K Diffuse、OpenGL Normal 和 Roughness，粗糙度反相后写进 HDRP Mask 的光滑度通道；标线材质 `WhiteRoadMarking.mat`、`YellowRoadMarking.mat` 在同目录。
- 道路碰撞由局部整平后的 Terrain 提供，不是路面网格。
- 上级目录的 `align-gate-approach.cs` 调线形和门洞，`grade-widened-approach.cs` 修局部路基，都是对当前场景的一次性编辑记录。

## 注意

- 公路长 930 米、路面宽 15.5 米、中心路拱 0.155 米，两端各 65 米渐入砂地；连接路约 350 米，两条贴地网格各 8.5 米宽、中心间距约 6.224 米，重叠后路面约 14.7 米；门洞约 15.3 米，门卫室侧门柱不动，另一侧接墙已裁短。
- 柏油贴图按原素材约 2.1 米尺度平铺。
- 改了地形要同步 Main、Draft、高度源和边缘缓存并刷新热浪；不要重跑旧的 `setup.cs` 覆盖现有布局。
- 实际通行和远景效果待 Play Mode 确认。
