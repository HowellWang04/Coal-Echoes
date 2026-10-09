var s=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var m=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CablewayMovementManager>(true)).Single();
if(EditorApplication.isPlaying || AnimationMode.InAnimationMode())throw new Exception("Idle edit mode required");
m.PrepareMovement();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
var at=m.GetType().GetMethod("PositionAtDistance",flags);
Vector3 Position(float d)=>(Vector3)at.Invoke(m,new object[]{d});
float maxStep=0f;
for(float d=0;d<m.RouteLength;d+=.25f){float step=Vector3.Distance(Position(d),Position(d+.25f));if(float.IsNaN(step)||step>.252f)throw new Exception("Route discontinuity at "+d);maxStep=Mathf.Max(maxStep,step);}
var authored=m.Buckets.Select(t=>EditorJsonUtility.ToJson(t)).ToArray();
var initial=m.Buckets.Select(t=>t.position).ToArray();
var e=Editor.CreateEditor(m,typeof(CablewayMovementManagerEditor));
var start=e.GetType().GetMethod("StartPreview",flags);var stop=e.GetType().GetMethod("StopPreview",flags);var sample=e.GetType().GetMethod("SamplePreview",flags);
object report;
try {
start.Invoke(e,null);sample.Invoke(e,new object[]{0f});
var snaps=m.Buckets.Select((t,i)=>new {parent=t.parent.name,name=t.name,meters=Vector3.Distance(t.position,initial[i])}).ToArray();
var positions=m.Buckets.Select(t=>t.position).ToArray();
var rotations=m.Buckets.Select(t=>t.rotation).ToArray();
sample.Invoke(e,new object[]{m.RouteLength});
if(m.Buckets.Where((t,i)=>Vector3.Distance(t.position,positions[i])>.001f||Quaternion.Angle(t.rotation,rotations[i])>.01f).Any())throw new Exception("Full loop mismatch");
report=new {routeLength=m.RouteLength,routeSamples=(int)(m.RouteLength/.25f)+1,maxStep,seamGap=Vector3.Distance(Position(-.001f),Position(.001f)),fullLoopPassed=true,snaps,buckets=m.Buckets.Length,staticObjects=m.Buckets.Sum(t=>t.GetComponentsInChildren<Transform>().Count(x=>x.gameObject.isStatic))};
} finally {stop.Invoke(e,null);UnityEngine.Object.DestroyImmediate(e);}
if(!m.Buckets.Select((t,i)=>EditorJsonUtility.ToJson(t)==authored[i]).All(x=>x))throw new Exception("Poses not restored");
System.IO.File.WriteAllText("ArtSource/Cableway/Movement/route_verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
