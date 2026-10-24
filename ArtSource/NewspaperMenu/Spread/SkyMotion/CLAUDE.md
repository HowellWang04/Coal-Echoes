# ArtSource/NewspaperMenu/Spread/SkyMotion/

## 用途

主菜单照片转场期间整片天空连同 HDRI 里的云彩快速掠过的记录。运行代码是 `Assets/Scripts/MainMenu/OpeningSkyMotion.cs`。

## 内容

- `Verify.cs`：编辑模式下用一次性天空覆盖渲染真实菜单构图。
- `original.png`、`frame0.png` 至 `frame4.png`：原天空和各进度帧。
- `checks.json`、`scene-checks.json`：数值和场景引用检查。

## 与其他部分的关系

- 组件挂在 `In-game UI Root/MainMenuUIRoot/MenuPresentation/Newspaper` 上，Source Volume 指当前天空的 Volume，Sky Volume 是子对象 `Opening Sky Volume`，用第 0 层让菜单相机的 Volume Mask 能看到，编辑器里禁用，没有序列化的 Profile。
- `Prepare()` 从当前天空 Profile 读最终朝向，在运行时 Profile 里创建只覆盖旋转的 HDRISky，初始朝向落后目标 Sweep Angle 度；`SetProgress()` 推进到最终朝向；`Restore()` 移除临时覆盖。进度由 `NewspaperMenuUI` 的 DOTween Sequence 驱动，与 `../SandAccumulation/` 和 `../../../WorkersDormitory/CleanAging/` 同步。

## 注意

- 设置角度时按 360 度取模，HDRISky 的角度范围会把负值截断。
- 环境更新模式沿用原天空的 OnChanged，演出期间环境光和取天空色的雾每帧跟着旋转重算。不能改成 OnDemand：HDRP 只在第一次算环境光，整段演出会停在起始朝向，结束移除覆盖时建筑会突然变亮。
- Sweep Angle 绝对值越大同样时长里转得越快，负值反向；时长由 `NewspaperMenuUI` 的停顿、放大、消退之和决定。
- 只转天空贴图，没有体积云，太阳方向不变。
