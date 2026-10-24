# 地形高度与材质同步

实现位于 `Assets/Scripts/Terrain/Editor/MapMagicTerrainTextureSync.cs`，是编辑器工具，无需给场景对象增加组件。

## 自动与手动使用

自动开关默认开启，位于 `Tools > Coal Echoes > Auto Align Terrain Textures`。捕获 Unity 高度变化后合并改动区域，等待至少半秒、鼠标操作结束、MapMagic 生成和地形协程结束，再逐块处理。延迟 GPU 高度会先同步到 CPU，材质按当前实际高度计算，沿用现有 Slope、Cavity 和三层 Textures 配置，不改高度或触发 MapMagic Generate。

同时处理相同世界区域的 Main、Draft 与受影响邻块，各自匹配自己的现有高度。更新实际 Alphamap 和材质边缘缓存，修改区域之外的 Alpha 不写入。每通道相差约一个 8 位量化单位以内视为已对齐，不重复写入。矩阵计算使用完整块和邻块边缘采样，实际贴图只写局部区域。

工具会标记地形资源和缓存所属场景待保存，不自动保存其他工作。代码编辑器可调用 `QueueRegion(Terrain, RectInt)` 请求更新高度像素范围，或调用 `RefreshTerrain(Terrain, Rect, bool)` 立即对齐指定世界 XZ 矩形。后者只对传入 Terrain 生效；自动队列会包含对应 LOD 与邻块。

选中具体 Terrain 对象后，`Tools > Coal Echoes > Align Selected Terrain Textures` 对齐该地形覆盖范围内的 Main、Draft 材质。它比单点局部修复范围大，使用前注意选中的地形。自动开关关闭时仍可以手动执行。

此工具只管理现有三层自然地表。前三层之后的额外层（如[厂区地面](../FactoryGround/README.md)）保留原权重，自然层按剩余权重刷新。人工绘制与坡度规则不同的地面时关闭自动开关。Play Mode 不运行此自动对齐。历史印迹不会仅因安装脚本就全图重刷，11 个旧塔脚目前仍在待办。

## 撤销与生命周期

材质属于高度编辑的派生结果，自动更新不添加第二个撤销步骤。记录本次脚本加载期间的高度编辑 Undo 组与影响范围，撤销和重做后重新对齐这些范围。脚本重载前的旧撤销记录不在这份内存记录中，需要时使用手动菜单。手动对齐自身支持 Undo。

## 验证

`verify_editor.cs` 是通过 Unity MCP 执行的 Editor 方法体，不导入项目程序集。依次执行四次，让各次调用之间经过编辑器更新，即完成建立临时场景、自动对齐验证、撤销验证、重做验证和清理。若返回 Waiting，等待编辑器更新后再调用。测试使用独立 Preview Scene、禁用生成器的临时 MapMagic 对象、Main、Draft 和相邻地形，没有改写正式地图。

最终 `regression_stage_1.json` 至 `regression_stage_3.json` 均通过，覆盖实际高度事件、SetHeightsDelayLOD、跨块和 Draft 更新、范围外材质保留、全部高度样本不被材质工具改变、撤销、重做和重复执行零写入。初版的撤销遗漏在加入 Undo 组跟踪后已修复，早期 `undo_test.json` 保留失败发现记录，以最终三阶段回归结果为准。

Unity 编辑器编译通过，Console 没有错误。正式场景中大范围连续编辑的交互耗时及用户实际刷地形观感尚未验收。

结束时核对 58 份地形高度均与既有快照一致，圈 2 已修八份材质与修复后的快照一致。主场景在编辑器中显示未保存标记，本轮未清除此标记、未保存主场景文件，以保留用户当前编辑状态。

## API 依据

Unity 的 [TerrainCallbacks](https://docs.unity3d.com/cn/current/ScriptReference/TerrainCallbacks.html) 提供高度变化事件。[UndoRedoInfo](https://docs.unity3d.com/cn/current/ScriptReference/UndoRedoInfo.html) 提供撤销组标识，用于仅重新检查对应的高度编辑区域。
