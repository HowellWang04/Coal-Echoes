var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(!scene.IsValid() || !scene.isLoaded || UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Main scene must be loaded in Edit Mode");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var towers=all.Where(t=>System.Text.RegularExpressions.Regex.IsMatch(t.name,@"^CablewayTower_\d+$") && t.parent!=null && t.parent.name=="索道群").OrderBy(t=>t.name).ToArray();
if(towers.Length!=11)throw new System.Exception("Expected 11 tower roots, found "+towers.Length);
float[] V(UnityEngine.Vector3 v)=>new[]{v.x,v.y,v.z};
var positions=towers.Select(t=>t.position).ToArray();var rotations=towers.Select(t=>t.rotation).ToArray();
var before=towers.Select(t=>new{name=t.name,position=V(t.position),rotation=V(t.eulerAngles),localScale=V(t.localScale)}).ToArray();
var folder="ArtSource/Cableway/TowerScale1200/";
System.IO.File.WriteAllText(folder+"before.json",Newtonsoft.Json.JsonConvert.SerializeObject(before,Newtonsoft.Json.Formatting.Indented));
UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Set all cableway tower scales to 1200");
foreach(var t in towers){
 UnityEditor.Undo.RecordObject(t,"Set cableway tower scale");t.localScale=UnityEngine.Vector3.one*1200;
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);
}
UnityEditor.Undo.CollapseUndoOperations(group);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed");
var checks=towers.Select((t,i)=>new{name=t.name,localScale=V(t.localScale),positionUnchanged=t.position==positions[i],rotationUnchanged=t.rotation==rotations[i],towerHeight=t.GetComponentsInChildren<UnityEngine.Renderer>(true).Where(r=>r.name=="CablewayTower").Select(r=>r.bounds.size.y).FirstOrDefault()}).ToArray();
if(checks.Any(c=>c.localScale.Any(x=>x!=1200)||!c.positionUnchanged||!c.rotationUnchanged))throw new System.Exception("Transform verification failed");
var result=new{scene=scene.path,count=towers.Length,saved=!scene.isDirty,checks};
System.IO.File.WriteAllText(folder+"verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;

