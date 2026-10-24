# Assets/Art Resources/Environment/Gate Area/FactoryBoundary/

## 用途

厂区围墙的 Unity 资源：9 米长墙六个变体、独立砖柱、3 米旧墙段、门卫室旁和大门旁的短墙，以及大门预制体。主场景的围墙由这里的长墙和柱子混排而成。

## 内容

- `Prefabs/FactoryWall_9m_01` 至 `_06`：场景用的长墙，六版几何和材质一致，只有逐砖颜色分布不同，都没有白灰。
- `Prefabs/FactoryPillar`：独立四面砖柱，原点在柱底中心，可用于直线或直角连接。
- `Prefabs/FactoryWall_3m`：旧的 3 米双面墙段，不含柱子，留作历史资源。
- `Prefabs/FactoryGate`：两根门柱、双扇门、锁链挂锁和门卫室一侧短墙的预制体；场景里的大门直接用 `../FactoryGate_Rebuilt/FactoryGate_Rebuilt.glb`，不用它。
- `Models/`：以上各件的 GLB，另有 `FactoryWall_GuardhouseShort.glb`（门卫室旁补缺口）和 `FactoryWall_GateShort.glb`（拓宽门洞后裁短的接墙）。GLB 不嵌贴图。

## 与其他部分的关系

- 三个基础预制体共享 `../FactoryGate_Rebuilt/` 大门 GLB 里的材质，移动目录要保留资源引用。
- 场景 `In-Scene Objects/工厂围墙` 下 88 段长墙和 89 根柱固定随机混排，相邻版本不同；转角和入口按实际距离缩短收口，坡地砖体高度不变，下方用混凝土基座接地。
- Blender 源文件、拼接预览和生成脚本在 `ArtSource/FactoryBoundary/`，大门源文件在 `ArtSource/FactoryGate/`。

## 注意

- 长墙柱心间距 9 米，墙身长 8.25 米、高约 3.68 米、厚 0.50 米，两端定位点在本地 X 为 0 和 9。拖入时缩放保持 1，不要直接拖未配置共享材质的 9 米 GLB。
- 旧 3 米模块的拼接：柱子和墙段都从原点起，每 3 米放一根柱子，墙的 Snap_Start、Snap_End 标记两端柱心，墙端稍伸入柱体避免漏光；大门未接门卫室一侧用 Snap_OuterPillar（大门本地 X 负 3.17 米），墙相对大门绕 Y 转 180 度，下一根柱在 X 负 6.17 米；转角共用一根柱，新墙绕柱底中心转 90 度。定位点只是参照，没有自动吸附。
- 大门、柱子、墙段分别有 5、3、2 个 Box Collider，门扇碰撞体随门扇，没有开门或攀爬逻辑。围墙是完整墙段，关卡可翻越位置要另外安排。
- 逐砖色差固定，复制模块会重复相同的污渍分布，没有实例随机系统。Play Mode 通行待验收。
