# 开场天空快速掠过

`OpeningSkyMotion` 用于报纸照片转场。当前 HDRI 的云彩属于天空贴图，采用天空朝向推进，表现整片天空连同云彩快速掠过；没有增加体积云模拟或改变太阳方向。

## 运行逻辑

`Prepare()` 从当前天空 Profile 读取最终朝向，在独立的运行时 Profile 中创建只覆盖旋转与环境更新模式的 HDRISky。初始朝向落后目标 Sweep Angle 度，菜单等待时保持静止。设置角度时使用 360 度取模，避免 HDRISky 的角度范围把负值截断。

点击 Start Game 后，NewspaperMenuUI 用同一个 DOTween Sequence 同时驱动报纸、积沙与天空。天空在现有 3.2 秒转场内推进至最终朝向，使用 InOutSine 起止缓动，中段快速移动，末段平滑减速。完成后移除临时覆盖，继续抬头和对白。中途关闭报纸也释放临时 Profile 与 HDRISky。

独立 Volume 在 `MainMenuUIRoot/MenuPresentation/Newspaper/Opening Sky Volume`，使用第零层，让菜单相机的 Volume Mask 能看到它。编辑器中禁用，没有序列化的 Profile 资源。

环境更新模式在演出期间使用 OnDemand，避免每帧随天空旋转重建环境光。天空素材、曝光、纸墨处理与相机取景沿用原配置。

## 调整

在 Newspaper 对象的 Opening Sky Motion 组件中调整 Sweep Angle，当前为 120 度。绝对值越大，同样时长内移动越快；负值反向。总时长由 NewspaperMenuUI 的 Zoom Duration 与 Reveal Duration 之和决定。
