# 技术栈

## 引擎与渲染

- Unity 6000.4.6f1，HDRP 17.4.0
- Shader Graph、VFX Graph
- HDRP Local Volumetric Fog 做沙尘，Custom Pass 做热浪
- 主菜单报纸照片：自写 HLSL 半调网点 Shader（网点部分改写自 Daniel Ilett Halftone Shader，MIT），HDRP Custom Pass 抓取相机画面
- 开场天空旋转：运行时 Volume 覆盖 HDRI Sky 朝向
- 宿舍楼材质老化：GPU RenderTexture 混合干净与斑驳贴图
- 地形去重复：HDRP TerrainLit Shader Graph 加自写 HLSL，Hex Tiling 采样并在远处混入放大采样

## 地形与场景

- MapMagic 管理地块与 LOD，高度来自 USGS Sheep Range 1 米 DEM
- Gradient Terrain Authoring 离线求解地形过渡（C++、OpenGL、Python）。论文 Guérin et al., “Gradient Terrain Authoring”, Computer Graphics Forum 41(2), Eurographics 2022；代码来自作者仓库 eric-guerin/gradient-terrains，MIT 许可
- Terrain Tools 5.3.2
- Splines 2.9.1 做厂区土路路线
- 开场积沙：运行时复制 TerrainData 做高度动画
- 场景素材包：Industrial Harbour、RocksGobi、Lands、RockyDesert、Military VOL4 Furniture、Terrain Sample Assets、URP Tree Pack、beton stair
- 地形贴图：ADG ground vol1，Poly Haven 的 gravelly sand、rocky trail、aerial ground rock，ambientCG Ground104

## 玩法与 UI

- Input System 1.19
- TextMesh Pro，报纸菜单用 Bitter Regular、思源宋体 Medium、马善政楷书，烘焙静态 TMP 字库
- DOTween 免费版做 UI 动效
- UIEffect 5.11.7 做溶解和翻页光影
- JsonUtility 读写存档和日记数据

## 美术工具

- Blender 5.2.2（Steam 版），Python 脚本参数化建模并烘焙贴图，模型导出 FBX 或 GLB
- glTFast 6.20.0 导入 GLB 模型
- 品牌 Logo：Python 脚本用 Pillow 和 fontTools 从字体生成轮廓与透明图
- 天空 HDRI：Poly Haven Kloofendal 48d Partly Cloudy Pure Sky（CC0）

## AI 协作

- Claude Code（Claude Opus 5.5）与 Codex（GPT 6 Astra，high 推理）写代码和文档，共用 AGENTS.md 规则，进度记在仓库文档里，两边可以接着做
- Unity MCP（CoplayDev/unity-mcp v10.2.0）：AI 直接改场景、Inspector 和资源引用

## 版本管理

- Git 与 Git LFS
- 地形数据、DEM、主场景、.blend、字体和大部分贴图模型走 LFS
- RocksGobi、Lands、RockyDesert 三个素材包（约 10GB）只在本机，不进 git

## 许可与来源

| 内容 | 许可 |
| --- | --- |
| Halftone Shader 网点部分、gradient-terrains | MIT |
| Kloofendal HDRI、Poly Haven 与 ambientCG 地形贴图 | CC0 |
| Bitter、思源宋体、马善政楷书 | SIL OFL |
| DOTween 免费版 | Demigiant 免费许可 |
| UIEffect | MIT |
| 场景素材包 | 各自商店许可 |
