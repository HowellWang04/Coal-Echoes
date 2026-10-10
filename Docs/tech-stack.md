# 技术栈

## 引擎与渲染

- Unity 6000.4.6f1
- HDRP 17.4.0（2026 年 9 月从 URP 迁移）
- Shader Graph、VFX Graph
- HDRP Local Volumetric Fog 用于沙尘，Custom Pass 用于热浪

## 玩法与 UI

- Input System 1.19
- TextMesh Pro
- DOTween 免费版用于 UI 动效
- UIEffect 5.11.7 用于溶解及翻页光影
- JsonUtility 用于存档及日记数据读写

## 地形与场景

* MapMagic 生成地形。USGS Sheep Range 1 米 DEM 按实际尺度导入。
* Gradient Terrain Authoring 重建过渡。C++ 与 OpenGL 执行离线求解，Python 处理数据、精化求解并验证结果。
* C# 节点 `TerrainHeightDelta` 将高度差接入 MapMagic。地形输出与坡度材质使用同一高度源。
* `MapMagicTerrainTextureSync` 在编辑器内按最终高度同步局部材质。
* Terrain Tools 5.3.2 用于地形编辑。
* 场景素材采用 Industrial Harbour、RocksGobi、Lands、RockyDesert。

仅允许局部修改地形。高度修改后，必须同步 Main 与 Draft 的材质及边缘缓存，刷新热浪并保存场景。

接入与操作见[地形应用记录](../TerrainReferences/WholeMapGradient800/README.md)和[材质同步说明](../TerrainReferences/TerrainTextureSync/README.md)。

## 美术工具

- Blender 5.2.2 Steam 版用于建模，Python 脚本用于参数化生成
- 天空 HDRI 来自 Poly Haven（Kloofendal 48d Partly Cloudy Pure Sky，CC0）

## AI 协作

- Unity MCP v10.2.0 与 Unity AI Assistant 用于场景操作及 Console 检查
- Blender MCP
- Git 与 Git LFS 用于版本管理
