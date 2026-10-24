# ArtSource/Cableway/SheaveAlignment/

## 用途

标准塔和进站支架顶部轮组方向修正的脚本和检查记录。修正后轮轴横跨绳索，两个轮心沿绳索方向前后排列。

## 内容

- `inspect_blender.py`、`fix_blender.py`：测量轮盘主轴，并把每侧轮组绕竖直轴转 90 度。
- `verify_unity.cs`：Unity CLI 一次性脚本，重新导入后核对模型和场景塔架引用。
- `before.json`、`after.json`：修正前后轮盘几何的主轴测量。`fix_report.json`：修改范围和 UV 检查。`unity_verification.json`：重新导入后的验证。`after.png`：修正后截图。
- `Before/`：修正前的两份 Blender 源文件、导出 FBX、Unity 用的 FBX 和 `build_tower.py`。

## 与其他部分的关系

- 修正结果已写进 `../Tajia02_Metric15/` 的两份源文件、导出 FBX 和 `Scripts/build_tower.py`，以及 `Assets/Art Resources/Environment/Cableway/CablewayTower_Metric15/CablewayTower_Metric15.fbx` 和 `CablewayEntryTower.fbx`。
- 模型 GUID 没变，场景塔架自动用修正后的网格，不用旋转塔架本身。

## 注意

- 判定标准是轮轴与绳索方向的点积绝对值接近 0，重做轮组时照此检查。
- 只改了轮组在水平面内的方向，没动塔架变换、绳索线路和轮组俯仰。
