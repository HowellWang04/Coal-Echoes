# Assets/Art Resources/Environment/WallMurals/

## 用途

厂区墙壁和围墙使用的老化手绘墙画贴花。男检修工戴布工作帽、持检修扳手，女煤质化验员穿浅色工作罩衣、持装有灰黑色煤样悬液的玻璃锥形瓶，两张图保持相同的褪色手绘风格。

## 内容

`CoalWashWorker_Faded.png` 是保留原始透明度的褪色工人图。

`CoalWashWorker_Faded.mat` 是只影响墙面颜色的 HDRP Decal 材质。

`CoalWashWorker_Faded_Decal.prefab` 是已绑定材质的 Decal Projector 预制体，可拖入场景摆放。

`CoalQualityTechnician_Faded.png` 是女性煤质化验员的透明底墙画，由内置 imagegen 以检修工图作为风格参考生成，提示方向为八十年代洗煤厂化验员、浅灰蓝和淡土色颜料、几十年日晒雨淋后的褪色磨损。

`CoalQualityTechnician_Faded.mat` 与 `CoalQualityTechnician_Faded_Decal.prefab` 是女化验员对应的颜色贴花材质和投影预制体。

## 与其他部分的关系

预制体通过材质引用贴图，接收墙面的凹凸和粗糙度保留。主场景 `In-Scene Objects/工厂围墙/Gate Wall Murals` 下各放了一个实例，都在从厂外看大门左边的围墙上、贴在朝厂外的一面，高度一致。男检修工在再往左一个柱子外侧那段长墙上，靠近柱子；女化验员在这根柱子和大门门柱之间的短墙上，稍偏门柱一侧，不居中。Decal Projector 尺寸在场景里改过。

## 注意

将预制体拖入场景，旋转投影方向朝向墙面，让投影盒与墙面相交。保持 Transform 的 X 和 Y 等比缩放可避免人物变形，具体尺寸以 Decal Projector 为准。接收面的材质需要支持并开启 Receive Decals。

贴图使用原图 Alpha、透明边缘处理和 Clamp，保留原始长宽比及半透明磨损。材质不影响法线、金属度、遮蔽、光滑度或自发光。原图颜料几乎不透明、比砖墙亮得多，直接贴像贴纸；两个预制体的 Opacity 已调低，让砖色和灰缝透出来，两份材质的 Base Color 压成暖灰，让颜色暗下去、贴近沙尘里的墙。再调深浅时改这两处，墙面的颜色和光照也会影响最终观感。

最终墙面效果由用户摆放后确认。
