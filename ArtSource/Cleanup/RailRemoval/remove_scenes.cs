if (UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling) throw new System.Exception("Editor must be idle in Edit Mode");
var paths = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(System.IO.File.ReadAllText("ArtSource/Cleanup/RailRemoval/scene_paths.json"));
var backup = System.IO.File.ReadAllText("ArtSource/Cleanup/RailRemoval/backup_location.txt").Trim();
var report = new System.Collections.Generic.List<object>();
var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
foreach (var path in paths) {
    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
    bool opened = !scene.IsValid() || !scene.isLoaded;
    if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t => t.gameObject).ToArray();
    var matches = objects.Where(o => System.Text.RegularExpressions.Regex.IsMatch(o.name, "^SM[_ ]?Rails", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToArray();
    var roots = matches.Where(o => !matches.Any(p => p != o && o.transform.IsChildOf(p.transform))).ToArray();
    var instances = matches.Count(o => UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o).EndsWith("SM_Rails.prefab") && UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(o) == o);
    var unexpected = roots.SelectMany(o => o.GetComponentsInChildren<UnityEngine.Transform>(true)).Where(t => !System.Text.RegularExpressions.Regex.IsMatch(t.name, "^SM[_ ]?Rails", System.Text.RegularExpressions.RegexOptions.IgnoreCase) && t.name != "rails").ToArray();
    if (unexpected.Length != 0) throw new System.Exception("Unexpected child in removal group " + path);
    var liveBackup = System.IO.Path.Combine(backup, "Live", path);
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(liveBackup));
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, liveBackup, true)) throw new System.Exception("Backup failed " + path);
    var removedNames = roots.Select(o => UnityEditor.AnimationUtility.CalculateTransformPath(o.transform, null)).ToArray();
    foreach (var root in roots) UnityEditor.Undo.DestroyObjectImmediate(root);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path)) throw new System.Exception("Save failed " + path);
    var remaining = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).Count(t => System.Text.RegularExpressions.Regex.IsMatch(t.name, "^SM[_ ]?Rails", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    report.Add(new { path, instancesRemoved = instances, matchingObjectsRemoved = matches.Length, removedGroups = removedNames, remaining, saved = true });
    if (opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
System.IO.File.WriteAllText("ArtSource/Cleanup/RailRemoval/scene_removal.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return report;
