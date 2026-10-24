var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var frame=all.First(t=>t.name=="工厂道路向内");var gate=all.First(t=>t.name=="FactoryGate");
UnityEngine.Physics.SyncTransforms();
var roads=UnityEngine.Object.FindObjectsByType<FactoryDirtRoadSpline>(UnityEngine.FindObjectsSortMode.None);
var hits=new System.Collections.Generic.HashSet<string>();int samples=0;float maxGroundError=0,maxCrossSlope=0,minW=100,maxW=0;
foreach(var road in roads){
    var mesh=road.GetComponent<UnityEngine.MeshFilter>().sharedMesh;var vertices=mesh.vertices;var colors=mesh.colors;
    int columns=UnityEngine.Mathf.CeilToInt(road.width/.4f)+1;
    for(int i=0;i<vertices.Length;i+=columns*10){
        var center=road.transform.TransformPoint(vertices[i+columns/2]);var local=frame.InverseTransformPoint(center);
        if(local.x<74)continue;
        minW=UnityEngine.Mathf.Min(minW,colors[i].r);maxW=UnityEngine.Mathf.Max(maxW,colors[i].r);
        var first=road.transform.TransformPoint(vertices[i]);var last=road.transform.TransformPoint(vertices[i+columns-1]);
        var horizontal=last-first;horizontal.y=0;maxCrossSlope=UnityEngine.Mathf.Max(maxCrossSlope,UnityEngine.Mathf.Abs(last.y-first.y)/horizontal.magnitude);
        foreach(int col in new[]{3,columns/2,columns-4}){
            var p=road.transform.TransformPoint(vertices[i+col]);samples++;
            foreach(var hit in UnityEngine.Physics.OverlapCapsule(p+UnityEngine.Vector3.up*.5f,p+UnityEngine.Vector3.up*1.5f,.3f,~0,UnityEngine.QueryTriggerInteraction.Ignore)){
                if(hit is UnityEngine.TerrainCollider||hit.transform.IsChildOf(gate)||hit.name.StartsWith("SM_StreetLight"))continue;
                hits.Add(hit.name);
            }
            foreach(var terrain in UnityEngine.Terrain.activeTerrains){
                var tc=terrain.GetComponent<UnityEngine.TerrainCollider>();UnityEngine.RaycastHit hit;
                if(tc.Raycast(new UnityEngine.Ray(p+UnityEngine.Vector3.up*5,UnityEngine.Vector3.down),out hit,10)){maxGroundError=UnityEngine.Mathf.Max(maxGroundError,UnityEngine.Mathf.Abs(p.y-hit.point.y-.065f));break;}
            }
        }
    }
}
var allowed=Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.HashSet<int>>(System.IO.File.ReadAllText("Library/RoadGateAlignment/allowed.json"));
var before=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/RoadGateAlignment/before.json"));
var current=all.ToDictionary(t=>t.GetInstanceID());var changed=new System.Collections.Generic.List<string>();int preserved=0;
foreach(var old in before["transforms"]){
    int id=(int)old["id"];if(allowed.Contains(id))continue;UnityEngine.Transform t;if(!current.TryGetValue(id,out t)){changed.Add((string)old["name"]+" removed");continue;}
    var p=old["p"].Values<float>().ToArray();var q=old["q"].Values<float>().ToArray();var s=old["s"].Values<float>().ToArray();
    bool same=UnityEngine.Vector3.Distance(t.localPosition,new UnityEngine.Vector3(p[0],p[1],p[2]))<.00001f&&UnityEngine.Mathf.Abs(t.localRotation.x-q[0])+UnityEngine.Mathf.Abs(t.localRotation.y-q[1])+UnityEngine.Mathf.Abs(t.localRotation.z-q[2])+UnityEngine.Mathf.Abs(t.localRotation.w-q[3])<.00001f&&UnityEngine.Vector3.Distance(t.localScale,new UnityEngine.Vector3(s[0],s[1],s[2]))<.00001f&&t.gameObject.activeSelf==(bool)old["active"];
    if(same)preserved++;else changed.Add(t.name);
}
var gateColliders=gate.GetComponents<UnityEngine.BoxCollider>().Where(c=>c.enabled).OrderBy(c=>c.center.x).ToArray();
float opening=gateColliders[1].center.x-gateColliders[1].size.x/2-(gateColliders[0].center.x+gateColliders[0].size.x/2);
var result=new{samples,obstacles=hits.ToArray(),maxGroundError,maxCrossSlope,minLaneMeshWidth=minW,maxLaneMeshWidth=maxW,gateOpening=opening,edgeMargin=(opening-14.724f)*.5f,preserved,unrelatedChanges=changed};
System.IO.File.WriteAllText("Library/RoadGateAlignment/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
