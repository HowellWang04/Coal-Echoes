using UnityEngine;

// Both frames draw after scene post-processing. The handheld frame follows the screen's projection.
[RequireComponent(typeof(CanvasRenderer))]
public class EchoViewfinderGraphic : UnityEngine.UI.MaskableGraphic
{
    [SerializeField] private float thickness = 3f;
    private readonly Vector2[] points = new Vector2[11];
    private readonly Vector2[] corners = new Vector2[4];
    private bool projected;
    private float expansion = 1f;

    public void SetExpansion(float value)
    {
        if (Mathf.Approximately(expansion, value)) return;
        expansion = value;
        SetVerticesDirty();
    }

    public void ProjectOntoScreen(Camera camera, Transform screen)
    {
        projected = true;
        for (int i = 0; i < 4; i++)
        {
            // CameraScreen is a unit XY quad. Its central 40 percent is the aiming frame.
            Vector3 local = new Vector3((i & 1) == 0 ? -.2f : .2f, (i & 2) == 0 ? -.2f : .2f, 0f);
            Vector3 pixel = camera.WorldToScreenPoint(screen.TransformPoint(local));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, pixel, null, out corners[i]);
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        if (!projected)
        {
            corners[0] = new Vector2(rect.xMin, rect.yMin);
            corners[1] = new Vector2(rect.xMax, rect.yMin);
            corners[2] = new Vector2(rect.xMin, rect.yMax);
            corners[3] = new Vector2(rect.xMax, rect.yMax);
        }
        float shortSide = Mathf.Min(Vector2.Distance(corners[0], corners[1]), Vector2.Distance(corners[0], corners[2]));
        float length = shortSide * .14f;
        float radius = shortSide * .035f;
        Vector2 center = (corners[0] + corners[1] + corners[2] + corners[3]) * .25f;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 origin = center + (corners[corner] - center) * expansion;
            Vector2 inwardX = (corners[corner ^ 1] - corners[corner]).normalized;
            Vector2 inwardY = (corners[corner ^ 2] - corners[corner]).normalized;
            points[0] = new Vector2(0, length);
            for (int i = 0; i <= 8; i++)
            {
                float angle = Mathf.Lerp(Mathf.PI, Mathf.PI * 1.5f, i / 8f);
                points[i + 1] = new Vector2(radius + Mathf.Cos(angle) * radius, radius + Mathf.Sin(angle) * radius);
            }
            points[10] = new Vector2(length, 0);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 a = origin + inwardX * points[i].x + inwardY * points[i].y;
                Vector2 b = origin + inwardX * points[i + 1].x + inwardY * points[i + 1].y;
                Vector2 direction = (b - a).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * .5f);
                int start = vh.currentVertCount;
                vh.AddVert(a - normal, color, Vector2.zero);
                vh.AddVert(a + normal, color, Vector2.zero);
                vh.AddVert(b + normal, color, Vector2.zero);
                vh.AddVert(b - normal, color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
