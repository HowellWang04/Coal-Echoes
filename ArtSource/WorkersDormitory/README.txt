矿区洗煤厂员工宿舍

2026年10月4日按用户确认的照片与概念方案制作。

WorkersDormitory.blend 为可编辑 Blender 源文件，建筑按米制作，原点位于地面中央。当前建筑场景名为 WorkersDormitory。WorkersDormitory_Asset 为模型，WorkersDormitory_PreviewOnly 为摄影棚地面、灯光及相机。

三层宿舍，三个入口，四组竖向阳台，共十二个阳台，其中三个开放、九个封闭。主体平面约 32 米乘 9.6 米，包含窗台、阳台和屋顶通风口的完整外包尺寸约 32.71 米乘 11.725 米乘 10.825 米。

模型包含四面砖墙、真实窗洞、分格窗框、部分缺损玻璃、浅室内遮挡、混凝土阳台、门扇、台阶、雨棚、落水管、低女儿墙及通风口。房间内部仅用于遮光，不包含可游玩的室内、楼梯、家具、碰撞体或交互。

Exports 中提供 GLB 与 FBX。两个格式的重新导入检查均通过，均有 14 个网格、106599 个三角面、UV 和材质。导出中不含摄影棚地面、灯光、相机或默认立方体。源文件保留 137 个可分别编辑的网格，导出版本按材质合并。

GLB 内嵌 PBR 材质贴图。FBX 同时保留几何与材质槽，Unity HDRP 使用时需配置 HDRP 材质。Textures 文件夹内提供底色、重建后的切线空间法线、ORM 与 HDRP Mask。HDRP Mask 的红通道为金属度，绿通道为遮蔽，蓝通道为细节遮罩，透明度通道为光滑度。Runoff 为带透明度的局部雨水痕贴图。

砖和混凝土纹理来自本项目已存在的 Industrial_Harbour 资源包。砖底色已派生为灰黄配色，保留原资源。原法线仅存储 RG 信息，宿舍法线已重建 Z 分量后再用于 Blender 和导出。本资产派生贴图延续原资源包的使用授权范围。

Previews 内的 Hero、Front、Rear、Detail 均为实际模型渲染。GLB_Verified 是导出 GLB 重新导入后渲染的结果。Reference 内的 Approved_Concept 是此前用户确认的生成概念图，Original_Reference 是原始圈画照片。

verification.json 记录重新导入检查，asset_report.json 记录规格。当前尚未放入 Unity 场景，未进行 Play Mode 验证。

需要重新生成时，在新的 Blender 进程中先执行 build_dormitory.py，再执行 finish_dormitory.py。verify_and_render.py 用于读取保存的源文件、检查两个导出格式并生成预览。这些脚本按单次资产制作流程使用。


中央入口修订

正面中央的普通门已改为双扇钢框入口大门，门洞宽 2.98 米、高 2.65 米，配套加宽混凝土门套、4.05 米宽雨棚和 3.8 米宽台阶。原门两侧的两扇底层小窗并入入口区域，上层窗户和其他入口保留。Previews 中新增 Dormitory_Entrance.png 入口近景。entrance_revision.json 记录修改范围及 109 个原有网格的几何未变检查。

后门修订

后墙中央增加净宽约 1.2 米的灰绿钢框后门，带玻璃上窗、把手、混凝土门套、紧凑台阶及小雨棚。门扇有独立门轴，默认关闭并向内开启，适应与相邻厂房之间的窄巷。门后仅清出入口空间，完整可游玩室内和开门交互尚未制作。

add_rear_door.py 在未添加后门的源模型上执行一次，保留原有材质和导出标识；apply-rear-door.cs 通过 Unity CLI 更新碰撞并验证开关状态，结果见 rear_door_scene_checks.json。GLB、FBX、Blender 源文件和主场景均已更新。

