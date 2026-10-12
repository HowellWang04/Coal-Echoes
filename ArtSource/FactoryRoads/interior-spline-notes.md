# 厂内 Spline 土路

主场景对象 `Factory Interior Dirt Road Spline` 使用 Unity Splines 2.9.1，共 8 个控制点，门内路长约 104.4 米，含透明碎石边缘宽 8 米。

选中该对象的 Spline Container 编辑控制点，`FactoryDirtRoadSpline` 会重新生成沿现有 Terrain 的网格贴花。Width 控制路宽，Texture Length 为每次纵向纹理重复的米数。对象保持当前单位缩放，地形改动后使用组件菜单 Rebuild Road 重新贴地。

网格为临时生成数据，启用组件或加载场景后重建，不生成二进制 Mesh 资源。材质使用 HDRP 17.4 的网格贴花渲染路径，并读取顶点透明度处理最后 7 米的渐淡收尾。升级 HDRP 时需复查自定义 DirtRoadSpline Shader。

门外原贴花及门槛间隔保留，原门内 Dirt Road 5 到 8 停用，便于对照。未改变地形、建筑位置或碰撞。验证记录与三个编辑器渲染图在同目录，Play Mode 行走观感仍待确认。
