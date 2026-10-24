# ArtSource/Cableway/Movement/

## 用途

索道吊斗沿缆线循环运动的装配脚本和检查记录。运行代码是 `Assets/Scripts/Environment/CablewayMovementManager.cs` 和 `Editor/CablewayMovementManagerEditor.cs`。

## 内容

- `setup.cs`：一次性装配脚本，创建管理对象、提取路线并绑定吊斗。`setup.json`：装配结果。
- `verify_logic.cs`、`verify_preview.cs`、`verify_route.cs`、`start_live_preview.cs`：组件逻辑、Inspector 预览、路线连续性和真实编辑器预览的检查脚本，对应 `logic_verification.json`、`preview_verification.json`、`route_verification.json`、`live_preview_start.json`、`live_preview_verification.json`、`final_verification.json`。`live_preview.png` 是预览截图。
- `Before/`：装配前的磁盘版和现场版场景，以及 12 个吊斗的初始变换。
- 目录内 C# 文件都给 Unity CLI 的 `eval_file` 用，不参与游戏编译。

## 与其他部分的关系

- 管理对象是主场景 `In-Scene Objects/索道群/索道移动管理`，绑定全部 12 个吊斗，吊斗父级不变。吊斗保持竖直，随线路改变水平朝向，在两站回转后继续循环。
- 路线从 `Assets/TerrainData/SheepRangeDEM/Cable_0.asset` 和 `Cable_1.asset` 每 8 个顶点的截面中心提取，两条反向拼接后去重，序列化在管理组件里；运行时按累计世界距离采样，跨距和坡度变化时速度不变。
- Inspector 可设自动启动和速度，Play Mode 有启停按钮；编辑模式有 Scene 预览按钮和进度滑块，停止预览、关闭 Inspector、保存场景、重新编译或进入 Play Mode 时恢复原位。
- 关卡触发时关掉自动启动，在交互对象的 UnityEvent 里挂 `CablewayMovementManager.StartMovement()` 和 `StopMovement()`，例如 `ItemUseTrigger` 的 onUse。

## 注意

- 改了绳索形状要重新生成路线点。`setup.cs` 会拒绝在已有管理器时重复创建，不能直接用来重建路线。
- 预览会让场景出现未保存标记，保存时先恢复原位，不会把预览位置存成摆放位置。
- 具体由哪个关卡操作启动还没定，现在没有绑定任何交互物。
- 没有滑轮旋转、装卸煤、站内脱索上轨和玩家乘坐。Play Mode 和站内通行效果待确认。
