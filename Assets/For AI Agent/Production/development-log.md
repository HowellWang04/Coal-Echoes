# 开发日志

[查看 Todo List](todolist.md)

[查看代码结构门户](../Technical/code-structure.md)

本日志按日期记录实际完成内容、设计决定、测试结果和下一步。未完成任务仍以 Todo List 为准。

## 2026-10-01

### 线距4米、8米进站支架与山脚回转站

用户确认后把索道线距由6米改为4米。塔架脚本的横梁半长改为2米参数，10座15米标准塔重新生成并替换。1号塔改为同一脚本生成的8米进站支架，去掉外伸平台，只保留4.15米检修平台，加长腿按其底宽重新计算。站棚与站口滚轮架沿线路后移22米，滚轮架距1号塔37米，缆线由15米经8米降到站口约4.85米。

站棚屋面钢梁只有4.1至4.7米，低于站口缆线高度，回转轮放不进棚内。因此站棚向侧面让开3.7米，敞开侧朝向新的回转站。滚轮架复制为两组，分别位于两条线下方。新建CablewayReturnTerminal，包括直径4米的水平回转轮、钢支架、减速箱与电机、混凝土基础，以及一圈U形转运轨道和9根悬臂立柱，轮心在滚轮架后8米。

吊斗按真实构造重做为CablewayCoalBucket，含抱索器、可上轨道的小车轮、吊架、翻斗销轴与锁扣。用户要求再大一些后定为约1立方米，斗体长1.5米、宽1.1米、深0.9米，总高2.68米，按尺寸重建而非缩放。两份塔架预制体的吊斗同步替换，11个吊斗挂在缆线上距各塔8米处。

两条缆线在站内连成一圈，经回转轮远侧半圈和两组滚轮架接到各塔滑轮。垂度改为跨距的约2.2%，短跨0.3米起，长跨不超过3.5米。全线缆线最低离地2.94米，位于5号与6号塔之间。

用户反馈梯子从平台小方洞穿出空间过小。改为侧出式：平台前沿收在梯子内侧，护笼延伸到平台面以上1米，前栏杆留0.6米开口，梯子扶手高出平台1.1米。15米塔与8米进站支架同时更新。

主场景已保存，Console无新增错误。期间Unity编辑器因长时间导入出现大量Resource ID out of range报错，重启后消失。未运行Play Mode。站棚新位置地面高约3.6米且有坡，局部埋入地面，留待后续整平。用户观察到游戏中吊斗显小，原因判断为Main Camera竖直FOV为80，用户决定暂不调整。脚本与备份位于[Tajia02_Metric15](../../../../ArtSource/Cableway/Tajia02_Metric15/README.md)。

## 2026-09-30

### 真实尺寸塔架、地形恢复与高低腿基础

核对比例：USGS 1米DEM按1比1导入，玩家胶囊1.8米。眼高原为离地1.73米，Main Camera本地Y由1.75改为1.70，离地约1.68米。旧塔架实际高12.6米，但螺栓头约14厘米、连接板约0.6米、梯子横档间距0.35米，零件比例偏大，人眼看去像放大的模型。

新写参数化脚本按米制重建塔架，场景缩放为1。总高15米，6节塔身，主柱L160角钢，M20螺栓，梯子横档间距0.297米、净宽0.45米，2.4米以上设护笼，平台栏杆1.05米。贴图沿用Tajia01旧塔锈蚀钢材条带按构件方向取样。原版右侧竖向拉杆位于吊斗通行路线上，予以去除。旧缆线一直停在31米旧塔高度，同时重新生成。

11个塔基平台原为写入高度图RAW的圆形平地，最深处与原始地形相差约18米。只把这11块区域的高差写回三块地形的Main与Draft，未整体重生成MapMagic，接缝差为零，11个塔基锁解锁，RAW、Matrix与图导入版本同步更新。

塔基改为高低腿做法：塔身水平坐在上坡腿上，下坡腿用同规格角钢顺原斜度加长，最长约3.3米，加长超过0.6米时增加斜撑。每腿一个0.9米见方、3.5米深的水泥墩，墩顶一般比墩中心地面高0.4米，陡坡处保证上坡角不被埋。梯子同样向下接到独立小墩。首次装配时因FBX导出X轴镜像导致加长腿装反，已修正并核对每段加长腿正好落在墩顶。各塔加长量记录在leg_extensions.json。

## 2026-09-28

### 山脚始发站落位与厂区地形恢复

按用户授权，通过 Unity 编辑器在第一座塔前方的线路侧边放置 CablewayStationShell 与 CablewayStationRollerFrame，归入 CablewayFoothillStation。站棚敞开侧朝线路，滚轮排列方向平行于线路，保留模型原始尺度。仅处理山脚始发站，未新增绳索或碰撞配置，未平整站台附近地形。站棚占地内原地面高差约0.425米，地板高于最高地面，基座底部最大离地约0.055米。滚轮架基础嵌入地面约0.08至0.12米，两件模型与第一座塔的Renderer包围盒不相交。

撤销此前厂区545米乘380米矩形及90米过渡范围的平整。利用原USGS高度图与历史裁切、海拔换算记录恢复输入高度图，再将恢复差值应用于对应四块地形的Main和Draft数据。保留塔脚36米保护范围、其他地形叠加和全部建筑变换，未触发MapMagic整体重生成。八份TerrainData发生变化，另外51份地形记录哈希不变；恢复掩码外高度采样零变化，受影响块接缝一致，量化误差不超过0.015274米。源高度RAW、Matrix资源和图导入版本同步更新，恢复前场景与地形数据完整备份。

主场景已保存，17个Renderer网格与HDRP材质有效，11座塔、两条既有线路和厂房群变换保持不变，放置模型后地形哈希仍一致。Console零错误、零警告，无编译失败，未运行Play Mode。原塔架和吊斗尺度仍需后续与站台及绳索通行高度一起校准；恢复坡面后的厂房贴地问题留待局部调整。详见[放置及恢复记录](../../../../ArtSource/Cableway/FoothillStationPlacement/README.md)。

### 独立站口滚轮架建模与导出

按用户确认的概念造型，用Steam版Blender 5.2.2后台Python制作CablewayStationRollerFrame，未使用MCP。模型包含敞开门架、上部斜撑、螺栓连接、独立混凝土基础，以及沿线路前后排列的三只独立槽轮。槽轮原点位于各自轮轴，可分别编辑和绕局部X轴旋转，未制作动画。

模型共11个网格、30,028个三角面，名义尺寸为宽3.62米、沿线路深2.52米、高4.835米。三套2K材质贴图加一套简单边缘氧化材质，钢材采用与站棚协调的灰米色和褐色锈斑，混凝土贴图复制自站棚。保存Blender源文件、FBX、GLB和实际渲染预览。源网格无非流形边和零面积面，两种导出重新导入后尺寸、面数、UV、贴图加载与轮轴原点通过核验，另完成GLB同机位渲染比较。详见[模型说明](../../../../ArtSource/Cableway/CablewayStationRollerFrame/README.md)和[核验报告](../../../../ArtSource/Cableway/CablewayStationRollerFrame/verification.json)。

当前仅交付独立静态模型。未修改Unity场景、配置碰撞或验证Play Mode。现有场景验证记录中吊斗高约12.37米，与约4.93米的站棚尺度不一致，装配时需重新校准，并检查承载索、牵引索、吊架与门架横梁的通行关系。

用户随后要求下方架体与滚轮排列方向平行。已将下方双柱架、横梁、斜撑、地脚及其连接件整体转向90度，保留轮组方向及独立轮轴。当前整体尺寸为横向0.88米、沿线路3.62米、高4.835米，面数不变。模型明确不含绳索，留待后续单独配置连接。源文件、FBX、GLB和实际渲染已更新，两种导出均检查了支架与轮组的平行关系。

## 2026-09-27

### 清理旧 suodao-S 与 house 1

通过 Unity 编辑器删除主场景中的两个 suodao-S 实例并保存场景。house 1 在清理前已没有场景实例。扫描其余3525个资源确认没有外部依赖后，删除两个旧FBX、Material.001、Material.002、Material.009三份材质，以及对应六张颜色和法线贴图，共11个资源及对应meta。

Assets内旧模型文件名、场景对象名和已删GUID引用均为零。与删除前编辑器场景副本比较，仅移除四个实例及变换序列化块，并从索道群父级列表移除两个引用，其他对象块保持一致。Console无新增错误，无编译失败，未运行Play Mode。新CablewayStationShell的Blender源文件与FBX、GLB哈希和最近保存记录一致，历史场景快照保留。详见[清理核验](../../../../ArtSource/Cleanup/LegacyCablewayRemoval/verification.json)与[删除清单](../../../../ArtSource/Cleanup/LegacyCablewayRemoval/removal.json)。

### 删除 SM_Rails 轨道模型与全部场景实例

通过 Unity 编辑器删除主场景中的78个 SM_Rails 实例，以及四个 Industrial_Harbour 示例场景中的80个实例，共158个。包含实例的 SM_Rails 分组一并删除，五个场景均已保存。当前 SmokePortal_Test 编辑场景保持原状。

检查其余3536个资源没有依赖后，通过 AssetDatabase 删除 SM_Rails.fbx、SM_Rails.prefab、M_Rails.mat、两份 MI_Rails.mat 和 T_Rails 的颜色、法线、ORM贴图，共8个资源及8份meta。独立的 M_DirtRails_3.mat 和 T_DirtRails_B.PNG 仍被示例场景的污渍贴花使用，予以保留。

Assets内SM_Rails文件名、场景对象名和已删资源GUID引用均为零。与删除前的编辑器场景副本逐对象比较，只有轨道对象块被移除及父级子对象列表发生变化，无新增对象块。Console有首次只读审计的5秒主线程超时记录，延长等待后操作成功，无编译失败。未运行Play Mode。清理范围为当前项目资源和场景，ArtSource内既有历史场景快照保持原样。详见[场景清理记录](../../../../ArtSource/Cleanup/RailRemoval/scene_removal.json)和[资源删除记录](../../../../ArtSource/Cleanup/RailRemoval/asset_removal.json)。

### 索道站棚紧凑版本建模与导出

通过 Steam Blender 5.2.2 LTS 的 MCP 连接完成 CablewayStationShell。以用户确认的紧凑灰模继续制作，仅一侧长墙包板，保留单坡残缺屋顶，另一侧及两端敞开。按反馈移除敞开侧的全部腰栏杆和小立柱，保留全高承重柱与顶部斜撑。

完成灰米色钢材、赭色锈迹、波纹板和混凝土底座表面，调整钢材纹理方向与混凝土UV比例，底座增加轻微倒角。最终约长11.35米、深3.66米、高4.93米，共20,308个三角面，六个网格、三套2K材质。这些是制作尺寸，不是参考图片的实测尺寸。

源网格无非流形边和零面积面。FBX与GLB重新导入后网格数、面数、UV和贴图加载均通过检查，尺寸最大偏差小于0.000002米。修复打包图像缓存导致的旧贴图导出问题，GLB同机位渲染平均RGB绝对差约0.000024。源文件、导出、实际渲染和检查结果位于[站棚资源目录](../../../../ArtSource/Cableway/CablewayStationShell/Design.md)。旧大体量棚房版本单独归档。未修改Unity模型引用或场景，未进行碰撞配置与Play Mode验证。

### 全部索道塔架替换并清理旧模型

通过 Unity 编辑器将其余10座旧塔架替换为最新 CablewayTower_Rebuilt 预制体。主场景11座塔架均使用同一重建组合，分别包含 CablewayTower 和 CoalBucket。保留每座根对象的父级、位置、旋转和缩放。新增实例的子对象按原塔架高度及塔底中心校准，替换前后高度约31.56555米，测得塔底偏差为零。原第09座保持不变。

Unity 扫描3543个资源，确认旧模型没有剩余依赖后删除 CablewayTower_Legacy_01、02、03 三份FBX及对应.meta。同时删除ArtSource归档中的8份旧版塔架模型，保留最新塔架与吊斗的Blender源文件及FBX、GLB导出。历史图片、贴图和检查记录保留。

已保存主场景，保存后无未保存标记。11座塔架共22个Renderer的网格与材质引用有效，塔架无Missing Script。逐对象比较修改前场景，仅10个PrefabInstance块及其10个Transform引用块变化，36个MapMagic预览距离缓存已恢复，其他场景数据一致。Assets内旧模型GUID引用为零。Console新增一条Pipeline请求超时记录，发生于全项目依赖扫描；操作随后完成且已通过独立文件和编辑器核验，无编译失败。未运行Play Mode，未进行画面验收。

详见[替换检查](../../../../ArtSource/Cableway/Tajia01/UnityIntegration/all_towers_verification.json)与[静态比较](../../../../ArtSource/Cableway/Tajia01/UnityIntegration/all_towers_static_check.json)。

### 索道塔架英文命名

11 座场景塔架按索道路线顺序统一为 CablewayTower_01 至 CablewayTower_11，地基标签使用 CablewayTowerFooting 加相同编号。重建组合为第09座，资源目录和预制体统一为 CablewayTower_Rebuilt，塔架与吊斗子对象、材质分别为 CablewayTower 和 CoalBucket。旧 FBX 使用 CablewayTower_Legacy 加原模型编号。

资源配对移动原有 .meta，FBX 与 .meta 字节保持一致。与修改前磁盘副本比较，主场景只有22处名称行变化，预制体与材质只改显示名称，未修改变换或引用。Unity Console、重新导入后的显示和 Play Mode 尚未验证。制作源文件、历史采样与历史脚本保留原名称，复用时应按[命名对照](../../../../ArtSource/Cableway/Tajia01/Naming.md)调整查询和路径。

## 2026-09-25

### 塔架对照旧模型细修锈斑形状

按用户近景参考，取tajia03原始颜色贴图内连续的旧钢材区域，保留灰白磨蚀与赭黄锈斑形状。按每根构件主轴重新铺设纹理，加入宽缓暖褐变化，烘焙至现有4K图集。近景发现整塔最近表面转移会在斜撑产生条带拉伸，因此最终改用构件局部映射。程序化碎锈版本归档于ArtSource/Cableway/Tajia01/Previous/Before_Legacy_Surface_Transfer。

同步更新Blender源文件、FBX、GLB与Unity三张贴图，法线恢复较轻的0.45强度，材质颜色乘数对齐旧塔架约0.864。几何与UV哈希和上一版一致，仍为48,776个三角面。FBX与GLB重新导入的网格、UV、尺寸检查通过，同机位GLB渲染平均RGB绝对差约0.00002944。

已查看Unity实际Scene近景，确认三张贴图均为4096分辨率且颜色空间正确，塔高31.289398米，其余10座旧塔架仍未替换。没有新增Console错误，未运行Play Mode。当前材质核对与截图保存在ArtSource/Cableway/Tajia01/UnityIntegration。

### 塔架增加侵蚀贴图细节

按用户要求，在已确认灰米褐底色上增加局部碎锈、断续边缘侵蚀、接缝积锈和细小蚀坑。使用几何曲率、原有AO与多尺度噪声限制分布，并在近景检查后降低均匀细锈点覆盖，保留完整钢材区域。

更新4K颜色、法线、金属度、粗糙度和侵蚀遮罩，重新打包HDRP Mask与ORM，同步更新Blender源文件和导出。几何、UV与AO保持不变。蚀坑为贴图细节，未增加面数。原法线强度合入新烘焙图，塔架Unity法线强度改为1，继续保持金属度上限0.414和哑光设置。修改前文件归档在ArtSource/Cableway/Tajia01/Previous/Before_Corrosion_Detail。

### 塔架改用旧资源配色

用户在批量替换前提供旧塔架截图，要求改用旧配色。读取tajia03实际HDRP材质和颜色贴图，从灰米褐色钢材与赭色锈迹采样，重新烘焙新塔架颜色图。更新Blender源文件、FBX、GLB和实际预览，网格、UV与其他数据贴图哈希保持一致。GLB重新导入渲染平均RGB绝对差约0.00003939。

Unity中的已替换塔架使用新颜色图，并对齐旧资源的低反射哑光观感，金属度重映射上限为0.414，光滑度重映射上下限均为0。已查看实际Scene近景，未产生新增Console错误。场景文件哈希不变，吊斗未调色，其余10座tajia03仍未替换。修改前文件与当前验证记录位于ArtSource/Cableway/Tajia01。

### tajia01 主场景替换

用户确认塔架与吊斗后要求替换已有模型。通过Unity编辑器将Main Game Scene中索道群下唯一的tajia01实例替换为新预制体，包含独立Tower和CoalBucket子对象，并保存主场景。保持原父级和根对象的位置、旋转、缩放，以旧网格高度及塔底低处顶点校准新资源。替换前后塔高均为31.289398米，两个子对象合计76,292个三角面。

配置两个独立HDRP/Lit材质及六张4K贴图。颜色启用sRGB，法线和HDRP遮罩采用线性导入，法线强度0.45。已检查材质、贴图和网格引用，查看同机位替换前后Scene视图。贴图导入时出现工具等待超时，后续确认全部导入完成。替换操作没有新增Console错误，未运行Play Mode。原对象无碰撞体，本次未增加碰撞、LOD或动画。

原FBX继续保留，新资源位于Assets/Art Resources/Environment/Cableway/Tajia01_Rebuilt。替换前场景副本、操作脚本与检查记录位于ArtSource/Cableway/Tajia01/UnityIntegration。

最终逐对象对照替换前场景副本，仅tajia01的PrefabInstance和Transform引用块发生变化。已通过编辑器恢复预览视角引起的36个MapMagic地形距离缓存，旧FBX哈希保持不变。

### tajia01 独立塔架首版

用户确认塔架和运煤吊斗概念图后，要求先制作塔架。按塔架参考图独立重建角钢、横梁、连接板、螺栓、平台格栅、爬梯及外侧竖杆，制作局部旧漆和锈蚀的灰黑钢材。新模型不含吊斗，原 FBX 与场景引用保持不变。

交付 Blender 源文件、FBX、内嵌材质的 GLB、4K 颜色及法线贴图、HDRP Mask 和 glTF 表面贴图。导出模型为一个网格和一个材质，共48,776个三角面。高度按原 FBX 对齐，原点位于塔底中央。

FBX、GLB重新导入后的网格数量、面数、UV和世界尺寸检查通过。网格非流形边与零面积面均为零。已查看导出 GLB 的正面、背面和连接件近景；与导出前同机位渲染平均 RGB 绝对差约0.00002223。原模型制作前后 SHA256 一致。Unity 材质绑定、场景替换、碰撞体、LOD与Play Mode尚未配置或验证。

产物与验证记录位于 `ArtSource/Cableway/Tajia01`。吊斗目前仅有已确认概念图。

### 塔架配色降低对比

用户反馈银灰钢材和橙红锈迹对比过强，确认改为暖深灰、低饱和灰褐锈迹及暗灰红褐旧漆。使用相同灯光和机位重新烘焙颜色贴图并渲染，混凝土塔脚保留原色。Blender、FBX、GLB和近景预览同步更新。

网格及UV数据哈希一致，法线、金属度、粗糙度、AO及打包贴图哈希一致。更新后的FBX与GLB重新导入检查通过，GLB与源材质同机位渲染平均RGB绝对差约0.00001860。修改前颜色贴图及预览保存在 `ArtSource/Cableway/Tajia01/Previous/Before_Palette_Adjustment`。Unity场景保持不变。

### 独立运煤吊斗首版

继续按已确认概念图制作运煤吊斗，沿用塔架调整后的暖深灰和灰褐锈色。完成有壁厚的敞口斗身、翻边、接缝紧固件、侧转轴、弯折吊架和顶部双槽轮，斗内为空腔并使用暗色煤尘材质。

交付Blender、FBX、GLB及4K贴图。模型为一个网格和一个材质，27,516个三角面，使用与塔架相同的导出缩放，原点位于滑轮下方的钢缆接触中心。导出中不含塔架和钢缆。

FBX与GLB重新导入后的网格数、面数、UV、材质和世界尺寸检查通过。非流形边与零面积面均为零。已查看整体、斗内、滑轮和背面渲染，导出与源材质同机位渲染平均RGB绝对差约0.00000351。原tajia01、已完成的塔架文件和Unity场景保持不变。Unity挂接、碰撞、LOD和动画仍未制作。

文件与检查记录位于 `ArtSource/Cableway/Tajia01/CoalBucket`。

## 2026-09-24

### 厂区边界三个基础模块

按用户要求直接制作，没有新增备份。核对门卫室位于大门 Unity 本地正 X 一侧，保留该侧短墙，删除另一侧突出短墙。更新当前大门 Blender、GLB 和 FBX，并沿用原 GLB 路径，使现有场景引用同步显示新的轮廓。

制作大门、独立柱子、标准围墙三个 Prefab，统一放在 Gate Area/FactoryBoundary/Prefabs。分别为61,551、4,352和14,226个三角面，碰撞体分别为5、3、2个。墙段采用3米柱心间距，支持直线与90度转角，附带定位点。所有 Prefab 共享现有大门材质，保留当前暖色逐砖色差。实例随机系统尚未制作。

已检查两个 Blender 实际预览，确认直线连接和转角轮廓。Unity 实际导入检查确认UV、法线、切线、砖网格顶点颜色与受支持的 Shader，三个 Prefab 没有缺失脚本，材质引用共享。确认场景大门外侧短墙已删除，门卫室侧短墙存在。没有布置完整围墙，没有进行 Play Mode 验收。

源文件、预览及检查记录位于 ArtSource/FactoryBoundary。拼接说明位于 FactoryBoundary 资源目录的 README.md。

### 大门砖色按照片调整

后续按用户反馈收回过灰的方案，改为暖土黄、浅赭和褪色砖红的自然变化，少量砖面稍偏橙红。逐砖分配顶点颜色并加入细微明度变化，避免灰砖与橙砖形成明显两组。同步更新 Blender、GLB、FBX 和预览，Unity 已确认8个砖网格的颜色属性与新材质。位置、拓扑、UV和三角面数保持不变，GLB重新导入渲染检查通过。增加逐砖色差前的文件位于 `Previous/Before_Individual_Brick_Colors`。

按用户提供的旧砖墙照片，将大门砖色由深橙红调整为较浅、低饱和的土红与赭色，增加灰褐色变化。保留既有白灰、墙脚污染、砖块几何、法线和粗糙度。

更新 Blender 源文件、GLB 内嵌贴图、两张砖底色 PNG 和相关预览，并通过 Unity 编辑器重新导入。场景实例引用新材质，位置与缩放保持不变。GLB 网格数据完全相同，22 张内嵌图像中仅两张砖底色发生变化。导出重新导入检查通过，源文件与 GLB 渲染平均像素差约 0.0000091。最终 Unity 验证在编辑模式完成。

修订前文件保存在 `ArtSource/FactoryGate/Previous/Before_Brick_Color_Revision`，检查记录为 `brick_color_revision.json` 和 `brick_color_unity.json`。

### 主场景大门替换

通过已连接的 Unity 编辑器，将 `Main Game Scene` 中 `In-scene Objects/Factory Entrance/FactoryGate` 替换为新版 GLB，并保存场景。以旧门底部中央和正面方向定位，修正旧模型的特殊轴向与八倍缩放，新门使用一倍缩放。

编辑器检查确认 72,331 个三角面、8 种材质及有效的颜色、法线和金属粗糙度贴图，旧门实例为零。已查看实际场景整体和近景。原门没有交互脚本、碰撞组件或外部引用，本次未添加碰撞与交互功能，Play Mode 仍待用户确认。

替换前磁盘文件和现场场景副本保存在 `ArtSource/FactoryGate/Previous/Before_Unity_Replacement`，保留用户此前未保存的状态。与现场副本的差异仅涉及旧门、新门及父对象子项引用。此次替换未产生新错误，Console 保留此前贴图生成期间的历史读取错误，相关贴图当前均可加载。

操作脚本、检查记录和 Unity 预览位于 `ArtSource/FactoryGate`。

## 2026-09-23

### 白灰残留与闭合锁链修正

- 将白灰由横向整片改为左右不同的自然残留斑块，增加大片缺失、内部露砖与零散小块。
- 将原来的两段独立链条替换成一条穿过两侧把手的链条，两端由挂锁锁梁套住闭合。21 个链环经过方向调整，整体贴近把手承托处。
- 静态构造曲线检查确认相邻链环逐对套接，锁梁穿过两个末端链环，路线穿过两个把手开口。邻近链环无穿插，距两个把手的最小表面间隙约为 0.16 毫米和 0.29 毫米。
- 重新生成 Blender、GLB、FBX 和所有预览，增加锁链俯视近景。导出尺寸、面数、UV、法线、贴图和钢板穿孔检查通过。当前模型共 72,331 个三角面。
- 此次修改前的源文件保留为 `ArtSource/FactoryGate/Previous/Before_Chain_Revision.blend`。Unity 内显示及碰撞仍待用户验收。

### 厂区大门参考图细节修订

- 按已确认清单补充 561 片实际翘起漆皮，厚约 0.3 毫米。两扇门共 16 处贯穿腐蚀孔洞，孔边轻微变形。
- 重排底部、接缝和左上部锈蚀，降低边框明度。放大砖块并加强墙脚污染，将白灰移至短墙中下部，保留下方露砖。
- 简化混凝土柱帽轮廓，增加破角、孔隙与旧化，调整短墙顶部横向砖层。
- 修订版共 73,497 个三角面，28 个网格、8 种材质，约 8.516 米宽、4.391 米高、1.006 米厚。GLB 约 77.8 MiB。
- GLB 和 FBX 重新导入后尺寸、面数、UV、法线及贴图检查通过，GLB 切线完整。16 处孔洞射线测试全部贯通，完整钢板对照点命中。源文件与 GLB 同光照渲染平均像素误差约 0.0000083。
- 源文件内嵌 22 张图像，默认开启材质预览。新增漆皮、腐蚀孔洞和白灰近景。修订前源文件和整体渲染保留在 `ArtSource/FactoryGate/Previous`。
- Unity 场景未替换，HDRP 实际显示、碰撞和 Play Mode 仍待用户验证。见[制作与导入说明](../../../ArtSource/FactoryGate/README.md)。

### 选煤厂设定与厂区大门重制

- 确认主场景工厂为采用洗选工艺的选煤厂，人物日常称洗煤厂。厂区位于山脚，附近旧煤矿与旧运输路线归入周边矿区。
- 将游戏设计、关卡设计、故事走向及相关门户中的旧称选矿厂更新为选煤厂，并在根目录 `CONTEXT.md` 记录术语。
- 用 Blender 5.2.2 的 `bpy` 制作平直横梁、方钢竖杆、双扇钢板门、红砖柱、混凝土压顶与铁链挂锁。保留独立门扇与铰链轴心，制作背面加强筋。
- 新模型为 68,548 个三角面，原模型为 1,500,000 个三角面，减少约 95.4%。整体约 8.494 米宽、4.483 米高、0.981 米厚。
- 材质分为漆面、铁件、砖、灰缝、混凝土与旧白灰，导出 PBR 贴图和 HDRP Mask Map。左右门扇锈蚀分布不同。
- 源文件、生成脚本、实际渲染和检查记录位于 `ArtSource/FactoryGate`。GLB、FBX 与独立贴图位于 `Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt`。
- GLB 和 FBX 的重新导入检查通过，尺寸、面数、UV、法线与贴图引用正确。GLB 切线完整，重新渲染与源文件基本一致。
- 原 `FactoryGate.glb` 保留，未替换场景对象。Unity 导入画面、HDRP 材质显示、碰撞及 Play Mode 待用户验收。
- [源文件与使用说明](../../../ArtSource/FactoryGate/README.md)

## 2026-08-22

### 截至本日日志建立前的项目基线

#### 角色控制

- 已实现基于 `CharacterController` 的 WASD 移动、加速、减速和重力
- 已实现鼠标水平转向和摄像机垂直俯仰
- 已实现玩家控制开关，供相机、玩家面板和物品检视临时暂停角色控制
- 已实现根据移动与着地状态播放脚步音效

#### 相机拍照

- 已实现 `Disabled`、`Active`、`Capturing` 和 `Reviewing` 四状态相机状态机
- 已实现数字 1 举起和放下相机，以及对应的相机模型动画
- 已实现鼠标左键拍照和关闭拍摄后预览
- 已实现鼠标右键切换相机设置光标模式
- 已实现焦距、景深光圈、暗角、曝光、Bloom、饱和度和构图网格调节
- 已实现帧末屏幕读取、固定 1920×1080 照片输出和 PNG 保存
- 已实现照片文件名 Manifest、照片加载和缩略图缓存
- 已实现拍摄后闪光、淡入和照片预览
- 已实现相册分页、缩略图打开、选择删除、全部删除和右键菜单
- 已实现相机取景范围内物体检测、遮挡确认和高亮材质恢复
- 已实现相机模式、玩家面板和角色控制之间的基本互斥

#### 玩家面板与背包

- 已实现通过 Tab 打开和关闭玩家面板
- 已实现相册、手账、爷爷日记和背包四个页签
- 已实现打开面板时暂停玩家控制并显示光标，关闭时恢复此前状态
- 已实现可检视物品的名称、描述、展示距离、缩放和初始旋转配置
- 已实现远距离圆点提示、近距离 F 键检视提示和最近目标选择
- 已实现检视物体克隆、左键旋转、滚轮缩放和退出检视
- 已实现检视后把场景物品加入背包并禁用原物体
- 已实现背包槽位、独立 RenderTexture 三维预览和从背包重新检视物品
- 已实现持有指定背包物品时按 F 触发场景 UnityEvent

#### 手账系统

- 已实现从相册右键菜单把照片登记到手账
- 已实现照片、贴纸和文本框工具栏分类
- 已实现把照片和装饰从工具栏拖入手账画布
- 已实现画布元素左键移动、右键旋转、滚轮缩放、点击置顶和边界限制
- 已实现文本框双击编辑
- 已实现照片登记列表 JSON 存储
- 已实现画布元素位置、旋转、缩放、类型、来源和文本内容的 JSON 存储与重建
- 已实现带 Echo 照片拖入手账后自动生成只读手账文字

#### Echo 与爷爷日记

- 已实现拍照时确认画面内真正入镜且未被遮挡的 Echo 物体
- 已实现照片文件名与 Echo 手账文字列表的 JSON 映射
- 已实现带 Echo 照片在手账工具栏中的发光提示
- 已实现发现 Echo 后显示统一弹窗并自动隐藏
- 已实现 `EchoTrigger` 的手账文字和日记便签槽 ID 配置
- 已实现拍摄 Echo 后解锁对应爷爷日记便签
- 已实现爷爷日记 JSON 页面数据、文字 marker 和便签槽数据模型
- 已实现日记页面解析、排序、上一页、下一页、页码和本次会话内页面停留
- 已实现按页面 ID 显示爷爷原文和已解锁便签
- 已实现便签悬浮时降低透明度

#### 输入与技术文档

- 已建立 `Gameplay` Action Map 和统一的 `PlayerInputProvider`
- 角色移动、相机、玩家面板、物品检视、道具使用和相册菜单均已迁移到新版 Input System
- 旧版 `PlayerLocomotionInput`、旧生成输入包装类和废弃相机场景已删除
- 已建立代码结构门户和逐文件代码详解文档

### 基线中仍待 Unity 完整验收的部分

- 相册缩略图仍存在正方形框与横向照片比例不一致的问题
- 手账照片登记、布局存档和重建需要完成完整端到端测试
- Echo 照片绑定、手账高亮和手账文字需要完成完整端到端测试
- 爷爷日记页面对象、文字区域、翻页按钮和便签槽仍需检查场景引用并完成翻页测试
- `EchoSystemManager` 当前只提示遍历中遇到的第一个 Echo，多个 Echo 同时入镜的反馈仍待调整
- 回响收集进度、日记字体和后续关卡内容尚未实现

### 本日新增与完成

- 项目自定义运行时代码已完成新版 Input System 迁移
- Active Input Handling 已切换为只使用 Input System Package
- 已测试角色移动、相机输入、玩家面板、物品检视和相册右键菜单
- 代码结构文档已改为门户，并建立逐文件代码详解文档

### 相机功能设计决定

- 照片只保留横向拍摄，不制作横拍与竖拍切换
- 新增手持取景和全屏取景两种显示方式
- 数字 1 举起相机后默认进入手持取景
- 手持取景中相机模型保留在玩家面前，模型屏幕实时显示镜头画面，屏幕外仍能看到真实场景
- 全屏取景复用当前相机模型隐藏和全屏相机 UI 的效果
- 两种取景方式都允许拍照
- 视图切换键确认为 `V`
- 实时屏幕使用低分辨率 `RenderTexture` 和独立预览 Camera，并在相机模型不可见时停止渲染

### 当前状态

- 手持取景与全屏取景的概念和功能边界已经明确
- 视图切换键已经确认为 `V`
- 已确认相机模型只有一个 Mesh Renderer 和一个共用材质，没有可独立赋材质的屏幕 Mesh
- 已在相机模型下建立 `CameraScreen` Quad，并完成屏幕覆盖、方向和独立渲染面确认
- 已新增独立的 `CameraViewMode`，定义 `Handheld` 和 `Fullscreen`，没有改动现有拍照状态机职责
- 已在 Gameplay 新增 `CameraViewToggle`，绑定键盘 `V`，并由 Unity 重新生成 `PlayerControls.cs`
- `CameraSystemManager` 已持有当前取景状态，每次成功进入相机模式前都会重置为 `Handheld`
- 举起动画结束后会保留相机模型，放下动画结束后仍会隐藏模型
- `V` 键已经连接取景切换逻辑，`Handheld` 显示相机模型并隐藏全屏相机 UI，`Fullscreen` 执行相反显示
- 鼠标右键光标模式只会在 `Fullscreen` 中生效，从光标模式返回 `Handheld` 时会自动恢复锁定光标和角色移动
- 已创建 512 × 288 的 `CameraScreenPreview` RenderTexture 和 URP Unlit 的 `CameraScreenPreviewMaterial`，材质 Base Map 已绑定实时预览纹理
- 已创建 `CameraPreview`，输出到 `CameraScreenPreview`，并通过 `CameraPreviewSync` 同步 Main Camera 的位置、旋转、视野角和裁剪距离
- `CameraPreview` 已启用后期处理、移除重复 Audio Listener，并保持 Camera 组件关闭等待运行时控制
- `CameraScreen` 已绑定 `CameraScreenPreviewMaterial`，关闭投射阴影，并随相机模型使用 `CameraModel` Layer
- `CameraPreview` 的 Culling Mask 已排除 `CameraModel`，避免实时屏幕拍到实体相机和自身画面
- Main Camera 保留 `CameraModel` Layer 渲染，`CameraPreview` 继续排除 `CameraModel`
- 实时预览 Camera 现在只在相机模型可见时启用，模型隐藏后停止渲染
- 已停止让相机调节代码修改 `ProjectCoal Global Volume`
- `CameraTweakUIManager` 已拆分为 Preview Volume 和 Fullscreen Volume 两个引用，同一组相机调节会同步写入两份独立 Profile
- `CameraSystemManager` 只在全屏取景时把 Fullscreen Photo Volume 权重设为 1，普通、手持和拍后预览状态设为 0
- `CameraRaiseAnimationHandler` 只在相机模型可见时启用 Handheld Focus Volume，并使用 Main Camera 前向深度把焦点同步到 `focusTarget`，未绑定时回退到实体相机轴心
- CameraPreview 的 Volume Mask 不应包含 Default；否则 `ProjectCoal Global Volume` 会先处理 RenderTexture，再由 Main Camera 对 CameraScreen 进行第二次处理，造成手持屏幕泛灰
- 已新增 `CameraViewTransitionHandler`；手持切全屏时相机模型按可调局部位移、旋转和缩放向镜头推进，同时通过 CanvasGroup 淡入全屏 UI，返回手持时反向播放
- 全屏相机 Volume 的权重切换已延后到取景过渡完成回调；双向动画播放期间维持来源模式的 Volume，避免相机模型尚未退出画面时外部场景提前套用全屏相机效果
- 手持模式拍照时会在帧末读取前临时隐藏 CameraModel、停止 CameraPreview、关闭 Handheld Focus Volume 并启用 Fullscreen Photo Volume；读取结束后立即恢复手持渲染状态，避免最终照片包含实体相机和实时屏幕
- 取景过渡期间会拒绝重复切换、拍照、放下相机和进入光标模式，避免多个流程同时修改相机模型与 UI
- 已新增 `CameraModelInertia`；独立惯性 Pivot 会根据 Main Camera 的旋转变化产生可调俯仰、偏航和滚转拖拽，并按可调时间平滑回正
- `CameraModelInertia` 已扩展待机与移动晃动；使用 Main Camera 的水平位移估算移动强度，在缓慢的手臂待机漂移和随步伐变化的位置、旋转晃动之间平滑切换，不依赖角色控制器接口
- Unity 层级已采用 `CameraRaiseAnim/CameraInertiaPivot/CameraModel`，举起与取景过渡继续修改 CameraModel，惯性和自然晃动只修改独立 Pivot，避免两套动画覆盖同一 Transform

## 2026-08-23

### Unity 验证结果

- 普通探索、手持取景和全屏取景使用各自预期的 Volume，CameraPreview 不再重复叠加 Default Layer 的全局后处理
- 手持与全屏模式均可拍照，最终照片不包含实体相机、实时屏幕、构图网格或相机 UI
- 手持拍照结束后会恢复 CameraModel、CameraPreview 和 Handheld Focus Volume，全屏拍照结束后保持全屏取景状态
- 双向取景过渡、动画结束后切换全屏 Volume、连续按 V、过渡期间输入限制、两种模式放下相机、照片预览、玩家面板和光标恢复均通过运行测试
- 相机模型的旋转惯性、轻微滚转、待机手臂晃动、移动步伐晃动和停止后的平滑回正均通过运行测试
- 关闭相机后 CameraPreview 停止渲染，实时预览性能未发现阻塞后续流程的问题

### 下一步

- 手持与全屏取景功能 C0 至 C18、取景过渡与手持惯性 T1 至 T6 已完成，继续处理 Todo List 中其他未完成项目

## 2026-08-24

### 相册缩略图比例调整

- 根据相册实际画面确认单张缩略图最终显示尺寸采用横向 240×135，Grid Layout Group 使用 4 列和 10×10 间距
- 当前单张照片相对相册整体的视觉大小已经合适，后续边框调整不得缩小该尺寸
- 当前网格高度最多完整容纳 4 行，因此 `CameraPhotoAlbumUIManager.Page Size` 需要从 40 调整为 16
- 已移除相册内的 Echo 高亮代码与引用。回响照片在相册中不再有额外视觉标识
- 缩略图 Prefab 根节点已统一为横向 240×135。后续在 Prefab 中复用拍照预览的十六比九边框资源
- 已生成 `Photo_Album_Thumbnail_Frame_16x9_Overlay_Rounded.png`。它是较干净的暖象牙圆角相纸边框，中心窗口与外部区域均为透明 alpha，窗口比例精确为十六比九
- Unity 编译与运行验证待进行，重点检查边框、右键菜单、删除模式、分页和再次左键关闭预览

## 2026-08-25

### 相册大图预览关闭按钮移除

- 已恢复 `CameraPhotoAlbumUIManager` 的删除模式 Toggle、选择删除和全部删除引用及逻辑
- 已恢复 `PhotoThumbItem` 的缩略图选择 Toggle 引用及逻辑
- 已移除 `CameraPhotoPreviewUIManager` 的独立关闭按钮引用和显示控制
- 相册缩略图继续使用原有左键预览流程，再次左键同一张缩略图会关闭大图预览

## 2026-08-27

### 相机调节 UI 视觉统一

- 已确认 Camera Tab 的 Focal Length 与 Aperture Slider 使用完全一致的三段样式
- Background 使用 `SliderBG_0` 与米灰色，Fill 使用 `InputFieldBackground` 与暖棕色，Knob 使用 `Knob` 与白色
- 已把相同的 Source Image 与颜色复制到 Post Processing Tab 的 Vignette、Exposure、Bloom 和 Saturation Slider，共核验 12 个 Image 组件
- 本次只修改 Background、Fill 和 Knob 的 Sprite 与颜色，没有修改 Slider 数值范围、事件、导航或布局

### 相册缩略图存储与内存策略审计

- 当前照片在拍摄后统一为 1920×1080 RGB24，并使用无损 PNG 保存到 `Application.persistentDataPath/CapturedPhotos`
- 相册读取完整 PNG 后按最长边 256 像素生成 256×144 RGB24 内存缩略图，不单独保存缩略图文件
- 当前实测 16 张照片全部为 1920×1080，总体积约 25.09 MiB，平均每张约 1.57 MiB
- `PrewarmThumbnails()` 会逐帧预热并缓存全部照片，而不是只缓存当前页；照片数量较多时，启动解码和内存占用会随总照片数增长
- 大图预览会重新加载完整 PNG 并创建 Sprite，目前关闭或替换预览时没有主动销毁旧纹理与 Sprite；这是后续比缩略图尺寸更优先的内存优化点
- 本次只完成静态审计和实际照片文件统计，没有修改照片保存、压缩或缓存代码

### 相册大图预览边框统一

- 已确认旧大图预览仍使用 800×880 的竖向 `Photo FrameBG`，内部 `Photo Holder Mask` 为 720×720，会以方形区域裁切 1280×720 的照片
- 已把大图预览根框调整为横向 1280×720，把遮罩、黑色背景和照片显示区域统一为 1152×648
- 已在大图预览最上层添加 `PhotoFrameOverlay`，复用缩略图当前使用的 `Photo_Album_Thumbnail_Frame_16x9_Overlay_Rounded.png`
- 新覆盖边框保持白色、Simple Image 和关闭 Raycast Target；旧根背景改为透明但保留原有点击射线，避免破坏再次点击关闭预览的交互
- 没有修改 `CameraPhotoPreviewUIManager` 的预览逻辑、动画或序列化引用
- 本次创建的 Slider 与大图边框审计和应用脚本、对应 Meta、临时报告及空的 `Assets/Editor` 目录均已清理，项目中没有相关临时代码残留

### 待 Unity 保存与运行验证

- 保存 `Main Game Scene`，确认 Post Processing Tab 四个 Slider 的 Background、Fill 和 Knob 与 Camera Tab 视觉一致
- 从相册点击新旧照片，确认大图显示完整十六比九画面、圆角边框与缩略图一致且没有拉伸或方形裁切
- 再次点击同一缩略图确认可以关闭大图预览，并检查淡入动画、右键菜单、删除模式和分页未受影响

## 2026-08-29

### UIEffect 接入与照片删除反馈替换

- 已在 Package Manifest 中固定 UIEffect 5.11.7
- `PhotoThumbItem` 删除动画改为由 DOTween 驱动子级 UIEffect 的 Transition Rate
- 原有照片文件删除、缩略图缓存清理、批量延迟和删除后重排逻辑保持不变
- 缩略图 Prefab 的 `PhotoImage` 已配置 UIEffect `Transition-Dissolve` 预设，默认 Transition Rate 设为 0
- `PhotoFrameOverlay` 已配置 UIEffectReplica，并以 `PhotoImage` 的 UIEffect 作为 Target，使照片与相框共用消散参数
- Inspector 配置已保存；Play Mode 中实际删除动画、照片与相框同步消散及删除后重排已确认正常
