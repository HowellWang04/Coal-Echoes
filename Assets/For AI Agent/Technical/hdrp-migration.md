# HDRP 迁移记录

## 当前状态

2026年9月20日开始迁移。Unity 保持 6000.4.6f1，HDRP 保持已安装的 17.4.0。
用户已完成 HDRP Wizard 的基础检查及 Quality 管线覆盖清理。2026年9月21日通过用户授权的 Unity 插件继续处理地形和场景物品材质。当前仍为迁移中间状态，不能视为可游玩的完成版本。

## 已完成

1. 当前主场景引用的20份 TerrainData 已整理至 Assets/TerrainData/MainGameScene，GUID 与二进制内容保留，用户确认地形显示正常。
2. CameraRaiseAnimationHandler 改用 HDRP DepthOfField，保留手持对焦距离计算。
3. CameraTweakUIManager 改用 HDRP 后处理类型，光圈写入 Camera.aperture，焦距与现有 FOV 控制同步。Bloom 的 UI 数值范围映射到 HDRP 的0至1范围，重置保存的是 HDRP 原生强度。
4. CameraPreviewSync 同步传感器尺寸、焦距和光圈，保持现有位置、FOV、裁切面同步。
5. HighlightShader 改为 HDRP ForwardOnly 输出，并使用绝对世界坐标计算扫光，避免相机相对坐标改变扫光位置。
6. 19个 MapMagic Terrain 对象及 MapMagic 材质模板已绑定 Terrain_HDRP。未重新生成地形，用户确认完整地形恢复显示。
7. 场景 Renderer 使用的22个独立 URP 材质原位转换为 HDRP，保留 BaseMap、颜色、平铺、偏移、法线和透明设置。5张金属贴图转换为独立 HDRP MaskMap，保持金属红通道和光滑度 Alpha 通道，AO 与细节遮罩填白。
8. URP 包默认 Lit 单独对应 URPDefaultLit_HDRP，替换7个场景材质槽，未修改包文件。
9. 工业资源的7个 Shader Graph 恢复原有 HDRP Target 和数据配置，保留节点及连线。包括建筑、简单物品、地面、围栏及水面。
10. 主相机及活动 Scene 视图曾实际使用 Automatic 曝光。为抑制转向后的亮度适应，在 ProjectCoalGlobalVolumeProfile 增加 HDRP Exposure，暂设 Fixed EV100 14。增加 VisualEnvironment 和 HDRISky，恢复 Skybox_Mat 原有采石场天空 Cubemap 和旋转，按 HDRP 编辑器计算其上半球亮度并归一到20000 Lux。亮度与室内外观感尚待用户确认，旧 URP 后处理组件仍待后续迁移。

## 下一阶段的编辑器配置

1. 用户确认场景物品实际外观，重点检查工厂、索道、室内家具、透明材质及水面。
2. 保持已完成的 HDRP Global Settings、Graphics 与 Quality 配置，初期不启用光线追踪。
3. 为手持对焦、取景器和全屏摄影建立 HDRP Volume Profile。景深使用 Physical Camera 模式，对焦距离来源使用 Volume；保留旧 Profile 的对焦距离，并将旧光圈数值转移到相关 Camera。颜色调整、暗角、Bloom 需要对应的 HDRP 组件，不能继续使用 URP Profile。
4. 对比旧摄影 Profile 与 Camera 的默认光圈。目前主相机序列化光圈为16，需要按原设计确认默认值；手持对焦同样使用主相机光圈，需检查近距离清晰度。
5. 校准固定曝光、天空、太阳和环境遮蔽，再迁移地形、建筑、树木与透明材质。MapMagic 只处理材质模板，不重新生成地形。
6. 检查运行时背包预览相机的 HDRP 附加配置、透明 RenderTexture、曝光和 Volume 范围。
7. 核对 Build Profile 的场景列表。EditorBuildSettings 目前仍指向 SampleScene，需要确认实际构建入口。
8. 基础功能验证通过后，再接入 SmokePortal 的烟雾素材和 SDF 表面交互。

## 验证状态

2026年9月21日按用户选择，将 HDRISky 更换为 Poly Haven 的 Kloofendal 48d Partly Cloudy Pure Sky。来源 https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky ，CC0，作者 Greg Zaal，天空编辑 Jarod Guest。4K EXR 保存于 Assets/Art Resources/Environment/Skyboxes，Unity 导入为2048面尺寸的 BC6H Cubemap。重新计算天空上半球亮度，保持20000 Lux和固定 EV100 14。此前的采石场天空资源保留。此项已保存 Volume Profile，天空尾迹及最终视觉效果仍需实际画面确认。

已核对本机 HDRP 17.4 源码中的景深、后处理参数与 Shader 辅助函数。
通过实时编辑器检查，场景 Renderer 的普通 URP 材质引用为0，使用的10种 Shader 均支持 HDRP，检查时没有 Shader 编译错误。22个转换材质的基础贴图与颜色保留检查全部通过。Shader Graph 节点及连线仍需配合实际画面验收，不能以无编译错误代替外观验证。
Volume 绑定、Console 与 Play Mode 的完整功能仍待确认。
需要验证举起相机、全屏切换、焦距与光圈调节、Bloom 与重置、交互高亮、拍照入册、剪贴簿、背包预览和实际构建性能。

## 文件保护

初始代码阶段修改四个摄影系统代码或 Shader 文件。后续编辑器阶段修改了场景材质引用、MapMagic 材质模板、物品材质和工业 Shader Graph。场景引用修改暂留编辑器内存供用户查看，尚未主动保存主场景。TerrainData 与地形 Transform 未修改，包版本未修改。
材质及 Shader Graph 改动前副本位于本机临时目录 coal-hdrp-material-baseline，该副本不是完整项目备份。家具和楼梯已有部分法线与金属贴图命名疑似对应错误，此次保留原有输入，后续单独检查。
未提交或推送 Git。四个文件的改动前副本保存在本机临时目录 coal-hdrp-code-baseline，供本阶段核对；该临时副本不是完整项目备份。

## 2026年9月21日后处理配置迁移结果

本节更新上文的待迁移状态。通过实时 Unity 编辑器完成当前主场景五份 Volume Profile 的配置迁移并保存主场景。

1. ProjectCoalGlobalVolumeProfile、Fullscreen Photo Volume Profile、CameraPreview Volume Profile、HandheldFocusVolumeProfile 已添加对应 HDRP 组件。旧 URP 组件停用保留。
2. 第三方 General Global Volume Profile 复制为 Assets/Settings/Volume/IndustrialHarbour_HDRP_Profile.asset 后迁移，当前场景引用副本，原资源不变。保留 Volume 权重与优先级，其中第三方原对比度约负51.6，仍可能影响最终画面。
3. Color Adjustments、White Balance、Vignette 的对应参数及覆盖状态直接迁移。Bloom 的强度按原摄影 UI 的0至10映射至HDRP的0至1，仅作初始适配，不代表两条管线的视觉完全等价。URP 特有的质量与 clamp 设置未强行映射。
4. 四份景深组件采用 HDRP UsePhysicalCamera 和 Volume 对焦距离。三个普通配置的距离为10，手持配置为0.4。Main Camera 与 CameraPreview 光圈设为原摄影配置的5.6，FOV和焦距未改。HDRP光圈由相机共享，不能像旧URP那样单独混合手持Profile中的光圈8，因此手持近景的视觉效果仍须验收。
5. 对比迁移前序列化参数，HDRP Exposure、VisualEnvironment、HDRISky、Tonemapping、Fog 全部一致。当前天空60000 Lux，固定EV100 14，Neutral，体积雾开启。
6. 实时核对五份配置中启用的URP组件为0，新增HDRP子资源全部持久化，摄影UI及手持对焦脚本Volume引用存在。主场景保存成功。Console错误为0，警告192条，抽查最近8条均为BoxCollider负缩放警告，未在此次改动碰撞体。
7. 本次未进入Play Mode，举机、光圈、摄影调色滑条、重置及最终画面仍待用户运行验收。配置迁移完成不等同于画面验收完成。

迁移前Profile副本与序列化记录，以及保存前主场景磁盘副本位于本机临时目录 coal-hdrp-volume-20260921。此次未修改代码、TerrainData或地形Transform，未提交或推送Git。

## 同日景深状态修正

按既有 CameraSystemManager 与 CameraRaiseAnimationHandler 状态逻辑修正配置，未修改脚本。全局 HDRP 景深改为 Off，手持及全屏 Volume 初始权重为0。两台相机补充 HDAdditionalCameraData，迁移原 URP 的 Volume 层遮罩和锚点，主相机6145，取景器1024，避免取景器 Volume 影响主相机。抗锯齿保持当前HDRP的None，动态分辨率关闭。

独立 VolumeStack 混合检查依次通过探索 Off、手持 UsePhysicalCamera 距离0.4、全屏 UsePhysicalCamera 距离10、退出 Off。主相机实时栈也确认为Off。权重测试后恢复并保存场景。此项是编辑器参数混合验证，举机动画和摄影UI仍需Play Mode验收。修改前副本位于本机临时目录 coal-dof-fix-20260921-182209。

## 同日摄影画面调节复查

两份摄影Profile的HDRP Bloom阈值覆盖设为1，Scatter覆盖设为0.5，初始Intensity覆盖设为0。曝光补偿、饱和度与暗角强度的初始覆盖统一为原滑条默认值0，避免首次调节或重置突然切换继承状态。

发现并修正取景器只读取摄影专用层而遗漏场景环境的问题。Fullscreen Photo Volume移至已有FullscreenPhotoPostProcessing层11，Handheld Focus Volume移至已有HandheldFocusPostProcessing层12，CameraPreview的HDRP Volume遮罩设为1025，包含默认场景环境及取景器层10，排除手持与全屏专用层。主相机保留6145。配置已保存，未修改摄影脚本。

独立VolumeStack检查探索、手持、全屏、退出四种状态，确认两台相机都使用固定曝光14与Neutral，主相机探索关闭景深，手持近焦0.4不影响取景器的距离10，全屏与取景器的摄影Bloom参数一致。临时非激活对象及独立Profile副本执行六项调节回调，验证85mm、f2.8、暗角0.3、曝光1、Bloom滑条2对应强度0.2、饱和度25均写入正确。重置还原初始值并保留阈值1及Scatter0.5。测试副本清理完成。此项是回调与混合参数验证，不是实际渲染画面或UI点击验收。

取景器RenderTexture此前读数为512乘288，其显示清晰度仍低于主画面，未在此次擅自增加分辨率。HDRP Bloom和景深观感仍需实际游戏确认。修改前副本位于本机临时目录 coal-photo-review-20260921-184416。
