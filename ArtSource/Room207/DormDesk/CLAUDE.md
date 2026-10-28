# ArtSource/Room207/DormDesk/

## 用途

207 室窗下的宿舍两屉桌和它半拉开的左抽屉，山路图收在这只抽屉里。照门卫室桌椅的做法在后台 Blender 里建模、烘焙漆面，红棕漆掉漆积灰，和门卫室成套。

## 内容

- `Scripts/build_dorm_desk.py`：后台 Blender 建模、烘焙、导出 FBX、渲染预览，约一分钟。烘焙材质和 Piece 工具照抄 `../../Props/GuardhouseFurniture/Scripts/build_furniture.py`，多了一种不刷漆的原木，用在抽屉箱、隔板和滑条上。`build.log`、`verification.json` 是运行输出，后者记着抽屉内底高度和内腔范围。
- `Blender/DormDesk.blend`：源文件，贴图已打包，PreviewStudio 集合不导出。
- `Exports/`：`DormDesk.fbx` 是桌子，右抽屉关着、只有面板；`DormDeskDrawer.fbx` 是整只左抽屉，按关着的位置建模。
- `Textures/`：两件各一套 BaseColor、Roughness、Metallic、Normal 和 HDRP_Mask。
- `Previews/`：整体和抽屉特写两张 Cycles 渲染，抽屉拉开的距离和场景里一样。
- `UnityIntegration/ImportDormDesk.cs`：设贴图导入，生成两个 HDRP/Lit 材质，不做预制体、不动场景。
- `UnityIntegration/PlaceDormDesk.cs`：删掉 207 里原来的桌子和抽屉，摆上这张桌子，拉出左抽屉，放 `Map Spot`；不保存场景。

## 与其他部分的关系

- Assets 在 `Assets/Art Resources/Environment/Room207/DormDesk/` 的 `Models/`、`Textures/`、`Materials/`。
- 场景里是 `207/Furniture/Table`：根节点带 BoxCollider，只包桌体、不包拉出的抽屉，射线能直接打到抽屉里的纸片；子对象 `Model` 和 `Table Drawer`，后者下面是 `Model` 和 `Map Spot`。桌子背靠窗墙，居中在两张上下铺之间，抽屉朝屋里，拉开的是面向窗户时左手边那只。
- `Map Spot` 在抽屉露出桌外那一段的底板上，朝向和 207 一致（+Z 朝窗）。山路图纸片 `207/Mountain Road Map` 按它的位置和朝向背面朝上摆，不挂在它下面，见 `Docs/Specs/Levels/level-two-spec.md` 山路图一节和 `../../Props/MountainRoadMap/`。
- 207 的其余部分见 `../CLAUDE.md`。

## 注意

- 两件 FBX 和门卫室的一样，导入后根节点缩放 100、旋转 270，实际尺寸是对的；桌子正面在模型 +Z，所以 `Table` 转了 180 度。
- 抽屉模型的原点和桌子相同，拉开多少只改 `Table Drawer` 的局部位置，`Map Spot` 跟着走；改了拉开距离，要把纸片挪到新的 `Map Spot`，再核对它露在桌外的部分。桌面上的 `207/Desk Spot` 不在 `Table` 下，重建桌子不受影响，但桌子挪了位置要跟着挪。
- 窗台板比桌面高几厘米，桌子后沿伸到窗台下面不穿插；改桌高要看窗台。
- 改了 `build_dorm_desk.py` 要重跑，再把两个 FBX 和三张贴图（BaseColor、Normal、HDRP_Mask）拷到 Assets 下，跑 `ImportDormDesk.cs`；只改摆放就跑 `PlaceDormDesk.cs`，它会重建 `Table`，`Map Spot` 也跟着重建，挂在上面的组件会丢。
