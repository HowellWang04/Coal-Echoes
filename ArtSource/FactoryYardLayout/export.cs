// Survey of the factory yard for plan_layout.py. Run in the editor (Unity MCP execute_code) with the layout still untouched.
// Writes Survey/*.json in the factory frame: u along the main road, v across it.
var dir="ArtSource/FactoryYardLayout/Survey/";
System.IO.Directory.CreateDirectory(dir);
Physics.SyncTransforms();
var inv=System.Globalization.CultureInfo.InvariantCulture;
var lodRx=new System.Text.RegularExpressions.Regex("_LOD[1-9]");
var C=new Vector3(-2320,0,-1830); var U=new Vector3(0.7314f,0,0.682f); var V=new Vector3(0.682f,0,-0.7314f);
System.Func<Vector3,Vector2> uv=w=>{var d=w-C;return new Vector2(Vector3.Dot(d,U),Vector3.Dot(d,V));};
var tiles=Terrain.activeTerrains.Where(t=>t.name=="Main Terrain").ToArray();
System.Func<Vector3,float> TH=w=>{foreach(var t in tiles){var p=t.GetPosition();var s=t.terrainData.size;if(w.x>=p.x&&w.x<p.x+s.x&&w.z>=p.z&&w.z<p.z+s.z)return t.SampleHeight(w)+p.y;}return -430.65f;};
System.Func<System.Collections.Generic.List<Vector2>,System.Collections.Generic.List<Vector2>> hull=P=>{
    var p=P.Distinct().OrderBy(a=>a.x).ThenBy(a=>a.y).ToList(); if(p.Count<3) return p;
    System.Func<Vector2,Vector2,Vector2,float> cr=(o,a,b)=>(a.x-o.x)*(b.y-o.y)-(a.y-o.y)*(b.x-o.x);
    var H=new Vector2[2*p.Count]; int k=0;
    for(int i=0;i<p.Count;i++){while(k>=2&&cr(H[k-2],H[k-1],p[i])<=0)k--;H[k++]=p[i];}
    for(int i=p.Count-2,t=k+1;i>=0;i--){while(k>=t&&cr(H[k-2],H[k-1],p[i])<=0)k--;H[k++]=p[i];}
    return H.Take(k-1).ToList();};
System.Func<System.Collections.Generic.List<Vector2>,string> pj=L=>"["+string.Join(",",L.Select(a=>string.Format(inv,"[{0:F3},{1:F3}]",a.x,a.y)))+"]";
System.Func<MeshFilter,bool> visible=mf=>!lodRx.IsMatch(mf.name)&&mf.sharedMesh!=null&&mf.gameObject.activeInHierarchy&&mf.GetComponent<Renderer>()!=null&&mf.GetComponent<Renderer>().enabled;
var root=GameObject.Find("In-Scene Objects/工厂建筑群").transform;

// 1. movable yard items: footprint hull and what each one stands on
var items=new System.Collections.Generic.List<(string grp,Transform t)>();
foreach(var cat in new[]{"04 桶罐与容器","05 堆放物与杂物","06 搬运与检修设施"}) foreach(Transform g in root.Find(cat)){ if(g.name=="煤堆") continue; foreach(Transform p in g) if(p.gameObject.activeInHierarchy) items.Add((g.name,p)); }
var owner=new System.Collections.Generic.Dictionary<Collider,int>();
foreach(var (g,t) in items) foreach(var c in t.GetComponentsInChildren<Collider>()) owner[c]=t.gameObject.GetInstanceID();
var sb=new System.Text.StringBuilder("{\"items\":[");
foreach(var (g,t) in items){
    var pts=new System.Collections.Generic.List<Vector3>();
    foreach(var mf in t.GetComponentsInChildren<MeshFilter>().Where(visible)) foreach(var v in mf.sharedMesh.vertices) pts.Add(mf.transform.TransformPoint(v));
    if(pts.Count==0) continue;
    float minY=pts.Min(p=>p.y);
    var bot=pts.Where(p=>p.y<minY+0.06f).ToList(); var self=new System.Collections.Generic.HashSet<Collider>(t.GetComponentsInChildren<Collider>());
    var sup=new System.Collections.Generic.Dictionary<string,int>(); int step=Mathf.Max(1,bot.Count/80);
    for(int i=0;i<bot.Count;i+=step){ var p=bot[i]; var hit=Physics.RaycastAll(p+Vector3.up*0.25f,Vector3.down,3f,~0,QueryTriggerInteraction.Ignore).Where(x=>!self.Contains(x.collider)).OrderBy(x=>x.distance).FirstOrDefault();
        string key; if(hit.collider==null||p.y-hit.point.y>0.3f) key="air"; else if(owner.ContainsKey(hit.collider)) key=owner[hit.collider].ToString(); else if(hit.collider is TerrainCollider) key="terrain"; else if(hit.collider.name.Contains("ModulFloor")||hit.collider.name.StartsWith("SM_Ground")) key="slab"; else key="x:"+hit.collider.name;
        sup[key]=sup.ContainsKey(key)?sup[key]+1:1; }
    sb.Append(string.Format(inv,"{{\"id\":{0},\"n\":\"{1}\",\"g\":\"{2}\",\"e\":[{3:F2},{4:F2},{5:F2}],\"hull\":{6},\"sup\":{{{7}}}}},",
        t.gameObject.GetInstanceID(),t.name,g,t.eulerAngles.x,t.eulerAngles.y,t.eulerAngles.z,pj(hull(pts.Select(uv).ToList())),string.Join(",",sup.Select(kv=>"\""+kv.Key.Replace("\"","")+"\":"+kv.Value))));
}
sb.Length--; sb.Append("]}");
System.IO.File.WriteAllText(dir+"yard_export.json",sb.ToString());

// 2. obstacles: footprint of everything below 2.4 m above ground, per mesh
var obsRoots=new System.Collections.Generic.List<(string kind,Transform t)>();
foreach(Transform cat in root) foreach(Transform g in cat){
    string kind=cat.name.StartsWith("01")?(g.name=="立柱与支架"?"prop":"struct"):cat.name.StartsWith("03")?(g.name=="通风与空调"?"prop":"struct"):cat.name.StartsWith("02")?(g.name=="路面与路沿"?"floor":(g.name=="出入口"?"struct":"prop")):cat.name.StartsWith("07")?"prop":(g.name=="煤堆"?"struct":null);
    if(kind!=null) obsRoots.Add((kind,g)); }
obsRoots.Add(("wall",GameObject.Find("In-Scene Objects/工厂围墙").transform));
obsRoots.Add(("struct",GameObject.Find("In-Scene Objects/工厂大门/FactoryGate").transform));
obsRoots.Add(("struct",GameObject.Find("In-Scene Objects/工厂大门/Guard Post").transform));
obsRoots.Add(("prop",GameObject.Find("In-Scene Objects/环路内枯树").transform));
sb=new System.Text.StringBuilder("[");
foreach(var (kind,rt) in obsRoots) foreach(var mf in rt.GetComponentsInChildren<MeshFilter>().Where(visible)){
    var vs=mf.sharedMesh.vertices; int st=Mathf.Max(1,vs.Length/4000); var low=new System.Collections.Generic.List<Vector2>();
    for(int i=0;i<vs.Length;i+=st){ var w=mf.transform.TransformPoint(vs[i]); if(w.y<TH(w)+2.4f) low.Add(uv(w)); }
    if(low.Count<3) continue; var h=hull(low); if(h.Count<3) continue;
    sb.Append(string.Format(inv,"{{\"k\":\"{0}\",\"g\":\"{1}\",\"n\":\"{2}\",\"hull\":{3}}},",kind,rt.name,mf.name.Replace("\"",""),pj(h))); }
sb.Length--; sb.Append("]");
System.IO.File.WriteAllText(dir+"obstacles.json",sb.ToString());

// 3. equipment on legs and pipe packs: one hull per object so nothing ends up between the legs
sb=new System.Text.StringBuilder("[");
foreach(var gp in new[]{"03 工业设备/料仓与储罐","03 工业设备/管道","01 建筑与构筑物/立柱与支架","02 道路与围栏/出入口"}) foreach(Transform item in root.Find(gp)){
    if(!item.gameObject.activeInHierarchy) continue; var low=new System.Collections.Generic.List<Vector2>();
    foreach(var mf in item.GetComponentsInChildren<MeshFilter>().Where(visible)) foreach(var v in mf.sharedMesh.vertices){ var w=mf.transform.TransformPoint(v); if(w.y<TH(w)+2.4f) low.Add(uv(w)); }
    if(low.Count<3) continue; var h=hull(low); if(h.Count<3) continue;
    sb.Append(string.Format(inv,"{{\"g\":\"{0}\",\"n\":\"{1}\",\"hull\":{2}}},",gp,item.name,pj(h))); }
sb.Length--; sb.Append("]");
System.IO.File.WriteAllText(dir+"equip.json",sb.ToString());

// 4. building footprints (filled, so nothing is planned indoors)
sb=new System.Text.StringBuilder("[");
foreach(var gname in new[]{"大型厂房","混凝土厂房","砖砌厂房"}) foreach(Transform b in root.Find("01 建筑与构筑物/"+gname)){
    if(!b.gameObject.activeInHierarchy) continue; float u0=1e9f,u1=-1e9f,v0=1e9f,v1=-1e9f; int n=0;
    foreach(var mf in b.GetComponentsInChildren<MeshFilter>().Where(visible)){ var vs=mf.sharedMesh.vertices; int st=Mathf.Max(1,vs.Length/3000);
        for(int i=0;i<vs.Length;i+=st){ var w=mf.transform.TransformPoint(vs[i]); if(w.y<TH(w)+1.5f){ var q=uv(w); u0=Mathf.Min(u0,q.x);u1=Mathf.Max(u1,q.x);v0=Mathf.Min(v0,q.y);v1=Mathf.Max(v1,q.y); n++; } } }
    if(n>0) sb.Append(string.Format(inv,"{{\"b\":\"{0}\",\"r\":[{1:F2},{2:F2},{3:F2},{4:F2}]}},",b.name,u0,u1,v0,v1)); }
sb.Length--; sb.Append("]");
System.IO.File.WriteAllText(dir+"buildings.json",sb.ToString());

// 5. road meshes (main lanes and branches) and ground-floor doors
sb=new System.Text.StringBuilder("{\"roads\":[");
foreach(var road in UnityEngine.Object.FindObjectsByType<FactoryDirtRoadSpline>()){ var mf=road.GetComponent<MeshFilter>(); var m=mf.sharedMesh; var tr=m.triangles;
    var q=m.vertices.Select(v=>uv(mf.transform.TransformPoint(v))).ToArray(); sb.Append("[");
    for(int i=0;i<tr.Length;i+=3) sb.Append(string.Format(inv,"[{0:F2},{1:F2},{2:F2},{3:F2},{4:F2},{5:F2}],",q[tr[i]].x,q[tr[i]].y,q[tr[i+1]].x,q[tr[i+1]].y,q[tr[i+2]].x,q[tr[i+2]].y));
    sb.Length--; sb.Append("],"); }
sb.Length--; sb.Append("],\"doors\":[");
foreach(var r in root.Find("01 建筑与构筑物").GetComponentsInChildren<Renderer>()){
    if(!r.name.Contains("Door")||lodRx.IsMatch(r.name)||!r.gameObject.activeInHierarchy) continue;
    var mf=r.GetComponent<MeshFilter>(); var lb=mf.sharedMesh.bounds; float u0=1e9f,u1=-1e9f,v0=1e9f,v1=-1e9f;
    for(int i=0;i<8;i++){ var c=new Vector3((i&1)==0?lb.min.x:lb.max.x,(i&2)==0?lb.min.y:lb.max.y,(i&4)==0?lb.min.z:lb.max.z); var q=uv(mf.transform.TransformPoint(c)); u0=Mathf.Min(u0,q.x);u1=Mathf.Max(u1,q.x);v0=Mathf.Min(v0,q.y);v1=Mathf.Max(v1,q.y); }
    sb.Append(string.Format(inv,"[{0:F2},{1:F2},{2:F2},{3:F2},{4:F2}],",u0,u1,v0,v1,r.bounds.min.y)); }
sb.Length--; sb.Append("]}");
System.IO.File.WriteAllText(dir+"extra.json",sb.ToString());

// 6. walkable ground height (terrain or concrete slab) on a 0.5 m grid
float U0=-150,U1=136,V0=-58,V1=52,R=0.5f; int nu=(int)((U1-U0)/R), nvv=(int)((V1-V0)/R);
var terr=new float[nu*nvv]; var surf=new float[nu*nvv];
for(int i=0;i<nu;i++) for(int j=0;j<nvv;j++){
    var w=C+U*(U0+(i+0.5f)*R)+V*(V0+(j+0.5f)*R);
    float tt=float.NaN, s=float.NaN;
    foreach(var h in Physics.RaycastAll(new Vector3(w.x,-380f,w.z),Vector3.down,120f,~0,QueryTriggerInteraction.Ignore)){
        if(h.collider is TerrainCollider){ if(float.IsNaN(tt)||h.point.y>tt) tt=h.point.y; }
        else if(h.collider.name.Contains("ModulFloor")||h.collider.name.StartsWith("SM_Ground")){ if(float.IsNaN(s)||h.point.y>s) s=h.point.y; } }
    terr[i*nvv+j]=tt; surf[i*nvv+j]=float.IsNaN(s)?tt:(float.IsNaN(tt)?s:Mathf.Max(s,tt)); }
System.Func<float[],string> arr=a=>string.Join(",",a.Select(f=>float.IsNaN(f)?"null":f.ToString("F3",inv)));
System.IO.File.WriteAllText(dir+"ground.json",string.Format(inv,"{{\"U0\":{0},\"V0\":{1},\"R\":{2},\"nu\":{3},\"nv\":{4},\"terr\":[{5}],\"surf\":[{6}]}}",U0,V0,R,nu,nvv,arr(terr),arr(surf)));
return "items="+items.Count;
