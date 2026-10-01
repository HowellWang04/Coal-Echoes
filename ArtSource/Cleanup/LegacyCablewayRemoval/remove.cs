if (UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling) throw new System.Exception("Editor must be idle in Edit Mode");
var audit = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/Cleanup/LegacyCablewayRemoval/audit.json"));
var targetPaths = audit["targets"].Select(t => (string)t["path"]).ToArray();
var candidates = audit["candidates"].Select(t => (string)t["path"]).ToArray();
if (candidates.Any(p => !p.StartsWith("Assets/Art Resources/Environment/Cableway/"))) throw new System.Exception("Unexpected deletion path");
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if (!scene.IsValid() || !scene.isLoaded) throw new System.Exception("Main scene must be loaded");
var matches = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t => t.gameObject).Where(o => targetPaths.Contains(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o)) && UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(o) == o).ToArray();
if (matches.Length != 2) throw new System.Exception("Scene changed since audit");
if (matches.Any(o => o.GetComponentsInChildren<UnityEngine.Transform>(true).Length != 1)) throw new System.Exception("Unexpected child objects");
var backup = System.IO.File.ReadAllText("ArtSource/Cleanup/LegacyCablewayRemoval/backup_location.txt").Trim();
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, System.IO.Path.Combine(backup, "Main Game Scene.live.unity"), true)) throw new System.Exception("Scene backup failed");
var removedObjects = matches.Select(o => new { name = o.name, path = UnityEditor.AnimationUtility.CalculateTransformPath(o.transform, null) }).ToArray();
foreach (var o in matches) UnityEditor.Undo.DestroyObjectImmediate(o);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.Exception("Scene save failed");
System.IO.File.WriteAllText("ArtSource/Cleanup/LegacyCablewayRemoval/scene_removal.json", Newtonsoft.Json.JsonConvert.SerializeObject(removedObjects, Newtonsoft.Json.Formatting.Indented));
var targetSet = new System.Collections.Generic.HashSet<string>(candidates);
var external = new System.Collections.Generic.List<object>();
int scanned = 0;
foreach (var path in UnityEditor.AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !UnityEditor.AssetDatabase.IsValidFolder(p) && !targetSet.Contains(p))) {
    scanned++;
    var hits = UnityEditor.AssetDatabase.GetDependencies(path, false).Where(targetSet.Contains).ToArray();
    if (hits.Length > 0) external.Add(new { path, dependencies = hits });
}
if (external.Count > 0) { System.IO.File.WriteAllText("ArtSource/Cleanup/LegacyCablewayRemoval/remaining_dependencies.json", Newtonsoft.Json.JsonConvert.SerializeObject(external, Newtonsoft.Json.Formatting.Indented)); return new { removedObjects, assetsDeleted = false, external }; }
var removedAssets = new System.Collections.Generic.List<object>();
foreach (var path in candidates.OrderBy(p => p.EndsWith(".fbx") ? 0 : p.EndsWith(".mat") ? 1 : 2)) {
    var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
    if (!UnityEditor.AssetDatabase.DeleteAsset(path)) throw new System.Exception("Asset deletion failed " + path);
    removedAssets.Add(new { path, guid });
}
var report = new { removedObjects, removedAssets, scanned, sceneSaved = !scene.isDirty, playModeTested = false };
System.IO.File.WriteAllText("ArtSource/Cleanup/LegacyCablewayRemoval/removal.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return report;
