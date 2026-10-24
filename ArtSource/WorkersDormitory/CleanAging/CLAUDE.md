# ArtSource/WorkersDormitory/CleanAging/

## 用途

宿舍楼的干净材质，以及主菜单照片转场期间从干净外观过渡到原来斑驳外观的记录。运行代码是 `Assets/Scripts/MainMenu/DormitoryMaterialAging.cs` 和 `DormitoryTextureBlend.shader`，编辑状态下场景仍显示原材质。

## 内容

- `Setup.cs`：Unity CLI `eval_file` 脚本，配置材质对应关系和场景引用。`Verify.cs`：渲染 clean、half、aged 三个阶段的预览，完成后恢复原材质。
- `clean.png`、`half.png`、`aged.png`、`nearEnd.png`、`restored.png`：各进度的宿舍楼渲染和恢复检查。`menuClean.png`、`menuAged.png`：菜单相机视角。
- `checks.json`、`scene-checks.json`：数值和场景引用检查。
- `prompts.txt`：两张干净底色的 imagegen 提示词。

## 与其他部分的关系

- 宿舍楼是 `In-Scene Objects/工厂建筑群/01 建筑与构筑物/大型厂房/WorkersDormitory`，模型 `Assets/Art Resources/Environment/WorkersDormitory/WorkersDormitory.glb`，原材质、UV、碰撞不动。
- 干净材质和底色在 `Assets/Art Resources/Environment/WorkersDormitory/Clean/`：砖墙、混凝土、屋顶、棕色窗框、绿色窗框、灰色窗框、雨水痕七组；屋顶和混凝土共用干净底色，台基共用混凝土材质，所以影响八个 Renderer，玻璃和室内不参与。底色由 imagegen 编辑原 Industrial Harbour 贴图得到，沿用原素材的授权范围。
- 组件挂在 `In-game UI Root/MainMenuUIRoot/MenuPresentation/Newspaper` 上，Dormitory 指向宿舍楼，Materials 存原材质与干净材质的对应。`NewspaperMenuUI.Show` 调 `Prepare()`，菜单等待时保持干净，转场里与 `../../NewspaperMenu/Spread/SkyMotion/` 和 `SandAccumulation/` 共用同一进度。

## 注意

- 只在目标宿舍楼的子 Renderer 里替换对应材质，运行时保留 glTF shader，用混合 shader 把干净和原底色按线性颜色混到三份带 mipmap 的 sRGB RenderTexture，法线强度、粗糙度和颜色同步插值，不重渲染场景。
- 终点直接恢复原材质引用，OnDisable 同样释放临时材质和纹理。
- 只混合底色、法线强度、粗糙度和颜色；以后若用独立的干净法线或 ORM，要同步扩展混合逻辑。
- 没有重建缺损玻璃和几何破损，干净版只是材质。
