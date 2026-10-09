var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if (!scene.isLoaded || EditorApplication.isPlaying) throw new Exception("Main scene must be loaded in edit mode.");
Func<Transform, string> path = null;
path = t => t.parent == null ? t.name : path(t.parent) + "/" + t.name;
Func<Vector3, float[]> vec = v => new[] { v.x, v.y, v.z };
var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
var rows = new List<object>();
foreach (var t in all.Where(t => t.name.Contains("Cableway") || t.name.Contains("Cable_") || t.name == "CoalBucket" || t.name.Contains("Footing")))
{
    var renderers = t.GetComponentsInChildren<Renderer>(true);
    var b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(t.position, Vector3.zero);
    foreach (var r in renderers) b.Encapsulate(r.bounds);
    var mf = t.GetComponent<MeshFilter>();
    rows.Add(new { name = t.name, path = path(t), pos = vec(t.position), rot = vec(t.eulerAngles), scale = vec(t.lossyScale), min = vec(b.min), max = vec(b.max), children = t.Cast<Transform>().Select(c => c.name).ToArray(), mesh = mf != null ? AssetDatabase.GetAssetPath(mf.sharedMesh) : null, vertices = mf != null && mf.sharedMesh != null ? mf.sharedMesh.vertexCount : 0 });
}
var terrainRows = Terrain.activeTerrains.Select(t => new { name = t.name, asset = AssetDatabase.GetAssetPath(t.terrainData), pos = vec(t.transform.position), size = vec(t.terrainData.size) }).ToArray();
var report = new { scene = scene.path, dirty = scene.isDirty, objects = rows, terrains = terrainRows };
System.IO.File.WriteAllText("ArtSource/Cableway/SummitStation/survey.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return report;
