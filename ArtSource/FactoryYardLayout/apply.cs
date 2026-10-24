// Applies apply_plan.json (from plan_layout.py). Run in the editor (Unity MCP execute_code); does not save the scene.
// Resets the movable props (04, 05, 06) to transforms-before.json first, so it can be re-run after a new plan.
var dir="ArtSource/FactoryYardLayout/";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(EditorApplication.isPlaying||!scene.isLoaded) throw new System.Exception("Main Game Scene not loaded in edit mode");
var lodRx=new System.Text.RegularExpressions.Regex("_LOD[1-9]");
var rows=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(dir+"apply_plan.json"));
var before=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"transforms-before.json"))["items"]
    .Where(b=>new[]{"04 ","05 ","06 "}.Any(c=>((string)b["path"]).StartsWith(c))).ToList();
var root=GameObject.Find("In-Scene Objects/工厂建筑群").transform;
var tiles=Terrain.activeTerrains.Where(t=>t.name=="Main Terrain").ToArray();
System.Func<Collider,bool> ground=c=>c is TerrainCollider||c.name.Contains("ModulFloor")||c.name.StartsWith("SM_Ground");
System.Func<int,Transform> T=id=>((GameObject)EditorUtility.InstanceIDToObject(id)).transform;
System.Func<Transform,System.Collections.Generic.List<Vector3>> verts=t=>{var pts=new System.Collections.Generic.List<Vector3>();foreach(var mf in t.GetComponentsInChildren<MeshFilter>().Where(m=>!lodRx.IsMatch(m.name)&&m.sharedMesh!=null&&m.GetComponent<Renderer>()!=null&&m.GetComponent<Renderer>().enabled))foreach(var v in mf.sharedMesh.vertices)pts.Add(mf.transform.TransformPoint(v));return pts;};

Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Factory yard layout");
var all=new System.Collections.Generic.List<Transform>();
foreach(var b in before){ var t=T((int)b["id"]); if(t==null) throw new System.Exception("Missing "+(string)b["path"]); all.Add(t); }
Undo.RecordObjects(all.Cast<UnityEngine.Object>().ToArray(),"Factory yard layout");
foreach(var b in before){ var t=T((int)b["id"]); var p=b["lp"]; var r=b["lr"];
    t.localPosition=new Vector3((float)p[0],(float)p[1],(float)p[2]); t.localRotation=new Quaternion((float)r[0],(float)r[1],(float)r[2],(float)r[3]); }
Physics.SyncTransforms();

// 1. move and turn every stack as a rigid group (fallen ladders and cylinders are stood up)
var stacks=new System.Collections.Generic.List<(Transform b,System.Collections.Generic.List<Transform> ts,Vector3 n)>();
foreach(var row in rows){
    var ts=row["ids"].Select(i=>T((int)i)).ToList(); var n=new Vector3((float)row["normal"][0],0,(float)row["normal"][1]);
    var to=new Vector3((float)row["to"][0],0,(float)row["to"][1]);
    if(row["standup"].Type!=Newtonsoft.Json.Linq.JTokenType.Null){
        var t=ts[0]; t.rotation=Quaternion.Euler(0,(float)row["standupYaw"],0);
        var v=verts(t); t.position+=new Vector3(to.x-(v.Min(p=>p.x)+v.Max(p=>p.x))/2,0,to.z-(v.Min(p=>p.z)+v.Max(p=>p.z))/2);
    } else {
        var from=new Vector3((float)row["from"][0],0,(float)row["from"][1]);
        foreach(var t in ts){ t.RotateAround(from,Vector3.up,(float)row["dyaw"]); t.position+=to-from; }
    }
    stacks.Add((T((int)row["base"]),ts,n));
}
var movedSet=new System.Collections.Generic.HashSet<Transform>(stacks.SelectMany(s=>s.ts));
var stackOf=new System.Collections.Generic.Dictionary<Transform,int>(); for(int k=0;k<stacks.Count;k++) foreach(var t in stacks[k].ts) stackOf[t]=k;

// 2. rest each stack on terrain or a concrete slab (never on the wider collision boxes of wall footings);
//    where the ground under it is not flat (slab edge), slide it up to 1.5 m to a flat spot
System.Func<Transform,float[]> groundUnder=(t)=>{ var pts=verts(t); float minY=pts.Min(p=>p.y); var bot=pts.Where(p=>p.y<minY+0.06f).ToList(); int st=Mathf.Max(1,bot.Count/60); var hs=new System.Collections.Generic.List<float>();
    for(int i=0;i<bot.Count;i+=st){ var p=bot[i]; var h=Physics.RaycastAll(new Vector3(p.x,minY+2f,p.z),Vector3.down,8f,~0,QueryTriggerInteraction.Ignore).Where(x=>ground(x.collider)).OrderBy(x=>x.distance).FirstOrDefault(); if(h.collider) hs.Add(h.point.y); }
    hs.Sort(); return new[]{minY,hs[(int)(hs.Count*0.85f)],hs[hs.Count-1]-hs[0]}; };
System.Action<int> settle=k=>{ var s=stacks[k]; Physics.SyncTransforms(); var g=groundUnder(s.b); float dy=g[1]-0.02f-g[0]; foreach(var t in s.ts) t.position+=Vector3.up*dy; };
int slid=0;
for(int k=0;k<stacks.Count;k++){
    Physics.SyncTransforms(); var s=stacks[k];
    if(groundUnder(s.b)[2]>0.12f){
        var tan=new Vector3(-s.n.z,0,s.n.x); Vector3 best=Vector3.zero; float bd=99;
        var self=new System.Collections.Generic.HashSet<Collider>(s.ts.SelectMany(t=>t.GetComponentsInChildren<Collider>()));
        foreach(var ax in new[]{s.n,tan,-tan,-s.n}) for(float d=0.1f;d<=1.5f&&d<bd;d+=0.1f){
            foreach(var t in s.ts) t.position+=ax*d; Physics.SyncTransforms();
            bool flat=groundUnder(s.b)[2]<=0.05f; bool clear=true;
            if(flat) foreach(var c in self){ var b=c.bounds; if(Physics.OverlapBox(b.center+Vector3.up*0.05f,b.extents*0.95f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(o=>!self.Contains(o)&&!ground(o))){clear=false;break;} }
            foreach(var t in s.ts) t.position-=ax*d;
            if(flat&&clear){ bd=d; best=ax*d; break; } }
        if(bd<99){ foreach(var t in s.ts) t.position+=best; slid++; }
    }
    settle(k);
}

// 3. visual clearance: push a stack away from its wall when its mesh cuts into anything it was not planned to touch
System.Func<System.Collections.Generic.List<Vector2>,System.Collections.Generic.List<Vector2>> hull=P=>{
    var p=P.Distinct().OrderBy(a=>a.x).ThenBy(a=>a.y).ToList(); if(p.Count<3) return p;
    System.Func<Vector2,Vector2,Vector2,float> cr=(o,a,b)=>(a.x-o.x)*(b.y-o.y)-(a.y-o.y)*(b.x-o.x);
    var H=new Vector2[2*p.Count]; int k=0;
    for(int i=0;i<p.Count;i++){while(k>=2&&cr(H[k-2],H[k-1],p[i])<=0)k--;H[k++]=p[i];}
    for(int i=p.Count-2,t=k+1;i>=0;i--){while(k>=t&&cr(H[k-2],H[k-1],p[i])<=0)k--;H[k++]=p[i];}
    return H.Take(k-1).ToList();};
System.Func<System.Collections.Generic.List<Vector2>,System.Collections.Generic.List<Vector2>,float> sat=(A,Bp)=>{ float best=float.MaxValue;
    foreach(var P in new[]{A,Bp}) for(int i=0;i<P.Count;i++){ var e=P[(i+1)%P.Count]-P[i]; var nn=new Vector2(-e.y,e.x); if(nn.sqrMagnitude<1e-10) continue; nn.Normalize();
        float a0=A.Min(p=>Vector2.Dot(p,nn)),a1=A.Max(p=>Vector2.Dot(p,nn)),b0=Bp.Min(p=>Vector2.Dot(p,nn)),b1=Bp.Max(p=>Vector2.Dot(p,nn));
        float o=Mathf.Min(a1,b1)-Mathf.Max(a0,b0); if(o<=0) return 0f; best=Mathf.Min(best,o);} return best; };
var scope=new Bounds(new Vector3(-2320,-430,-1830),new Vector3(360,40,360));
var grass=GameObject.Find("In-Scene Objects/工厂大门/门口干草").transform;
System.Func<Transform,Transform> ownerOf=t=>{ var o=t; while(o.parent!=null&&o.parent.parent!=null&&o.parent.parent.parent!=null&&o.parent.parent.parent!=root) o=o.parent; return (o.parent!=null&&o.parent.parent!=null&&o.parent.parent.parent==root)?o:t; };
System.Func<bool,System.Collections.Generic.List<(Transform own,System.Collections.Generic.List<Vector2> h,float y0,Bounds b)>> bands=(movedOnly)=>{
    var parts=new System.Collections.Generic.List<(Transform own,System.Collections.Generic.List<Vector2> h,float y0,Bounds b)>();
    foreach(var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
        if(lodRx.IsMatch(mf.name)||mf.sharedMesh==null||!mf.gameObject.activeInHierarchy) continue; var r=mf.GetComponent<Renderer>(); if(r==null||!r.enabled) continue;
        if(!scope.Intersects(r.bounds)||r.bounds.size.x>60||r.bounds.size.z>60||mf.transform.IsChildOf(grass)) continue;
        if(mf.name.Contains("ModulFloor")||mf.name.StartsWith("SM_Ground")||mf.name.Contains("Road")||mf.name.Contains("Threshold")) continue;
        if(mf.GetComponent<FactoryDirtRoadSpline>()!=null) continue;   // road decals lie flat on the ground
        var own=ownerOf(mf.transform); if(movedOnly&&!movedSet.Contains(own)) continue;
        var vs=mf.sharedMesh.vertices; int st=Mathf.Max(1,vs.Length/2500); var wv=new System.Collections.Generic.List<Vector3>(); for(int i=0;i<vs.Length;i+=st) wv.Add(mf.transform.TransformPoint(vs[i]));
        float ymin=wv.Min(p=>p.y), ymax=wv.Max(p=>p.y);
        for(float y=ymin;y<ymax&&y<ymin+4f;y+=0.25f){ var h=hull(wv.Where(p=>p.y>=y&&p.y<=y+0.25f).Select(p=>new Vector2(p.x,p.z)).ToList()); if(h.Count<3) continue;
            var bb=new Bounds(new Vector3(h[0].x,y,h[0].y),Vector3.zero); foreach(var p in h){bb.Encapsulate(new Vector3(p.x,y,p.y));bb.Encapsulate(new Vector3(p.x,y+0.25f,p.y));}
            parts.Add((own,h,y,bb)); } }
    return parts; };
var fixedParts=bands(false).Where(p=>!movedSet.Contains(p.own)).ToList();
int pushed=0;
for(int pass=0;pass<3;pass++){
    Physics.SyncTransforms(); var mp=bands(true); var depth=new float[stacks.Count];
    foreach(var a in mp){ int k=stackOf[a.own];
        foreach(var b in fixedParts.Concat(mp)){ if(b.own==a.own||(movedSet.Contains(b.own)&&stackOf[b.own]==k)) continue;
            if(a.y0+0.25f<=b.y0+0.03f||b.y0+0.25f<=a.y0+0.03f||!a.b.Intersects(b.b)) continue;
            float d=sat(a.h,b.h); if(d>0.03f) depth[k]=Mathf.Max(depth[k],d); } }
    int n=0; for(int k=0;k<stacks.Count;k++) if(depth[k]>0){ foreach(var t in stacks[k].ts) t.position+=stacks[k].n*(depth[k]+0.03f); settle(k); n++; }
    pushed+=n; if(n==0) break;
}

foreach(var t in movedSet) PrefabUtility.RecordPrefabInstancePropertyModifications(t);
Undo.CollapseUndoOperations(group);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
var inv=System.Globalization.CultureInfo.InvariantCulture; var sb=new System.Text.StringBuilder("{\"items\":[");
foreach(Transform cat in root) foreach(Transform g in cat) foreach(Transform p in g){ var lp=p.localPosition; var lr=p.localRotation;
    sb.Append(string.Format(inv,"{{\"path\":\"{0}/{1}/{2}#{3}\",\"id\":{4},\"lp\":[{5:R},{6:R},{7:R}],\"lr\":[{8:R},{9:R},{10:R},{11:R}]}},",cat.name,g.name,p.name,p.GetSiblingIndex(),p.gameObject.GetInstanceID(),lp.x,lp.y,lp.z,lr.x,lr.y,lr.z,lr.w)); }
sb.Length--; sb.Append("]}");
System.IO.File.WriteAllText(dir+"transforms-after.json",sb.ToString());
var result=new{stacks=stacks.Count,transforms=movedSet.Count,slidOffSlabEdge=slid,pushedFromWalls=pushed,undoGroup=group,saved=false};
System.IO.File.WriteAllText(dir+"applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
