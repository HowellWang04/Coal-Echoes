# 月光 Spec

第四关和结尾的月夜。到山顶之前是白天，第三关快到站时的黑屏过场里切成有月亮的晴夜，吊斗在夜里开到山顶站，之后一直保持。怎么坐吊斗上山、怎么跳过和过场文字见[第三关 Spec](../Levels/level-three-spec.md)。意境以[剧本](../../script.md)的结尾一场为准，关卡规则以[设计文档](../../design.md)为准。

月夜切换本身已经做好，可以用 Debug Tools 直接切；游戏流程里由第三关的 `CablewayRideManager` 在到站前过场的黑屏里调。Play Mode 效果待确认。

## 意境

外公和工友想看的是夜里厂区的灯火，主人公看到的是灯灭以后的月光和星空。画面落在几件事上。

- 晴夜，有月亮，满天星星，只听得见风
- 山下厂区一片漆黑，没有一盏灯；厂房、煤堆、沙丘和一座座索道塔架靠月光照出轮廓
- 冷、静、看得清，不是恐怖片那种黑；情绪从遗憾转向释然
- 结尾玩家在山脊上把厂区和星空拍进同一张照片，和开头的旧合照首尾呼应。月亮的方位按这张照片的构图定，从侧面照，让厂房和塔架有明暗面，不放在取景正中

## 什么时候切

- 只在第三关快到站时的黑屏过场里切一次，跳过和一直坐着都一样，淡入后吊斗再在夜里走完最后一段。`CablewayRideManager` 在过场的 `onBlack` 里调 `MoonlightController.SetNight(true)`，等两三帧天空和环境光刷新后再淡入
- 白天和月夜之间不做渐变，乘坐途中天色不变
- 切过以后第四关和结尾都是月夜

## 做法

不需要重新加载场景或天空盒。主场景没有烘焙光照，光照贴图为 0，没有 Probe Volume，环境光是 Dynamic，太阳是实时光，`灯光与环境/Reflection Probe` 是覆盖全图的实时反射探针，只在启用时拍一次。天空、环境光和雾都从全局 Volume 来，换一份 Volume 覆盖再改平行光就行，HDRP 会自己刷新环境光，反射探针要切换后重新启用一次才会重拍。开场的 `OpeningSkyMotion` 也是用高优先级的全局 Volume 临时覆盖天空。

| 项目 | 白天 | 月夜 |
| --- | --- | --- |
| 天空 | `Global Volume` 的 HDRISky，白天多云的 kloofendal | 夜空 HDRI qwantani_moonrise，旋转到月亮和平行光方向一致 |
| 曝光 | 固定曝光，按白天设定 | 覆盖成夜景的固定曝光 |
| 平行光 | `Directional Light`，晴天的太阳 | 同一盏灯改成月光，转到月亮方位，照度降到月光量级，偏冷色温 |
| 雾 | 体积雾，橙色调，贴着谷底浓、往上变稀，厂区泡在雾层里 | 冷色调，Mean Free Path 和白天原来一样，高度分布覆盖回均匀的旧值（白天的谷底雾层不带进夜里），远山只剩剪影，把地图边界藏住；反照率调低，月亮周围的光晕不至于铺满半边天 |
| 调色 | WhiteBalance、ColorAdjustments 偏暖 | WhiteBalance 偏冷，对比度略加；不动饱和度和后期曝光，这两项相机调节会覆盖，动了会让取景器和主画面对不上 |
| 闪光灯 | `Camera Flash` 按白天曝光设的强度 | 按夜间曝光等比缩小，拍出来和白天开闪光的亮度关系一致 |
| 热浪、沙尘 | `Factory Sandstorm` 的橙色沙尘雾盒开着，`Factory Heat Haze` 在场景里关着 | 沙尘雾盒关掉，切回白天再打开；热浪保持关 |
| 厂区灯 | `灯光与环境/Lights` 下的灯全关 | 保持关，厂区一片漆黑是剧情要求 |

- HDRP 的贴图类参数不随权重渐变，换天空只能硬切，所以要在黑屏里切
- 反射探针存的是上次拍时的亮度，切到月夜不重拍的话，夜间曝光会把白天的反射放大，地面和建筑一片白
- 只有白天和月夜两个状态

### 实现位置

| 位置 | 职责 |
| --- | --- |
| `Assets/Scripts/Environment/MoonlightController.cs`，场景对象 `Gameplay Root/Managers/MoonlightController` | `SetNight(bool)` 切换；把 `Moonlight Volume` 的权重设成 0 或 1，把太阳改成月光或还原，换闪光灯强度，打开或关掉夜风，关掉或打开厂区沙尘雾盒 `灯光与环境/Factory Sandstorm`，再把反射探针关了再开，让它按新的天空重拍。是不是月夜看这个 Volume 的权重；白天的太阳朝向、照度、色温和闪光灯强度在切到月夜那一刻记进隐藏的序列化字段，编辑模式下脚本重编译后也还在，切回白天时还原。开局按到站进度切到对应状态。月光朝向、照度、色温和夜间闪光强度在 Inspector 里调 |
| 场景根下的 `Moonlight Volume` | 全局 Volume，Default 层，优先级高于 `Global Volume` 和开场的 `Opening Sky Volume`，平时权重 0。Main Camera 和手持预览相机的 Volume Mask 都含 Default 层，所以取景、预览和存下来的照片都是月夜 |
| `Assets/Settings/Volume/MoonlightVolumeProfile.asset` | 月夜覆盖的天空、曝光、雾和调色，夜景观感在这里调 |
| `Assets/Art Resources/Environment/Skyboxes/qwantani_moonrise_puresky_4k.exr` | 夜空 HDRI，Poly Haven，CC0，导入设置照白天那张，走 Git LFS |
| `Gameplay Root/Managers/MoonlightController/Night Wind` | 夜风，2D 循环 AudioSource，Play On Awake；月夜时对象打开就开始响，白天关掉。音频是 `Assets/Art Resources/Audio/NightWind.wav`，合成脚本见 `ArtSource/NightWind/` |
| `DebugTools` 的 Moonlight 一组 | 勾选 Moonlit Night 立刻切成月夜，取消勾选回到白天，不存档；编辑模式和 Play Mode 都能用，编辑模式下用来对着场景调夜景。存场景时自动先切回白天再写文件，存完再切回月夜，所以场景文件里永远是白天。勾选框本身会随场景保存，勾着存了，下次进 Play Mode 一开局就是月夜。下面的月光亮度滑条打开时显示 `MoonlightController` 当前的月光照度，拖动时实时改，编辑模式和 Play Mode 都能用，用来试数值；Play Mode 里拖的值退出就没了，试好的数要写回 `MoonlightController` 才算定下 |

- 月光朝向要和 HDRI 里的月亮对齐。月亮在 HDRI 里高约 14 度，HDRISky 的旋转和 `MoonlightController` 的月光朝向要一起改，只改一边月亮和光的方向就对不上
- Profile 里 HDRISky 用 Lux 模式。换夜空图时，上半球照度要重新算；在 Inspector 里重新指定一次贴图，HDRP 会自己算

## 存档

- 月夜本身不另存。开局时 `GameProgress.IsUsed(MoonlightController.SummitArrivalId)` 为真就直接切成月夜，id 是 `summit_arrival`，由第三关站到上站站台时记下。切成月夜到站上站台之间只有几秒，这几秒里退出的话读档回到白天的下站，重新坐一趟
- 订阅 `GameSaveManager.SavesCleared`，新游戏时回到白天

## 美术资源

| 资源 | 现状 | 说明 |
| --- | --- | --- |
| 夜空 HDRI | 已下载，qwantani_moonrise_puresky_4k | 月亮低，侧光长影；天色偏蓝，地平线一圈淡暖色；星星偏淡 |
| 夜风环境声 | 临时占位，48 秒无缝循环，用噪声合成 | 结尾“只听得见风”。全部场景、功能和流程做完后统一做音效，到时换成实录风声，删掉合成脚本和这段音频；候选素材库记在开发日志 |

## 待确认

1. 月亮的具体方位按第四关山头观景平台上俯拍厂区的构图调，平台已放好，见 `ArtSource/SummitBoardwalk/`。现在放在从山顶站望向厂区时视线左侧约 40 度，在画面左边能看到。从平台望向厂门一带，广角下 900 米外被月夜的雾遮得看不清；焦距拉到 275mm 时宿舍楼和后面的厂房暗但看得出轮廓。站房挡在正南偏东，平台上只有靠东的栏杆边看得见宿舍楼，雾要一起看
2. HDRI 的星星偏淡，满天星星的感觉不够；要不要换图或另外加星空

## 验收

1. 切换发生在黑屏里，淡入时天空和环境光已经刷新完，看不到跳变
2. 月夜里天空有月亮和星星，厂区无灯，厂房、塔架和沙丘的轮廓看得清
3. 手持预览、全屏取景和存下来的照片都是月夜效果，相机调节 UI 照常工作；夜里开闪光灯拍照，近处亮但不是一片死白
4. 读档后已到站的存档直接是月夜；新游戏回到白天，月夜 Volume 不残留
5. Debug Tools 勾选 Moonlit Night 立刻变月夜，取消勾选回到白天，太阳的朝向、亮度和色温都还原；编辑模式下也一样，勾着存场景后场景文件仍是白天，编辑器里仍是月夜
6. Play Mode 里切到月夜就听到风，循环处听不出接缝；切回白天风声停