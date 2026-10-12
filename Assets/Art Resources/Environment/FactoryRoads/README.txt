厂区旧柏油路资源

Asphalt02 内的 Asphalt02.mat 是 HDRP Lit 底材，配置了 4K 颜色、法线和 Mask。原始扫描覆盖约 3 米乘 3 米，路面网格 UV 每 3 米重复一次。保留高度图，默认不启用位移。

SullyCracksDamage 提供 15 套裂缝破损贴花。
G87RoadRepair 提供 85 套补丁接缝贴花及 7 套由 Netlane 图案转换的贴花。
每个图案文件夹有 Decal.mat 和 Decal.prefab。把预制体拖入场景并放到路面高度，通过 Decal Projector 的 Size 调节范围。投影默认朝下，深度 0.5 米，覆盖尺寸沿用原始配置。Netlane 仅导入矩形贴花，没有沿曲线铺设行为。

颜色图使用 sRGB，法线使用 Normal Map，Mask 和 Height 使用线性采样。柏油 Mask 的 RGBA 分别为 Metallic、AO、全白 Detail Mask 和反转 Roughness 得到的 Smoothness。
模组贴花用颜色 alpha 控制透明度，Mask 只参与 Smoothness，光滑度上限沿用原配置。近景强度需结合实际场景调整。

Poly Haven Asphalt 02 采用 CC0。两个模组包内均未找到授权文件，跨游戏复用授权尚未确认，目前作为本地评估素材。未导入模组 DLL 或运行时代码。
原始文件、来源与验证结果在项目 ArtSource/FactoryRoads 目录。
