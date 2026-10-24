# 厂区边界制作源文件

## 当前围墙

2026年10月10日新增 9 米规格墙段。`build_wall_variants.py` 输出 `FactoryWall_9m_Variants.blend` 与六个 `FactoryWall_9m_01` 至 `FactoryWall_9m_06` GLB。六版共享完全相同的几何、砖块排列、UV 和材质，只改变逐砖顶点颜色分布，取消白灰网格；`wall_variants_report.json` 核对几何与颜色哈希。

GLB 不重复嵌入贴图。Unity 中使用同名 Prefab，复用现有大门材质及贴图，定位点间隔 9 米，实际墙身长 8.25 米。主场景沿既有路线混排 88 段墙和 89 根柱，相邻墙段不重复，转角之间按实际距离收口，坡地采用混凝土基座。摆放及接缝记录在 `Library/FactoryBoundaryPlacement/`，实际观感与通行待 Play Mode 验收。

2026年10月20日用 `crop_guardhouse_wall.py` 从 `FactoryWall_3m.glb` 裁出 0.82 米的 `FactoryWall_GuardhouseShort.glb`，放在 `Wall_030_V06` 下，补上 `Pillar_030` 与门卫室之间的缺口，替代原来的矮回墙柱。保留模块完成的一端插入墙柱，裁开的一端藏在门卫室外墙里。不含白灰，以便和门口一侧的短墙一致。实景见 [Unity 预览](Previews/Unity_Guardhouse_Rear_Wall_Detail.png)。

以下为早期 3 米基础模块的制作记录。

2026年9月24日制作三个基础资源，直接修改当前大门，没有新增备份。

1. 大门保留门卫室一侧短墙，删除另一侧突出墙段。当前为 61,551 个三角面、23 个网格。
2. 独立柱子为 4,352 个三角面、4 个网格。四面砖体完整，移除铰链配件。
3. 标准围墙为 14,226 个三角面、5 个网格。双面砖体及白灰残留，柱心间距 3 米，墙高约 3.68 米，墙身厚 0.50 米。

## 文件

源模型为 FactoryBoundary.blend，其中三个模块分开展示。贴图已经打包。大门的独立源文件仍为相邻 FactoryGate 目录中的 FactoryGate_Rebuilt.blend。

Previews/Boundary_Modules.png 为三个基础模块的实际 Blender 渲染，Previews/Boundary_Joined.png 为直线连接及直角转弯的实际模型演示。演示排列未写入游戏场景。

boundary_report.json 记录导出尺寸与网格数据。unity_prefab_report.json 记录生成 Prefab 后的三角面数、碰撞体、材质共享和定位点。unity_import_verification.json 记录 Unity 实际导入的顶点属性、受支持的 Shader 与场景大门短墙状态。

## 重建

通过 Blender 后台执行本目录 build_boundary.py。脚本读取当前大门源文件，覆盖大门导出，重建立柱与围墙，输出打包源文件和两张预览，不创建备份。

随后通过已连接的 Unity 编辑器执行 ArtSource/FactoryGate/BuildBoundaryPrefabs.cs 中的 Build 方法，重新保存三个 Prefab。Validate 方法只检查并记录导入结果。

新 Prefab 位于 Assets/Art Resources/Environment/Gate Area/FactoryBoundary/Prefabs。使用说明见该 FactoryBoundary 资源目录中的 README.md。

本轮仅制作基础模块，保留固定逐砖色差，未实现实例随机材质。主场景继续引用原路径的大门 GLB，导入后自动更新为单侧短墙。新增 Prefab 已具有简单碰撞体，尚未替换场景中的原有大门实例或进行 Play Mode 验收。

refresh_brick_color.py 是模块拆分前的历史调色脚本，读取旧备份。不要在当前模块上直接运行，否则会恢复已删除的短墙。后续调色应以当前源文件为输入。
