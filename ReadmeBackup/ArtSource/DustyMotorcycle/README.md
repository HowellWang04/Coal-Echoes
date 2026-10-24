# 原创复古红色摩托车

用于第一关开场预览，按用户要求制作旧红色车身、重积尘、锈斑、掉漆及油泥，并在后货架上增加军绿色小储物箱。箱体包含箱盖、密封缝、搭扣、提手和固定绑带。本资源由程序生成原创几何与材质，没有使用第三方模型、扫描网格或图片贴图。

## 文件

`build_motorcycle.py` 在 Blender 5.2 中生成模型并烘焙 4096 像素纹理。`Dusty_Red_Motorcycle.blend` 保留可编辑模型及摄影棚预览，`Dusty_Red_Motorcycle.glb` 是交换文件。FBX 和运行贴图输出到 `Assets/Art Resources/Environment/Gate Area/DustyMotorcycle/`，摄影棚地面、相机和灯光不包含在导出模型内。

`ImportMotorcycle.cs` 与 `PlaceMotorcycle.cs` 是给 Unity CLI eval 执行的一次性编辑器代码，不属于运行时脚本。前者设置 HDRP 材质与 Prefab，后者把实例放在玩家左前方并保留玩家位置。两者均通过现有 Unity Editor 执行。

## 材质与尺度

颜色图使用 sRGB，法线图使用切线空间。HDRP Mask 的红色通道为金属度，绿色为 1，蓝色为 0，透明通道为光滑度。近距离环境遮蔽已烘焙在颜色图内。ORM 供 GLB 使用，Unity 材质不使用 ORM。

模型长约 2.34 米、宽约 0.79 米、高约 1.41 米。Blender 导出为 181220 三角面，Unity 导入优化后为 180384 三角面，使用一个网格和一个材质。当前为近景预览版本，尚无独立 LOD。

Prefab 的本地正 X 指向车头，正 Y 朝上。FBX 子对象保留导入变换并绕 Y 转 180 度，与根对象上的五个简化 Box Collider 对齐。模型没有 Rigidbody 或驾驶逻辑。

## 场景与检查

实例位于主场景 `场景内Object/Dusty Motorcycle Preview`。停车点在玩家前方约 2.4 米、左侧约 1.2 米，按两轮接地点调整纵向倾斜，没有修改玩家或地形。当前坐标和配置见 `unity_validation.json`，原始统计见 `asset_report.json`。

已检查 HDRP 材质及贴图引用、实际尺度、简化碰撞、与周边非地形物件无碰撞穿插，并保存主场景。`Preview/unity_player.png` 是编辑模式下主相机渲染，`Preview/unity_closeup.png` 是同场景的临时近景相机渲染，临时相机已移除。`Preview/motorcycle_three_quarter.png` 和 `Preview/motorcycle_side.png` 为中性摄影棚预览。

未进入 Play Mode。碰撞通行和最终观感由用户试玩确认，F 获取日记已接通，储物箱当前只作外观陈设。Prefab 根对象的 `GrandpaDiaryPickup` 和 `DiaryPromptAnchor` 接入现有交互管理器，导入脚本也保留这项配置。

靠近并对准摩托车后使用现成 F 图标显示领取提示，按 F 后日记页签及回响系统开启，摩托车留在原位。正常游戏将持有状态保存到 `GrandpaDiary/Acquisition.json`，已有旧存档默认已持有。`Tools > Coal Echoes > Diary > Preview Pickup Next Play` 仅在下一次 Play 临时设为未持有，领取时不写入日记持有存档；它不是独立游戏存档，照片、回响和便签仍沿用原有保存位置。

修改场景前的备份在本地 `Before/MainGameScene.live.unity`，包含开始摆放前的未保存场景改动，不用于替换当前主场景。
