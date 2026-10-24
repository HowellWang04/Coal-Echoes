// Apply once to the surveyed scene. The existing inner loop stays unchanged.
if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var inbound=all.First(t=>t.name=="工厂道路向内").GetComponent<FactoryDirtRoadSpline>();
var outbound=all.First(t=>t.name=="工厂道路向外").GetComponent<FactoryDirtRoadSpline>();
var frame=inbound.transform;
var outsideIn=all.First(t=>t.name=="厂门连接路向内").GetComponent<FactoryDirtRoadSpline>();
var outsideOut=all.First(t=>t.name=="厂门连接路向外").GetComponent<FactoryDirtRoadSpline>();
var gate=all.First(t=>t.name=="FactoryGate");
if(UnityEngine.Mathf.Abs(gate.Find("Left_GateLeaf").localPosition.x+4.6f)>.01f)throw new System.Exception("Gate is no longer at the surveyed width");
const float halfSeparation=3.112f;
const float gateShift=3.06f;
float gateZ=frame.InverseTransformPoint(gate.position).z-gateShift;
UnityEditor.Undo.IncrementCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Align and widen factory entrance road");int undo=UnityEditor.Undo.GetCurrentGroup();
var allowed=new System.Collections.Generic.HashSet<int>();
System.Action<UnityEngine.Transform> record=t=>{UnityEditor.Undo.RecordObject(t,"Align factory entrance");allowed.Add(t.GetInstanceID());};
System.Func<float,float,UnityEngine.Vector3> center=(x,y)=>new UnityEngine.Vector3(x,y,10.33f+(gateZ-10.33f)*UnityEngine.Mathf.SmoothStep(0,1,UnityEngine.Mathf.Clamp01((x-74)/42)));
System.Func<float,float,UnityEngine.Vector3> lane=(x,sign)=>{
    float t=UnityEngine.Mathf.Clamp01((x-74)/42);
    float dz=(gateZ-10.33f)*6*t*(1-t)/42;
    return center(x,0)+new UnityEngine.Vector3(-dz,0,1).normalized*(sign*halfSeparation);
};
System.Func<float,float,UnityEngine.Vector3> derivative=(x,sign)=>(lane(x+.001f,sign)-lane(x-.001f,sign))/.002f;
System.Func<float,UnityEngine.Splines.BezierKnot[]> approach=sign=>{
    var xs=new[]{74f,81f,88f,95f,102f,109f,116f,134f};
    return xs.Select((x,i)=>new UnityEngine.Splines.BezierKnot((Unity.Mathematics.float3)lane(x,sign),
        (Unity.Mathematics.float3)(-derivative(x,sign)*(i==0?0:(x-xs[i-1])/3)),
        (Unity.Mathematics.float3)(derivative(x,sign)*(i==xs.Length-1?0:(xs[i+1]-x)/3)))).ToArray();
};
foreach(var road in new[]{inbound,outbound}){
    var container=road.GetComponent<UnityEngine.Splines.SplineContainer>();var old=container.Spline.ToArray();
    UnityEditor.Undo.RecordObject(container,"Remove entrance dogleg");UnityEditor.Undo.RecordObject(road,"Unify road width");
    var curve=approach(road==inbound?1:-1);
    var knots=new System.Collections.Generic.List<UnityEngine.Splines.BezierKnot>();
    if(road==inbound){
        for(int i=curve.Length-1;i>=0;i--){var k=curve[i];var swap=k.TangentIn;k.TangentIn=k.TangentOut;k.TangentOut=swap;if(i==0)k.TangentOut=Unity.Mathematics.math.mul(old[9].Rotation,old[9].TangentOut);knots.Add(k);}
        knots.AddRange(old.Skip(10));
    }else{
        knots.AddRange(old.Take(13));var k=curve[0];k.TangentIn=Unity.Mathematics.math.mul(old[13].Rotation,old[13].TangentIn);knots.Add(k);knots.AddRange(curve.Skip(1));
    }
    var spline=new UnityEngine.Splines.Spline();foreach(var knot in knots)spline.Add(knot,UnityEngine.Splines.TangentMode.Broken);container.Spline=spline;
    road.width=8.5f;road.widthMultiplier=UnityEngine.AnimationCurve.Constant(0,1,1);
    UnityEditor.EditorUtility.SetDirty(container);UnityEditor.EditorUtility.SetDirty(road);road.Rebuild();
}
// Reuse the existing downhill centerline. Only lane separation and the flat
// entrance join change; the lower valley route and elevation profile remain.
var inContainer=outsideIn.GetComponent<UnityEngine.Splines.SplineContainer>();
var outContainer=outsideOut.GetComponent<UnityEngine.Splines.SplineContainer>();
var a=inContainer.Spline.ToArray();var b=outContainer.Spline.ToArray();
var centers=new UnityEngine.Vector3[a.Length];var ti=new UnityEngine.Vector3[a.Length];var to=new UnityEngine.Vector3[a.Length];
for(int i=0;i<a.Length;i++){
    centers[i]=frame.InverseTransformPoint((outsideIn.transform.TransformPoint((UnityEngine.Vector3)a[i].Position)+outsideOut.transform.TransformPoint((UnityEngine.Vector3)b[i].Position))*.5f);
    ti[i]=frame.InverseTransformVector((outsideIn.transform.TransformVector((UnityEngine.Vector3)Unity.Mathematics.math.mul(a[i].Rotation,a[i].TangentIn))+outsideOut.transform.TransformVector((UnityEngine.Vector3)Unity.Mathematics.math.mul(b[i].Rotation,b[i].TangentIn)))*.5f);
    to[i]=frame.InverseTransformVector((outsideIn.transform.TransformVector((UnityEngine.Vector3)Unity.Mathematics.math.mul(a[i].Rotation,a[i].TangentOut))+outsideOut.transform.TransformVector((UnityEngine.Vector3)Unity.Mathematics.math.mul(b[i].Rotation,b[i].TangentOut)))*.5f);
}
centers[0]=new UnityEngine.Vector3(120,centers[0].y,gateZ);centers[1]=new UnityEngine.Vector3(134,centers[1].y,gateZ);
ti[0]=new UnityEngine.Vector3(-4.666667f,0,0);to[0]=-ti[0];ti[1]=ti[0];to[1]=new UnityEngine.Vector3(6,0,0);
foreach(var road in new[]{outsideIn,outsideOut}){
    var container=road.GetComponent<UnityEngine.Splines.SplineContainer>();UnityEditor.Undo.RecordObject(container,"Align outside entrance");UnityEditor.Undo.RecordObject(road,"Unify outside road width");
    var spline=new UnityEngine.Splines.Spline();float sign=road==outsideIn?1:-1;
    for(int i=0;i<centers.Length;i++){
        var direction=to[i].normalized;direction.y=0;direction.Normalize();
        var across=new UnityEngine.Vector3(-direction.z,0,direction.x);
        var p=road.transform.InverseTransformPoint(frame.TransformPoint(centers[i]+across*(sign*halfSeparation)));
        var incoming=road.transform.InverseTransformVector(frame.TransformVector(ti[i]));
        var outgoing=road.transform.InverseTransformVector(frame.TransformVector(to[i]));
        spline.Add(new UnityEngine.Splines.BezierKnot((Unity.Mathematics.float3)p,(Unity.Mathematics.float3)incoming,(Unity.Mathematics.float3)outgoing),UnityEngine.Splines.TangentMode.Broken);
    }
    container.Spline=spline;road.width=8.5f;road.widthMultiplier=UnityEngine.AnimationCurve.Constant(0,1,1);
    UnityEditor.EditorUtility.SetDirty(container);UnityEditor.EditorUtility.SetDirty(road);road.Rebuild();
}
// Widen away from the guardhouse. The positive-side pier stays in world space.
record(gate);gate.position-=gate.right*gateShift;
foreach(var name in new[]{"Left_Pier","Left_WingWall","Right_Pier","Left_GateLeaf","Right_GateLeaf"}){
    var t=gate.Find(name);record(t);
    if(name.Contains("GateLeaf")){float sign=UnityEngine.Mathf.Sign(t.localPosition.x);var p=t.localPosition;p.x=sign*7.66f;t.localPosition=p;var s=t.localScale;s.x*=7.66f/4.6f;t.localScale=s;}
    else t.localPosition+=UnityEngine.Vector3.right*(name=="Right_Pier"?-gateShift:gateShift);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);
}
foreach(var c in gate.GetComponents<UnityEngine.BoxCollider>()){
    UnityEditor.Undo.RecordObject(c,"Widen gate pier collision");var p=c.center;p.x+=UnityEngine.Mathf.Sign(p.x)*gateShift;c.center=p;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);
}
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
var threshold=all.First(t=>t.name=="Gate Concrete Threshold");record(threshold);threshold.position-=gate.right*gateShift;var thresholdScale=threshold.localScale;thresholdScale.x+=gateShift*2;threshold.localScale=thresholdScale;
// Move only the two lights on the replaced bend; retain their ground offset.
foreach(var name in new[]{"SM_StreetLight5","SM_StreetLight3"}){
    var t=all.First(v=>v.name==name);record(t);var local=frame.InverseTransformPoint(t.position);var pos=center(local.x,local.y);t.position=frame.TransformPoint(pos);
    var tangent=(center(local.x+.1f,0)-center(local.x-.1f,0)).normalized;
    t.rotation=UnityEngine.Quaternion.LookRotation(frame.TransformDirection(new UnityEngine.Vector3(-tangent.z,0,tangent.x)),UnityEngine.Vector3.up);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);
}
UnityEditor.Undo.CollapseUndoOperations(undo);UnityEngine.Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
System.IO.File.WriteAllText("Library/RoadGateAlignment/allowed.json",Newtonsoft.Json.JsonConvert.SerializeObject(allowed));
return new{gateOpening=15.32,roadCoverage=14.724,gateCenter=gateZ,movedLights=2,wallTrimPending=true};
