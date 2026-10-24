# ArtSource/Cableway/CablewayStationRollerFrame/

## 用途

站口滚轮架模型，三只滑轮排成一排，下方双柱架与滑轮排平行，带上部斜撑、螺栓连接和两个混凝土脚，用在山脚站和山顶站的缆线进出口。

## 内容

- `Blender/CablewayStationRollerFrame.blend`：源文件，贴图已打包，摄影棚相机、灯光和地面留在源文件里，不导出。
- `Exports/`：FBX 和带内嵌材质的 GLB。
- `Scripts/build_roller_frame.py`、`verify_roller_frame.py`：在后台 Blender 进程里建模和检查，构建脚本读相邻站棚目录的混凝土贴图。`build.log`、`verification.log`、`verification.json`：运行输出和检查结果。
- `Textures/`：Steel、DarkSteel、Concrete 三套，各有 BaseColor、Normal、Roughness、Metallic、ORM 和 HDRP_Mask。
- `Previews/`：整体、滑轮近景、正面和 GLB 回导的渲染。
- `References/Approved_Concept.png`：定稿概念图。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/Cableway/CablewayStationRollerFrame/`，场景实例是 `CablewayFoothillStation` 和 `CablewaySummitStation` 下的 `CablewayStationRollerFrame_LineA`、`_LineB`，摆放见 `../FoothillStationPlacement/` 和 `../SummitStation/`。
- 混凝土贴图复制自 `../CablewayStationShell/`，钢材配色跟站棚一致。

## 注意

- 每只滑轮是独立对象，原点在轴上，可绕本地 X 转，没有旋转动画。
- Blender 里名义尺寸宽 0.88 米、长 3.62 米、高 4.835 米，脚底在 Z 零，是配合站棚定的建模尺寸，不是考据尺寸。
- 模型不含缆线，ORM 和 HDRP_Mask 的遮蔽通道都是全白，没有烘焙 AO。
- 开口架本身不证明吊斗能通过，纵梁和滑轮托架要按实际绳索和吊架路径核对。
