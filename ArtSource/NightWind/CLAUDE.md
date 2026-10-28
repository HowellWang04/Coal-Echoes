# ArtSource/NightWind/

## 用途

第四关和结尾月夜里的风声。用噪声合成的立体声循环，没有录音素材。

## 内容

- `make_night_wind.py`：合成脚本。低频风身、中频风声和很轻的高频气声三段噪声，按一条缓慢起伏的阵风曲线混合，阵风强时再带出一点音高漂移的哨声；尾部交叉淡入开头，导出的文件首尾无缝。参数是输出路径、秒数和随机种子，默认 48 秒、种子 1988，用 numpy 和 scipy。

## 与其他部分的关系

- 导出到 `Assets/Art Resources/Audio/NightWind.wav`，走 Git LFS；导入设置是流式加载、Vorbis 压缩。
- 场景对象是 `Gameplay Root/Managers/MoonlightController/Night Wind`，2D 的循环 AudioSource，Play On Awake。`MoonlightController` 切到月夜时把这个对象打开，回到白天时关掉。
- 用途和切换时机见 `Docs/Specs/Systems/moonlight-spec.md`。

## 注意

- 重新生成会覆盖导出的 wav，换种子就是另一段风。
- 只在 Play Mode 里能听到；编辑模式下切月夜不会出声。
- 项目里没有混音器，音量直接在 AudioSource 上调。
