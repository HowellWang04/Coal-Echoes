# 外围山体已应用

2026 年 10 月 9 日，用户确认开始实施方案 01，并单独确认将主场景原有未保存改动与本次修改一起保存。主场景已保存，修改前的磁盘版本和包含原有未保存改动的完整场景副本均在 `Backup/`。

## 实际结果

[落地定位图](terrain_proposal.png)由修改后的 Unity Terrain 回读生成，标记厂区、厂门、上下站及十座塔架。[前后对比](terrain_comparison.png)使用相同光照和范围。另有[厂区视角](factory_clean.png)、[上站附近视角](summit_factory_view.png)和[整体视角](applied_overview.png)的 Scene View 截图。

东侧长山脊、南侧错位山口、北侧背景山体已写入现有地形。外围使用原始 1 米 DEM 细节，生成 6144 方形高度与遮罩矩阵。相比初版预览，地图四边增加了渐隐衔接，北侧个别超过现有高度范围的峰顶进行了平滑压低。白线已改标为预留出山通道，仍是空间意向，不是已建成道路。

在现有 MapMagic 图末端增加两个 Import 和一个 Mask，接到 Height Output 及供坡度、凹凸材质使用的 HEIGHT 入口。原有生成链、中心高度来源及锁区配置保留。两个新增 MatrixAsset 位于 `Assets/TerrainData/SheepRangeDEM/`，名称以 `SheepRange_Peripheral_` 开头，命中已有 LFS 规则。矩阵以浮点数据持久化，Source 为 New，原始重建数据位于本目录两个 `import_*.f32` 文件中。

没有运行全图 Generate。现有 58 份地形数据中，42 份涉及外围修改，16 份没有写入。受影响的 Main 与 Draft 均局部写入高度，并使用当前 MapMagic 的 Slope、Cavity 和分层混合算法更新遮罩内的贴图权重，同步边缘缓存。工厂热浪采样已刷新，沙尘和热浪原有关闭状态已恢复。

## 验证

`verification.json` 记录中心保留区 7669426 个原生高度采样值完全不变，最大高差为零。批次报告记录未修改高度样本及贴图权重均未漂移。`scene_verification.json` 记录 21193 个 Transform 的位置、旋转和缩放均未变化，主场景保存成功且不再处于未保存状态。

Main 相邻块最大高差约 0.03053 米。原本存在的 Draft 接缝最大高差约 28.84 米，Main 与 Draft 对应采样最大差约 13.89 米，本次均未扩大；未将这些旧差异宣称为已修复。`import_tests.json` 使用项目实际的 129 与 1025 分辨率检查新增 Import，中心样本块遮罩为零，外围结果有效且处于合法范围。

Scene View 已检查厂区、上站和整体形态。尚待用户在 Play Mode 确认行走及索道移动时的远景轮廓、山口遮挡和 LOD 观感；静态截图不能替代运行验收。拍摄时临时隐藏的编辑器 UI 已恢复。

## 恢复资料

`Backup/` 保存全部原 TerrainData 与 meta、原 MapMagic 图、主场景磁盘版与修改前完整现场副本，以及热浪高度纹理和材质。`Before/` 和 `After/` 保存原生高度数组。恢复时应关闭或卸载受影响场景再还原对应资产，不能在正在生成时覆盖文件。两个新增矩阵若需要移除，应先恢复原图引用。
