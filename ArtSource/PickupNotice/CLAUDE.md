# ArtSource/PickupNotice/

## 用途

右下角拾取提示的手 icon 和效果图。拿到物品时右下角出一行手 icon 加物品名，场景里已搭好。

## 内容

- `build_pickup_notice.py`：用 Pillow 生成手 icon、icon 对照图和效果图，`python build_pickup_notice.py` 重新导出；icon 形状、效果图里的行高、字号、边距和暗带浓度都在源码里。
- `Icons/PickupIcon_Hand.png`：256 像素白色透明底的手 icon，一只手从上往下抓住一件实心小物件；画法和独白的云朵思考 icon 一样，同样线宽，外形内收一圈成描边。
- `Previews/PickupIcon_Sheet.png`：手 icon 和云朵思考 icon 在深浅底上的大图和 32 像素小图对照。
- `Previews/PickupNotice_GateView.png`：三行提示按场景尺寸画在独白效果图 G 上，用来定暗带浓度。
- `Previews/Unity_PickupNotice_Edit.png`：场景里实际提示的编辑器渲染，画布临时改成相机空间才渲染得到，受景深和雾影响偏糊偏淡，只用来核对位置。

## 与其他部分的关系

- `PickupIcon_Hand.png` 复制到 `Assets/Art Resources/UI/Pickup Notice/`，导入为 Single Sprite 并开了 mipmap（游戏里只显示 30 像素左右，不开会锯齿）；改了图要重新运行脚本并再复制一次。
- 场景对象是 `In-game UI Root/PickupNoticeUIRoot`，排在 PlayerPanelUIRoot 前面，面板打开时被盖住。控制脚本 `Assets/Scripts/In-Game Systems/Pickup Notice System/PickupNoticeUI.cs`，行模板是 `Rows/RowTemplate`（暗带 `Band`、`Icon`、`Label`），运行时复制出每一行。
- 文字材质 `Assets/Art Resources/UI/Fonts/Imprima 400 SDF - Pickup Underlay.mat` 是煤灰黑柔影，参数照独白的 Underlay 材质；物品查看界面、手持相机按键提示和 F 交互提示的文字也用它，改参数会一起变；暗带复用 `Assets/Art Resources/UI/Inner Monologue/MonologueBand.png`，染成煤灰黑。
- 背包物品经 `ItemBackpackManager.OnItemAdded` 出提示，摩托车的日记和合照由 `GrandpaDiaryPickup` 调 `PickupNoticeUI.Show`。行为要求见 `Docs/Specs/Levels/opening-level-one-spec.md` 的“右下角拾取提示”一节，文字汇总在 `Docs/game-text.md`。

## 注意

- 效果图的行高、字号、边距和暗带浓度是照场景里的值写进脚本的，改了场景要同步脚本里的常量再出图。
- 效果图的背景是独白效果图 G，独白效果图重新导出后这里也要重新运行。
- 编辑器渲染图偏暗偏糊，对比度以 Play Mode 为准。
