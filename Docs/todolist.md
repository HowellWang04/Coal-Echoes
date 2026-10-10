# 待办

做完就删掉，过程记到[开发日志](development-log.md)。

## 设计与内容

- [ ] 第二关上山索道与第三关旧装煤站、值班室、站外山脊的具体流程、物品和文字
- [ ] 按爷爷与工友未完成的夜景约定修订运行日记 JSON、第一关值班记录与便签，保留既有页面和存档键
- [ ] 第一关的 C 类物品
- [ ] 开篇引子与夜景结尾文字，确定工友姓名、年代、排班及上山限制的具体表述
- [ ] 夜景照明和结尾拍照入手账的交互，确定是否为强制完成条件
- [ ] 爷爷日记和主角便签的两套字体（先确认授权）

## 功能

- [ ] 每关显示剩余未收集的回响数量
- [ ] 多个回响同时入镜时，`EchoSystemManager` 目前只处理第一个
- [ ] 把索道的 `StartMovement()` 绑到关卡事件上
- [ ] 日记：逐张校准四种便签样式的文字区域和角度，验证通过后删掉旧的逐页场景对象 `DiaryPage_p01` 到 `p03`
- [ ] 相册内存：大图预览关闭时没有销毁旧纹理；缩略图预热会缓存全部照片而不只是当前页
- [ ] 构建场景列表从 `SampleScene` 改成主场景

## 场景与美术

* [ ] 确认并局部应用 Tile 2,2 的离线断层修复候选，同步局部材质、Main 与 Draft 缓存及热浪，再重做北侧降山候选，见 `TerrainReferences/Tile22CliffRepairPreview/README.md`

* [ ] 清理同批 11 个旧塔脚的纯沙土圆形材质残留，只按现有高度更新局部材质及 Main、Draft 缓存，见 `TerrainReferences/PeripheralMountainsPreview/Diagnosis/Circle2/README.md`

* [ ] 在 Play Mode 验收已应用的全图外围 800 米过渡，检查北侧、东侧和南侧山脚、贴图及 LOD 切换，并单独处理原有 Draft 接缝，见 `TerrainReferences/WholeMapGradient800/README.md`

- [ ] 按[第一关美术配置](art-assets.md)补齐铁链、断线钳及回响取景提示，核对大门、围墙、门卫室和街道照片的配置
- [ ] 山脚站棚局部埋进坡里，需要整平
- [ ] 岩石素材选型摆放，确认简化碰撞、接缝与 LOD 切换的 Play Mode 效果
- [ ] 确认并完成 URP Tree Pack 六个树木材质的 HDRP 转换
- [ ] UI 美术：回响弹窗、便签贴纸、回响收集提示
- [ ] 音频：快门声、环境风声、背景音乐

## 等用户在 Play Mode 里确认

* [ ] 圈 2 旧厂区矩形材质印迹的修复效果与 Main、Draft 切换，见 `TerrainReferences/PeripheralMountainsPreview/Diagnosis/Circle2/README.md`

* [ ] 外围山体围合、预留出山通道遮挡及 Main 与 Draft 切换观感，留意原有 Draft 接缝差异，见 `TerrainReferences/PeripheralMountainsPreview/Applied/README.md`

- [ ] 厂区建筑和物品贴地效果，高坡侧埋入后的门窗遮挡与通行
- [ ] 索道吊斗运行、山顶终点站
- [ ] HDRP 迁移后的整体画面，拍照调节滑条、景深和重置
- [ ] 沙尘、热浪的观感和帧率
- [ ] Lands、RocksGobi、RockyDesert 调色后在地形上的效果
- [ ] 日记翻页、便签左右页侧和随机样式是否保持
- [ ] UI 动效：按钮反馈、面板滑入、照片飞入手账
