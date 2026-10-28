# ArtSource/Portfolio/LevelDesign/

## 用途

作品集总排版里的 Level design 展示页。旧版是按早期宿舍灰盒画的，新版 `LevelDesign_207.png` 按第二关 207 室里实际的 GameObject 排布和灯光重画，画风照旧版。

## 内容

- `LevelDesign_207.png`：新版展示页，1940 × 1120。
- `room207_layout.json`：从场景导出的 207 俯视布局，家具、门窗的矩形，`Map Spot`、出生点和灯的位置，窗口阳光的落点。
- `Scripts/export_room207_layout.cs`：Unity MCP `execute_code` 跑的导出脚本，写出上面的 JSON；阳光落点按 `Window Sunlight` 的真实位置和方向，穿过窗洞（算上墙厚），落到桌面、下铺铺面或地面。
- `Scripts/draw_level_design.py`：读 JSON 用 PIL 画页面，右侧两张截图取自 `../../Room207/Previews/`。`python draw_level_design.py` 直接输出。
- `Scripts/push_to_figma.py`：用同一份 JSON 在作品集 Figma 文件里搭可编辑的新版，字体、字号、颜色、圆点大小、线宽和截图尺寸照旧版在 Figma 里的实际数值；每跑一次新建一个画框。
- `Scripts/figwright-bridge.mjs`：把本机装在 Codex 下的第三方 Figma MCP Figwright 拉起来，在 `127.0.0.1:47555` 上转发工具调用，供上面的推送脚本用。
- `Reference/old-level-design.png`：旧版那一页的截图，1062 × 564，左边一条深色是截图时带进来的软件边框，不是页面的一部分。

## 与其他部分的关系

- 画的是 `In-Scene Objects/室内空间/207`，房间的做法见 `Docs/Specs/Levels/level-two-spec.md` 的 207 室一节，对象名和层级见 `../../Room207/CLAUDE.md`。
- 房间横放，门在左、窗在右，是玩家从门口走向窗下抽屉的方向；屋里面向窗户时的左侧在图的上方。
- 207 的家具、门窗、灯或截图改了，先重跑导出脚本和 `../../Room207/Scripts/render-previews.cs`，再跑画图脚本。
- 作品集 Figma 文件是“王冠豪-总排版”，项目一页面。Level design 在 `Frame 15`（节点 `641:547`）的右半边：新版是放在里面的画框 `Level design (Room 207)`（节点 `688:101`），标题和旧版一样从 x 1057 起；上一版光分三层画的 `Level design (Room 207, layered light)`（节点 `679:49`）隐藏在同一位置；旧版的标题、平面图组 `Group 6`、标签、Goals、图例和两张截图（`641:606`、`641:607`、`641:639` 到 `641:650`）都隐藏着没删，要换回来就重新显示它们、隐藏新画框。
- `push_to_figma.py` 每次在 `Frame 15` 右侧新建一个画框，不会自己替换；重推后要手动把新画框移进 `Frame 15`、删掉或隐藏上一版。
- 推送到 Figma 要先用 Figma 桌面版打开那个文件、运行开发插件 Figwright（显示 Connected），再 `node figwright-bridge.mjs` 起桥接，然后跑 `push_to_figma.py`。Figwright 只配在 `~/.codex/config.toml` 里，Claude Code 的配置里没有。

## 注意

旧版的画风要照着保留：

- 白底，左上角大号蓝色标题 Level design，颜色约 `#311FFA`，新版下面一行黑字注明是第二关的卧室 Room 207；右上角黑字 Goals 和两条编号目标。
- 平面图是俯视的扁平色块，没有描边、没有阴影：地面 `#EEEEEE`，床白色 `#FFFFFF`，桌子灰 `#A3A3A3`，柜子浅绿 `#D2DEB5`。
- 两种圆点盖在家具上：黄色 `#F8EFB8` 是用灯光做视觉引导，红色 `#F5B5B5` 是可交互的地方；黄圈用黑色细线连到一个标着 Lighting as visual guidance 的黄圈，红圈用灰色细线连到标着 Interactable areas 的红圈，标签文字压在标签圆点上。
- 旧版没画墙；207 是封闭的一间，墙按真实墙厚画成地面外的一圈深灰，门和窗在墙上留出开口，填各自的颜色，窗口中间一条细线表示玻璃。墙厚写在画图脚本里，和 `../../Room207/Scripts/build-shell.cs` 一致，改了墙厚两边一起改。
- 底部图例是小色块加文字：Table、Bed（白块带黑框）、Cabinet，207 新加的 Wall、Stove、Door、Window、Sunlight 用同样的扁平色块；Sunlight 的色块是那道光盖在白底上的颜色，和平面图里的光一致；炉子只画炉身的圆，不画烟筒，用铁锈棕，免得和墙的深灰混在一起。
- 右侧一列竖排两张截图，用搭好的 207：从 `207 Spawn` 看进屋，和半开的抽屉。
- 文字用无衬线字体（Segoe UI），全英文。

新版的内容：

- 灯光引导画的是真实的窗口阳光，只画一道光：从窗口开口到光落下的地方（桌面左半、抽屉旁的地面和左边下铺）是一个形状、一个颜色、一个透明度，比黄圈浅，盖在家具上面、圆点和连线下面，压在下面的桌子和床透出来，但不再分块画。用户看过分层的版本，嫌有色差、桌面那块像另一个图案，所以不要再拆成光束加光斑。两个黄圈压在这道光里的桌面和地面上。光照不进抽屉里面（抽屉缩在桌面下），所以图上是光落在抽屉旁边、把视线引过去。
- 可交互的是两处：半开的抽屉（看山路图、拍照）和门（按 F 离开）。
- Goals 两条是初稿：从外公日记里的话找线索；用光把视线从门口引到抽屉。用户确认前可以改。
