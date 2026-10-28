# ArtSource/Flagpole/

## 用途

宿舍楼正门前那根不挂旗的老旗杆。照八十年代厂区的样式在后台 Blender 里建模、烘焙：两级方形砖砌抹灰基座，三段变径钢管，顶上圆球和侧挂滑轮，杆身两个羊角扣；铝粉漆剥落生锈，升旗绳一边断了吊在半空、一边松垂地系在羊角扣上，基座开裂掉角、露出砖，迎风一面积沙。只做环境叙事，没有交互。

## 内容

- `Scripts/build_flagpole.py`：后台 Blender 建模、烘焙、导出 FBX、渲染预览，约三分钟。烘焙材质的写法照抄 `../Room207/DormDesk/Scripts/build_dorm_desk.py`，换成油漆钢管、锈铁、褪色金漆圆球和抹灰砖四种。钢管按 1.2 米一段自己展 UV，其余零件用 Smart UV，再统一密度打包。`build.log`、`verification.json` 是运行输出，后者记着各件尺寸、面数和关键高度。
- `Blender/Flagpole.blend`：源文件，贴图已打包，PreviewStudio 集合不导出。
- `Exports/Flagpole.fbx`：一个 FBX 四个网格，`Flagpole_Plinth` 基座、`Flagpole_Pole` 杆子和全部铁件、`Flagpole_Rope` 绳子、`Flagpole_Sand` 积沙。
- `Textures/`：基座和杆子各一套 BaseColor、Roughness、Metallic、Normal 和 HDRP_Mask。绳子和积沙不烘贴图。
- `Previews/`：`Flagpole_` 开头的是 Blender 渲染（整体、基座、顶部滑轮、羊角扣），`Unity_` 开头的是场景里编辑模式的截图，`Unity_GateToDorm` 是从大门内看宿舍的检查图。
- `UnityIntegration/ImportFlagpole.cs`：设贴图导入，生成三个 HDRP/Lit 材质，给 FBX 做材质重映射，存带碰撞体的预制体；不动场景。

## 与其他部分的关系

- Assets 在 `Assets/Art Resources/Environment/Flagpole/`，用法见那里的 CLAUDE.md。积沙用 `../SandDune/` 的 `SandDune.mat`，UV 按米铺，和前院沙堆一样。
- 场景对象是 `In-Scene Objects/工厂建筑群/01 建筑与构筑物/旗杆`：子对象 `Flagpole` 是预制体实例，另有四簇 RockyDesert 包的枯草，一簇从背风面台阶缝里长出来，三簇在台阶脚下。旗杆在宿舍楼正门轴线上、离门约 12 米，离大门到宿舍门的直线路线约 7.7 米，不碰 `Dorm Entrance Zone`、`Dorm Entrance Lock` 和门前的水泥袋托盘。
- 旗杆朝向是宿舍楼朝向加 180 度，积沙一面对着宿舍楼，也就是迎风面；前院风向见 `../SandDune/CLAUDE.md`。

## 注意

- 尺寸：顶端圆球离地 10.01 米；下级台阶 2.3 米见方，顶面 0.18 米，往地下埋 0.07 米；上级 1.4 米见方，顶面 0.58 米；钢管 114、89、60 毫米三段，变径在 4.2 米和 7.6 米；滑轮 9.74 米，羊角扣 1.56 米和 1.26 米，断绳下端约 6.1 米。
- FBX 导入 Unity 后整体绕竖轴转了 180 度：Blender 的 -Y 是 Unity +Z，-X 是 Unity +X。Blender 里积沙在 -Y、绳子在 -X，摆场景时按 Unity 的轴算。
- 基座和杆子材质的 Base Color 压了一层暖灰，不压的话在前院地面上偏亮偏干净。重烘贴图后先看场景里的效果再定这层颜色。
- 积沙的 MeshCollider 让玩家走上沙坡；迎风面沙堆到上级基座半腰，玩家能从那里迈上基座顶面，但穿不过杆子。
- 开场的积沙升起动画 `FactorySandAccumulation` 不含这堆积沙，它需要 LODGroup，旗杆没有。
- 改了 `build_flagpole.py` 要重跑，再把 FBX 和两套 BaseColor、Normal、HDRP_Mask 拷到 Assets 下，跑 `ImportFlagpole.cs`；它会重存预制体，场景实例跟着更新。Play Mode 里的观感和走动待用户确认。
