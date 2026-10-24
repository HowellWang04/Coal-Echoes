// Unity MCP execute_code method body. Scatters RockyDesert dead grass around the factory gate.
// Re-running replaces the children of 工厂大门/门口干草 with the same deterministic layout.
var rng = new System.Random(20261020);
float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
float Gauss() => (float)(System.Math.Sqrt(-2 * System.Math.Log(1 - rng.NextDouble())) * System.Math.Cos(2 * System.Math.PI * rng.NextDouble()));

string folder = "Assets/Art Resources/ThirdPartyPacks/RockyDesert/Prefabs/Grass/";
var prefabs = new[] { "GrassDry_Desert_01", "GrassDry_Desert_02", "GrassDry_Desert_03" }
    .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(folder + n + ".prefab")).ToArray();
// GrassDry_Desert_01 switches to a harsher cutoff material on its far LODs; every LOD uses this one instead.
var grassMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art Resources/ThirdPartyPacks/RockyDesert/Materials/MI_DeadGrass_A.mat");
// Same LOD world size for every clump, so small and large ones switch and cull at the same distance
// (with the prefab thresholds: LOD1 5 m, LOD2 16 m, LOD3 40 m, culled at 80 m for a 80 degree FOV).
const float LodWorldSize = 1.34f;

// Wall frame through the gate: a runs along the wall, s is the outward distance.
var gate = new Vector2(-2232.48f, -1727.19f);
var along = new Vector2(0.68f, -0.733f).normalized;
var outward = new Vector2(along.y * -1f, along.x);
if (Vector2.Dot(outward, new Vector2(-2218.8f, -1721.3f) - gate) < 0) outward = -outward;
Vector2 Frame(float a, float s) => gate + along * a + outward * s;

// Road midline from the two connector lanes, plus the inner road lanes to keep clear.
System.Collections.Generic.List<Vector2> Sample(string name)
{
    var sc = GameObject.Find(name).GetComponent<UnityEngine.Splines.SplineContainer>();
    float len = sc.CalculateLength();
    var pts = new System.Collections.Generic.List<Vector2>();
    for (float d = 0; d <= len; d += 1f) { Vector3 p = sc.EvaluatePosition(d / len); pts.Add(new Vector2(p.x, p.z)); }
    return pts;
}
var laneA = Sample("厂门连接路向内");
var laneB = Sample("厂门连接路向外");
var midline = laneA.Select(p => (p + laneB.OrderBy(q => (q - p).sqrMagnitude).First()) * 0.5f).ToList();
var innerLanes = Sample("工厂道路向内").Concat(Sample("工厂道路向外")).ToList();
float Dist(System.Collections.Generic.List<Vector2> pts, Vector2 p) => Mathf.Sqrt(pts.Min(q => (q - p).sqrMagnitude));

// Guard post box in a yaw-only frame.
var guard = GameObject.Find("Guard Post").transform;
var guardToLocal = Matrix4x4.TRS(guard.position, Quaternion.Euler(0, guard.eulerAngles.y, 0), Vector3.one).inverse;
var gMin = new Vector2(float.MaxValue, float.MaxValue); var gMax = -gMin;
foreach (var r in guard.GetComponentsInChildren<Renderer>())
{
    var b = r.localBounds;
    for (int i = 0; i < 8; i++)
    {
        var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
        var l = guardToLocal.MultiplyPoint3x4(r.localToWorldMatrix.MultiplyPoint3x4(c));
        gMin = Vector2.Min(gMin, new Vector2(l.x, l.z)); gMax = Vector2.Max(gMax, new Vector2(l.x, l.z));
    }
}
float GuardDist(Vector2 p)
{
    var l = guardToLocal.MultiplyPoint3x4(new Vector3(p.x, 0, p.y));
    float dx = Mathf.Max(gMin.x - l.x, l.x - gMax.x), dz = Mathf.Max(gMin.y - l.z, l.z - gMax.y);
    return new Vector2(Mathf.Max(dx, 0), Mathf.Max(dz, 0)).magnitude + Mathf.Min(Mathf.Max(dx, dz), 0);
}

var keepClear = new[]
{
    (new Vector2(-2218.8f, -1721.3f), 3f),   // motorcycle
    (new Vector2(-2220.8f, -1720.7f), 3f),   // player start
    (new Vector2(-2238.32f, -1714.96f), 2.5f), // guard post front door
    (new Vector2(-2242.67f, -1717.41f), 2.5f), // guard post rear door
};

bool Allowed(Vector2 p, bool centreStrip)
{
    float a = Vector2.Dot(p - gate, along), s = Vector2.Dot(p - gate, outward);
    if (Mathf.Abs(s) < 0.45f) return false;                       // wall footing
    if (Mathf.Abs(a) < 4.5f && Mathf.Abs(s) < 7f) return false;    // gate opening and threshold
    if (GuardDist(p) < 0.25f) return false;
    foreach (var (c, r) in keepClear) if ((p - c).magnitude < r) return false;
    float road = Dist(midline, p);
    if (s > 0 && !centreStrip && road < 6.5f) return false;
    if (s < 0 && Dist(innerLanes, p) < 5.5f) return false;
    return true;
}

// Cluster seeds: (position, clumps, scale range, centre strip).
var seeds = new System.Collections.Generic.List<(Vector2 p, int n, float min, float max, bool strip)>();
foreach (float side in new[] { -1f, 1f })
{
    for (float a = 4.5f; a < 32f; a += Rand(0.9f, 1.7f))
    {
        // Pier corners and wall pillars (every ~8.6 m) collect the most.
        float pillar = Mathf.Abs(Mathf.Repeat(a + 4.3f, 8.6f) - 4.3f);
        float p = Mathf.Lerp(0.85f, 0.35f, Mathf.InverseLerp(4.5f, 32f, a)) + (pillar < 1.2f ? 0.3f : 0);
        if (rng.NextDouble() < p) seeds.Add((Frame(side * a, Rand(0.6f, 1.8f)), rng.Next(2, 6), 0.9f, 1.4f, false));
        if (a < 24f && rng.NextDouble() < p * 0.5f) seeds.Add((Frame(side * a, -Rand(0.6f, 1.6f)), rng.Next(1, 5), 0.8f, 1.2f, false));
    }
}
for (int i = 0; i < 14; i++)
{
    var l = new Vector2(Rand(gMin.x - 1.2f, gMax.x + 1.2f), Rand(gMin.y - 1.2f, gMax.y + 1.2f));
    var w = guardToLocal.inverse.MultiplyPoint3x4(new Vector3(l.x, 0, l.y));
    var p = new Vector2(w.x, w.z);
    if (GuardDist(p) > 0.3f && GuardDist(p) < 1.4f) seeds.Add((p, rng.Next(1, 4), 0.8f, 1.2f, false));
}
for (int i = 1; i < midline.Count - 1; i++)
{
    var p = midline[i];
    float s = Vector2.Dot(p - gate, outward);
    if (s < 3f || s > 45f) continue;
    var dir = (midline[i + 1] - midline[i - 1]).normalized;
    var side = new Vector2(-dir.y, dir.x);
    if (i % 2 == 0 && rng.NextDouble() < 0.55)
        seeds.Add((p + side * (rng.NextDouble() < 0.5 ? -1 : 1) * Rand(6.8f, 9.5f), rng.Next(1, 4), 0.6f, 1f, false));
    if (s > 6f && i % 2 == 0 && rng.NextDouble() < 0.6)
        seeds.Add((p + side * Rand(-0.4f, 0.4f), rng.Next(1, 3), 0.45f, 0.7f, true));
}

var root = GameObject.Find("工厂大门").transform;
var parent = root.Find("门口干草");
if (parent != null) Undo.DestroyObjectImmediate(parent.gameObject);
parent = new GameObject("门口干草").transform;
parent.SetParent(root, false);
Undo.RegisterCreatedObjectUndo(parent.gameObject, "Place gate grass");

var colliders = Terrain.activeTerrains.Select(t => t.GetComponent<TerrainCollider>()).ToArray();
var placed = new System.Collections.Generic.List<Vector2>();
foreach (var seed in seeds)
    for (int k = 0; k < seed.n; k++)
    {
        float spread = seed.strip ? 0.35f : 0.55f;
        var p = seed.p + new Vector2(Gauss(), Gauss()) * spread;
        if (!Allowed(p, seed.strip) || placed.Any(q => (q - p).sqrMagnitude < 0.45f * 0.45f)) continue;
        RaycastHit hit = default; bool grounded = false;
        var ray = new Ray(new Vector3(p.x, 0, p.y), Vector3.down);
        foreach (var c in colliders) if (c.Raycast(ray, out hit, 2000f)) { grounded = true; break; }
        if (!grounded) continue;
        float scale = Rand(seed.min, seed.max);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[rng.Next(prefabs.Length)], parent);
        var up = Vector3.Slerp(Vector3.up, hit.normal, 0.5f);
        var tilt = Quaternion.Euler(Rand(-6f, 6f), 0, Rand(-6f, 6f));
        go.transform.SetPositionAndRotation(hit.point - up * 0.05f * scale,
            Quaternion.FromToRotation(Vector3.up, up) * tilt * Quaternion.Euler(0, Rand(0f, 360f), 0));
        go.transform.localScale = new Vector3(scale, scale * Rand(0.85f, 1.15f), scale);
        go.GetComponent<LODGroup>().size = LodWorldSize / Mathf.Max(go.transform.localScale.x, go.transform.localScale.y);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            if (r.sharedMaterial != grassMaterial) r.sharedMaterial = grassMaterial;
        placed.Add(p);
    }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
return "seeds " + seeds.Count + " clumps " + placed.Count;
