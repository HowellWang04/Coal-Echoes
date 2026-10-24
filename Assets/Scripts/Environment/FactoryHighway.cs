using System.Collections.Generic;
using UnityEngine;

// The saved profile matches the locally graded terrain, which supplies the collision.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class FactoryHighway : MonoBehaviour
{
    public float start = -650f;
    public float spacing = 2f;
    public float[] heights;
    public float width = 15.5f;
    public float crown = 0.155f;
    public float junctionStation;
    public float junctionHalfWidth = 12f;

    private Mesh mesh;
    private bool rebuildPending;
    private Terrain[] terrains;
    private float nextTerrainCheck;
    private int terrainHash;

    private void OnEnable() => rebuildPending = true;
    private void OnValidate() => rebuildPending = true;
    private void Update()
    {
        if (Time.realtimeSinceStartup >= nextTerrainCheck)
        {
            nextTerrainCheck = Time.realtimeSinceStartup + 1f;
            var active = Terrain.activeTerrains;
            int hash = 0;
            foreach (var terrain in active) hash ^= terrain.GetEntityId().GetHashCode();
            if (hash != terrainHash)
            {
                terrainHash = hash;
                rebuildPending = true;
                foreach (var road in transform.parent.GetComponentsInChildren<FactoryDirtRoadSpline>())
                    road.Rebuild();
            }
        }
        if (rebuildPending) Rebuild();
    }

    private void OnDisable()
    {
        if (mesh == null) return;
        if (Application.isPlaying) Destroy(mesh);
        else DestroyImmediate(mesh);
    }

    private Vector3 Point(float station, float across)
    {
        float index = Mathf.Clamp((station - start) / spacing, 0f, heights.Length - 1f);
        int i = Mathf.Min(Mathf.FloorToInt(index), heights.Length - 2);
        float y = Mathf.Lerp(heights[i], heights[i + 1], index - i);
        var point = new Vector3(across, y + crown * (1f - Mathf.Abs(across) / (width * 0.5f)), station);
        Vector3 world = transform.TransformPoint(point);
        // Main and Draft share the roadbed but interpolate it at different resolutions.
        foreach (var terrain in terrains)
        {
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (world.x < origin.x || world.z < origin.z || world.x > origin.x + size.x || world.z > origin.z + size.z)
                continue;
            world.y = terrain.SampleHeight(world) + origin.y;
            point = transform.InverseTransformPoint(world);
            break;
        }
        return point + Vector3.up * 0.08f;
    }

    [ContextMenu("Rebuild Highway")]
    public void Rebuild()
    {
        rebuildPending = false;
        if (heights == null || heights.Length < 2) return;
        terrains = Terrain.activeTerrains;
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
        float end = start + spacing * (heights.Length - 1);

        float Fade(float station) => Mathf.SmoothStep(0f, 1f, (station - start) / 65f)
            * Mathf.SmoothStep(0f, 1f, (end - station) / 65f);

        void Quad(float a, float b, float left, float right, int material)
        {
            int first = vertices.Count;
            foreach (var p in new[] { new Vector2(left, a), new Vector2(right, a), new Vector2(left, b), new Vector2(right, b) })
            {
                vertices.Add(Point(p.y, p.x));
                uv.Add(material == 0 ? new Vector2((p.x + width * 0.5f) / 2.1f, p.y / 2.1f)
                    : new Vector2((p.x - left) / (right - left), p.y));
                colors.Add(new Color(material == 0 ? width : right - left, 1f, 1f, Fade(p.y)));
            }
            triangles[material].AddRange(new[] { first, first + 2, first + 3, first, first + 3, first + 1 });
        }

        for (float s = start; s < end - 0.01f; s += spacing)
        {
            float next = Mathf.Min(s + spacing, end);
            for (int c = 0; c < 32; c++)
                Quad(s, next, width * (c / 32f - 0.5f), width * ((c + 1) / 32f - 0.5f), 0);
            bool junction = Mathf.Abs((s + next) * 0.5f - junctionStation) < junctionHalfWidth;
            if (!junction) Quad(s, next, -7.35f, -7.2f, 1);
            Quad(s, next, 7.2f, 7.35f, 1);
            if (junction) continue;
            Quad(s, next, -0.225f, -0.075f, 2);
            Quad(s, next, 0.075f, 0.225f, 2);
        }
        for (float s = start + 4f; s < end - 6f; s += 12f)
        {
            if (Mathf.Abs(s + 3f - junctionStation) < junctionHalfWidth + 6f) continue;
            foreach (float across in new[] { -3.7f, 3.7f })
                for (float d = s; d < s + 6f; d += spacing)
                    Quad(d, d + spacing, across - 0.075f, across + 0.075f, 1);
        }
        if (mesh == null) mesh = new Mesh { name = "Factory Highway Surface", hideFlags = HideFlags.DontSave };
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetColors(colors);
        mesh.subMeshCount = 3;
        for (int i = 0; i < 3; i++) mesh.SetTriangles(triangles[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}
