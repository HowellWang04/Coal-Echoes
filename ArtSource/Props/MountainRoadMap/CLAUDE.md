# ArtSource/Props/MountainRoadMap/

## 用途

第二关 207 室桌子抽屉里的山路图：工友赵建国 1980 年画在一张旧工票背面的上山路线。正面是印好并填写过的工票，背面是铅笔画的路线图，路线照场景实测布局画：从宿舍 207 出发，沿主路走到底，转进小路到索道下站，扒上回山的吊斗，经过 8 座塔架到上站，从上站背后走栈道上到山脊，观景平台上画叉。

## 内容

- `layout.json`：厂区、道路、建筑、料仓、煤堆、两座站房、塔架、山顶栈道、围墙和地形高度，坐标是厂区坐标系（`FactoryGate` 轴心为原点，u 沿主路指向厂内，v 指向主路右侧，单位米），另记了正北在这个坐标系里的方向。`Scripts/export_layout.cs` 导出。
- `Scripts/export_layout.cs`：Unity MCP `execute_code` 的方法体，从主场景读出上面这些写进 `layout.json`。场景布局改了要重跑。站房占地不算挂在站房下的吊斗；建筑组里的枯草和宿舍楼前的旗杆不算建筑；栈道只导出中线和观景平台四角，用的是栈道局部坐标里的平桥长度、转角、台阶水平长度和平台尺寸，照抄 `ArtSource/SummitBoardwalk/Scripts/build_boardwalk.py` 的布局常量。
- `Scripts/make_ticket_textures.py`：普通 Python（numpy、Pillow、scipy、contourpy），读 `layout.json` 画正反两面，命令行第一个参数是输出目录，第二个是预览目录。纸张底色、纤维、茶杯水渍、折痕和发黄的边是“穿透纸张”的，正面画好后左右镜像给背面用；背面还透出镜像的正面印刷和墨迹。同时导出 `MountainRoadMap_Translations.json`：每写一处字记下它在贴图上的框，换成该面 Quad 的 uv，和英文一起写进去，几行合成一处的取外接框；英文就写在脚本里紧跟着画字的那一行。
- `Fonts/`：龙藏体 Long Cang（`LongCang-Regular.ttf`，SIL OFL，许可在 `LongCang-OFL.txt`），用作工友的手写字。工票的印刷字用 `Assets/Art Resources/UI/NewspaperMenu/Fonts/` 里的思源宋体和朱雀仿宋，都是 OFL。
- `Previews/`：`route-plan-view.png` 是按实测坐标画的平面图，用来对路线；`both-sides.png` 是正反两面的缩小拼图。

## 与其他部分的关系

- 输出在 `Assets/Art Resources/Props/MountainRoadMap/`：`MountainRoadMap_Front.png`、`MountainRoadMap_Back.png`，都是 2048 × 1424，对应 18 × 12.5 厘米的纸片。导入设置把 Non-Power of 2 设为 None、Wrap 设为 Clamp，否则会被缩成 2048 × 1024。同目录的 `MountainRoadMap_Translations.json` 给场景里纸片上的 `PaperTranslation` 读，查看时按 T 和相册里的回响照片悬停翻译都用它。
- 同目录的 `MountainRoadMap_Front.mat`、`MountainRoadMap_Back.mat` 从旧相框照片的材质复制，HDRP/Lit，底色压到 0.8，光滑度 0.1。
- 场景里的纸片是 `In-Scene Objects/室内空间/207/Mountain Road Map`，按抽屉里 `Map Spot` 的位置和朝向摆（见 `../../Room207/DormDesk/`），背面朝上。两张背靠背的 Unity 自带 Quad，`Front` 绕竖轴转了 180 度，等于左右翻面，和贴图的画法对上；按 E 查看、悬停翻译和拍照回响都挂在根物体上；第一次看完放到桌面的 `207/Desk Spot`，之后一直在那里，见 `Docs/Specs/Levels/level-two-spec.md` 山路图一节。
- 路线取自 `In-Scene Objects/工厂道路向内`、`工厂支路/支路 至索道站 LineB`、`索道群/站房`、`索道群/塔架` 和 `山顶栈道/SummitBoardwalk`（见 `../../SummitBoardwalk/`）；宿舍楼见 `../../WorkersDormitory/`，索道见 `../../Cableway/`，沙丘见 `../../SandDune/`（1980 年还没有沙丘，图上不画）。
- 设计和流程见 `Docs/design.md` 关卡2、`Docs/Specs/Levels/level-two-spec.md`，美术资源列表见 `Docs/art-resources.md`。

## 注意

- 厂区按真实比例画，每米约 3.6 像素；上山那段在 v 58 米以上压扁成约 0.73 像素每米，好把一公里的索道放进纸片，所以图上塔架的上下间距不是真实比例。比例和留白在脚本的 map projection 一段改。
- 栈道是局部放大的示意：以上站方框为准，横竖都按厂区的比例画，否则压扁后只剩几个像素。平桥画成疏的横档，台阶画成密的横档，台阶的左转在图上画成实际的两倍，好看出拐弯；观景平台放大 1.8 倍，叉画在平台方框里，外面一圈圆。
- 图上文字是中文手写：大门、宿舍 207、主路、车间、煤场、小路、下站、扒吊斗、吊斗往上走、塔架 1 到 8、上站、栈道、山脊、看灯，左上角是“歇班那晚 一起上去 80.12.4”和指北箭头。正面填的是运输车间、索道班、赵建国、1980 年 11 月 20 日、下站发吊斗清理落煤、托轮加油，盖着“已记工”红章。按 E 查看时显示的名字 The Way Up 和悬停翻译的英文对照写在[游戏文本](../../../Docs/game-text.md)第二关的山路图一节，改图上的字或英文后要重跑脚本、把 JSON 拷进 Assets，并同步那里。
- 随机数种子固定，重跑结果一样；改了布局、文字或位置后要重新看一遍两张图有没有文字压线。
- 正面工票上工友是运输车间索道班的人，和日记第二页“分到下站的索道班”对上；第三页 1983 年冲垮的是第五座塔架，图上塔架编号要和它一致。
