var rows = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/FactoryHeightReview/survey.json"));
Vector3 V(Newtonsoft.Json.Linq.JToken a) => new Vector3((float)a[0],(float)a[1],(float)a[2]);
var objects = rows.ToDictionary(r=>(int)r["id"],r=>((GameObject)EditorUtility.InstanceIDToObject((int)r["id"])).transform);
var filters = rows.ToDictionary(r=>(int)r["id"],r=>r["members"].SelectMany(id=>((GameObject)EditorUtility.InstanceIDToObject((int)id)).GetComponentsInChildren<MeshFilter>(true)).Where(m=>m.sharedMesh!=null && m.gameObject.activeInHierarchy && !System.Text.RegularExpressions.Regex.IsMatch(m.name,@"LOD[1-9]",System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToArray());
var intersect = typeof(HandleUtility).GetMethod("IntersectRayMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(Ray),typeof(Mesh),typeof(Matrix4x4),typeof(RaycastHit).MakeByRefType()},null);
var output = new System.Collections.Generic.List<object>();
foreach(var a in rows) {
    int id=(int)a["id"];
    var min=V(a["min"]); var max=V(a["max"]);
    float reach=new[]{-151858,-43730,-51992,-59622}.Contains(id)?2f:.35f;
    var points=a["contacts"].Select(c=>V(c["point"])).ToArray();
    int stride=Mathf.Max(1,Mathf.CeilToInt(points.Length/36f));
    var supports=new System.Collections.Generic.List<object>();
    var nearby=new System.Collections.Generic.List<int>();
    foreach(var b in rows) {
        int bid=(int)b["id"]; if(id==bid)continue;
        var bmin=V(b["min"]);var bmax=V(b["max"]);
        if(bmin.x>max.x+.4f || bmax.x<min.x-.4f || bmin.z>max.z+.4f || bmax.z<min.z-.4f)continue;
        bool building=((string)b["path"]).Contains("厂房");
        if(building && max.y>bmin.y+.5f && min.y<bmax.y+1f)nearby.Add(bid);
        if(bmin.y>min.y-.08f || bmax.y<min.y-reach)continue;
        int hits=0;float gapSum=0;
        for(int i=0;i<points.Length;i+=stride) {
            var p=points[i];var ray=new Ray(p+Vector3.up*.3f,Vector3.down);
            float nearest=float.PositiveInfinity;
            foreach(var m in filters[bid]) {
                var renderer=m.GetComponent<Renderer>();
                if(renderer!=null && !renderer.bounds.IntersectRay(ray))continue;
                var args=new object[]{ray,m.sharedMesh,m.transform.localToWorldMatrix,new RaycastHit()};
                if((bool)intersect.Invoke(null,args)) {var hit=(RaycastHit)args[3]; if(hit.distance<=reach+.3f)nearest=Mathf.Min(nearest,hit.distance-.3f);}
            }
            if(!float.IsPositiveInfinity(nearest)){hits++;gapSum+=nearest;}
        }
        if(hits>0)supports.Add(new{id=bid,name=(string)b["name"],hits,gap=gapSum/hits});
    }
    output.Add(new{id,name=(string)a["name"],supports,nearbyBuildings=nearby});
}
System.IO.File.WriteAllText("ArtSource/FactoryHeightReview/supports.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
return new{objects=output.Count};
