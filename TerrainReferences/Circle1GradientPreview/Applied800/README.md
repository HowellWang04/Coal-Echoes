# 圈 1 的 800 米方案应用记录

2026 年 10 月 9 日，按用户选择将候选 B 局部应用到主场景。圈 3 留待后续处理。

随后用户认可圈 1，授权扩展到全图外围过渡，已完成的后续工作见[全图记录](../../WholeMapGradient800/README.md)。下文保留圈 1 单独应用时的记录。

## 查看

[应用前](scene_before.png)与[应用后](scene_after.png)使用相同 Scene View 视角。[细节视角](scene_after_detail.png)隐藏编辑器网格与雾显示，未改游戏的环境设置，拍摄后已恢复编辑器视角。

## 改动

读取候选 B 相对当前快照的浮点高度差，按世界坐标双线性采样并叠加到原生地形，保留原来的细节。更新 2 份 Main 和 6 份 Draft，坡度和凹凸贴图只写高度变化及其邻域，保护区与邻域外材质使用原始值。

现有 MapMagic Import 在放大采样时会截去负修正，且 Main 与 Draft 的导入路径不同。实际接入使用 `Assets/Scripts/Terrain/TerrainHeightDelta.cs`，支持有正有负的米制双线性采样。它位于外围 Mask 后面，同时接到 Height Output 与 HEIGHT 入口，未来生成会读取保存的同一修正矩阵。没有运行全图重新生成。

矩阵保存在 `Assets/TerrainData/SheepRangeDEM/SheepRange_Circle1_800m_delta.asset`，采用 New 来源，不调用 RAW 重载。矩阵覆盖 2048 米见方，1025 × 1025，数值为归一化高度差，边界及固定区为零。现有 LFS 路径规则已涵盖它。

Main 与 Draft 高度边缘缓存、局部材质边缘缓存同步更新，热浪高度缓存刷新后保存主场景。自动贴图对齐设置仍然开启，Factory Sandstorm 与 Factory Heat Haze 保留原先的关闭状态。

## 验证

58 份地形中仅 8 份高度改变，其余 50 份逐位相同。中心保护区高度变化为零，修正量为零的位置高度变化为零，贴图遮罩以外逐位相同。全部 21193 个 Transform 和 Lock 参数保持不变。

自定义节点与独立 Python 双线性参考的最大采样差约 0.000015 米。图副本在 Main、Draft 和保护区域上完成局部数值生成核对，未向真实地形提交生成结果。图序列化副本保留节点、引用和输入输出连接。Unity 写入 TerrainData 后的最大高度量化误差约 0.0153 米，符合现有高度存储精度。

受影响 Main 同精度接缝仍为零。原有 Draft 接缝尚在，其中一处原先约 28.84 米，本次仍相同。某段旧 Draft 接缝的局部差值因量化最多变化约 0.0305 米，其最大缝差没有增加。此轮不是全图接缝修复，不能据此宣称所有 LOD 都通过了视觉验收。

编辑器 Console 未发现错误。圈 1 的山脚形状、远近材质及切换 LOD 的实际体验仍需用户在 Play Mode 查看，圈 3 尚未处理。

## 备份与证据

`Backup/Main Game Scene.live.unity` 是修改前含未保存改动的完整主场景副本，`Main Game Scene.disk.unity` 是当时磁盘版本。Backup 同时保留涉及的 TerrainData、旧 MapMagic 图和热浪资源。Before 与 After 存放实时浮点回读，Prepared 为实际写入所用数据。

`prepared.json` 记录更新范围，`verification.json` 记录高度、贴图和接缝核对，`graph-dry-run.json` 记录图副本试算，`persistence.json` 记录保存及图序列化检查。`application.json` 中 painted 为临时矩形刷新数量，最终范围外像素已恢复，最终实际变化以 verification 为准。

执行用脚本位于上级目录。`prepare_import.cs` 调用最终的 TerrainHeightDelta 节点输出独立采样，`prepare_application.py` 制作局部更新与贴图遮罩，`dry_graph.cs` 试算图副本，`apply800.cs` 执行单次写入，`readback800.cs` 和 `verify_application.py` 核对结果。不要再次运行单次写入脚本叠加同一修正。
