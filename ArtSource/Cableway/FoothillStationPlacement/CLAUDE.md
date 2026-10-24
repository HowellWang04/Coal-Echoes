# ArtSource/Cableway/FoothillStationPlacement/

## 用途

山脚始发站的摆放记录，以及同一次操作里把厂区地形从旧的整平恢复到 DEM 原貌的记录。

## 内容

- `Scripts/`：`survey.cs`、`model_survey.cs` 测现场和模型，`search_station.py` 选站址，`import_models.cs` 导入站棚和滚轮架，`place_station.cs`、`refine_placement.cs` 摆放，`backup_terrain.cs`、`prepare_restoration.py`、`apply_restoration.cs`、`audit_restoration.cs` 做地形恢复，`final_verify.cs` 收尾检查。都是一次性脚本。
- `placement.json`：站体变换和贴地情况。`terrain_before.json`、`terrain_after.json`、`terrain_audit_cli.json`、`restoration_plan.json`、`restoration_applied.json`：地形恢复的计划和审计。`verification.json`：保存后的场景、地形哈希和渲染器检查。其余 JSON 是选址、导入和差异记录。
- `RestoredFactory_Height.raw`、`Tile_*_actual.f32`、`Tile_*_restored.f32`：恢复用的高度数据。
- `Previews/`：选址、摆放和厂区地形恢复前后的 Scene 视图截图。
- `Before/`：恢复前的场景、四块地形的 Main 与 Draft 资源、输入 RAW、Matrix 和 MapMagic 图。

## 与其他部分的关系

- 场景对象是 `In-Scene Objects/索道群/站房/CablewayFoothillStation`，子对象 `CablewayStationShell_LineA`、`_LineB`、`CablewayStationRollerFrame_LineA`、`_LineB`、`CablewayReturnTerminal`。站棚模型见 `../CablewayStationShell/`，滚轮架见 `../CablewayStationRollerFrame/`，回转站见 `../Tajia02_Metric15/`。
- 站棚开口长边朝向缆线，滚轮架与缆线平行、立在开口一侧外面，两者保持建模时的缩放。
- 地形恢复保留了塔架地基，塔中心 36 米内的采样没有改。

## 注意

- 这里的地形恢复针对的是 MapMagic 时期的地形，之后地形已改为原生 DEM 高度源，`Before/` 里的 MapMagic 图和 RAW 只作存档。
- 站棚一侧地面有坡，局部埋入地面，待整平；碰撞、缆线连接和吊斗通行没有在 Play Mode 里测过。
