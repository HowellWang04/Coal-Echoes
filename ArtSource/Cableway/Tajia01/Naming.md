# Cableway naming

2026年9月27日

Unity 场景与资源使用英文命名。场景实例编号遵循已保存的索道路线顺序，模型资源编号表示旧模型类型，两者含义不同。

## Scene objects

| Previous name | Current name |
| --- | --- |
| `tajia03` | `CablewayTower_01` |
| `tajia03 (10)` | `CablewayTower_02` |
| `tajia03 (7)` | `CablewayTower_03` |
| `tajia03 (11)` | `CablewayTower_04` |
| `tajia03 (4)` | `CablewayTower_05` |
| `tajia03 (5)` | `CablewayTower_06` |
| `tajia03 (6)` | `CablewayTower_07` |
| `tajia03 (3)` | `CablewayTower_08` |
| `tajia01` | `CablewayTower_09` |
| `tajia03 (1)` | `CablewayTower_10` |
| `tajia03 (8)` | `CablewayTower_11` |

对应地基标签使用 `CablewayTowerFooting_01` 至 `CablewayTowerFooting_11`。11座塔架均使用最新重建模型，原先完成的实例为 `CablewayTower_09`，子对象为 `CablewayTower` 与 `CoalBucket`。

## Unity assets

资源位于 `Assets/Art Resources/Environment/Cableway`。

| Previous relative path | Current relative path |
| --- | --- |
| `tajia01.fbx` | `CablewayTower_Legacy_01.fbx` |
| `tajia02.fbx` | `CablewayTower_Legacy_02.fbx` |
| `tajia03.fbx` | `CablewayTower_Legacy_03.fbx` |
| `Tajia01_Rebuilt/Tajia01_Tower_v01.fbx` | `CablewayTower_Rebuilt/CablewayTower_v01.fbx` |
| `Tajia01_Rebuilt/Tajia01_CoalBucket_v01.fbx` | `CablewayTower_Rebuilt/CoalBucket_v01.fbx` |
| `Tajia01_Rebuilt/Tajia01_Rebuilt.prefab` | `CablewayTower_Rebuilt/CablewayTower_Rebuilt.prefab` |
| `Tajia01_Rebuilt/Materials/Tajia01_Tower.mat` | `CablewayTower_Rebuilt/Materials/CablewayTower.mat` |
| `Tajia01_Rebuilt/Materials/Tajia01_CoalBucket.mat` | `CablewayTower_Rebuilt/Materials/CoalBucket.mat` |

材质显示名称为 `CablewayTower` 与 `CoalBucket`。资源及其 `.meta` 配对改名，GUID 和导入设置保持不变。FBX 内部对象、网格及材质标识保留原值，避免改变导入子资源和材质映射。

## Authoring archive

`ArtSource/Cableway/Tajia01` 内的 Blender 源文件、导出副本、制作脚本及验证记录保留制作时名称。`TerrainReferences` 内的旧场景采样和一次性操作脚本也保留历史名称。它们属于制作记录，不代表当前 Unity 名称。再次使用历史脚本操作当前场景或导入资源前，须按上表调整对象查询和目标资源路径。

## Verification

静态检查已通过。11 个场景实例及 11 个地基标签仅修改名称，所有其他场景字节保持一致。5 个 FBX 文件及全部改名资源的 `.meta` 字节保持一致。预制体和材质仅修改显示名称。Unity Console、重新导入后的材质和场景显示、Play Mode 尚未验证。


## Legacy model removal

三份 CablewayTower_Legacy 模型及其.meta已删除，表格中旧模型资源路径仅用于历史对照。Previous目录中的8份旧版模型已删除。所有场景塔架已使用最新重建组合，最新制作源文件保留。详见[替换检查](UnityIntegration/all_towers_verification.json)及[删除清单](UnityIntegration/archived_models_removed.json)。
