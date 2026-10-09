var plan = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/FactoryGrounding/survey.json"));
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath((string)plan["scene"]);
if (!scene.isLoaded || EditorApplication.isPlaying) throw new System.Exception("Scene is not ready");
var factory = scene.GetRootGameObjects().Single(x => x.name == "In-scene Objects").transform.Find("工厂建筑群");
var road = factory.Find("02 道路与围栏/路面与路沿/SM_Road");
if (road == null || road.gameObject.GetInstanceID() != (int)plan["roadInstanceId"]) throw new System.Exception("Road selection changed");
foreach (var row in plan["buildings"])
{
    var building = EditorUtility.InstanceIDToObject((int)row["instanceId"]) as GameObject;
    if (building == null || GlobalObjectId.GetGlobalObjectIdSlow(building).ToString() != (string)row["globalId"]) throw new System.Exception("Building selection changed");
    var before = new Vector3((float)row["x"], (float)row["y"], (float)row["z"]);
    if ((building.transform.position - before).sqrMagnitude > 0.000001f) throw new System.Exception("Building moved since survey");
}
var backup = "ArtSource/FactoryGrounding/Before/Main Game Scene.before-grounding.unity";
if (System.IO.File.Exists(backup)) throw new System.Exception("Backup already exists");
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, backup, true)) throw new System.Exception("Could not save scene backup");
Undo.IncrementCurrentGroup();
int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("Ground flat factory buildings and remove model road");
var changed = new System.Collections.Generic.List<string>();
foreach (var row in plan["buildings"])
{
    if (!(bool)row["eligible"]) continue;
    var building = ((GameObject)EditorUtility.InstanceIDToObject((int)row["instanceId"])).transform;
    Undo.RecordObject(building, "Ground factory building");
    building.position += Vector3.up * (float)row["deltaY"];
    PrefabUtility.RecordPrefabInstancePropertyModifications(building);
    changed.Add((string)row["path"]);
}
Undo.DestroyObjectImmediate(road.gameObject);
Undo.CollapseUndoOperations(undoGroup);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.Exception("Could not save modified scene");
var result = new { adjustedBuildings = changed, preservedBuildings = plan["buildings"].Count() - changed.Count, roadRemoved = factory.Find("02 道路与围栏/路面与路沿/SM_Road") == null, backup, sceneSaved = !scene.isDirty };
System.IO.File.WriteAllText("ArtSource/FactoryGrounding/applied.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
