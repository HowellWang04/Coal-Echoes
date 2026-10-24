# 宿舍楼干净材质与开场老化

作用于主场景 WorkersDormitory。保留模型、UV、碰撞、原 GLB 和原材质，主场景编辑状态仍显示原来的斑驳外观。

## 资源与外观

干净材质和底色贴图在 `Assets/Art Resources/Environment/WorkersDormitory/Clean/`。七组材质为砖墙、混凝土、屋顶、棕色窗框、绿色窗框、灰色窗框及雨水痕。屋顶与混凝土共用干净底色，台基共用混凝土材质，因此影响八个 Renderer。玻璃、室内及其他建筑不参与。

砖墙维持灰黄砖和灰色砂浆。混凝土去掉大片泛白、污斑和裂纹底色，保留细颗粒。法线沿用现有贴图，砖墙强度由 0.65 降至 0.45，混凝土和屋顶降至 0.06。窗框颜色提高 8%，粗糙度降至 0.58；雨水痕干净版本的底色透明度为零。没有重建缺损玻璃或几何破损。

两张底色由内置 imagegen 编辑既有 `Textures/Dorm_BuffBrick_BaseColor.png` 和 `Textures/Dorm_Concrete_BaseColor.png` 得到。派生素材延续原 Industrial Harbour 素材的使用授权范围。精确提示词见 `prompts.txt`。

## 运行方式

`DormitoryMaterialAging` 挂在 Newspaper 上，`Dormitory` 指向本场景宿舍楼，`Materials` 保存原材质与干净材质的对应关系。它只在目标宿舍的子 Renderer 中替换对应材质。`NewspaperMenuUI.Show` 调用 Prepare，等待菜单期间保持干净外观，EnterPhoto 与天空和积沙共用 3.2 秒的缓动进度。

运行时保留 glTF Shader，用 `DormitoryTextureBlend.shader` 将干净与原底色按线性颜色混合到三份具有 mipmap 的 sRGB RenderTexture。只处理材质底色，不重新渲染场景。法线强度、粗糙度和颜色参数同步插值。三份底色目标按原贴图分辨率创建，当前均为 2048 平方，含 mipmap 约 64 MiB。终点直接恢复原引用，OnDisable 同样释放临时材质和纹理。

本组件对应当前 glTF 材质的底色、法线强度、粗糙度和颜色，其他贴图继续共用原材质。若后续改用独立干净法线或 ORM，需同步扩展混合逻辑。

## 配置与预览

`Setup.cs` 通过 Unity CLI eval_file 配置材质和场景引用。`Verify.cs` 渲染 clean、half、aged 三个阶段的预览，完成后恢复原材质。
