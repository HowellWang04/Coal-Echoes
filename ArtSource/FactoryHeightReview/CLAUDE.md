# ArtSource/FactoryHeightReview/

## 用途

厂区对象按地形落地的高度整理记录：厂房、门卫室、大门、工业设备、围栏和散落物按占地低处落地，高坡侧允许埋入，只调对象高度，不改地形。

## 内容

- `survey.cs`、`attachments.cs`、`supports.cs`：在编辑器里采集对象占地高度、固定附件和堆叠支撑关系，写 `survey.json`、`attachments.json`、`supports.json`。
- `plan.cjs`：Node 脚本，按采集结果算分组和高度差，输出 `plan-initial.json`、`plan.json`、`relationships.json`。
- `apply.cs`：按计划调高度，写 `applied.json`。`verify.cs`：复核，写 `verification.json`、`survey-after.json`、`terrain-verification.json`。
- `transforms-before.json`、`transforms-after.json`、`terrain-hashes-before.json`：调整前后变换和地形哈希。
- `Previews/`：调整前后的场景预览。`Before/Main Game Scene.unity`：调整前场景备份。

## 与其他部分的关系

- 建筑占地按约一米间距采样，用最高细节层网格检查；固定附件跟随建筑；散落物按底部轮廓接触地面，优先保持堆叠支撑，地形高于原支撑面时整组抬到地形。
- 九台分散空调按各自所在建筑处理，含两套料仓的分组按两套设备分别处理，层级没改。宿舍楼交叠范围内的六段落地围栏按地面放，不跟随楼内支撑面。
- 散落杂物之后又被 `../FactoryYardLayout/` 按种类重新摆放，`transforms-after.json` 不是杂物的现状。

## 注意

- 大坡差建筑的高坡侧可能遮住门窗和通道。
