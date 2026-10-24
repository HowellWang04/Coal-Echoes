// Unity MCP execute_code method body. Adds dead grass right in front of the factory gate, on top of 门口干草.
// Re-running replaces the children of 工厂大门/门口补草 with the same deterministic layout; 门口干草 is left untouched.
var rng = new System.Random(20261023);
float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
float Gauss() => (float)(System.Math.Sqrt(-2 * System.Math.Log(1 - rng.NextDouble())) * System.Math.Cos(2 * System.Math.PI * rng.NextDouble()));

string folder = "Assets/Art Resources/ThirdPartyPacks/RockyDesert/Prefabs/Grass/";
var prefabs = new[] { "GrassDry_Desert_01", "GrassDry_Desert_02", "GrassDry_Desert_03" }
    .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(folder + n + ".prefab")).ToArray();
var grassMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art Resources/ThirdPartyPacks/RockyDesert/Materials/MI_DeadGrass_A.mat");
const float LodWorldSize = 1.34f;

// Wall frame through the gate: a runs along the wall, s is the outward distance.
var gate = new Vector2(-2232.48f, -1727.19f);
var along = new Vector2(0.68f, -0.733f).normalized;
var outward = new Vector2(along.y * -1f, along.x);
if (Vector2.Dot(outward, new Vector2(-2218.8f, -1721.3f) - gate) < 0) outward = -outward;
Vector2 Frame(float a, float s) => gate + along * a + outward * s;

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
float Dist(System.Collections.Generic.List<Vector2> pts, Vector2 p) => Mathf.Sqrt(pts.Min(q => (q - p).sqrMagnitude));

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

// Outside the gate only (it swings inward). Piers and infill walls reach s 1.4, the threshold slab s 1.25.
// The wheel ruts run 1.6 to 4.6 m either side of the road midline; the foot of the closed gate ignores them.
bool Allowed(Vector2 p, bool inRuts)
{
    float a = Vector2.Dot(p - gate, along), s = Vector2.Dot(p - gate, outward);
    if (s < (Mathf.Abs(a) < 5.6f ? 1.1f : Mathf.Abs(a) < 9.6f ? 1.45f : 0.5f)) return false;
    if (GuardDist(p) < 0.25f) return false;
    foreach (var (c, r) in keepClear) if ((p - c).magnitude < r) return false;
    float road = Dist(midline, p);
    if (!inRuts && road > 1.6f && road < 4.6f) return false;
    return true;
}

// Cluster seeds: (position, clumps, scale range, allowed in ruts).
var seeds = new System.Collections.Generic.List<(Vector2 p, int n, float min, float max, bool ruts)>();
// Along the foot of the closed gate.
for (float a = -5.4f; a < 5.6f; a += Rand(0.7f, 1.2f))
    if (rng.NextDouble() < 0.65) seeds.Add((Frame(a, Rand(1.2f, 1.9f)), rng.Next(1, 4), 0.55f, 0.95f, true));
// Both corners of each pier.
foreach (float side in new[] { -1f, 1f })
{
    seeds.Add((Frame(side * Rand(5.6f, 6.4f), Rand(1.4f, 1.9f)), rng.Next(3, 6), 0.9f, 1.4f, true));
    seeds.Add((Frame(side * Rand(7.4f, 8.4f), Rand(1.6f, 2.2f)), rng.Next(2, 5), 0.9f, 1.4f, true));
}
// The apron between the gate and the motorcycle, thinning out with distance.
for (int i = 0; i < 45; i++)
{
    var p = Frame(Rand(-9f, 9f), Rand(2.5f, 13f));
    float s = Vector2.Dot(p - gate, outward);
    if (rng.NextDouble() > Mathf.Lerp(0.9f, 0.35f, Mathf.InverseLerp(2.5f, 13f, s))) continue;
    bool centre = Dist(midline, p) < 1.6f;
    seeds.Add((p, rng.Next(1, 4), centre ? 0.45f : 0.6f, centre ? 0.75f : 1f, false));
}
// Wall base beside the gate.
foreach (float side in new[] { -1f, 1f })
    for (float a = 9f; a < 18f; a += Rand(0.8f, 1.3f))
        if (rng.NextDouble() < 0.6) seeds.Add((Frame(side * a, Rand(0.6f, 2.2f)), rng.Next(2, 5), 0.9f, 1.4f, false));
// Road shoulders further out, inside the band the first pass left clear.
for (int i = 1; i < midline.Count - 1; i++)
{
    var p = midline[i];
    float s = Vector2.Dot(p - gate, outward);
    if (s < 13f || s > 24f || i % 2 != 0 || rng.NextDouble() > 0.6) continue;
    var dir = (midline[i + 1] - midline[i - 1]).normalized;
    var side = new Vector2(-dir.y, dir.x);
    seeds.Add((p + side * (rng.NextDouble() < 0.5 ? -1 : 1) * Rand(5.2f, 6.6f), rng.Next(1, 4), 0.6f, 1f, false));
}

var root = GameObject.Find("工厂大门").transform;
var placed = root.Find("门口干草").Cast<Transform>().Select(t => new Vector2(t.position.x, t.position.z)).ToList();
var parent = root.Find("门口补草");
if (parent != null) Undo.DestroyObjectImmediate(parent.gameObject);
parent = new GameObject("门口补草").transform;
parent.SetParent(root, false);
Undo.RegisterCreatedObjectUndo(parent.gameObject, "Add gate front grass");

var colliders = Terrain.activeTerrains.Select(t => t.GetComponent<TerrainCollider>()).ToArray();
int added = 0;
foreach (var seed in seeds)
    for (int k = 0; k < seed.n; k++)
    {
        var p = seed.p + new Vector2(Gauss(), Gauss()) * 0.5f;
        if (!Allowed(p, seed.ruts) || placed.Any(q => (q - p).sqrMagnitude < 0.45f * 0.45f)) continue;
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
        added++;
    }
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
return "seeds " + seeds.Count + " clumps " + added;
