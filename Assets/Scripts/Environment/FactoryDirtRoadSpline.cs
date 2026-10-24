using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

// The spline stays editable; the temporary decal mesh follows the existing terrain.
[ExecuteAlways]
[RequireComponent(typeof(SplineContainer), typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class FactoryDirtRoadSpline : MonoBehaviour
{
    [Min(1f)] public float width = 8f;
    public AnimationCurve widthMultiplier = AnimationCurve.Constant(0f, 1f, 1f);
    [Min(1f)] public float textureLength = 16f;
    public float textureOffset = 25.35f;
    [Min(0.5f)] public float endFade = 7f;

    private Mesh roadMesh;
    private bool rebuildPending;

    private void OnEnable()
    {
        Spline.Changed += OnSplineChanged;
        rebuildPending = true;
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
        if (roadMesh != null)
        {
            if (Application.isPlaying) Destroy(roadMesh);
            else DestroyImmediate(roadMesh);
        }
    }

    private void OnValidate() => rebuildPending = true;

    private void OnSplineChanged(Spline spline, int knot, SplineModification modification)
    {
        if (spline == GetComponent<SplineContainer>().Spline)
            rebuildPending = true;
    }

    private void Update()
    {
        if (rebuildPending)
            Rebuild();
    }

    [ContextMenu("Rebuild Road")]
    public void Rebuild()
    {
        rebuildPending = false;
        var container = GetComponent<SplineContainer>();
        var spline = container.Spline;
        if (spline.Count < 2)
            return;

        var terrainColliders = new List<TerrainCollider>();
        foreach (var terrain in Terrain.activeTerrains)
            terrainColliders.Add(terrain.GetComponent<TerrainCollider>());

        const float spacing = 0.4f;
        int columns = Mathf.CeilToInt(width / spacing) + 1;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        float length = container.CalculateLength();
        float t = 0f;
        float distance = 0f;
        Vector3 previous = container.EvaluatePosition(0f);
        int row = 0;

        while (true)
        {
            Vector3 center = container.EvaluatePosition(t);
            Vector3 tangent = container.EvaluateTangent(t);
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
            float rowWidth = width * widthMultiplier.Evaluate(t);
            distance += Vector3.Distance(previous, center);
            previous = center;

            for (int column = 0; column < columns; column++)
            {
                float u = column / (float)(columns - 1);
                Vector3 point = center + right * ((u - 0.5f) * rowWidth);
                var ray = new Ray(point + Vector3.up * 100f, Vector3.down);
                bool grounded = false;
                foreach (var terrain in terrainColliders)
                {
                    if (!terrain.Raycast(ray, out RaycastHit hit, 200f))
                        continue;
                    point.y = hit.point.y + 0.065f;
                    grounded = true;
                    break;
                }
                if (!grounded)
                {
                    Debug.LogError("Road spline extends beyond the loaded terrain.", this);
                    return;
                }
                vertices.Add(transform.InverseTransformPoint(point));
                uv.Add(new Vector2(u, (textureOffset + distance) / textureLength));
                float fade = Mathf.SmoothStep(0f, 1f, distance / endFade)
                    * Mathf.SmoothStep(0f, 1f, (length - distance) / endFade);
                // Red carries the local width so the shader blends road shoulders in metres.
                colors.Add(new Color(rowWidth, 1f, 1f, fade));

                if (row == 0 || column == 0)
                    continue;
                int d = row * columns + column;
                int a = d - columns - 1;
                triangles.Add(a);
                triangles.Add(d - 1);
                triangles.Add(d);
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(a + 1);
            }

            if (t >= 1f)
                break;
            SplineUtility.GetPointAtLinearDistance(spline, t, spacing, out t);
            row++;
        }

        if (roadMesh == null)
            roadMesh = new Mesh { name = "Factory Dirt Road Spline Mesh", hideFlags = HideFlags.DontSave };
        roadMesh.Clear();
        roadMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        roadMesh.SetVertices(vertices);
        roadMesh.SetUVs(0, uv);
        roadMesh.SetColors(colors);
        roadMesh.SetTriangles(triangles, 0);
        roadMesh.RecalculateNormals();
        roadMesh.RecalculateTangents();
        roadMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = roadMesh;
    }
}
