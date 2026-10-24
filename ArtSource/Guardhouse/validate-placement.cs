var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var house=all.First(t=>t.name=="Guard Post");var gate=all.First(t=>t.name=="FactoryGate");
var wall=all.First(t=>t.name=="Wall_030_V06");
UnityEngine.Physics.SyncTransforms();
var blocked=new System.Collections.Generic.List<object>();
for(int i=0;i<=80;i++){
    float z=UnityEngine.Mathf.Lerp(-4,4,i/80f);
    var low=house.TransformPoint(new UnityEngine.Vector3(-1.4f,.53f,z));
    var high=house.TransformPoint(new UnityEngine.Vector3(-1.4f,1.73f,z));
    var hits=UnityEngine.Physics.OverlapCapsule(low,high,.30f,~0,UnityEngine.QueryTriggerInteraction.Ignore).Where(c=>c.enabled).ToArray();
    if(hits.Length>0)blocked.Add(new{z,objects=hits.Select(c=>c.name).Distinct().ToArray()});
}
float center=UnityEngine.Vector3.Dot(house.position-gate.position,gate.right);
var leaks=new System.Collections.Generic.List<object>();
int barrierSamples=0;
foreach(float height in new[]{.6f,1.3f,2.4f})for(int i=0;i<=160;i++){
    float x=5.25f+i*.05f;
    if(x>center+.78f&&x<center+2.02f&&height<2.25f)continue;
    var origin=gate.position+gate.right*x+gate.forward*2+UnityEngine.Vector3.up*height;
    var hits=UnityEngine.Physics.RaycastAll(origin,-gate.forward,8,~0,UnityEngine.QueryTriggerInteraction.Ignore);
    barrierSamples++;
    if(!hits.Any(h=>h.collider.transform.IsChildOf(house)||h.collider.transform.IsChildOf(wall)||h.collider.transform.IsChildOf(gate)||h.collider.name.StartsWith("Pillar")))leaks.Add(new{x,height});
}
var before=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/GuardhousePlacement/before-transforms.json"));
var allowed=new System.Collections.Generic.HashSet<int>(before["allowed"].Values<int>());
var current=all.ToDictionary(t=>t.GetInstanceID());
var unrelatedChanges=new System.Collections.Generic.List<string>();int preserved=0;
foreach(var old in before["transforms"]){
    int id=(int)old["id"];if(allowed.Contains(id))continue;
    UnityEngine.Transform t;
    if(!current.TryGetValue(id,out t)){unrelatedChanges.Add((string)old["name"]+" removed");continue;}
    var p=old["position"].Values<float>().ToArray();var q=old["rotation"].Values<float>().ToArray();var scale=old["scale"].Values<float>().ToArray();
    bool same=UnityEngine.Vector3.Distance(t.localPosition,new UnityEngine.Vector3(p[0],p[1],p[2]))<.00001f&&UnityEngine.Mathf.Abs(t.localRotation.x-q[0])+UnityEngine.Mathf.Abs(t.localRotation.y-q[1])+UnityEngine.Mathf.Abs(t.localRotation.z-q[2])+UnityEngine.Mathf.Abs(t.localRotation.w-q[3])<.00001f&&UnityEngine.Vector3.Distance(t.localScale,new UnityEngine.Vector3(scale[0],scale[1],scale[2]))<.00001f&&(t.parent?t.parent.GetInstanceID():0)==(int)old["parent"]&&t.gameObject.activeSelf==(bool)old["active"];
    if(!same)unrelatedChanges.Add(t.name);else preserved++;
}
var materials=house.GetComponentsInChildren<UnityEngine.Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Select(m=>new{m.name,shader=m.shader.name,supported=m.shader.isSupported}).ToArray();
var report=new{corridorCapsuleHeight=1.8,corridorCapsuleDiameter=.6,corridorSamples=81,blocked,barrierSamples,leaks,preservedTransforms=preserved,unrelatedChanges,materials,doors=house.GetComponentsInChildren<UnityEngine.Transform>().Where(t=>t.name.Contains("Hinge_Game")).Select(t=>new{t.name,angle=t.localEulerAngles.y}).ToArray()};
System.IO.File.WriteAllText("Library/GuardhousePlacement/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
