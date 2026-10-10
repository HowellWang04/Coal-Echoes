// Execute this body repeatedly through Unity MCP, allowing editor updates between calls.
// Uses a disposable preview scene. No project terrain is assigned or modified.
string key = "CoalTextureSyncRegression";
string folder = "TerrainReferences/TerrainTextureSync";
System.IO.Directory.CreateDirectory(folder);
string state = SessionState.GetString(key, "");
if (state == "")
{
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
    var original = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).First();
    var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
    var root = new GameObject("Terrain texture sync regression");
    root.SetActive(false);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
    var mm = root.AddComponent<MapMagic.Core.MapMagicObject>();
    mm.enabled = false;
    mm.graph = original.graph;
    var terrains = new System.Collections.Generic.List<Terrain>();
    foreach (int index in new[] { 0, 1, 2 })
    {
        int n = index == 1 ? 65 : 129;
        var td = new TerrainData { name = "TextureSyncRegression_" + index, heightmapResolution = n, alphamapResolution = 128, size = new Vector3(128, 128, 128) };
        td.terrainLayers = original.graph.generators.OfType<MapMagic.Nodes.MatrixGenerators.TexturesOutput200>().Single().layers.Select(l => l.prototype).ToArray();
        var h = new float[n, n];
        for (int z = 0; z < n; z++) for (int x = 0; x < n; x++) h[z, x] = .2f;
        td.SetHeights(0, 0, h);
        var alpha = new float[128, 128, 3];
        for (int z = 0; z < 128; z++) for (int x = 0; x < 128; x++) alpha[z, x, 1] = 1;
        td.SetAlphamaps(0, 0, alpha);
        var go = Terrain.CreateTerrainGameObject(td);
        go.transform.SetParent(root.transform, false);
        if (index == 2) go.transform.localPosition = new Vector3(128, 0, 0);
        terrains.Add(go.GetComponent<Terrain>());
    }
    root.SetActive(true);
    Undo.IncrementCurrentGroup();
    Undo.RegisterCompleteObjectUndo(terrains[0].terrainData, "Terrain texture sync regression height");
    var patch = new float[9, 9];
    for (int z = 0; z < 9; z++) for (int x = 0; x < 9; x++) patch[z, x] = .3f;
    terrains[0].terrainData.SetHeights(60, 60, patch);
    for (int z = 0; z < 9; z++) for (int x = 0; x < 9; x++) patch[z, x] = .35f;
    terrains[0].terrainData.SetHeightsDelayLOD(120, 60, patch);
    SessionState.SetString(key, Newtonsoft.Json.JsonConvert.SerializeObject(new { stage = 1, root = root.GetInstanceID(), terrains = terrains.Select(t => t.GetInstanceID()).ToArray() }));
    return "Height callbacks queued in preview scene; call again after editor updates.";
}

var data = Newtonsoft.Json.Linq.JObject.Parse(state);
var pending = (System.Collections.IDictionary)typeof(MapMagicTerrainTextureSync).GetField("Pending", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
if (pending.Count != 0) return "Waiting for automatic alignment";
int stage = (int)data["stage"];
var targets = data["terrains"].Select(id => (Terrain)EditorUtility.InstanceIDToObject((int)id)).ToArray();
var results = new System.Collections.Generic.List<object>();
bool passed = true;
for (int i = 0; i < targets.Length; i++)
{
    var t = targets[i]; var td = t.terrainData; int n = td.heightmapResolution;
    var h = td.GetHeights(0, 0, n, n); int wrongHeights = 0;
    for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
    {
        float expected = .2f;
        if (i == 0 && stage != 2 && z >= 60 && z <= 68)
        {
            if (x >= 60 && x <= 68) expected = .3f;
            if (x >= 120) expected = .35f;
        }
        if (Mathf.Abs(h[z, x] - expected) > .00003f) wrongHeights++;
    }
    float outside = td.GetAlphamaps(10, 10, 1, 1)[0, 0, 1];
    int centerX = i == 2 ? 4 : 64;
    float rock = td.GetAlphamaps(centerX, 64, 1, 1)[0, 0, 1];
    int secondWrite = MapMagicTerrainTextureSync.RefreshTerrain(t, i == 2 ? new Rect(128, 60, 4, 8) : new Rect(60, 60, 68, 8), false);
    bool ok = wrongHeights == 0 && outside == 1 && rock < .05f && secondWrite == 0;
    passed &= ok;
    results.Add(new { terrain = td.name, wrongHeights, outsideRock = outside, centerRock = rock, repeatedWriteCount = secondWrite, passed = ok });
}
System.IO.File.WriteAllText(folder + "/regression_stage_" + stage + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(new { stage, passed, results }, Newtonsoft.Json.Formatting.Indented));
if (!passed || stage == 3)
{
    var root = (GameObject)EditorUtility.InstanceIDToObject((int)data["root"]);
    var terrainData = targets.Select(t => t.terrainData).ToArray();
    foreach (var td in terrainData) Undo.ClearUndo(td);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(root.scene);
    foreach (var td in terrainData) UnityEngine.Object.DestroyImmediate(td);
    SessionState.EraseString(key);
    if (!passed) throw new System.Exception("Terrain texture regression failed at stage " + stage);
    return "PASS height callbacks, delayed sync, neighbour and Draft updates, outside preservation, undo, redo and idempotence. Preview scene removed.";
}
data["stage"] = stage + 1;
SessionState.SetString(key, data.ToString());
Undo.FlushUndoRecordObjects();
if (stage == 1) Undo.PerformUndo(); else Undo.PerformRedo();
return "Stage " + stage + " passed; call again after editor updates for " + (stage == 1 ? "undo" : "redo");
