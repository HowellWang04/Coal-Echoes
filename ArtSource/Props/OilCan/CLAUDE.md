# ArtSource/Props/OilCan/

## 用途

第三关索道下站驱动电机旁的 Oil Can，玩家按 F 拾取进背包，再拿去给电机上油，流程见 `Docs/Specs/Levels/level-three-spec.md` 的中段。外观是八十年代的手压式机油壶：冲压钢壶身刷褪色红漆，磕瘪了三处，漆面剥落处露出镀锡铁皮和锈，壶口往下流着发黑的油痕；螺口、手压泵、拇指压杆和细长油嘴都是发暗的黄铜。

## 内容

- `Scripts/build_oilcan.py`：在后台 Blender 进程里生成材质、模型、预览和图标，导出 FBX 和 GLB。`build.log`、`verification.json` 是运行输出。
- `Blender/OilCan.blend`：源文件，贴图已打包。
- `Exports/`：FBX 和 GLB。
- `Textures/`：Paint、Brass 两套，各有 BaseColor、Normal、Roughness、Metallic 和 HDRP_Mask。
- `Previews/`：整体和凹痕一侧的渲染；`OilCan_Icon.png` 是 512 × 512 透明底图标，复制到 `Assets/Art Resources/Props/OilCan/Icons/` 作背包缩略图。
- `UnityIntegration/ImportOilCan.cs`：通过 Unity MCP 执行的导入脚本，建材质、预制体和背包物品资源，不碰场景。

## 与其他部分的关系

- 预制体 `Assets/Art Resources/Props/OilCan/OilCan.prefab`，HDRP/Lit 独立材质，根对象下的 `Model` 以壶底中心为轴心，并绕壶底整体放大过：按真实手压油壶的尺寸摆在两米长的电机旁太小，倍数在 `ImportOilCan.cs` 里，背包物品的查看距离按同一倍数放远，查看时的画面大小不变。
- 背包物品资源 `Assets/Art Resources/Props/OilCan/OilCan_BackpackItem.asset`，编号 `oil_can`，说明文字和 `Docs/game-text.md` 的背包一节一致，引用上面的预制体和图标。
- 场景实例 `In-Scene Objects/索道群/站房/CablewayFoothillStation/Oil Can`，在 LineB 站棚里驱动电机旁的木托盘 `SM_Pallet38` 上；实例上加了 BoxCollider 和 `InspectableItem`（`backpackItem` 填上面的资源），T3.2 出现前由 `Level3HintFlowManager` 关着。驱动电机见 `../../Cableway/DriveMotor/`。
- 素材包里只有现代塑料桶 `SM_PlasticCanister` 和 `SM_PlasticGasTank`，年代和大小都对不上，所以这件自己做。

## 注意

- Blender 里 Z 朝上、油嘴朝 +X；导出勾了 Apply Transform，Unity 里油嘴朝本地 −X。
- 轴心取壶底中心，不取包围盒中心，油嘴会把包围盒拉偏。
- Blender 里的尺寸是真实大小，放大只在 Unity 预制体里做；改倍数要同时改场景实例上 BoxCollider 的大小和中心、`InspectableItem` 的查看距离。
- 壶身贴图按圆周展开，U 绕壶一圈，V 是高度除以周长；黄铜件按 5 厘米一块盒式投影。
- 图标用 Blender 默认的 AgX 色彩渲染，红漆在图标里比 Unity 里偏粉。
- 新导入的 PNG 默认是 Multiple 精灵，图标要改成 Single，否则这个路径下读不到 Sprite。
