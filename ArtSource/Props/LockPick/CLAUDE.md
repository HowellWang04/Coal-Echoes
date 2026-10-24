# ArtSource/Props/LockPick/

## 用途

第一关从摩托车储物箱拿到的撬锁工具，用来撬开门卫室门上的旧式插芯锁，流程见 `Docs/design.md` 第一关。一把单钩撬片和一把 L 形张力扳手，钩片手柄缠旧布胶带，两件用一根发硬的橡皮筋捆在一起，作为一个背包物品拾取。

## 内容

- `Blender/LockPick.blend`：源文件，贴图已打包。
- `Exports/`：FBX 和带内嵌贴图的 GLB。
- `Textures/`：Steel（BaseColor、Roughness、Metallic、Normal、HDRP_Mask）、Tape（BaseColor、Roughness、Normal、HDRP_Mask）、Rubber（BaseColor）。
- `Previews/`：整体、俯视、钩尖局部和 GLB 回导的渲染；`LockPick_Icon.png` 是 512 × 512 透明底图标，复制到 `Assets/Art Resources/Props/LockPick/Icons/` 作背包缩略图。
- `Scripts/build_lockpick.py`、`verify_lockpick.py`：在后台 Blender 进程里生成材质、模型、预览和图标，以及重新导入导出文件核对。`build.log`、`verify.log`、`verification.json` 是运行输出。
- `UnityIntegration/ImportLockPick.cs`：通过 Unity MCP 执行的导入脚本。

## 与其他部分的关系

- 预制体 `Assets/Art Resources/Props/LockPick/LockPick.prefab`，HDRP/Lit 独立材质，根对象下的 `Model` 已平移到底面中心为轴心，平放时局部 +Y 朝上。场景里不摆实物，玩家在摩托车按 F 时直接进背包。
- 背包物品资源 `Assets/Art Resources/Props/LockPick/LockPick_BackpackItem.asset`，编号 `lock_pick`，引用上面的预制体和图标。
- 摩托车见 `../../DustyMotorcycle/`，`GrandpaDiaryPickup` 的 `lockPick` 字段引用上面的背包物品资源；门卫室门锁的 `ItemUsePoint` 也引用它。

## 注意

- 整套长约 0.1148 米，是真实尺寸的小件，近景才看得清。Blender 里 Z 朝上、长度沿 X、钩尖朝 +X 并向 +Y 弯，原点在底面中心；导出文件 Y 朝上。
- HDRP_Mask 的 AO 和细节遮罩通道是全白，Normal 用 OpenGL 约定。
- 没有碰撞体。
