var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
var top = all.Single(t => t.name == "CablewayTower_11");
var previous = all.Single(t => t.name == "CablewayTower_10");
var forward = Vector3.ProjectOnPlane(top.position - previous.position, Vector3.up).normalized;
var right = Vector3.Cross(Vector3.up, forward);
float[] V(Vector3 p) => new[] { p.x, p.y, p.z };
var terrain = Terrain.activeTerrains.Single(t => AssetDatabase.GetAssetPath(t.terrainData) == "Assets/TerrainData/SheepRangeGenerated/Tile_0_0_Main.asset");
float Ground(Vector3 p) => terrain.SampleHeight(p) + terrain.transform.position.y;
var grid = new List<object>();
for (int z = -30; z <= 40; z += 2) for (int x = -20; x <= 20; x += 2) {
    var p = top.position + right * x + forward * z;
    grid.Add(new { x, z, height = Ground(p) });
}
var cables = new List<object>();
foreach (var mf in all.Select(t => t.GetComponent<MeshFilter>()).Where(m => m != null && m.sharedMesh != null && AssetDatabase.GetAssetPath(m.sharedMesh).Contains("/Cable_"))) {
    var mesh = mf.sharedMesh;
    cables.Add(new { name = mf.name, path = AnimationUtility.CalculateTransformPath(mf.transform, null), asset = AssetDatabase.GetAssetPath(mesh), position = V(mf.transform.position), rotation = V(mf.transform.eulerAngles), scale = V(mf.transform.lossyScale), vertices = mesh.vertices.Select(v => V(mf.transform.TransformPoint(v))).ToArray(), triangles = mesh.triangles, uv = mesh.uv.Select(v => new[] {v.x,v.y}).ToArray() });
}
var report = new { origin = V(top.position), forward = V(forward), right = V(right), grid, cables, hierarchy = all.Where(t => AnimationUtility.CalculateTransformPath(t, null).Contains("索道群") && t.GetComponentsInParent<Transform>().Length <= 5).Select(t => new { path = AnimationUtility.CalculateTransformPath(t, null), components = t.GetComponents<Component>().Select(c => c == null ? "Missing" : c.GetType().Name).ToArray() }).ToArray() };
System.IO.File.WriteAllText("ArtSource/Cableway/SummitStation/geometry.json", Newtonsoft.Json.JsonConvert.SerializeObject(report));
var view = SceneView.lastActiveSceneView;
view.LookAtDirect(top.position + Vector3.up * 3, Quaternion.LookRotation((forward + right * .7f + Vector3.down * .65f).normalized), 35);
view.Repaint();
return new { top = V(top.position), ground = Ground(top.position), forward = V(forward), cables = cables.Count, sceneDirty = scene.isDirty };
