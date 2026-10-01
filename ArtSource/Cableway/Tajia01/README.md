# Tajia01 独立塔架与运煤吊斗

2026年9月25日

用户已确认概念图并要求先制作塔架。独立塔架首版已完成，按确认图重建钢构、连接板、螺栓、平台格栅、爬梯和外侧竖杆，已制作灰黑钢材、局部旧漆和锈蚀材质。

塔架与运煤吊斗已分别重建为独立模型，并已替换主场景中的 `tajia01` 实例。原 `tajia01.fbx` 保留为旧资源，新组合通过独立预制体引用两个 FBX。本文以下为塔架说明，吊斗文件和预览见[独立运煤吊斗](CoalBucket/README.md)。

## Unity 场景替换

已通过 Unity 编辑器将 Main Game Scene 内索道群下的唯一 tajia01 实例替换并保存。根对象的父级、位置、旋转与缩放保持不变。根据旧模型实际网格高度和塔底低处顶点测量校准两个新子对象，塔架世界高度在替换前后均为31.289398米。Tower 和 CoalBucket 可独立选择。

新资源位于 `Assets/Art Resources/Environment/Cableway/Tajia01_Rebuilt`。两个独立HDRP/Lit材质使用4K颜色、法线和HDRP遮罩图，颜色启用sRGB，法线和遮罩关闭sRGB。塔架与吊斗法线强度均为0.45。

[实际场景预览](UnityIntegration/Scene_After.png)

[替换与验证记录](UnityIntegration/verification.json)

与替换前磁盘副本逐个比较场景序列化对象，最终只有tajia01的预制体引用块和对应Transform引用块改变。移动Scene视角引起的36个MapMagic预览距离缓存已通过编辑器恢复，其他场景对象数据保持一致。旧FBX的SHA256保持不变。

已确认模型、材质和纹理引用有效，并查看实际 Scene 视图。导入过程中工具等待曾超时，随后确认导入全部完成。替换后没有新增Console错误，Play Mode尚未验收。原实例没有碰撞体，本次没有添加碰撞体、LOD或动画。替换前场景副本保存在 `UnityIntegration/Main Game Scene.before.unity`。

## 当前配色与锈蚀纹理

当前主场景11座塔架均已替换为最新 `CablewayTower_Rebuilt`，并保存。三份旧塔架FBX及对应.meta、Previous目录中的8份旧版模型已删除。最新塔架与吊斗源文件和导出保留，历史贴图和截图保留。下文为制作时记录，旧模型路径不再有效。替换、清理及验证结果见[全部塔架检查](UnityIntegration/all_towers_verification.json)。

Unity 中的当前名称已于2026年9月27日统一，详见[命名对照](Naming.md)。本文制作记录保留历史名称。

按用户提供的旧塔架近景重新细修。当前纹理直接取自tajia03原始颜色图 `texture_pbr_20250901.002 1.png` 内部连续的旧钢材区域，保留灰白磨蚀、赭黄锈斑和破碎边缘，并加入宽缓的暖褐色变化。根据每根钢构件的主轴单独排列纹理，再烘焙到原有4K UV图集，避免跨构件最近表面投射造成的条带拉伸。最初的整塔最近表面转移方案已被此构件映射方案取代。

重新生成金属度、粗糙度、侵蚀遮罩、HDRP Mask与ORM。恢复较轻的原始微表面法线，强度0.45。几何、UV与原有AO保持一致，未增加面数。Unity颜色乘数取自旧塔架材质，约为0.864，金属度重映射上限为0.414，光滑度重映射上下限均为0。

当前修改只应用于已经替换的tajia01塔架，其余10座旧塔架与吊斗保持原状态。Blender源文件、FBX、GLB及Unity使用的三张贴图已同步更新。

[当前实际场景近景](UnityIntegration/Scene_LegacySurface_Close.png)

[纹理制作与几何验证](legacy_surface_transfer.json)

[Unity材质检查](UnityIntegration/legacy_surface_verification.json)

## 之前的配色与侵蚀版本

暖深灰方案保存在 `Previous/Before_OldTower_Palette`。之前的程序化碎锈与蚀坑版本保存在 `Previous/Before_Legacy_Surface_Transfer`，已不作为当前外观。历史检查记录保留用于回溯。

## 模型文件

[Blender 可编辑源文件](Blender/Tajia01_Tower_v01.blend)

[FBX 独立塔架](Exports/Tajia01_Tower_v01.fbx)

[GLB 独立塔架及内嵌材质](Exports/Tajia01_Tower_v01.glb)

导出塔架包含一个网格、一个材质、26144 个建模顶点和48776个三角面。不同引擎可能按 UV 与法线拆分顶点。贴图分辨率为4096乘4096。原点位于塔底中央。

导出后的 Blender 世界坐标包围盒为宽0.638180、深0.268648、高1.035036，与原 FBX 高度一致。这是对齐旧资源的导入尺度，不代表真实建筑的米数。Unity 中的最终尺寸需要结合场景实例缩放确认。

Blender 源文件保留放大的建模场景、隐藏的独立构件集合、烘焙后的塔架和预览灯光。导出时使用0.10034278072840624的统一缩放。源文件用于编辑与渲染，游戏导入使用 Exports 下的 FBX 或 GLB。

## 实际模型预览

[导出 GLB 重新导入后的整体渲染](Previews/Tower_v01_ExportCheck.png)

[连接件与平台近景](Previews/Tower_v01_Detail.png)

[背面](Previews/Tower_v01_Rear.png)

这些图片由实际三维模型渲染，概念图另见下文。

## 贴图

`Textures/Tower_BaseColor.png` 为 sRGB 颜色贴图，不包含灯光。

`Textures/Tower_Normal.png` 为切线空间法线，采用较轻的原始微表面法线，当前材质法线强度为0.45。

`Textures/Tower_HDRP_Mask.png` 按 HDRP 通道布局打包，R 为金属度，G 为环境遮蔽，B 为全白细节遮罩，A 为光滑度。作为数据贴图使用，关闭 sRGB。

`Textures/Tower_OcclusionRoughnessMetallic.png` 用于 GLB，R 为环境遮蔽，G 为粗糙度，B 为金属度。GLB 已内嵌颜色、法线及该贴图。

Metallic、Roughness 和 AO 单通道原始烘焙图也保存在 Textures 中。FBX 依赖相邻 Textures 目录，不能假定 FBX 自动生成的 Unity 材质已经正确配置 HDRP。

## 已执行验证

FBX 与 GLB 均重新导入 Blender，确认各自只有一个塔架网格、一个材质，三角面数和 UV 正确。FBX 世界尺寸最大误差约0.000000075，GLB 尺寸误差为零。

导出前网格非流形边为零，零面积面为零。GLB 包含一个场景、一个网格、一个材质和三张内嵌贴图，没有旧模型、吊斗或预览灯光。

当前GLB已重新导入并与导出前同机位渲染对照，平均RGB绝对差见机器检查记录。整体、近景与背面预览已同步更新。

原 FBX 在制作前后的 SHA256 一致，为 `9364BA106E2CCFF1A4B69C824F0AF70D61F2EE434421BAB39AB1F7511418BDB1`。

后续已完成Unity材质绑定与场景替换，见上方记录。碰撞体、LOD和Play Mode尚未配置或验证。

[机器检查记录](verification.json)

## 原模型检查

通过 Blender MCP 在 Blender 5.2.2 LTS 的独立检查场景导入 `Assets/Art Resources/Environment/Cableway/tajia01.fbx`。

导入结果为一个网格对象 `material`，84575 个顶点，72570 个多边形，一个材质槽 `Material.005`。

检查视图采用 Unity 材质实际引用的 `Assets/Art Resources/Environment/Texture/texture_pbr_20250901.png` 和对应法线图。FBX 自动解析到 Cableway 下的同名贴图，因此检查场景中重新指向上级 Texture 路径。该检查视图不等同于 Unity HDRP 的最终渲染。

[原模型视图](References/Tajia01_Original_Overview.png)

## 已确认概念图

[塔架](Concepts/Tower_Concept_v01.png)

[运煤吊斗](Concepts/CoalBucket_Concept_v01.png)

两张图由内置 imagegen 根据原模型视图生成。用户已确认风格。塔架首版已依照该方向制作，机械连接仅作为游戏美术表达，不是工程施工设计。

运煤吊斗首版已按确认的轮廓和塔架调整后的配色制作，见[吊斗说明](CoalBucket/README.md)。

[完整生成提示词](generation-prompts.md)
