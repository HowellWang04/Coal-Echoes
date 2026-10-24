# 双页主菜单排版预览

2026 年 10 月 12 日完成第一版真实 Unity uGUI 排版，预览见 `menu-spread-1920.png`。参考画布为 1920 × 1080，报纸铺满画面，中央竖折缝与横向纸痕保留。随后用户确认继续，正式场景已接入实时照片与放大转场，当前实现和检查见 `Runtime/CLAUDE.md`。

下文记录独立排版审核稿。这个 Preview Prefab 保留静态截图，不会自动更新相机画面，也不绑定开始或退出事件；正式场景已使用实时照片和原有按钮事件。

## 布局与文字

左页放朱红报头、英文名称、厂区通讯小框、索道消息和英文菜单。下方穿插食堂与电影通知。右页使用完整的 16 比 9 厂区照片，下方安排工具登记、修车、借阅和失物招领。日期定为 1980 年 10 月 3 日，外公进厂当天，见下方“日期与头条”。期号仍未确定，小框暂用栏目资料。

全部 39 个标题、正文和按钮标签使用 TextMeshPro，未将文字烘焙进纸面。中文正文草案在 `copy.zh-CN.json`，均为本项目虚构的日常消息，不引用参考报纸的文章。厂区配图只标注全景，不将当前废弃厂区声称为八十年代作业现场。

英文沿用 Bitter Regular，正文沿用 Source Han Serif SC Medium。报头尝试 [Ma Shan Zheng](https://github.com/google/fonts/tree/main/ofl/mashanzheng) 的书法字形，源字体与 SIL OFL 1.1 许可保存在 `Assets/Art Resources/UI/NewspaperMenu/Fonts/`。此字形仍待用户看图确认。

新增两个静态 SDF 字库只供本预览使用。没有清理或修改其他界面的动态字库。以后扩充正文，需要重新烘焙本预览的中文字库。

## 纸墨与照片

预览纸底与照片共用原有 Newsprint 贴图及同一份材质参数。shader 增加 `_PaperUV`、`_PrintRect` 与 `_PaperSize`，先将照片局部坐标映射到整张报纸，再取纸纹和纤维噪声。照片内的横折痕因此与外面的纸面接续，不再单独取一块带折痕的小纸。

报纸取纸纹的范围为 scale `(0.91, 0.9)`、offset `(0.046, 0.054)`。主照片矩形为 `(1008, 158, 852, 479.25)`，坐标从画布左上计算，宽高比为 16 比 9。新增参数的默认值保留原来的单张照片预览采样方式。

为适应版面中的较小照片，预览材质副本将 Uneven Ink Coverage 从 0.22 减为 0.09，减弱粗碎缺墨；原有保存材质未因此改值。纸张色、油墨色与其他图像参数继续读取当前保存材质。中央凹折的细阴影由独立 UI 美术层表现。

## 文件与复现

`BuildPreview.cs` 为一次性编辑器制作脚本，通过 Unity CLI 的 eval_file 执行，不编入运行时代码。它生成并更新以下独立预览资源。

| 文件 | 用途 |
| --- | --- |
| `Assets/Art Resources/UI/NewspaperMenu/SpreadPreview/NewspaperSpreadPreview.prefab` | 可在 Unity Prefab Mode 查看和编辑的排版 |
| `Assets/Art Resources/UI/NewspaperMenu/SpreadPreview/SpreadPaper.png` | 共用纸墨处理得到的空白纸面 |
| `Assets/Art Resources/UI/NewspaperMenu/SpreadPreview/CameraPhoto.png` | 本次相机截图经过纸墨处理后的照片 |
| `menu-spread-1920.png` | 完整 Unity 编辑器渲染预览 |
| `camera-original.png`、`effect-zero.png` | 同次相机原图与效果归零检查 |
| `checks.json` | 本次文字、比例及 shader 检查结果 |

只在 Edit Mode 执行，临时对象和渲染纹理在 finally 中销毁。主场景未保存，保留原有未保存状态。新字库资产使用 YAML 文本格式，保留各自 meta。

## 检查与后续

已检查 1920 × 1080 实际渲染。39 个文字对象无溢出和缺字，照片比例正确，shader 无错误。效果归零后与同次原相机图的最大 RGB 通道差为 0。

实时照片、照片放大至全屏、纸墨消退与原有抬头对白入口现已接入正式场景，旧飞走动画已移除。不同画幅预览及编辑器隔离检查已完成，连续动画和完整操作仍待 Play Mode 验收。

## 日期与头条

2026 年 10 月 21 日，报头加上日期“一九八〇年十月三日　星期五　农历庚申年八月廿五”，即外公日记第一页记下的进厂当天。日期是新增的 `IssueDate`，复制自 `InfoBody` 沿用同一中文字库与墨色，16 号字右对齐到 x 720，基线与 COAL ECHOES 同在 y 负 212.1，读取 `newspaper-menu.json` 的 `issueDate`。

左页头条从索道检修改为欢迎这一批新职工进厂，右下角的工具领用归还改为索道检修完成，正文都在 `newspaper-menu.json`。新增的 83 个汉字已补进 `Spread Chinese SDF` 静态字库：临时把源字体指回 `SourceHanSerifSC-Medium.otf`，切为动态加字后再切回静态并清空源字体引用，资产 GUID 不变，字库从 412 字增至 495 字。1920 × 1080 渲染检查无溢出、无缺字，截图为 `dated-after.png`，改前为 `dated-before.png`。`copy.zh-CN.json` 是排版预览的旧稿，未同步。

## 年代字体

2026 年 10 月 22 日，为贴近 1980 年铅字报纸，字体按用途分开，均为 SIL OFL 许可，源文件与许可放在 `Assets/Art Resources/UI/NewspaperMenu/Fonts/`。

| 用途 | 字体 | 字库资产 |
| --- | --- | --- |
| 正文栏、启事、图注、日期、栏目条、页脚 | 思源宋体 Regular（`SourceHanSerifCN-Regular.otf`），比原来的 Medium 细，接近铅字书宋 | `SpreadPreview/Spread Body SDF` |
| 副题、正文开头的“本报讯” | 朱雀仿宋（`ZhuqueFangsong-Regular.ttf`，TrionesType/zhuque v0.212） | `Resources/Fonts & Materials/Spread Fangsong SDF` |
| 头条 | 思源宋体 Heavy（`SourceHanSerifCN-Heavy.otf`），取代原来 Medium 加 TMP 合成粗体 | `SpreadPreview/Spread Headline SDF` |
| 其他小标题、报栏名 | 保持思源宋体 Medium | `SpreadPreview/Spread Chinese SDF` |

三份字库都是 2048 静态图集，44 号采样、间距 6，按当前 `newspaper-menu.json` 和报纸上全部文字烘焙 421 字；以后加新字要补字库。仿宋字库放在 Resources 下，是因为“本报讯”由 `NewspaperMenuUI.ApplyCopy` 包上 TMP 的 `<font>` 标签换字体，TMP 按名字从 `Resources/Fonts & Materials/` 读取；组件上的 Dateline Font 指向它，JSON 里的正文不用写标签。

洇墨用 TMP 材质模拟，字形略微外扩并柔化边缘：正文 Face Dilate 0.05、Softness 0.05，仿宋 0.04、0.04，头条 0.02、0.03。头条调得更轻，0.12 的柔化在大字上像模糊而不像洇墨。前后对照见 `fonts-compare.png`（上为原样，下为新字体加洇墨），整版为 `fonts-period.png`，渲染检查无溢出、无缺字。
