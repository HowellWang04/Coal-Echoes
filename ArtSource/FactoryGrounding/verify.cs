var plan = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/FactoryGrounding/survey.json"));
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath((string)plan["scene"]);
var changes = new System.Collections.Generic.List<string>();
foreach (var row in plan["buildings"])
{
    var obj = EditorUtility.InstanceIDToObject((int)row["instanceId"]) as GameObject;
    if (obj == null) { changes.Add("Missing " + (string)row["path"]); continue; }
    var before = new Vector3((float)row["x"], (float)row["y"], (float)row["z"]);
    if ((obj.transform.position - before).sqrMagnitude > 0.000001f) changes.Add("Position changed " + (string)row["path"]);
    var r = row["rotation"];
    if (Quaternion.Angle(obj.transform.rotation, new Quaternion((float)r[0], (float)r[1], (float)r[2], (float)r[3])) > 0.001f) changes.Add("Rotation changed " + (string)row["path"]);
    var s = row["scale"];
    if ((obj.transform.localScale - new Vector3((float)s[0], (float)s[1], (float)s[2])).sqrMagnitude > 0.000001f) changes.Add("Scale changed " + (string)row["path"]);
}
var factory = scene.GetRootGameObjects().Single(x => x.name == "In-scene Objects").transform.Find("工厂建筑群");
var surface = factory.Find("02 道路与围栏/路面与路沿");
var result = new { checkedBuildings = plan["buildings"].Count(), unexpectedBuildingChanges = changes, roadAbsent = surface.Find("SM_Road") == null, remainingSurfaceChildren = surface.childCount, sceneSaved = !scene.isDirty, compilationFailed = EditorUtility.scriptCompilationFailed };
System.IO.File.WriteAllText("ArtSource/FactoryGrounding/verification.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
