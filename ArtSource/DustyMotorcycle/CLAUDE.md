# ArtSource/DustyMotorcycle/

## 用途

第一关开场停在玩家出生点旁的原创复古红色摩托车：旧红车身、重积尘、锈斑、掉漆和油泥，后货架上一个军绿色小储物箱。玩家在这里按 F 领取外公日记、Lock Pick 和 Group Photo。几何和材质全部程序生成，没有用第三方模型、扫描或图片贴图。

## 内容

- `build_motorcycle.py`：在 Blender 5.2 里生成模型并烘焙 4096 贴图，`build.log` 是运行输出，`Motorcycle_Packed.bake.png` 是烘焙图集。
- `Dusty_Red_Motorcycle.blend`：可编辑模型和摄影棚预览，摄影棚不导出。`Dusty_Red_Motorcycle.glb`：交换文件。
- `ImportMotorcycle.cs`、`PlaceMotorcycle.cs`：Unity CLI eval 的一次性编辑器代码，前者设 HDRP 材质和预制体并保留日记拾取配置，后者把实例放到玩家左前方。
- `asset_report.json`、`unity_validation.json`：模型统计和场景里的坐标与检查。
- `Preview/`：摄影棚三视图、侧视图，以及编辑模式的主相机和临时近景渲染。
- `Before/MainGameScene.live.unity`：摆放前含未保存修改的场景副本，不用于替换当前场景。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/Gate Area/DustyMotorcycle/`：FBX、`Dusty_Red_Motorcycle.prefab`、`Motorcycle_Dusty_HDRP.mat` 和 `Textures/`。场景实例是 `In-Scene Objects/摩托车`，停在玩家前方约 2.4 米、左侧约 1.2 米，按两轮接地点调了纵向倾斜。
- 预制体根对象上的 `GrandpaDiaryPickup` 和 `DiaryPromptAnchor` 接现有交互管理器：靠近并对准后用现成 F 图标提示，按 F 后日记页签和回响系统开启，Lock Pick 进背包，Group Photo 进相册，摩托车留在原位。`lockPick`、`groupPhoto`、`photoAlbum` 三个字段在场景实例上填，预制体里是空的。持有状态存在 `GrandpaDiary/Acquisition.json`，旧存档默认已持有；`Tools > Coal Echoes > Diary > Preview Pickup Next Play` 只在下一次 Play 临时设为未持有。
- 主菜单和报纸照片转场期间整辆车被 `MainMenuUIRoot` 上的 `OpeningSceneState` 隐藏，开场镜头对着天空时才出现，见 `../MainMenu/Opening/`。
- 储物箱是第一关拿到撬锁工具的地方（见 `../Props/LockPick/`），现在只作外观陈设。门口枯草的摆放避开它，见 `../FactoryGateGrass/`。

## 注意

- 模型长约 2.34 米、宽约 0.79 米、高约 1.41 米，一个网格一个材质，约 18 万三角面，是近景预览版本，没有 LOD。
- 预制体本地 +X 指向车头、+Y 朝上；FBX 子对象保留导入变换并绕 Y 转 180 度，与根对象上五个简化 Box Collider 对齐。没有 Rigidbody 和驾驶逻辑。
- 颜色图 sRGB 且已烘入近距离环境遮蔽；HDRP Mask 的 R 金属度、G 为 1、B 为 0、A 光滑度；ORM 只给 GLB 用。
- 碰撞通行和观感待 Play Mode 确认。
