// One-time edit after align-gate-approach.cs; positions are relative offsets.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var frame=all.First(t=>t.name=="工厂道路向内");
var allowed=Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.HashSet<int>>(System.IO.File.ReadAllText("Library/RoadGateAlignment/allowed.json"));
UnityEditor.Undo.IncrementCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Clear approach road shoulders");int undo=UnityEditor.Undo.GetCurrentGroup();
foreach(var name in new[]{"SM_FuelTank4","SM_FuelTank8","SM_StreetSign10"}){
    var t=all.First(v=>v.name==name);UnityEditor.Undo.RecordObject(t,"Clear widened road edge");allowed.Add(t.GetInstanceID());
    t.position+=frame.forward*(name=="SM_StreetSign10"?-2f:3f);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);
}
System.IO.File.WriteAllText("Library/RoadGateAlignment/allowed.json",Newtonsoft.Json.JsonConvert.SerializeObject(allowed));
UnityEditor.Undo.CollapseUndoOperations(undo);UnityEngine.Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return new{movedTanks=2,movedSign=1};
