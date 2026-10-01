var report=new System.Collections.Generic.List<object>();
float[] V(UnityEngine.Vector3 v)=>new[]{v.x,v.y,v.z};
foreach(var name in new[]{"CablewayStationShell","CablewayStationRollerFrame"}){
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art Resources/Environment/Cableway/"+name+"/"+name+".fbx");
 var go=UnityEngine.Object.Instantiate(prefab);go.hideFlags=UnityEngine.HideFlags.HideAndDontSave;
 var rr=go.GetComponentsInChildren<UnityEngine.Renderer>();var bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);
 report.Add(new{name,size=V(bounds.size),min=V(bounds.min),max=V(bounds.max),children=rr.Select(r=>new{name=r.name,center=V(r.bounds.center),size=V(r.bounds.size)}).ToArray()});
 UnityEngine.Object.DestroyImmediate(go);
}
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
report.Add(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).Where(t=>t.name=="In-scene Objects"||t.name=="索道群").Select(t=>new{name=t.name,scale=V(t.lossyScale)}).ToArray());
System.IO.File.WriteAllText("ArtSource/Cableway/FoothillStationPlacement/model_survey.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
