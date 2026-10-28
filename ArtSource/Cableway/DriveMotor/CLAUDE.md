# ArtSource/Cableway/DriveMotor/

## 用途

索道山脚站驱动缆绳的电机组，第三关玩家用 Oil Can 给它上油后索道开始运行，流程见 `Docs/Specs/Levels/level-three-spec.md` 的中段。外观是停了几十年的旧设备：带散热片的电机、联轴器鼓、减速箱装在一副钢底座上，灰绿色机器漆褪色、透出锈斑，钢件整片生锈。

## 内容

- `Scripts/build_drivemotor.py`：在后台 Blender 进程里生成材质、模型和预览，导出 FBX 和 GLB。`build.log`、`verification.json` 是运行输出。
- `Blender/DriveMotor.blend`：源文件，贴图已打包。
- `Exports/`：FBX 和 GLB。
- `Textures/`：Paint、Steel、Yellow、Rubber 四套，各有 BaseColor、Normal、Roughness、Metallic 和 HDRP_Mask。
- `Previews/`：整体、正面、加油口和联轴器局部的渲染；`DriveMotor_InShed.png` 是摆进站棚后的场景截图。
- `UnityIntegration/ImportDriveMotor.cs`：通过 Unity MCP 执行的导入脚本，建材质和预制体，不碰场景。

## 与其他部分的关系

- 预制体 `Assets/Art Resources/Environment/Cableway/CablewayDriveMotor/CablewayDriveMotor.prefab`，HDRP/Lit 独立材质。根对象下的 `Model` 以钢底座底面中心为轴心，子物体是 `Skid`、`Motor`、`Rotor`、`Gearbox`；根对象下的 `OilFillerAnchor` 在减速箱加油口上方，给上油用的 `ItemUsePoint` 当 promptAnchor。
- `Rotor` 是电机和减速箱之间带两道黄漆的联轴器鼓，原点就在轴线上，绕本地 X 轴转，索道运行时由第三关流程管理器转它。
- 预制体本地 +Z 是正面（铭牌、加油口、油位窗），−Z 是电缆所在的背墙一侧，−X 是减速箱一端。
- 场景实例 `In-Scene Objects/索道群/站房/CablewayFoothillStation/Drive Motor`，在上山一侧的 LineB 站棚里、滚轮架南边，靠着后墙，坐在走道格栅上；实例上加了一个包住全部网格的 BoxCollider。Oil Can 在它旁边的木托盘上。
- 电机前面的 Oil Can 见 `../../Props/OilCan/`。

## 注意

- Blender 里 Z 朝上、轴线沿 X；导出勾了 Apply Transform，Unity 里四个子物体都没有残留旋转。Blender +X 对应 Unity −X，Blender −Y 对应 Unity +Z。
- 贴图按材质的平铺尺寸做盒式投影，漆面 1 米一块，其余 0.5 米一块，贴图本身可以平铺。
- 场景实例上挂了上油用的 `ItemUsePoint`（saveId `drive_motor`，要 Oil Can），索道启动和联轴器鼓转动都由 `Gameplay Root/Managers/Level3HintFlowManager` 管，见第三关 Spec 的中段。
