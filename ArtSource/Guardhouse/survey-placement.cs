var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var gate = all.First(t => t.name == "FactoryGate");
var guard = all.First(t => t.name == "Guard Post");
System.Func<UnityEngine.Vector3,float[]> V = v => new[]{v.x,v.y,v.z};
System.Func<UnityEngine.Transform,string> P = null;
P = t => t.parent ? P(t.parent)+"/"+t.name : t.name;
System.Func<UnityEngine.Transform,object> Describe = t => new {
    name=t.name,path=P(t),id=t.GetInstanceID(),position=V(t.position),rotation=V(t.eulerAngles),localScale=V(t.localScale),worldScale=V(t.lossyScale),right=V(t.right),forward=V(t.forward),up=V(t.up),
    renderers=t.GetComponentsInChildren<UnityEngine.Renderer>(true).Select(r=>new{name=r.name,center=V(r.bounds.center),size=V(r.bounds.size),materials=r.sharedMaterials.Select(m=>m?m.name+" | "+m.shader.name:"null").ToArray()}).ToArray(),
    colliders=t.GetComponentsInChildren<UnityEngine.Collider>(true).Select(c=>new{name=c.name,type=c.GetType().Name,center=V(c.bounds.center),size=V(c.bounds.size)}).ToArray(),
    scripts=t.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true).Select(c=>c?c.GetType().Name:"missing").ToArray()
};
var nearby=all.Where(t=>UnityEngine.Vector3.Distance(t.position,gate.position)<30 && (t.name.StartsWith("Wall_") || t.name.StartsWith("Pillar_") || t.name.StartsWith("Wall Footing_") || t.name.StartsWith("Pillar Footing_") || t.name=="Gate Concrete Threshold")).ToArray();
var report=new {scene=scene.path,dirty=scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,gate=Describe(gate),guard=Describe(guard),nearby=nearby.Select(Describe).ToArray(),parents=guard.parent.Cast<UnityEngine.Transform>().Select(t=>new{t.name,position=V(t.position)}).ToArray()};
System.IO.Directory.CreateDirectory("Library/GuardhousePlacement");
System.IO.File.WriteAllText("Library/GuardhousePlacement/survey.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
