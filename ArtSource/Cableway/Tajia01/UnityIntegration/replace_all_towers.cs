var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if (!scene.isLoaded || scene.isDirty || UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Expected saved main scene in edit mode");
var prefabPath = "Assets/Art Resources/Environment/Cableway/CablewayTower_Rebuilt/CablewayTower_Rebuilt.prefab";
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
var templateTower = prefab.transform.Find("CablewayTower");
var templateBucket = prefab.transform.Find("CoalBucket");
var towers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).Where(t => System.Text.RegularExpressions.Regex.IsMatch(t.name, "^CablewayTower_[0-9]{2}$")).OrderBy(t => t.name).ToArray();
if (towers.Length != 11) throw new System.Exception("Expected 11 towers");
var legacy = towers.Where(t => UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).Contains("CablewayTower_Legacy_")).ToArray();
if (legacy.Length != 10) throw new System.Exception("Expected 10 legacy instances");
var jobs = legacy.Select(t => {
    var mesh = t.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
    var bounds = mesh.bounds;
    var foot = mesh.vertices.Where(v => v.y < bounds.min.y + bounds.size.y * .1f).ToArray();
    var baseCenter = new UnityEngine.Vector3((foot.Min(v => v.x) + foot.Max(v => v.x)) * .5f, bounds.min.y, (foot.Min(v => v.z) + foot.Max(v => v.z)) * .5f);
    return new { target = t, name = t.name, parent = t.parent, position = t.localPosition, rotation = t.localRotation, scale = t.localScale, baseCenter, worldBase = t.TransformPoint(baseCenter), height = t.GetComponent<UnityEngine.Renderer>().bounds.size.y, ratio = bounds.size.y / templateTower.GetComponent<UnityEngine.MeshFilter>().sharedMesh.bounds.size.y, id = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString() };
}).ToArray();
var backup = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CoalTowerReplacement_" + System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".unity");
System.IO.File.Copy(scene.path, backup, false);
UnityEditor.Undo.IncrementCurrentGroup();
var undoGroup = UnityEditor.Undo.GetCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("Replace all legacy cableway towers");
var results = new System.Collections.Generic.List<object>();
try {
    foreach (var job in jobs) {
        var settings = new UnityEditor.PrefabReplacingSettings();
        settings.changeRootNameToAssetName = false;
        UnityEditor.PrefabUtility.ReplacePrefabAssetOfPrefabInstance(job.target.gameObject, prefab, settings, UnityEditor.InteractionMode.UserAction);
        var current = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).Single(t => t.name == job.name);
        if (current.localPosition != job.position || current.localRotation != job.rotation || current.localScale != job.scale || current.parent != job.parent) throw new System.Exception("Root transform changed " + job.name);
        var tower = current.Find("CablewayTower");
        var bucket = current.Find("CoalBucket");
        UnityEditor.Undo.RecordObject(tower, "Fit rebuilt tower to original footing");
        UnityEditor.Undo.RecordObject(bucket, "Fit rebuilt bucket with tower");
        var scaleFactor = job.ratio / templateTower.localScale.x;
        tower.localPosition = job.baseCenter;
        tower.localScale = UnityEngine.Vector3.one * job.ratio;
        bucket.localPosition = job.baseCenter + (templateBucket.localPosition - templateTower.localPosition) * scaleFactor;
        bucket.localScale = templateBucket.localScale * scaleFactor;
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(tower);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(bucket);
        var height = tower.GetComponent<UnityEngine.Renderer>().bounds.size.y;
        var baseError = UnityEngine.Vector3.Distance(tower.position, job.worldBase);
        if (System.Math.Abs(height - job.height) > .001f || baseError > .001f) throw new System.Exception("Bounds mismatch " + job.name);
        var renderers = current.GetComponentsInChildren<UnityEngine.Renderer>();
        if (renderers.Length != 2 || renderers.Any(r => r.sharedMaterial == null)) throw new System.Exception("Invalid rebuilt renderers " + job.name);
        results.Add(new { job.name, oldHeight = job.height, newHeight = height, baseError, rootTransformPreserved = true, rootIdPreserved = job.id == UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(current.gameObject).ToString(), towerLocalPosition = tower.localPosition.ToString("F9"), towerLocalScale = tower.localScale.ToString("F9") });
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.Exception("Save failed");
    UnityEditor.Undo.CollapseUndoOperations(undoGroup);
} catch { UnityEditor.Undo.RevertAllDownToGroup(undoGroup); throw; }
var report = Newtonsoft.Json.JsonConvert.SerializeObject(new { saved = !scene.isDirty, prefab = prefabPath, backup, replaced = results }, Newtonsoft.Json.Formatting.Indented);
System.IO.File.WriteAllText("ArtSource/Cableway/Tajia01/UnityIntegration/all_towers_replacement.json", report);
return report;
