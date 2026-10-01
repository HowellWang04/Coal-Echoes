var targets = new [] { "Assets/Art Resources/Environment/Cableway/suodao-S.fbx", "Assets/Art Resources/Environment/Cableway/house 1.fbx" };
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if (!scene.IsValid() || !scene.isLoaded) throw new System.Exception("Main scene is not loaded");
var objects = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t => t.gameObject).ToArray();
var matches = objects.Where(o => targets.Contains(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o)) && UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(o) == o).ToArray();
var materials = matches.SelectMany(o => o.GetComponentsInChildren<UnityEngine.Renderer>(true)).SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(UnityEditor.AssetDatabase.GetAssetPath).Distinct().ToArray();
var candidates = new System.Collections.Generic.HashSet<string>(targets.Concat(materials).SelectMany(p => UnityEditor.AssetDatabase.GetDependencies(p, true)).Where(p => p.StartsWith("Assets/")));
var external = new System.Collections.Generic.List<object>();
foreach (var path in UnityEditor.AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !UnityEditor.AssetDatabase.IsValidFolder(p) && !candidates.Contains(p))) {
    var hits = UnityEditor.AssetDatabase.GetDependencies(path, false).Where(candidates.Contains).ToArray();
    if (hits.Length > 0) external.Add(new { path, dependencies = hits });
}
var result = new { scene = scene.path, dirty = scene.isDirty, playing = UnityEditor.EditorApplication.isPlaying, targets = targets.Select(p => new { path = p, guid = UnityEditor.AssetDatabase.AssetPathToGUID(p) }).ToArray(), instances = matches.Select(o => new { name = o.name, id = o.GetInstanceID(), hierarchy = UnityEditor.AnimationUtility.CalculateTransformPath(o.transform, null), model = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o), children = o.GetComponentsInChildren<UnityEngine.Transform>(true).Select(t => new { name = t.name, source = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)) }).ToArray() }).ToArray(), candidates = candidates.Select(p => new { path = p, guid = UnityEditor.AssetDatabase.AssetPathToGUID(p), dependencies = UnityEditor.AssetDatabase.GetDependencies(p, false) }).ToArray(), external };
System.IO.File.WriteAllText("ArtSource/Cleanup/LegacyCablewayRemoval/audit.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
