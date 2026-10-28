# ArtSource/Props/GroupPhoto/

## 用途

第一关 Group Photo 的制作：1980 年 10 月 3 日十二名新职工在敞开的大门前、红横幅下的合照，以及大门前重拍站位的泛光圆圈。背景从游戏场景渲染，人物由 Codex 生图加入后只把人物贴回原渲染，最后由脚本做成黑白旧照。

## 内容

- `RenderGateBackground.cs`：在场景里按 1980 年的样子渲染大门（沙丘下沉、宿舍楼干净材质、大门敞开无铁链并换上 `CleanGate/` 的新漆贴图、门卫室墙面换上同目录的新抹灰、无摩托车、无三组枯草和地形草、无天线、隐藏玩家），两根门柱间临时挂一条带褶皱和垂度的横幅，渲染完全部还原。机位、横幅贴图和输出目录在开头的设置段里改，通过 MCP `execute_code` 执行。
- `CleanGate/`：大门 1980 年的贴图（钢门、门框、铁件、砖、水泥、白灰的 BaseColor 和 ORM，钢门另有 Normal），文件名和 GLB 里的贴图同名，渲染时按名字替换。`Scripts/make_clean_gate_textures.py` 生成，沿用 `../../FactoryGate/texture_maps.py` 的噪声，去掉锈斑、剥漆、锈痕、刮痕，砖和水泥的污渍减到三分之一。`concrete_wall_007_BaseColor.png` 是门卫室墙面的 1980 版，由 `../../Guardhouse/PolyHaven/Scripts/wall_eras.py` 生成，同样按名字替换。
- `Background/`：渲染结果。`gate_d16_y5_f42.png` 是定稿（大门正中往外 16 米、眼高 1.68 米、右偏 5 度、上仰 2 度、竖直视野 42 度，3840 × 2160，新漆大门、毛笔横幅），`GroupPhoto_Background_1920.png` 是它的 1920 版，供上传；`gate_d14_y4_f46.png`、`gate_d18_y6_f40.png` 是没选的机位，还是锈门和黑体横幅。
- `Banner/`：三种横幅贴图（黑体剪纸字、宋体剪纸字、毛笔手写），定稿用毛笔手写。`Scripts/make_banner.py` 生成，字体都是 SIL OFL（Noto Sans SC、思源宋体 Heavy、Ma Shan Zheng）。
- `people-prompt.md`：给外部生图工具加人物的 prompt、上传哪张图和挑图要点。
- `Generated/`：人物生成结果，生成图都是 1672 × 941。`group-photo-v2.png` 是定稿（十二人两排，人比透视正确的大小大约 1.6 倍），`group-photo-v2-composite.png` 是它贴回原渲染后的 3840 彩色成图，也是做旧的输入。其余是没选的：`group-photo-v1*` 用的是门卫室墙面修好前的背景，`group-photo-v2-2.png` 只有十人，`group-photo-v2-3.png` 未选，`group-photo-mcp-test.png` 是换漆前的测试图。
- `Scripts/composite_people.py`：把生成图里的人物贴回原渲染。SIFT 加单应变换对齐到 `Background/gate_d16_y5_f42.png`，用人群以外的地面算一张平滑增益图把生成图校到原渲染的明暗，再按差异取人物：每一列从人群最高的差异点填到最低点，人与人之间露出的是对齐好的生成背景；脚下比原渲染暗的地面当作阴影一起贴。1920 尺度第 470 行以上一律用原渲染，横幅不会被改。
- `Scripts/make_group_photo.py`：把彩色成图做成旧照，先取画面中间 80%（等于同一站位换成竖直视野约 34 度的标准镜头，两根门柱都在，门卫室只剩右边一条），再做全色黑白片、暖调褪色、颗粒、暗角、灰尘划痕、手写日期 1980.10.3、白边泛黄、折痕；输出不透明的 1920 × 1080。`Scripts/make_placeholder.py` 是旧占位图的生成脚本。
- `Scripts/make_retake_circle.py`：泛光圆圈的贴图，参数给 `Assets/Art Resources/Props/GroupPhoto/RetakeCircle.png`，同时写出旁边的自发光贴图 `RetakeCircle_Emissive.png`。
- `Previews/`：机位和横幅字体的对比图、无人背景的做旧试样。`GateRender_Base.png` 是旧占位图用的现状渲染。

## 与其他部分的关系

- 游戏里摩托车 `GrandpaDiaryPickup` 的 `groupPhoto` 引用 `Assets/Art Resources/Props/GroupPhoto/GroupPhoto_1980.png`（1920 × 1080，Read/Write、不压缩、无 mipmap），放进相册存成 `GroupPhoto_1980.png`；提示 T1.1 按相册里有没有这张打勾。同目录的 `GroupPhoto_1980_Placeholder.png` 是旧占位图，已不被引用。
- 泛光圆圈是场景对象 `In-Scene Objects/工厂大门/Group Photo Retake Circle`，HDRP DecalProjector，材质 `Assets/Art Resources/Props/GroupPhoto/RetakeCircle.mat`，放在同级 `Group Photo Retake View` 的正下方。`Group Photo Retake View` 是重拍机位，位置和朝向是用户在 Scene 视图里对着合照手动校的，不是渲染背景用的 16 米机位，因为游戏相机竖直视野 80 度，和合照的约 34 度不同。大门 `FactoryGate` 上的 `EchoTrigger`（`group_photo_retake`）以它为站位点，按半径和朝向角度判定；`Level1HintFlowManager` 只在 T1.5 未拍成时打开圆圈和回响。
- 大门、门柱和横幅位置取自 `../../FactoryGate/` 的场景对象，新漆贴图从它的贴图生成脚本派生，大门模型或贴图生成改了要重跑 `make_clean_gate_textures.py`；宿舍楼干净材质见 `../../WorkersDormitory/`；门卫室墙面贴图改了要在 `../../Guardhouse/PolyHaven/` 重跑墙面的 `break_tiling.py`，1980 版会一起更新。

## 注意

- 重做合照按顺序跑 `Scripts/composite_people.py <选定的生成图> Background/gate_d16_y5_f42.png Generated/<名字>-composite.png`，再 `Scripts/make_group_photo.py` 输出到 `Assets/Art Resources/Props/GroupPhoto/GroupPhoto_1980.png`。贴回脚本会打印人群范围；背景重渲后和生成图差别大时，范围会跑偏，要用新背景重新生成人物。
- 生图对人物大小和人数不可靠，每次多出几张再挑，挑图看 `people-prompt.md` 末尾。
- 合照保持 16:9，贴图开 Read/Write；已拿过合照的存档里相册存的是当时的图，换图后要清存档才会换。
- 重拍机位要重新校时，在 Scene 视图对好画面后选中 `Group Photo Retake View` 执行 GameObject > Align With View，再把泛光圆圈挪到它正下方（贴花比地面高 0.4 米）；圆圈不会自动跟着走。重渲合照背景不会改重拍机位。
- 相册缩略图不带透明通道，旧照不能做异形花边。
- HDRP 贴花的自发光只取贴图颜色、不看透明度，`RetakeCircle.mat` 的 Emissive Map 要用颜色已乘透明度的 `RetakeCircle_Emissive.png`；换成 `RetakeCircle.png` 会让整个投影方块发出淡橙色的光。
