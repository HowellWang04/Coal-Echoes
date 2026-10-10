var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(EditorApplication.isPlaying||!scene.isLoaded||scene.isDirty)throw new System.Exception("Scene changed or unavailable");
var rows=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/FactoryHeightReview/plan.json"));
var before=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/FactoryHeightReview/transforms-before.json")).ToDictionary(r=>(int)r["id"]);
Vector3 V(Newtonsoft.Json.Linq.JToken v)=>new Vector3((float)v[0],(float)v[1],(float)v[2]);
var changes=new System.Collections.Generic.Dictionary<Transform,float>();
foreach(var row in rows) {
    var go=(GameObject)EditorUtility.InstanceIDToObject((int)row["id"]);
    if(go==null||GlobalObjectId.GetGlobalObjectIdSlow(go).ToString()!=(string)row["globalId"]||(go.transform.position-V(row["before"])).sqrMagnitude>.000001f)throw new System.Exception("Survey stale");
    foreach(var id in row["members"]) {
        var t=((GameObject)EditorUtility.InstanceIDToObject((int)id)).transform;
        if(changes.ContainsKey(t))throw new System.Exception("Duplicate transform");
        if((t.localPosition-V(before[t.GetInstanceID()]["position"])).sqrMagnitude>.000001f)throw new System.Exception("Member moved");
        changes.Add(t,(float)row["deltaY"]);
    }
}
Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Fit factory object heights to existing terrain");
Undo.RecordObjects(changes.Keys.Cast<UnityEngine.Object>().ToArray(),"Fit factory object heights");
foreach(var change in changes) {
    change.Key.position+=Vector3.up*change.Value;
    PrefabUtility.RecordPrefabInstancePropertyModifications(change.Key);
}
Undo.CollapseUndoOperations(group);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
var result=new {objects=rows.Count,transforms=changes.Count,undoGroup=group,saved=false};
System.IO.File.WriteAllText("ArtSource/FactoryHeightReview/applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
