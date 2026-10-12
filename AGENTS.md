# AGENTS.md

项目介绍、目录结构和文档列表见 [README](README.md)，术语见 [CONTEXT](CONTEXT.md)。

## 工作方式

- 个人作品集项目，代码以清晰可读为先，不过度工程化。
- 场景、机制、叙事耦合紧，改动时注意影响范围。
- 动手前想清楚，有歧义就问；只写需要的代码；只改必须改的地方，保持现有风格。
- AI 负责代码和文档；需要改场景、Inspector 或资源引用时可以通过 Unity MCP 直接改，改完说明改了什么。
- 不用 Windows GUI 自动化操作 Unity，除非用户在当前对话里明确允许。
- Play Mode 测试和最终效果由用户确认，不为常规验证跑命令行编译。
- 改 git 前先问用户；提交说明用英文，推送前给用户过目。
- 完成一项工作后在[开发日志](Docs/development-log.md)里记一两句：做了什么、为什么、还差什么。做完的事从[待办](Docs/todolist.md)里删掉。

## 注意事项

- 脚本文件名必须和类名一致。移动或改名资源时 .meta 要一起移动，否则场景引用会丢。
- `Assets/Narrative/grandpa-diary-content.json` 由主场景直接引用。日记便签的 `slotId` 是存档键，定下后不要改。
- 输入改 `PlayerControls.inputactions`，`PlayerControls.cs` 是自动生成的，不要手改。
- UI 管理脚本挂在它控制的 UI 根对象上（依赖面板的开关时机），没有画面的系统放在 `Gameplay Root/Managers` 下。
- 相机后期分三份 Volume：取景器第 10 层，全屏拍照第 11 层，手持对焦第 12 层；Main Camera 的 Volume Mask 为 6145，CameraPreview 为 1025。HDRP 的光圈属于相机，不能按 Volume 混合。
- 相机调节 UI 只写取景器和全屏两份 Profile，不要改全局 Volume。
- 沙尘参数在材质上调，不在雾盒组件上调；Shader Graph 的 Time 节点在雾体积里无效，位移由 `SandstormTimeDriver` 累加。
- 改了地形或沙尘范围后，运行 `Tools > Coal Echoes > Refresh Factory Heat Haze` 并保存场景。
- 索道路线点存在场景里，改了绳索形状要重新生成。
- 改地形只改局部，不要整体重新生成 MapMagic。
- 尺度：DEM 1:1，玩家胶囊 1.8 米，眼高约 1.68 米，Main Camera 竖直 FOV 80。
- 存档都在 `Application.persistentDataPath` 下的 `CapturedPhotos/`、`ScrapBook/`、`Echo/`、`GrandpaDiary/`。
- RocksGobi、Lands、RockyDesert 三个素材包（约 10GB）不进 git，只在本机，新克隆的仓库里会显示丢失。
- `Assets/TerrainData/SheepRangeGenerated` 的地形走 Git LFS；其他二进制 .asset 会被 `*.asset text` 规则改坏，新增时要先加 LFS 规则。
- Industrial Harbour 素材重新导入时 58 个碰撞网格会提示缺法线，可以忽略。
- Blender 后台脚本可能在项目根目录留下乱码名的空文件夹，可以直接删。
- 构建场景列表还是 `SampleScene`，打包前要改。
