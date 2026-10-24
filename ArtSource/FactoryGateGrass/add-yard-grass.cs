// Unity MCP execute_code method body. Fills the yard just inside the factory gate with dead grass.
// Re-running replaces the children of 工厂大门/厂内干草 with the same deterministic layout; 门口干草 and 门口补草 are left untouched.
var rng = new System.Random(20261024);
float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
float Gauss() => (float)(System.Math.Sqrt(-2 * System.Math.Log(1 - rng.NextDouble())) * System.Math.Cos(2 * System.Math.PI * rng.NextDouble()));

string folder = "Assets/Art Resources/ThirdPartyPacks/RockyDesert/Prefabs/Grass/";
var prefabs = new[] { "GrassDry_Desert_01", "GrassDry_Desert_02", "GrassDry_Desert_03" }
    .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(folder + n + ".prefab")).ToArray();
var grassMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art Resources/ThirdPartyPacks/RockyDesert/Materials/MI_DeadGrass_A.mat");
const float LodWorldSize = 1.34f;

// Wall frame through the gate: a runs along the front wall, s is the outward distance (negative is inside).
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
var innerLanes = Sample("工厂道路向内").Concat(Sample("工厂道路向外")).ToList();
float Dist(System.Collections.Generic.List<Vector2> pts, Vector2 p) => Mathf.Sqrt(pts.Min(q => (q - p).sqrMagnitude));

// The two yards the user marked: east of the road up to the east wall and the dormitory,
// and west of the road along the front wall and down the side of the workshop.
// Walls: front s = 0, east a = -40, west a = 45. The dunes and the dormitory begin around s = -23.
bool InYard(float a, float s) => s < -0.5f && s > -23f && ((a > -39.3f && a < -6f) || (a > 6f && a < 44.4f));

// Gate leaves swing 90 degrees inward from piers at a = +-5.6. The inner road is 8.5 m wide per lane;
// grass creeps about a metre onto its edge, since the road bends toward the workshop and leaves only a narrow strip.
var terrain = Terrain.activeTerrains.Select(t => t.GetComponent<TerrainCollider>()).ToArray();
bool Clear(Vector2 p, out RaycastHit ground)
{
    ground = default;
    float a = Vector2.Dot(p - gate, along), s = Vector2.Dot(p - gate, outward);
    if (!InYard(a, s) || (Mathf.Abs(a) < 6.2f && s > -6.2f)) return false;
    if (Dist(innerLanes, p) < 3.2f) return false;
    bool grounded = false;
    var down = new Ray(new Vector3(p.x, 0, p.y), Vector3.down);
    foreach (var c in terrain) if (c.Raycast(down, out ground, 2000f)) { grounded = true; break; }
    if (!grounded) return false;
    // Nothing overhead (roofs, eaves, lamp heads) and nothing standing on the spot (walls, footings, pallets, dunes).
    var solid = QueryTriggerInteraction.Ignore;
    if (Physics.RaycastAll(ground.point + Vector3.up * 40f, Vector3.down, 39.85f, ~0, solid).Any(h => !(h.collider is TerrainCollider))) return false;
    var bottom = ground.point + Vector3.up * 0.15f;
    if (Physics.OverlapCapsule(bottom, bottom + Vector3.up * 2.4f, 0.3f, ~0, solid).Any(c => !(c is TerrainCollider))) return false;
    return true;
}

// Grass collects along the compound walls and around anything standing in the yard (workshop walls, pallets, footings).
float WallDist(float a, float s) => Mathf.Min(-s, Mathf.Min(a + 40f, 45f - a));
bool NearSolid(Vector2 p)
{
    foreach (var c in terrain)
        if (c.Raycast(new Ray(new Vector3(p.x, 0, p.y), Vector3.down), out var g, 2000f))
            return Physics.OverlapSphere(g.point + Vector3.up * 0.6f, 1.8f, ~0, QueryTriggerInteraction.Ignore).Any(x => !(x is TerrainCollider));
    return false;
}

// Cluster seeds on a jittered grid; a low-frequency noise makes some patches thicker than others.
var seeds = new System.Collections.Generic.List<(Vector2 p, int n, float min, float max, float spread)>();
float noiseX = Rand(0f, 100f), noiseY = Rand(0f, 100f);
const float Cell = 1.6f;
for (float a = -40f; a < 45f; a += Cell)
    for (float s = -23f; s < 0f; s += Cell)
    {
        float ja = a + Rand(0f, Cell), js = s + Rand(0f, Cell);
        if (!InYard(ja, js)) continue;
        float patch = Mathf.PerlinNoise(noiseX + ja * 0.12f, noiseY + js * 0.12f);
        bool edge = WallDist(ja, js) < 2.2f || NearSolid(Frame(ja, js));
        float chance = 0.3f + 0.45f * Mathf.SmoothStep(0.25f, 0.75f, patch) + (edge ? 0.35f : 0f);
        if (rng.NextDouble() < chance)
            seeds.Add((Frame(ja, js), rng.Next(1, edge ? 5 : 4), edge ? 0.85f : 0.55f, edge ? 1.4f : 1.15f, 0.55f));
    }
// The strip between the workshop and the road is only about 3.5 m wide, too narrow for the grid:
// every metre or so, seed among the spots that are actually free there.
for (float s = -1.5f; s > -22.5f; s -= Rand(0.7f, 1.1f))
{
    var free = new System.Collections.Generic.List<Vector2>();
    for (float a = 6.2f; a < 13f; a += 0.25f) if (Clear(Frame(a, s), out _)) free.Add(Frame(a, s));
    for (int k = rng.Next(1, 3); k > 0 && free.Count > 0; k--)
        seeds.Add((free[rng.Next(free.Count)], rng.Next(2, 4), 0.7f, 1.3f, 0.3f));
}

var root = GameObject.Find("工厂大门").transform;
var placed = new[] { "门口干草", "门口补草" }.SelectMany(n => root.Find(n).Cast<Transform>())
    .Select(t => new Vector2(t.position.x, t.position.z)).ToList();
var parent = root.Find("厂内干草");
if (parent != null) Undo.DestroyObjectImmediate(parent.gameObject);
parent = new GameObject("厂内干草").transform;
parent.SetParent(root, false);
Undo.RegisterCreatedObjectUndo(parent.gameObject, "Add yard grass");

int added = 0;
foreach (var seed in seeds)
    for (int k = 0; k < seed.n; k++)
    {
        var p = seed.p + new Vector2(Gauss(), Gauss()) * seed.spread;
        if (placed.Any(q => (q - p).sqrMagnitude < 0.45f * 0.45f) || !Clear(p, out var hit)) continue;
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
