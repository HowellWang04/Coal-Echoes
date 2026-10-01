var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var old = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).Single(t => t.name == "tajia01").gameObject;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art Resources/Environment/Cableway/Tajia01_Rebuilt/Tajia01_Rebuilt.prefab");
var p = old.transform.localPosition;
var r = old.transform.localRotation;
var s = old.transform.localScale;
var parent = old.transform.parent;
UnityEditor.Undo.IncrementCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("Replace Tajia01 with independent tower and bucket");
var settings = new UnityEditor.PrefabReplacingSettings();
settings.changeRootNameToAssetName = false;
UnityEditor.PrefabUtility.ReplacePrefabAssetOfPrefabInstance(old, prefab, settings, UnityEditor.InteractionMode.UserAction);
var current = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<UnityEngine.Transform>(true)).Single(t => t.name == "tajia01");
if (current.localPosition != p || current.localRotation != r || current.localScale != s || current.parent != parent)
    throw new System.Exception("Replacement changed root transform");
UnityEditor.Selection.activeGameObject = current.gameObject;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return Newtonsoft.Json.JsonConvert.SerializeObject(new { rootPreserved = true, children = current.Cast<UnityEngine.Transform>().Select(t => t.name).ToArray(), renderers = current.GetComponentsInChildren<UnityEngine.Renderer>().Select(x => new { x.name, bounds = x.bounds.ToString("F6"), material = UnityEditor.AssetDatabase.GetAssetPath(x.sharedMaterial) }).ToArray() });
