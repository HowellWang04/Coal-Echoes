# 圈 2 旧厂区矩形材质残留

## 结论

上一轮用外围新坡面的单点采样解释用户所说的矩形黑印，定位不准确。此次沿实际平缓地面印迹重新检查，发现旧厂区平台的沙土矩形和外围深色岩石环仍留在 Alphamap 中。历史平台中心为世界 X -2000、Z -1650，尺寸 545 米乘 380 米，旋转 60 度，外围过渡 90 米，与材质图的矩形位置和角度一致。历史开发日志记载平台高度已在 9 月 28 日恢复为真实坡面。

修改前 `maps.png` 中的实际高度与坡度没有对应矩形，深色岩石权重图却有明显矩形边框。约 7 度的地面仍带有超过 80% 的深色岩石权重。关闭 Scene View 场景灯光仍可见材质印迹。

矩形原范围内的当前高度和材质与本次外围山体改造前的备份逐值一致，见 `old_stamp_check.json`。因此不能将其归因于这次外围拼接或 Lock 重新激活。备份与当前场景的 33 个 Lock 参数一致，仅 Ridge factory 启用，relativeHeight 为 false，运行时 Lock 缓存数量为零。

## 修复

按现有 TerrainData 高度和现有 MapMagic Slope、Cavity、Textures 参数重算局部材质。旧平台及外围 95 米范围完整更新，之后 25 米柔和过渡回原材质，范围外逐值保留。四块受影响 Terrain 同时处理 Main 与 Draft，共八份现有 TerrainData，只更新 Alphamap 与 splat 边缘缓存，未重生成 MapMagic。

所有地形高度哈希一致，21193 个物体 Transform 不变，Lock 参数不变。修复区之外的材质通道变更数为零。修复前原材质与当前地形生成结果的最大误差为 1，修复后为 0.001961，处于 8 位 Alphamap 量化误差内。

`check_material_repair.py original` 返回失败，`check_material_repair.py actual` 返回通过，原始与实际读回数组均保留。此检查证明数据一致性；同一视角的 `factory_rectangle_before.png` 和 `factory_rectangle_after.png` 用于视觉复核，矩形边框已消失。Play Mode 由用户验收。

已刷新 Factory Heat Haze，恢复沙尘和热浪原启用状态，并保存主场景。未触碰圈 1、圈 3 的高度问题。

## 后续复发检查

当前 SheepRange_Height 的内存 Matrix 与从磁盘 RAW 重新读出的 37748736 个样本完全一致。旧厂区和塔脚所在区域另取 111166 个源像素对照原始 USGS DEM，最大差约 0.0153 米，处于 RAW 量化误差内，未发现旧平整高度仍保留在生成源。当前材质链由最终高度驱动 Slope 和 Cavity，已修八份 Main、Draft 的 splat 边缘缓存与实际 Alphamap 完全一致。

另以独立 TileData 对厂区两块地形分别做 Main 1025 与 Draft 129 的内存生成试算，没有 Finalize、Apply 或场景写入。两个原黑印采样点在 Main 试算中的坡度岩石权重约为 0.042 和 0.052，Draft 约为 0.013 和 0.028，没有再生成原来超过 0.8 的旧平台边框。此证据覆盖生成链与当前缓存，不等同于重开 Unity 或完整 Play Mode 生命周期验收。详见 `generation_dry_run.json`、`recurrence_checks.json`、`generation_source_native_check.json`。

同批 11 个旧塔脚仍留有独立的纯沙土圆形印迹。现场读数显示 11 个中心的沙土权重均为 1，其中塔脚 2 当前坡度约 40.5 度，塔脚 8 约 39.6 度。这些是尚未清理的同类材质残留，不是本次矩形修复后重新出现。此次仅排查，没有修改塔脚材质或高度。

## 文件

`Backup` 保存本轮修改前八份地形资产及 meta、主场景和热浪资源。`prepare_candidate.cs` 与 `apply_material_repair.cs` 是经 Unity MCP 执行的临时 Editor 方法体，没有安装到 Assets 或增加运行时脚本。原始、候选和实际 Alpha 数组及验证 JSON 均在此目录，供复查和回退。
