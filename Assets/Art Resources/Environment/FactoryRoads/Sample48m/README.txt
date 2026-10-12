48 米柏油路样段

FactoryRoad48m.prefab 宽 6 米，长 48 米，包含路面、砂砾路肩和 20 处裂缝破损。已按反馈移除全部 12 处修补贴花，路面与路肩有碰撞体。

Asphalt48m_DustEdges 使用 HDRP LayeredLit。两层柏油分别使用常规 UV 和旋转错位 UV，以覆盖整段道路的非重复遮罩混合，第三层使用项目已有 gravelly_sand 材质贴图形成积尘。DustBlend 的 R 通道控制柏油方向混合，G 通道控制积尘。
RoadUVBlend 是文本 YAML 格式网格资产，包含连续 UV0 和旋转错位 UV1。柏油与砂砾的纹理按约 3 米尺度铺贴。
Asphalt48m_Plain 是纯底材对照，只用于查看重复感。

ArtSource/FactoryRoads 内有整体、纯底材、眼高和近景四张编辑器渲染图，以及验证记录和一次性制作脚本。样段尚未放入主场景，未运行 Play Mode。
已通过方向混合、积尘和贴花弱化重复，但原始柏油的特征裂缝在俯视或长距离观察时仍可辨认，不代表正式道路的最终效果。
