# Assets/Art Resources/Environment/Flagpole/

## 用途

宿舍楼前那根不挂旗的老旗杆，Unity 用的模型、贴图、材质和预制体。

## 内容

- `Models/Flagpole.fbx`：基座、杆子、绳子、积沙四个网格，导入时材质按名字重映射到 `Materials/` 和沙丘材质。
- `Textures/`：`Flagpole_Plinth_` 和 `Flagpole_Pole_` 两套 BaseColor、Normal、HDRP_Mask。
- `Materials/`：`Flagpole_Concrete`（基座）、`Flagpole_PaintedSteel`（杆子和铁件）、`Flagpole_Rope`（绳子，无贴图），都是 HDRP/Lit，开了 Receive Decals。
- `Flagpole.prefab`：FBX 的变体，根节点带两个 BoxCollider（下级台阶、上级基座）和一个包住杆子与羊角扣的 CapsuleCollider，`Flagpole_Sand` 带 MeshCollider。

## 与其他部分的关系

- 源文件、生成脚本和导入脚本在 `ArtSource/Flagpole/`。积沙用 `../SandDune/Materials/SandDune.mat`。
- 场景里是 `In-Scene Objects/工厂建筑群/01 建筑与构筑物/旗杆/Flagpole`，第二关宿舍楼正门前的空地上。

## 注意

- 两个烘焙材质的 Base Color 不是白色，是压暗的暖灰，重跑 `ImportFlagpole.cs` 会写回同样的值。
- 原点在地面、杆子正下方；积沙一面是本地 +Z，摆放时让它对着迎风面。
