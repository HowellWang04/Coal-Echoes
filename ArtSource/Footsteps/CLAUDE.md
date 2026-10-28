# ArtSource/Footsteps/

## 用途

玩家走路的脚步声，按地面分沙土地、软沙堆、水泥地、木栈道四套，用现成的实录素材切成单步。

## 内容

- `Sources/Sonniss/`：Sonniss GDC 合集里的三段录音，只在本机，不进 git（`.gitignore` 排除），新克隆的仓库里会缺。`PMSFX/` 是 GDC 2019 第 3 包 PMSFX - STEPS Dirt & Gravel 的 `PM_SDNG_Stereo_Walk_Seamless_Loop_1.wav`，一段 26 秒的土路碎石走路，用作沙土地；`Tovusound/` 是 GDC 2017 第 8 包 Tovusound - Edward Foleyart Collection Add-On Extended Footsteps 的 `015_Foley_Footsteps_Asphalt_Boot_Walk_Fast_Run_Jog_Close.wav`，靴子走柏油地，用作水泥地；`Levan/` 是 GDC 2016 第 2 包 Levan Nadashvili - Civilian Footsteps 的 `FS Wood Civilian Crouch N03.wav`、`N05.wav`，在木地板上蹲着走的两步，用作木栈道。
- `Sources/Fantozzi/`：Fantozzi 的沙地单步 6 条，取自 OpenGameArt 的 Fantozzi's Footsteps (Grass/Sand & Stone)，CC0，进 git。用作软沙堆。
- `make_footsteps.py`：切分脚本。一个文件里可以是一步也可以是一整段走路，按 10 毫秒响度找每一步的起点，切到下一步之前或该地面的最长长度并淡出；一整段走路只留响度最接近中位数的几步，跺脚和蹭地的那几下不要；每步按最响 50 毫秒的电平统一响度，峰值不超过 -1 dBFS。每种地面的来源、留几步、低通、长度和电平在开头的 `SURFACES` 里。用法 `python make_footsteps.py <输出目录> [试听目录]`，给了试听目录会每种地面多写一段 12 步、0.35 秒一步的走路声。用 numpy、scipy，解码要 PATH 里有 ffmpeg。

## 与其他部分的关系

- 导出到 `Assets/Art Resources/Audio/Footsteps/`，文件名 `Footstep_<地面>_<编号>.wav`，48 kHz 单声道，走 Git LFS；导入设置是加载时解压、ADPCM。
- 代码在 `Assets/Scripts/In-Game Systems/Character Movement System/Scripts/`。`PlayerFootstepAudio` 挂在 `Player` 上，按走过的水平距离出一步；每步从脚下往下打一条射线，找碰撞体或它父物体上的 `FootstepSurface` 决定用哪套，找不到就用默认的沙土地；同一套里随机挑、不连着重复，音高和音量带一点随机。四套片段、各自的音量和步距在它的 Inspector 里配。坐吊斗不能走动时不出声。
- 场景里挂 `FootstepSurface` 的对象：水泥地是 `工厂大门/Guard Post`、`工厂围墙/Gate Concrete Threshold`、`室内空间/207`、宿舍楼 `WorkersDormitory`、`工厂建筑群/02 道路与围栏/路面与路沿`、`索道群/站房/CablewaySummitStation`；木栈道是 `山顶栈道`；软沙堆是 `沙丘`。地形、贴在地形上的土路和山脚站的积沙没挂，算沙土地。
- 每种地面的用途和听感见 `Docs/sound-effects.md` 通用一节的玩家。

## 注意

- Sonniss 授权免费、免版税、不用署名，可以随游戏发布，但不能把原始音效再分发，也禁止用于 AI 训练，所以原始录音不进仓库。换电脑要从 sonniss.com/gameaudiogdc 下对应年份的分包，取上面列的文件放回 `Sources/Sonniss/` 才能重新生成。
- 新加一块要单独出声的地面，在它的碰撞体或父物体上挂 `FootstepSurface`。射线只打默认层，Ignore Raycast 层上的地面会被当成它下面的东西；沙丘的隐形挡墙 `Blocker` 在这一层，不影响判定。
- `FootstepSurface` 挂在分组上，`PlaceBoardwalk.cs` 重建 `SummitBoardwalk` 不会丢掉它。
- 重新生成会覆盖 Assets 里同名的 wav，但不删多出来的旧编号；某套的条数变了，要删掉多余的文件，在 `Player` 的 Inspector 里重新拖一遍片段。
- 木栈道只有两步，靠随机音高区分，栈道台阶上连走可能听得出来回交替。
- 角色控制器自带的循环脚步 `Character Movement System/Audio/Steps.wav` 已经不用，文件还在。
- 四套声音都还没在 Play Mode 里听过，步距、各地面之间和其他声音的音量比例待用户确认。
