// Applies route.json (route.py): rebuilds the west part of both factory lanes, adds the branch roads under 工厂支路,
// and moves the dead trees, lamps and signs that stood on the old loop. Run in the editor (Unity MCP execute_code);
// writes before.json on the first run so the change can be undone, does not save the scene.
var dir="ArtSource/FactoryRoads/MainRoadReroute/";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(EditorApplication.isPlaying||!scene.isLoaded) throw new System.Exception("Main Game Scene not loaded in edit mode");
var route=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"route.json"));
var inv=System.Globalization.CultureInfo.InvariantCulture;
var C=new Vector3(-2320,0,-1830); var U=new Vector3(0.7314f,0,0.682f);
System.Func<Vector3,float> uOf=w=>Vector3.Dot(w-C,U);
float joinU=(float)route["joinU"];
var objects=GameObject.Find("In-Scene Objects").transform;
var laneIn=objects.Find("工厂道路向内").GetComponent<UnityEngine.Splines.SplineContainer>();
var laneOut=objects.Find("工厂道路向外").GetComponent<UnityEngine.Splines.SplineContainer>();
var lights=GameObject.Find("In-Scene Objects/工厂建筑群/07 标识与路灯").transform;
var trees=objects.Find("环路内枯树");
var tiles=Terrain.activeTerrains.Where(t=>t.name=="Main Terrain").ToArray();
System.Func<Vector3,float> TH=w=>{foreach(var t in tiles){var p=t.GetPosition();var s=t.terrainData.size;if(w.x>=p.x&&w.x<p.x+s.x&&w.z>=p.z&&w.z<p.z+s.z)return t.SampleHeight(w)+p.y;}throw new System.Exception("off terrain");};
var movable=new System.Collections.Generic.List<Transform>();
foreach(var n in route["lamps"].Children<Newtonsoft.Json.Linq.JProperty>().Select(p=>p.Name)) movable.Add(lights.Find("路灯/"+n));
foreach(var n in route["signs"].Children<Newtonsoft.Json.Linq.JProperty>().Select(p=>p.Name)) movable.Add(lights.Find("道路标识/"+n));
foreach(Transform t in trees) movable.Add(t);
if(movable.Any(t=>t==null)) throw new System.Exception("Missing lamp, sign or tree");

// before.json: the original lanes and transforms, written once
System.Func<UnityEngine.Splines.SplineContainer,string> knotsJson=c=>"["+string.Join(",",c.Spline.Knots.Select(k=>string.Format(inv,"[{0:R},{1:R},{2:R},{3:R},{4:R},{5:R},{6:R},{7:R},{8:R}]",k.Position.x,k.Position.y,k.Position.z,k.TangentIn.x,k.TangentIn.y,k.TangentIn.z,k.TangentOut.x,k.TangentOut.y,k.TangentOut.z)))+"]";
if(!System.IO.File.Exists(dir+"before.json")){
    if(objects.Find("工厂支路")!=null) throw new System.Exception("Branches exist but before.json is missing");
    var sb=new System.Text.StringBuilder("{\"laneIn\":"+knotsJson(laneIn)+",\"laneOut\":"+knotsJson(laneOut)+",\"transforms\":{");
    sb.Append(string.Join(",",movable.Select(t=>string.Format(inv,"\"{0}\":[{1:R},{2:R},{3:R},{4:R},{5:R},{6:R},{7:R}]",t.name,t.position.x,t.position.y,t.position.z,t.rotation.x,t.rotation.y,t.rotation.z,t.rotation.w))));
    sb.Append("}}"); System.IO.File.WriteAllText(dir+"before.json",sb.ToString()); }
var before=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"before.json"));

Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Factory road reroute");

// 1. lanes: keep the gate side up to the straight, replace everything west of joinU
System.Func<Newtonsoft.Json.Linq.JToken,UnityEngine.Splines.BezierKnot> oldKnot=a=>new UnityEngine.Splines.BezierKnot(
    new Unity.Mathematics.float3((float)a[0],(float)a[1],(float)a[2]),new Unity.Mathematics.float3((float)a[3],(float)a[4],(float)a[5]),new Unity.Mathematics.float3((float)a[6],(float)a[7],(float)a[8]),Quaternion.identity);
System.Func<UnityEngine.Splines.SplineContainer,Newtonsoft.Json.Linq.JToken,UnityEngine.Splines.BezierKnot> newKnot=(c,k)=>{
    var p=c.transform.InverseTransformPoint(new Vector3((float)k["p"][0],-430.65f,(float)k["p"][1]));
    var ti=c.transform.InverseTransformDirection(new Vector3((float)k["tin"][0],0,(float)k["tin"][1]));
    var to=c.transform.InverseTransformDirection(new Vector3((float)k["tout"][0],0,(float)k["tout"][1]));
    return new UnityEngine.Splines.BezierKnot(p,ti,to,Quaternion.identity); };
System.Func<UnityEngine.Splines.SplineContainer,Unity.Mathematics.float3,float> uLocal=(c,p)=>uOf(c.transform.TransformPoint(p));
System.Action<UnityEngine.Splines.SplineContainer,System.Collections.Generic.List<UnityEngine.Splines.BezierKnot>> setKnots=(c,list)=>{
    Undo.RecordObject(c,"Factory road reroute"); var s=c.Spline; s.Clear(); foreach(var k in list) s.Add(k,UnityEngine.Splines.TangentMode.Broken); };
// straight join: the tangents across the long straight segment are a third of its length
System.Action<System.Collections.Generic.List<UnityEngine.Splines.BezierKnot>,int> joinStraight=(list,i)=>{
    var a=list[i]; var b=list[i+1]; Vector3 d=(Vector3)(b.Position-a.Position); float L=d.magnitude/3f; d.Normalize();
    a.TangentOut=d*L; b.TangentIn=-d*L; list[i]=a; list[i+1]=b; };
var inList=before["laneIn"].Select(oldKnot).TakeWhile(k=>uLocal(laneIn,k.Position)>joinU+1).ToList();
int inJoin=inList.Count-1; inList.AddRange(route["laneIn"].Select(k=>newKnot(laneIn,k))); joinStraight(inList,inJoin);
var outOld=before["laneOut"].Select(oldKnot).ToList(); var outTail=outOld.Skip(outOld.FindIndex(k=>uLocal(laneOut,k.Position)>joinU+1)).ToList();
var outList=route["laneOut"].Select(k=>newKnot(laneOut,k)).ToList(); int outJoin=outList.Count-1; outList.AddRange(outTail); joinStraight(outList,outJoin);
setKnots(laneIn,inList); setKnots(laneOut,outList);

// 2. branches
var oldBranches=objects.Find("工厂支路"); if(oldBranches!=null) Undo.DestroyObjectImmediate(oldBranches.gameObject);
var parent=new GameObject("工厂支路"); Undo.RegisterCreatedObjectUndo(parent,"Factory road reroute");
parent.transform.SetParent(objects,false); parent.transform.SetSiblingIndex(laneOut.transform.GetSiblingIndex()+1);
parent.transform.position=Vector3.zero; parent.transform.rotation=Quaternion.identity;
var pathMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art Resources/Environment/FactoryRoads/DirtPath05/DirtPathSpline.mat");
var mainRenderer=laneIn.GetComponent<MeshRenderer>(); float bw=(float)route["branchWidth"];
foreach(var b in route["branches"].Children<Newtonsoft.Json.Linq.JProperty>()){
    var go=new GameObject(b.Name); go.transform.SetParent(parent.transform,false); go.layer=laneIn.gameObject.layer;
    var c=go.AddComponent<UnityEngine.Splines.SplineContainer>(); var s=c.Spline; s.Clear();
    foreach(var k in b.Value) s.Add(newKnot(c,k),UnityEngine.Splines.TangentMode.Broken);
    var road=go.AddComponent<FactoryDirtRoadSpline>(); road.width=bw; road.textureLength=bw*4f; road.textureOffset=0f; road.endFade=4f;
    var mr=go.GetComponent<MeshRenderer>(); mr.sharedMaterial=pathMat; mr.shadowCastingMode=mainRenderer.shadowCastingMode; mr.receiveShadows=mainRenderer.receiveShadows;
    GameObjectUtility.SetStaticEditorFlags(go,GameObjectUtility.GetStaticEditorFlags(laneIn.gameObject));
    road.Rebuild(); }
laneIn.GetComponent<FactoryDirtRoadSpline>().Rebuild(); laneOut.GetComponent<FactoryDirtRoadSpline>().Rebuild();

// 3. trees, lamps and signs keep their height above the ground
Undo.RecordObjects(movable.Cast<UnityEngine.Object>().ToArray(),"Factory road reroute");
System.Action<Transform,Vector2> moveTo=(t,xz)=>{ var o=before["transforms"][t.name]; var p0=new Vector3((float)o[0],(float)o[1],(float)o[2]);
    t.position=new Vector3(xz.x,TH(new Vector3(xz.x,0,xz.y))+(p0.y-TH(p0)),xz.y); };
foreach(Transform t in trees){ var to=route["trees"][t.name]["world"]; moveTo(t,new Vector2((float)to[0],(float)to[1])); }
foreach(var l in route["lamps"].Children<Newtonsoft.Json.Linq.JProperty>()){ var t=lights.Find("路灯/"+l.Name);
    moveTo(t,new Vector2((float)l.Value["to"][0],(float)l.Value["to"][1]));
    var dirXZ=new Vector3((float)l.Value["roadDir"][0],0,(float)l.Value["roadDir"][1]); t.rotation=Quaternion.LookRotation(Vector3.Cross(Vector3.up,dirXZ)); }
foreach(var s in route["signs"].Children<Newtonsoft.Json.Linq.JProperty>()){ var t=lights.Find("道路标识/"+s.Name);
    if(s.Value["to"].Type!=Newtonsoft.Json.Linq.JTokenType.Null) moveTo(t,new Vector2((float)s.Value["to"][0],(float)s.Value["to"][1]));
    t.rotation=Quaternion.LookRotation(new Vector3((float)s.Value["faceXZ"][0],0,(float)s.Value["faceXZ"][1])); }
foreach(var t in movable) PrefabUtility.RecordPrefabInstancePropertyModifications(t);

Undo.CollapseUndoOperations(group);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return new{laneInKnots=inList.Count,laneOutKnots=outList.Count,laneInLength=laneIn.CalculateLength(),laneOutLength=laneOut.CalculateLength(),branches=parent.transform.childCount,moved=movable.Count,undoGroup=group,saved=false};
