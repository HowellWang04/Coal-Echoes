var paths = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(System.IO.File.ReadAllText("ArtSource/Cleanup/RailRemoval/scene_paths.json"));
var report = new System.Collections.Generic.List<object>();
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
foreach (var path in paths) {
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
    bool opened = !scene.IsValid() || !scene.isLoaded;
    if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t => t.gameObject).ToArray();
    var matches = objects.Where(o => System.Text.RegularExpressions.Regex.IsMatch(o.name, "^SM[_ ]?Rails", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToArray();
    var roots = matches.Where(o => !matches.Any(p => p != o && o.transform.IsChildOf(p.transform))).ToArray();
    report.Add(new { path, dirty = scene.isDirty, matchingObjects = matches.Length, instances = matches.Count(o => UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o).EndsWith("SM_Rails.prefab") && UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(o) == o), roots = roots.Select(o => new { name = o.name, hierarchy = UnityEditor.AnimationUtility.CalculateTransformPath(o.transform, null), descendants = o.GetComponentsInChildren<UnityEngine.Transform>(true).Length, nonMatchingDescendants = o.GetComponentsInChildren<UnityEngine.Transform>(true).Where(t => !System.Text.RegularExpressions.Regex.IsMatch(t.name, "^SM[_ ]?Rails", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).Select(t => t.name).Distinct().ToArray() }).ToArray() });
    if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
var json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("ArtSource/Cleanup/RailRemoval/scene_audit.json", json);
return report;
