var rows=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/FactoryHeightReview/survey.json"));
Vector3 V(Newtonsoft.Json.Linq.JToken a)=>new Vector3((float)a[0],(float)a[1],(float)a[2]);
var buildings=rows.Where(r=>((string)r["path"]).Contains("厂房")).ToArray();
var filters=buildings.ToDictionary(r=>(int)r["id"],r=>((GameObject)EditorUtility.InstanceIDToObject((int)r["id"])).GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh!=null&&!System.Text.RegularExpressions.Regex.IsMatch(m.name,@"LOD[1-9]",System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToArray());
var intersect=typeof(HandleUtility).GetMethod("IntersectRayMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,null,new[]{typeof(Ray),typeof(Mesh),typeof(Matrix4x4),typeof(RaycastHit).MakeByRefType()},null);
var results=new System.Collections.Generic.List<object>();
foreach(var a in rows.Where(r=>((string)r["path"]).Contains("通风与空调")||((string)r["path"]).Contains("天线"))) {
    var min=V(a["min"]);var max=V(a["max"]);var center=(min+max)*.5f;
    var matches=new System.Collections.Generic.List<object>();
    foreach(var b in buildings) {
        var bmin=V(b["min"]);var bmax=V(b["max"]);
        if(bmin.x>max.x+12||bmax.x<min.x-12||bmin.z>max.z+12||bmax.z<min.z-12)continue;
        float down=float.PositiveInfinity,side=float.PositiveInfinity;
        var origins=new[]{center,new Vector3(min.x,center.y,min.z),new Vector3(max.x,center.y,min.z),new Vector3(min.x,center.y,max.z),new Vector3(max.x,center.y,max.z)};
        foreach(var p in origins)foreach(var direction in new[]{Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back}) {
            var ray=new Ray(direction==Vector3.down?new Vector3(p.x,min.y+.5f,p.z):p,direction);
            foreach(var m in filters[(int)b["id"]]) {
                if(!m.GetComponent<Renderer>().bounds.IntersectRay(ray))continue;
                var args=new object[]{ray,m.sharedMesh,m.transform.localToWorldMatrix,new RaycastHit()};
                if((bool)intersect.Invoke(null,args)) {
                    var h=(RaycastHit)args[3];
                    if(direction==Vector3.down)down=Mathf.Min(down,h.distance-.5f);else side=Mathf.Min(side,h.distance);
                }
            }
        }
        matches.Add(new{id=(int)b["id"],name=(string)b["name"],down,side,baseDifference=min.y-bmin.y});
    }
    results.Add(new{id=(int)a["id"],name=(string)a["name"],matches});
}
System.IO.File.WriteAllText("ArtSource/FactoryHeightReview/attachments.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
return results;
