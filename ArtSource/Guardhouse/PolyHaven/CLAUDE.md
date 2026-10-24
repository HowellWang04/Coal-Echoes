# ArtSource/Guardhouse/PolyHaven/

## 用途

门卫室墙面和水泥件用的 Poly Haven 摄影扫描贴图原件，替换掉建模脚本里噪声生成的水泥贴图。两套都是 CC0，可商用，不需要署名。

## 内容

- `concrete_wall_007/`：Concrete Wall 007（polyhaven.com/a/concrete_wall_007），灰色旧水泥抹灰墙，一张对应实物 2.16 米。2k JPG 的颜色、OpenGL 法线、粗糙度三张。
- `concrete_floor_worn_001/`：Concrete Floor Worn 001（polyhaven.com/a/concrete_floor_worn_001），室内旧水泥地，一张对应实物 3 米。同样三张。
- `Scripts/break_tiling.py`：把一张扫描随机切块、翻转后拼成 4096 的新无缝贴图，重复间隔拉长到墙面 6.48 米、地面 6 米，长过门卫室任何一面墙；大块颜色起伏先压掉两成，免得同一块污渍被认出来。颜色、法线、粗糙度用同一套切块，法线随翻转改符号。用法 `python break_tiling.py <扫描名> <原图实物米数> <倍数>`，墙面 `concrete_wall_007 2.16 3`，地面 `concrete_floor_worn_001 3.0 2`。跑墙面时会调用 `wall_eras.py`。
- `Scripts/wall_eras.py`：把拼好的墙面颜色分成两个年代。2026 版给游戏：扫描污渍加深、加水渍，再按高度画墙根返潮和溅泥带、檐下积灰和往下的流挂。1980 版给合照：污渍减到三分之一、略提亮，留抹灰纹理，存 2048 PNG。

## 与其他部分的关系

- Unity 里在 `Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/PolyHaven/`：颜色、法线和 `*_HDRP_Mask.png` 都是 `break_tiling.py` 的输出，不是原件，墙面颜色是 2026 版；遮罩由粗糙度换算（R 金属 0，G AO 1，B 细节 1，A 光滑度 = 1 − 粗糙度）。
- 墙面 1980 版写到 `../../Props/GroupPhoto/CleanGate/concrete_wall_007_BaseColor.png`，和 Unity 里的贴图同名，`RenderGateBackground.cs` 渲染合照时按名字换上。
- 材质 `GH_Wall_ConcreteWall007` 替换门卫室 glb 里的 `GH_GreyWhiteCement`（内外墙共用），`GH_Concrete_FloorWorn001` 替换 `Concrete`（地面、墙脚、窗台、门楣、顶板）。替换做在场景实例 `Guard Post/Guardhouse_Rebuilt` 的 `Guardhouse_Shell` 和 `Guardhouse_Roof` 渲染器上，glb 里的原材质没动。
- 两个材质都是 HDRP/Lit 的物体空间三向投影，不用模型 UV，`_BaseColorMap` 的 Tiling 取 1 ÷ 拼好后的重复长度（1/6.48、1/6），所有贴图跟它走；墙面法线加强，地面压低了光滑度。两者的 `_BaseColor` 染色是对着门卫室里的实际渲染调的：墙面扫描偏米黄、地面偏冷灰，场景的天光又偏蓝，按贴图平均色算会偏，最后以屏幕上墙和地的颜色差不到两个色阶为准。

## 注意

- 门卫室模型的 UV 是一面墙铺一整张图，照片贴图直接按 UV 铺会拉伸，所以只能用三向投影。
- HDRP/Lit 在物体空间映射下不乘 `_TexWorldScale`（保持 1），比例只能用 Tiling 设；Tiling 留 1 会把整张图压进 1 米，污渍和抹痕缩小六倍多，远看是一片平灰。
- 原扫描只有 2.16 米见方，直接平铺会在墙上排成格子，不要把 Unity 里的贴图换回原件；改了 `break_tiling.py` 的参数要重跑两次，Tiling 跟着倍数改。拼图把纹理密度从约 950 降到约 630 像素每米。
- 2026 版墙根和檐下的做旧按高度画进贴图：墙面上贴图 v 等于物体空间高度 ÷ 6.48，图的最下一行在门卫室原点高度，墙面从 0.11 米到 3.18 米（`wall_eras.py` 开头的常量）。改了墙面 Tiling、门卫室模型的原点或墙高，做旧会错位，要改常量重跑。水平面（窗洞内的窗台、过梁底）按进深取 v，会带上对应行的做旧，现在的窗台上看不出来，改窗洞位置后要再看一眼。
- 重新导出 `Guardhouse_Rebuilt.glb` 后，如果子网格顺序或材质名变了，场景里的材质替换会失效，要重新指定。
