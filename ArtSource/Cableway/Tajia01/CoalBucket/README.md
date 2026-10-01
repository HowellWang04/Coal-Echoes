# Tajia01 独立运煤吊斗

当前主场景11座塔架均使用包含独立CoalBucket的最新重建组合。见[全部塔架检查](../UnityIntegration/all_towers_verification.json)。

Unity 中的当前名称已于2026年9月27日统一，详见[命名对照](../Naming.md)。本文制作记录保留历史名称。

2026年9月25日

按已确认的吊斗概念图制作首版，配色沿用塔架后续确认的暖深灰钢材、灰褐锈迹和暗灰红褐旧漆。模型包括具有实际壁厚的敞口斗身、翻边、接缝、紧固件、侧转轴、弯折吊架、顶部双槽轮和吊销。斗内为空腔，采用较暗的煤尘材质。

## 文件

[Blender 可编辑源文件](Blender/Tajia01_CoalBucket_v01.blend)

[FBX 独立模型](Exports/Tajia01_CoalBucket_v01.fbx)

[GLB 模型及内嵌材质](Exports/Tajia01_CoalBucket_v01.glb)

[确认的概念图](../Concepts/CoalBucket_Concept_v01.png)

塔架与吊斗分别交付为独立模型。吊斗导出文件不含塔架、钢缆和预览灯光。原 `Assets/Art Resources/Environment/Cableway/tajia01.fbx` 保持不变。

## 实际模型预览

[整体](Previews/CoalBucket_v01_ExportCheck.png)

[斗身与内壁近景](Previews/CoalBucket_v01_Detail.png)

[滑轮与吊点近景](Previews/CoalBucket_v01_Trolley.png)

[背面](Previews/CoalBucket_v01_Rear.png)

这些图片由导出的 GLB 重新导入 Blender 后渲染。

## 网格与尺寸

一个网格、一个材质，14096个建模顶点，27516个三角面，一套UV。引擎可能按UV与法线进一步拆分顶点。

导出后的 Blender 世界坐标尺寸为宽0.254369、深0.170984、高0.405611。使用与塔架一致的0.10034278072840624导出缩放。该数值用于配合现有资产比例，不代表真实吊斗的米制尺寸，也不是对旧网格吊斗的精确测量。

导出原点位于两个滑轮下方的钢缆接触中心。Blender 建模场景中的对应位置为0、0、3.72。FBX和GLB已将该位置移到原点，后续可通过此点对齐索道。

源文件保留独立构件和烘焙后的导出网格。当前交付为静态模型，尚未制作翻斗动画、关节或物理模拟。

## 材质与贴图

所有贴图分辨率为4096乘4096。颜色、法线和金属粗糙度遮蔽图已内嵌至GLB。

`Textures/CoalBucket_BaseColor.png` 为sRGB颜色图，不包含灯光。

`Textures/CoalBucket_Normal.png` 为切线空间法线，当前强度为0.45。

`Textures/CoalBucket_HDRP_Mask.png` 的R为金属度，G为环境遮蔽，B为全白细节遮罩，A为光滑度。用作数据贴图，关闭sRGB。

`Textures/CoalBucket_OcclusionRoughnessMetallic.png` 的R为环境遮蔽，G为粗糙度，B为金属度，用于GLB。

另保留Metallic、Roughness和AO的单独烘焙图。FBX需与相邻Textures目录一起使用；Unity中的HDRP材质绑定仍需检查。

## 已执行验证

FBX与GLB均重新导入Blender，确认只有一个吊斗网格和一个材质，三角面数与UV正确。FBX最大尺寸误差约0.000000026，GLB为零。导出前非流形边与零面积面均为零。

GLB包含三张内嵌贴图，颜色、法线、金属粗糙度和遮蔽引用均存在。源材质与重新导入模型的同机位渲染平均RGB绝对差约0.00000351。已查看整体、斗内、滑轮和背面。

后续已完成Unity导入和HDRP材质绑定，并作为独立CoalBucket子对象替换到主场景tajia01组合。吊点按横梁下方位置对齐，轮组沿索道方向摆放。见[场景替换记录](../README.md)。碰撞体、LOD、动画及Play Mode尚未配置或验证。

[机器检查记录](verification.json)
