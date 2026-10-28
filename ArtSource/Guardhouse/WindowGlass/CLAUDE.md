# ArtSource/Guardhouse/WindowGlass/

## 用途

门卫室三扇窗和门上亮子的玻璃：屋里看是透明的积灰旧玻璃，能看到厂门和沙尘天；屋外看只有灰尘和反光，看不进屋。

## 内容

- `make_window_glass.py`：numpy 生成两张可平铺的 1024 贴图写到 Unity 的 WindowGlass 目录。颜色贴图 RGB 是灰尘色盖在玻璃色上、A 是不透明度；HDRP 遮罩 A 是光滑度（R 金属 0、G AO 1、B 细节 0）。灰尘由频谱滤波的周期噪声叠成：大块不均、细碎斑驳、竖向水痕和沙粒点，灰尘越厚越不透明、越粗糙。
- `build-window-glass.cs`：Unity MCP execute_code 的一次性脚本。把 `Guardhouse_Shell` 渲染器上 glb 自带的 `GH_DustyWindowGlass` 槽换成 `GH_WindowGlass`，建 `GH_WindowBlocker`，在场景里建 `Guard Post/Window Glass`（四片遮挡面片和一个反射探针）。窗洞的位置和大小从玻璃子网格算，重跑会重建这组对象、刷新两个材质。
- `render-previews.cs`：临时相机渲染屋内三张、屋外三张预览到 `Previews/`，先刷新探针。
- `Previews/`：预览截图。

## 与其他部分的关系

- Unity 资源在 `Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/WindowGlass/`：两张贴图，`GH_WindowGlass`（HDRP/Lit 透明、Alpha 混合、单面，颜色和遮罩贴图走窗格自带 UV），`GH_WindowBlocker`（HDRP/Lit 不透明、近黑、中等光滑，不用贴图）。
- 场景对象 `Guard Post/Window Glass`，变换和 `Guardhouse_Rebuilt` 相同（两者 Y 缩放都调大过，让门卫室屋顶和围墙顶齐平），下面是 `Service Window Blocker`、`Observation Window Blocker`、`Right Window Blocker`、`Transom Blocker` 四片 Quad 和 `Guardhouse Reflection Probe`。面片贴在每扇窗玻璃屋内一侧 2 厘米处、在窗框进深以内，法线朝外，只在屋外看得见，不投影、没有碰撞体。探针是实时、启用时拍一次的盒形探针，盒子包过玻璃，过渡 0.1 米。
- 玻璃槽的替换和墙面换扫描贴图一样做在场景渲染器上，glb 里的 `GH_DustyWindowGlass` 没动。Blender 里给它设的透射导出 glb 时带不过来，glTFast 导进来是不透明的，所以不能直接用 glb 的材质。
- 窗格 UV 是窗局部坐标乘 0.55，一张贴图铺 1.82 米，各窗格各取一块，灰尘的位置和窗格边没有对应关系。
- 取物遮挡检测打在 `Guardhouse_Shell` 的 MeshCollider 上，玻璃改透明不影响隔玻璃拿不到东西。

## 注意

- 玻璃材质开 Alpha Clipping 只是为了让阴影通道用自己的阈值：主通道阈值 0 不裁，阴影阈值 1 把整块玻璃从阴影里裁掉，阳光才进得了屋。关掉 Alpha Clipping 或去掉阴影阈值，屋里就回到没有阳光的一片灰。
- 遮挡面片的投影必须关着，不然它替玻璃挡住阳光。
- 重导 `Guardhouse_Rebuilt.glb` 后如果子网格顺序或材质名变了，玻璃槽的替换会失效；改了窗的位置或大小，面片也对不上。两种情况都重跑 `build-window-glass.cs`，重跑后把 `Window Glass` 的缩放改回和 `Guardhouse_Rebuilt` 一致。
- 改了门卫室里的家具或墙面，关开一次 `Guardhouse Reflection Probe` 刷新反射。
- 工程里 SSR 关着，玻璃的反射全靠这个探针；探针淡出的地方 HDRP 退回天空反射，盒子不要缩到玻璃以内。
- 屋里那面的积灰不明显，因为只受屋里的暗光；想更脏改 `make_window_glass.py` 里 alpha 的范围重跑。Play Mode 观感没核过。
