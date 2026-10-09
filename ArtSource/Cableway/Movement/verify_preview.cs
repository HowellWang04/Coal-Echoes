var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CablewayMovementManager>(true)).Single();
if(EditorApplication.isPlaying || AnimationMode.InAnimationMode())throw new Exception("Idle edit mode required.");
var positions=manager.Buckets.Select(t=>t.localPosition).ToArray();var rotations=manager.Buckets.Select(t=>t.localRotation).ToArray();
var overrides=manager.Buckets.Select(t=>EditorJsonUtility.ToJson(t)).ToArray();
var editor=Editor.CreateEditor(manager,typeof(CablewayMovementManagerEditor));
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var start=editor.GetType().GetMethod("StartPreview",flags);var stop=editor.GetType().GetMethod("StopPreview",flags);
bool Restored()=>manager.Buckets.Select((t,i)=>t.localPosition==positions[i] && Quaternion.Angle(t.localRotation,rotations[i])<.001f && EditorJsonUtility.ToJson(t)==overrides[i]).All(x=>x);
var sample=editor.GetType().GetMethod("SamplePreview",flags);
var checks=new List<string>();void Check(bool c,string s){if(!c)throw new Exception(s);checks.Add(s);}
try {
    start.Invoke(editor,null);Check(AnimationMode.InAnimationMode(),"Scene preview starts through the actual Inspector");
    sample.Invoke(editor,new object[]{18f});
    Check(manager.Buckets.Where((t,i)=>Vector3.Distance(t.localPosition,positions[i])>1).Count()==manager.Buckets.Length,"Preview moves every bound bucket");
    stop.Invoke(editor,null);
    Check(Restored(),"Stopping restores every original local pose and serialized transform");
    start.Invoke(editor,null);sample.Invoke(editor,new object[]{25f});
    System.IO.Directory.CreateDirectory("Temp/CablewayMovement");
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Temp/CablewayMovement/preview_save_test.unity",true))throw new Exception("Preview save test failed.");
    Check(!AnimationMode.InAnimationMode() && Restored(),"Saving while previewing stops preview and restores authored poses");
    start.Invoke(editor,null);sample.Invoke(editor,new object[]{40f});
    UnityEngine.Object.DestroyImmediate(editor);editor=null;
    Check(!AnimationMode.InAnimationMode() && Restored(),"Closing the Inspector restores authored poses");
    var report=new {passed=checks.Count,checks,sceneSaved=!scene.isDirty,playModeRun=false};
    System.IO.File.WriteAllText("ArtSource/Cableway/Movement/preview_verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
    return report;
} finally {
    if(editor!=null){stop.Invoke(editor,null);UnityEngine.Object.DestroyImmediate(editor);}
}
