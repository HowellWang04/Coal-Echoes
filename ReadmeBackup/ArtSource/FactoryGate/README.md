# 洗煤厂大门

按已确认的平顶工业厂门方案制作。设定为洗煤厂。

## 门扇按实际宽度重建

2026年10月20日按场景实际门洞重建两扇门。此前场景里两扇门的横向缩放是 2.319，竖杆间距、边框、铰链、把手和钢板贴图都被横向拉伸。现在每扇门按真实宽度建模，铰链到中缝约 6.219 米，缩放恢复为 1，门洞宽度和门柱位置都不变。

1. 每扇门中间加一根立柱，钢板和背面斜撑因此分成两格。顶部竖杆按原来约 0.24 米的间距排列，每扇 25 根。
2. 钢板贴图改为 6656×1536 的双门图集，每扇门占 3328×1536，横竖像素密度一致。锈斑、漆皮剥落、雨水锈痕、划痕和腐蚀孔洞按实际米数生成，不再随门宽拉伸。仍为 16 处贯穿孔洞，翘起漆皮按面积增至约 1400 片。
3. 门柱和短墙沿用已确认的砖块几何与逐砖色差，只把父节点移到新铰链外侧，Unity 场景里各部件位置保持原样。
4. 钢板经腐蚀孔洞布尔运算后会留下约 0.5 平方毫米的针状三角面，导出前一并删除。

当前共 94,127 个三角面、23 个网格，GLB 约 87.6 MiB。导出检查全部通过，Unity 重新导入后两扇门宽度、碰撞体与 Console 均已核对，实景见 [Unity 预览](Previews/Unity_Unstretched_Leaves.png)。只重建门扇用 `rebuild_leaves.py`，命令见下方重建与检查。下方各节中门扇尺寸、三角面数和贴图尺寸指本次之前的版本。

## 当前模块化版本

2026年9月24日删除远离门卫室一侧的短墙，保留两根门柱、双扇门和门卫室侧短墙。当前模型宽约 7.954 米、高约 4.391 米、厚约 1.006 米，共 61,551 个三角面和 23 个网格。源文件、GLB、FBX 已直接更新，没有新增备份。

另制作独立柱子和标准墙段，三个可用 Prefab 统一位于 Gate Area/FactoryBoundary/Prefabs。新 Prefab 含简单碰撞体和拼接定位点，场景原有 GLB 实例仍沿用原有组件配置。

当前外观见 [三个基础模块](../FactoryBoundary/Previews/Boundary_Modules.png) 与 [直线及转角拼接](../FactoryBoundary/Previews/Boundary_Joined.png)。使用说明和当前检查结果见 [模块制作记录](../FactoryBoundary/README.md)。下方调色与替换记录、旧预览和 verification.json 对应拆分短墙前的版本。

## 砖色修订

2026年9月24日按用户提供的旧砖墙照片调整砖底色。按后续反馈收回过灰的方案，最终以暖土黄、浅赭和褪色砖红为主体，少量砖面稍偏橙红。每块砖表面使用固定的独立色差，并加入细微明度变化，缩小不同色组之间的反差。原有白灰、墙脚污染、砖块几何、法线和粗糙度保留。

Blender 源文件、GLB、FBX 和独立 PNG 已更新。逐砖色差通过顶点颜色保存，Unity 中使用 GLB 导入材质，8个砖网格均已确认具备颜色属性。若另行采用 FBX，需使用会将顶点颜色乘入底色的材质。Unity 主场景实例已确认使用重新导入的材质，位置与缩放未变。本次最终检查在编辑模式完成，未进行新的 Play Mode 验收。

网格位置、拓扑和 UV 检查未变，新增逐砖顶点颜色属性，三角面数仍为72,331。22张内嵌图像中只调整 `Brick_BaseColor` 和 `BrickWeathered_BaseColor`。GLB 重新导入的尺寸、孔洞和渲染检查通过，最新数值见 `verification.json`。

初次调色前文件位于 `Previous/Before_Brick_Color_Revision`，增加逐砖色差前文件位于 `Previous/Before_Individual_Brick_Colors`。调整脚本为 `refresh_brick_color.py`，材质生成配方和逐砖色差已同步到生成流程。检查见 `brick_color_revision.json` 和 `brick_color_unity.json`。当前色调见下方砖柱近景，Unity 实景见 [更新后的场景预览](Previews/Unity_Brick_Color_Scene.png)。

## 文件

- [Blender 源文件](FactoryGate_Rebuilt.blend)
- [整体实际渲染](Previews/Gate_Hero.png)
- [导出 GLB 重新导入后的实际渲染](Previews/Gate_GLB_Roundtrip.png)
- [正面](Previews/Gate_Front.png)
- [锁链及材质近景](Previews/Gate_Lock_Detail.png)
- [锁链绕行与挂锁连接俯视近景](Previews/Gate_Lock_Top.png)
- [砖柱近景](Previews/Gate_Masonry_Detail.png)
- [真实翘起漆皮近景](Previews/Gate_Paint_Detail.png)
- [底部贯穿孔洞近景](Previews/Gate_Corrosion_Detail.png)
- [白灰残留与墙脚近景](Previews/Gate_Whitewash_Detail.png)
- [背面加强筋](Previews/Gate_Rear.png)
- [已确认的外观概念图](Previews/Approved_Concept.png)，用于设计参考，不是模型渲染
- [Unity GLB](../../Assets/Art%20Resources/Environment/Gate%20Area/FactoryGate_Rebuilt/FactoryGate_Rebuilt.glb)
- [Unity FBX](../../Assets/Art%20Resources/Environment/Gate%20Area/FactoryGate_Rebuilt/FactoryGate_Rebuilt.fbx)
- [独立材质贴图](../../Assets/Art%20Resources/Environment/Gate%20Area/FactoryGate_Rebuilt/Textures/)

## 参考图修订

根据用户确认的修改清单补充实际漆皮薄片与贯穿腐蚀孔洞，调整红砖尺度、旧白灰位置、雨水锈痕和墙脚污染。参考图用于位置、结构和质感对照，以下图片均为实际模型渲染。

1. 561 片实际漆皮几何，厚约 0.3 毫米，固定边贴近钢板，末端轻微翘起。主要集中于剥落区域边缘。
2. 两扇门共 16 处不规则贯穿腐蚀孔洞，孔边有轻微变形。孔洞位置参与贴图锈蚀分布。
3. 重锈集中于底部、接缝及左上部，中部保留灰蓝漆。边框采用较暗旧铁色。
4. 砖层高度由约 7.5 厘米调整至约 15.7 厘米，减少密集小砖观感，增加颗粒、棱角缺损和墙脚深色污染。
5. 白灰位于短墙中下部，左高右低，以不对称斑块、大片缺失、内部露砖和零散小块形成自然残留。白灰覆盖灰缝并保留浅凹、缺损与污染。
6. 混凝土压顶简化轮廓，增加破角与细小孔隙。短墙顶部采用横向砖层与薄压顶。
7. 锁链采用一条连续路径，穿过两个把手开口，两端由挂锁锁梁套住闭合。21 个链环逐对套接，链条整体下移并贴近两侧把手承托位置。

修改前源文件和整体渲染保留在 [Previous](Previous/) 目录。Unity 主场景已替换为新版资产，实际操作记录见下文。

## 拆分短墙前的模型规格

- 总宽约 8.516 米，总高约 4.391 米，最大厚度约 1.006 米。门扇净跨度约 5.4 米。
- 72,331 个三角面，相比原模型的 1,500,000 个三角面减少约 95.2%。
- 28 个网格，8 种材质。两扇门、两根砖柱、两段短墙与锁链分别归组。
- 左右门扇轴心在各自铰链处。整体原点在门洞底部中央，按米制作。
- 平直横梁、等距方钢竖杆、6 毫米钢板、背面加强筋、铰链、把手、锁链与挂锁均为实际网格。
- 砖缝、砖边小倒角与混凝土压顶为几何。隐蔽的砖体内部由实体砂浆基体承托，省去逐砖不可见内表面。
- 材质分别表现漆面、裸铁、锈层、砖、砂浆、混凝土与旧白灰层。左右门扇使用不同锈蚀分布。
- 钢板贴图为 4096×2048 的双门图集，每扇门使用其中一个 2048×2048 区域。砖与混凝土贴图为 2048×2048，框架与铁件为 1024×1024，砂浆和白灰为 512×512。
- GLB 内嵌 PBR 贴图，约 78.6 MiB。FBX 和独立 PNG 同时保留。

## 在 Unity 中检查

2026年9月24日已通过运行中的 Unity 编辑器替换并保存 `Main Game Scene`。层级为 `In-scene Objects/Factory Entrance/FactoryGate`，引用 `Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt/FactoryGate_Rebuilt.glb`，缩放为 `1, 1, 1`。已按旧门底部中央和正面方向校正轴向。

编辑器检查确认 72,331 个三角面、8 种有效材质，颜色、法线与金属粗糙度贴图均已绑定，旧模型实例为零。实际场景近景见 [Unity 场景预览](Previews/Unity_After_Replacement_Detail.png)。预览使用当前场景光照，正面处于阴影中。

原门没有交互脚本、碰撞组件或外部组件引用，本次保留该行为状态，未添加碰撞。Play Mode 画面与通行行为仍待用户确认。Console 中有此前生成贴图期间的读取错误，当前两张相关混凝土贴图均可加载，此次替换未出现新错误。

替换前磁盘文件和包含未保存修改的场景副本保存在 `Previous/Before_Unity_Replacement`。与替换前现场副本对比，仅旧门实例、新门实例及父对象子项引用发生变化。操作脚本为 `ReplaceGateInScene.cs`，结果见 `unity_replacement.json`、`unity_verification.json` 和 `unity_scene_replacement.diff`。脚本中的 `Replace` 仅用于此次旧门替换，完成后不要重复执行。

后续在其他场景放置时可按以下步骤检查。

1. 等 Unity 导入完成，先在临时位置拖入新的 GLB。新模型按米制作，Transform Scale 使用 `1, 1, 1`。旧模型在现有场景中的缩放为 8，不要把该缩放直接套在新模型上。
2. 以地面中央为定位点调整位置和朝向。模型正面有铁链和锁，背面有对称加强筋。新旧模型的轴向和原点不同，替换时用画面核对地面高度及方向。
3. 在 Scene 和 Game 视图检查灰蓝漆、砖与混凝土是否各有不同反光，确认没有粉色材质、法线翻转或材质丢失。
4. 碰撞可分别给两扇锁闭门、两根柱和两段短墙配置简单 Box Collider。此资产不含碰撞配置、开门动画或交互脚本。

Blender 的预览灯光仅用于观察形状和材质，导出模型不包含摄影棚、地面、灯光或相机。Unity 的天空、曝光和后处理会影响最终观感。

## FBX 与 HDRP 材质

如果使用 FBX，按贴图名称创建 HDRP/Lit 材质并绑定。FBX 自动生成的材质不作为 HDRP 画面验收依据。

| 贴图 | HDRP 对应输入 | 导入方式 |
| --- | --- | --- |
| `*_BaseColor.png` | Base Map | sRGB 开启，Base Color 为白色 |
| `*_Normal.png` | Normal Map | Texture Type 为 Normal map，初始强度 1 |
| `*_HDRPMask.png` | Mask Map | sRGB 关闭 |
| `*_ORM.png` | glTF PBR 数据 | R 为遮蔽，G 为粗糙度，B 为金属度，不直接放入 HDRP Mask Map |

HDRP Mask 的 R 为金属度，G 为遮蔽，B 为细节遮罩，A 为光滑度。光滑度与金属度重映射范围使用 0 到 1。`BrickWeathered` 使用自己的 BaseColor，与 `Brick` 共用 Normal 和 HDRPMask。

## 重建与检查

在项目根目录用 PowerShell 执行。

```powershell
& 'D:\Application\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python-exit-code 1 --python 'ArtSource\FactoryGate\build_gate.py'
& 'D:\Application\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python-exit-code 1 --python 'ArtSource\FactoryGate\verify_exports.py'
```

只重建门扇和钢板贴图，保留门柱和短墙。

```powershell
& 'D:\Application\Steam\steamapps\common\Blender\blender.exe' --background 'ArtSource\FactoryGate\FactoryGate_Rebuilt.blend' --python-exit-code 1 --python 'ArtSource\FactoryGate\rebuild_leaves.py'
ebuild_leaves.py'
```

`build_gate.py` 可在最后增加 `-- --skip-textures` 复用现有贴图，或增加 `-- --skip-render` 只输出资产。重建会覆盖本目录源文件、预览，以及新模型专属目录中的导出文件。原 `FactoryGate.glb` 不在输出范围内。

`FactoryGate_Rebuilt.blend` 已打包所用贴图，可以单独移动或发送。打开后 Layout 默认使用材质预览。当前窗口若显示灰模，将鼠标放在三维视口，按 Z 并选择材质预览。生成脚本仍使用项目中的独立贴图，重建时需保留项目目录。GLB 同样内嵌贴图。

## 验证结果

2026年9月23日使用 Blender 5.2.2 LTS 完成。

- GLB 和 FBX 均已重新导入，尺寸、三角面数与源文件一致。
- 所有导出网格具备 UV 和有效法线，GLB 所有图元具备切线。
- 所有导出材质包含法线和金属粗糙度贴图，砖色变化在 GLB 中保留。
- GLB 与源文件在相同灯光下的渲染像素平均绝对差约 0.0000083，按 0 到 1 的颜色数值计。
- GLB 与 FBX 的 16 处孔洞中心射线全部贯穿，完整钢板对照点命中，翘起漆皮几何保留。
- 已检查整体、正面、锁链近景、砖柱近景、漆皮近景、孔洞近景、白灰近景及背面。
- Blender 源文件已验证 22 张图像全部内嵌，Layout 默认使用材质预览。
- 锁链构造曲线检查通过，20 对相邻链环全部套接，锁梁穿过两个末端链环，闭合路线穿过两个把手。邻近链环无穿插，距两个把手的最小表面间隙分别约 0.16 毫米与 0.29 毫米。该检查为静态几何验证，不是刚体模拟。
- Unity 实际导入与主场景替换已于2026年9月24日验证，记录见上节。原门及新实例均未配置碰撞，Play Mode 尚未验证。

详细数据见 [asset_report.json](asset_report.json) 与 [verification.json](verification.json)。

锁链的构造曲线与检查结果见 [chain_geometry.json](chain_geometry.json) 和 [chain_verification.json](chain_verification.json)，通过 Blender 执行 `verify_chain.py` 可复查。修订前源文件保留为 `Previous/Before_Chain_Revision.blend`。
