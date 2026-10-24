# ArtSource/Cableway/CablewayStationShell/

## 用途

索道站棚模型：一面封板的长边、窄单坡屋顶、整面敞开的对面和两端，敞开侧只留通高立柱和上部斜撑。灰米色哑光金属、克制的赭色氧化、外露螺栓、缺失的波纹板和风化混凝土，山脚站和山顶站各用两座。

## 内容

- `Blender/CablewayStationShell.blend`：当前源文件，贴图已打包。`CablewayStationShell_Compact_Gray.blend`：定稿前的灰模。
- `Exports/`：FBX 和 GLB，只含六个模型网格，摄影棚灯光、相机和地面不导出。
- `Scripts/`：`build_station.py`、`build_compact_gray.py`、`finish_compact.py`、`refine_steel.py`、`export_compact.py` 是建模各阶段的脚本，依赖当时的实时会话辅助对象；`verify_exports.py` 重新导入导出文件核对。
- `Textures/`：Steel、Cladding、Concrete 三套，各有 BaseColor、Normal、Roughness、Metallic、ORM 和 HDRP_Mask。
- `Previews/`：当前封板侧、敞开侧和 GLB 回导的渲染，以及灰模阶段的预览。
- `References/`：定稿的紧凑单坡棚、单侧封板和整体概念参考。
- `verification.json`、`compact_gray_verification.json`：导出检查。
- `Previous/`：被否掉的大棚方案和一次会话保存前的源文件。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/Cableway/CablewayStationShell/`，场景实例是 `CablewayFoothillStation` 和 `CablewaySummitStation` 下的 `CablewayStationShell_LineA`、`_LineB`，摆放见 `../FoothillStationPlacement/` 和 `../SummitStation/`。
- `../CablewayStationRollerFrame/` 的混凝土贴图复制自这里，钢材配色取样自现有索道贴图。

## 注意

- Blender 里名义尺寸约长 11.35 米、深 3.66 米、高 4.93 米（含出檐），是建模尺寸，不是从参考图量出来的。Blender Z 朝上、米制，FBX 和 GLB 为 Y 朝上。
- ORM 的遮蔽通道和 HDRP_Mask 的绿蓝通道都是常量白，没有烘焙 AO。
- 只是站棚，不含驱动机械和吊斗。接着改从当前源文件开始，早期建模脚本不能直接重跑。
- 站棚所在地面有坡，局部埋入待整平；碰撞和通行没在 Play Mode 验证。
