# ArtSource/Props/

## 用途

第一关用到的小件道具模型，都是后台 Blender 脚本生成的原创资产。

## 内容

- `LockPick/`：从摩托车储物箱拿到的撬锁工具，撬门卫室的门。
- `BoltCutter/`：门卫室里的断线钳，剪大门铁链。
- `OldPhotoFrame/`：门卫室墙上的旧木相框，街市照片回响的载体，含归档的照片原图。
- `GuardhouseFurniture/`：门卫室的值班桌和椅子，以及门卫室室内陈设的摆放脚本。
- `DeskLamp/`：门卫室桌上摔坏的军绿色台灯，灯罩掉在桌面上。
- `GroupPhoto/`：Group Photo 合照的场景背景渲染、横幅、人物生成 prompt 和做旧脚本，以及重拍站位的泛光圆圈贴图，不是模型。
- 各目录详见各自 CLAUDE.md。

## 与其他部分的关系

- 预制体在 `Assets/Art Resources/Props/` 下同名目录，HDRP/Lit 独立材质，`Model` 子对象以底面中心为轴心。相框、桌椅、台灯和断线钳都在 `In-Scene Objects/工厂大门/Guard Post/室内陈设` 下；撬锁工具不摆实物，从摩托车直接进背包。
- 关卡流程见 `Docs/design.md` 第一关和 `Docs/Specs/Levels/opening-level-one-spec.md`；摩托车见 `../DustyMotorcycle/`，大门铁链见 `../FactoryGate/`。

## 注意

- 撬锁工具、断线钳、相框、台灯的预制体都没有碰撞体，桌椅预制体带 BoxCollider；断线钳的场景实例另加了 BoxCollider 和拾取组件；撬锁工具和断线钳已有背包物品资源和缩略图，放在 Assets 下各自目录。
- 贴图约定一致：HDRP_Mask 的 AO 和细节遮罩通道全白，Normal 用 OpenGL 约定，Blender Z 朝上，导出 Y 朝上。
