# ArtSource/FactoryGate/

## 用途

洗煤厂大门模型，平顶工业厂门：两根砖柱、两扇锁着铁链和挂锁的钢门、门卫室一侧的短墙。钢门有漆皮薄片和贯穿腐蚀孔洞，砖柱按旧砖墙照片调过色。

## 内容

- `FactoryGate_Rebuilt.blend`：源文件，22 张贴图已打包，Layout 默认材质预览。
- `build_gate.py`、`texture_maps.py`：全量重建，生成贴图、模型、预览和导出；末尾加 `-- --skip-textures` 复用现有贴图，`-- --skip-render` 只出资产。`rebuild_leaves.py`：只重建两扇门和钢板贴图，保留门柱和短墙。`verify_exports.py`：重新导入 GLB 和 FBX 核对尺寸、三角面、UV、贴图和渲染。`verify_chain.py`：复查锁链链环套接和绕行，结果在 `chain_geometry.json`、`chain_verification.json`。
- `refresh_brick_color.py`：砖色调整脚本，配方已同步进生成流程。`RefreshBrickMaterial.cs`：Unity 里刷新砖材质。`BuildBoundaryPrefabs.cs`：生成 `../FactoryBoundary/` 的三个基础预制体。`ReplaceGateInScene.cs`：一次性替换旧门，已完成，不要重跑。
- `Previews/`：Blender 实际渲染（整体、正面、锁链、砖柱、漆皮、孔洞、白灰、背面、GLB 回导）、`Approved_Concept.png` 概念图（不是模型渲染）和各阶段 Unity 截图。
- `asset_report.json`、`verification.json`、`brick_color_*.json`、`unity_*.json`、`scene_inspection*.json`、`unity_scene_replacement.diff`：导出、调色、场景替换的记录。
- `BrickColorStaging/`：砖色试验贴图和渲染脚本，`.gitignore` 忽略它以及 `*.log`、`*.blend1`、`__pycache__/`。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt/`：GLB、FBX 和 `Textures/`。场景实例 `In-Scene Objects/工厂大门/FactoryGate` 直接引用 GLB，缩放 1，用 GLB 导入材质，8 个砖网格带顶点颜色。同目录的旧 `FactoryGate.glb` 不在重建输出范围内。
- 门卫室是同级的 `Guard Post/Guardhouse_Rebuilt`，门卫室旁短墙和围墙模块见 `../FactoryBoundary/`。
- 铁链线径 10 毫米，第一关用 `../Props/BoltCutter/` 剪断。
- 场景里两扇门扇各挂一个 `SwingDoor`，`Chain_And_Padlock` 挂 BoxCollider 和 `ItemUsePoint`（要求 `bolt_cutter`，F 提示位置是子物体 `CutPromptAnchor`），T8 出现前由 `Level1HintFlowManager` 锁着，不出提示。用后两扇门向厂内转开、勾 T8、播 M7，最后隐藏锁链挂锁，事件接在 Inspector 里。saveId 是 `gate_chain`，读档时 onRestored 让两扇门直接停在开着的位置并隐藏锁链挂锁。
- `FactoryGate` 根上挂 `EchoTrigger`（`group_photo_retake`），是重拍合照的 A 类回响，站位点是门前的 `Group Photo Retake View`（脚下是泛光圆圈 `Group Photo Retake Circle`），拍成勾 T4、播 M4；场景里保存为禁用，由 `Level1HintFlowManager` 在 T4 打开。合照制作见 `../Props/GroupPhoto/`。

## 注意

- 按米建模，原点在门洞底部中央，左右门扇轴心在各自铰链处。每扇门按实际门洞宽度建模，铰链到中缝约 6.219 米，缩放保持 1；每扇中间一根立柱，钢板和背面斜撑分成两格。
- 钢板贴图是双门图集，锈斑、剥落、锈痕和孔洞按实际米数生成，不随门宽拉伸。腐蚀孔洞布尔后会留下针状小三角面，导出前要删。
- `Concrete` 贴图的 V 对应墙高（4.5 米一张），V 0 附近是墙根竖条污渍，顶部有两道压痕。混凝土件的 UV 要走 `concrete_uv`，水平面取 V 0.5 附近的干净段，否则压顶和柱帽顶面会出现条纹和中线接缝。
- 逐砖色差存在顶点颜色里，GLB 导入材质会用；若改用 FBX，材质必须把顶点颜色乘入底色。`BrickWeathered` 用自己的 BaseColor，与 `Brick` 共用 Normal 和 HDRPMask。
- 用 FBX 时按贴图名建 HDRP/Lit 材质：`*_BaseColor.png` 接 Base Map（sRGB 开），`*_Normal.png` 接 Normal Map，`*_HDRPMask.png` 接 Mask Map（sRGB 关，R 金属度、G 遮蔽、B 细节遮罩、A 光滑度），`*_ORM.png` 只给 glTF 用。
- 主菜单期间 `MainMenuUIRoot` 上的 `OpeningSceneState` 把 `Left_GateLeaf`、`Right_GateLeaf` 绕各自铰链向厂内转开（左扇负角、右扇正角）并隐藏 `Chain_And_Padlock`，开场镜头对着天空时还原；还原时看铁链使用点用过没有，没用过就关门挂链，用过就保持开门、不挂链。它在 Awake 读关门姿态，所以场景里保存的必须是关门挂链的姿态。门扇的 `SwingDoor` 和它转的是同一组 Transform，两边转角要一致。
- 剪链开门后的 Play Mode 通行待确认。
- 重建命令在项目根目录用 PowerShell 执行，重建会覆盖本目录源文件、预览和 Assets 里的导出：

```powershell
& 'D:\Application\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python-exit-code 1 --python 'ArtSource\FactoryGate\build_gate.py'
& 'D:\Application\Steam\steamapps\common\Blender\blender.exe' --background --factory-startup --python-exit-code 1 --python 'ArtSource\FactoryGate\verify_exports.py'
& 'D:\Application\Steam\steamapps\common\Blender\blender.exe' --background 'ArtSource\FactoryGate\FactoryGate_Rebuilt.blend' --python-exit-code 1 --python 'ArtSource\FactoryGate\rebuild_leaves.py'
```
