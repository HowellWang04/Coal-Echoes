// Checks the result of apply.cs. Run in the editor (Unity MCP execute_code); writes verification.json.
var dir="ArtSource/FactoryYardLayout/";
Physics.SyncTransforms();
var lodRx=new System.Text.RegularExpressions.Regex("_LOD[1-9]");
var rows=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(dir+"apply_plan.json"));
var root=GameObject.Find("In-Scene Objects/工厂建筑群").transform;
System.Func<int,Transform> T=id=>((GameObject)EditorUtility.InstanceIDToObject(id)).transform;
System.Func<Collider,bool> ground=c=>c is TerrainCollider||c.name.Contains("ModulFloor")||c.name.StartsWith("SM_Ground");
System.Func<Transform,System.Collections.Generic.List<Vector3>> verts=t=>{var pts=new System.Collections.Generic.List<Vector3>();foreach(var mf in t.GetComponentsInChildren<MeshFilter>().Where(m=>!lodRx.IsMatch(m.name)&&m.sharedMesh!=null&&m.GetComponent<Renderer>()!=null&&m.GetComponent<Renderer>().enabled))foreach(var v in mf.sharedMesh.vertices)pts.Add(mf.transform.TransformPoint(v));return pts;};
var stackOf=new System.Collections.Generic.Dictionary<Transform,int>(); var bases=new System.Collections.Generic.List<Transform>();
for(int k=0;k<rows.Count;k++){ bases.Add(T((int)rows[k]["base"])); foreach(var i in rows[k]["ids"]) stackOf[T((int)i)]=k; }

// 1. mesh overlaps of moved stacks with anything else (convex hulls in 0.25 m height bands)
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
var parts=new System.Collections.Generic.List<(Transform own,System.Collections.Generic.List<Vector2> h,float y0,Bounds b)>();
foreach(var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
    if(lodRx.IsMatch(mf.name)||mf.sharedMesh==null||!mf.gameObject.activeInHierarchy) continue; var r=mf.GetComponent<Renderer>(); if(r==null||!r.enabled) continue;
    if(!scope.Intersects(r.bounds)||r.bounds.size.x>60||r.bounds.size.z>60||mf.transform.IsChildOf(grass)) continue;
    if(mf.name.Contains("ModulFloor")||mf.name.StartsWith("SM_Ground")||mf.name.Contains("Road")||mf.name.Contains("Threshold")) continue;
    if(mf.GetComponent<FactoryDirtRoadSpline>()!=null) continue;
    var vs=mf.sharedMesh.vertices; int st=Mathf.Max(1,vs.Length/2500); var wv=new System.Collections.Generic.List<Vector3>(); for(int i=0;i<vs.Length;i+=st) wv.Add(mf.transform.TransformPoint(vs[i]));
    float ymin=wv.Min(p=>p.y), ymax=wv.Max(p=>p.y);
    for(float y=ymin;y<ymax&&y<ymin+4f;y+=0.25f){ var h=hull(wv.Where(p=>p.y>=y&&p.y<=y+0.25f).Select(p=>new Vector2(p.x,p.z)).ToList()); if(h.Count<3) continue;
        var bb=new Bounds(new Vector3(h[0].x,y,h[0].y),Vector3.zero); foreach(var p in h){bb.Encapsulate(new Vector3(p.x,y,p.y));bb.Encapsulate(new Vector3(p.x,y+0.25f,p.y));}
        parts.Add((ownerOf(mf.transform),h,y,bb)); } }
var overlaps=new System.Collections.Generic.Dictionary<string,float>();
foreach(var a in parts.Where(p=>stackOf.ContainsKey(p.own))) foreach(var b in parts){
    if(b.own==a.own||(stackOf.ContainsKey(b.own)&&stackOf[b.own]==stackOf[a.own])) continue;
    if(a.y0+0.25f<=b.y0+0.03f||b.y0+0.25f<=a.y0+0.03f||!a.b.Intersects(b.b)) continue;
    float d=sat(a.h,b.h); if(d<=0.03f) continue; var key=a.own.name+" x "+b.own.name; if(!overlaps.ContainsKey(key)||overlaps[key]<d) overlaps[key]=d; }

// 2. every moved stack rests on terrain or slab
var weak=new System.Collections.Generic.List<string>(); float worstGap=0;
foreach(var bt in bases){ var pts=verts(bt); float minY=pts.Min(p=>p.y); var bot=pts.Where(p=>p.y<minY+0.06f).ToList(); int st=Mathf.Max(1,bot.Count/40); int on=0,n=0; float mg=0;
    for(int i=0;i<bot.Count;i+=st){ var p=bot[i]; var h=Physics.RaycastAll(new Vector3(p.x,minY+2f,p.z),Vector3.down,8f,~0,QueryTriggerInteraction.Ignore).Where(x=>ground(x.collider)).OrderBy(x=>x.distance).FirstOrDefault(); n++; if(h.collider){ float g=p.y-h.point.y; mg=Mathf.Max(mg,g); if(g<0.08f) on++; } }
    worstGap=Mathf.Max(worstGap,mg); if((float)on/n<0.6f||mg>0.2f) weak.Add($"{bt.name} contact {(float)on/n:F2} gap {mg:F2}"); }

// 3. whole factory: nothing whose underside touches nothing (roof units and wall-mounted items count as touching)
var voids=new System.Collections.Generic.List<string>();
foreach(Transform cat in root) foreach(Transform g in cat){ if(cat.name.StartsWith("01")&&g.name!="立柱与支架") continue; if(g.name=="路面与路沿") continue;
    var list=new System.Collections.Generic.List<Transform>(); foreach(Transform p in g){ if(g.name=="通风与空调"&&p.name=="SM_AirConditioning") foreach(Transform c in p) list.Add(c); else list.Add(p); }
    foreach(var t in list){ if(!t.gameObject.activeInHierarchy) continue; var pts=verts(t); if(pts.Count==0) continue;
        var b=new Bounds(pts[0],Vector3.zero); foreach(var p in pts) b.Encapsulate(p);
        var self=new System.Collections.Generic.HashSet<Collider>(t.GetComponentsInChildren<Collider>());
        if(!Physics.OverlapBox(b.center,b.extents+Vector3.one*0.08f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Any(c=>!self.Contains(c))) voids.Add($"{g.name}/{t.name} at {b.center.ToString("F1")}"); } }

// 4. no prop stands on the visible part of a road (inner 80 % of the width, not in the end fades)
var roadCells=new System.Collections.Generic.HashSet<Vector2Int>();
foreach(var road in UnityEngine.Object.FindObjectsByType<FactoryDirtRoadSpline>()){
    var m=road.GetComponent<MeshFilter>().sharedMesh; var vs=m.vertices; var uvs=m.uv; var cols=m.colors; var tr=m.triangles;
    for(int i=0;i<tr.Length;i+=3){ int a=tr[i],b=tr[i+1],c=tr[i+2];
        if(new[]{a,b,c}.Any(x=>cols[x].a<0.5f||uvs[x].x<0.1f||uvs[x].x>0.9f)) continue;
        var cen=road.transform.TransformPoint((vs[a]+vs[b]+vs[c])/3f); roadCells.Add(new Vector2Int(Mathf.FloorToInt(cen.x/0.5f),Mathf.FloorToInt(cen.z/0.5f))); } }
var onRoad=new System.Collections.Generic.List<string>();
foreach(var cat in new[]{"04 桶罐与容器","05 堆放物与杂物","06 搬运与检修设施"}) foreach(Transform g in root.Find(cat)) foreach(Transform t in g){
    if(!t.gameObject.activeInHierarchy) continue; var pts=verts(t); if(pts.Count==0) continue; float minY=pts.Min(p=>p.y);
    int hit=pts.Where(p=>p.y<minY+0.3f).Count(p=>roadCells.Contains(new Vector2Int(Mathf.FloorToInt(p.x/0.5f),Mathf.FloorToInt(p.z/0.5f))));
    if(hit>0) onRoad.Add(g.name+"/"+t.name); }

var result=new{movedStacks=rows.Count,meshOverlapsOver3cm=overlaps.OrderByDescending(x=>x.Value).Select(x=>x.Key+" "+x.Value.ToString("F2")).ToArray(),
    weakGroundContact=weak.ToArray(),worstGapUnderMovedStack=System.Math.Round(worstGap,3),itemsTouchingNothing=voids.ToArray(),propsOnRoads=onRoad.ToArray(),playModeTested=false,sceneSaved=false};
System.IO.File.WriteAllText(dir+"verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
