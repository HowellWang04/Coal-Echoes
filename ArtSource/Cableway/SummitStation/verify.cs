var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var station=all.Single(t=>t.name=="CablewaySummitStation");
var terminal=station.Find("CablewayReturnTerminal");
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/Cableway/SummitStation/Before/baseline.json"));
var geometry=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/Cableway/SummitStation/geometry.json"));
Vector3 V(Newtonsoft.Json.Linq.JToken p)=>new Vector3((float)p[0],(float)p[1],(float)p[2]);
string Path(Transform t)=>AnimationUtility.CalculateTransformPath(t,null);
var lookup=all.ToDictionary(t=>GlobalObjectId.GetGlobalObjectIdSlow(t).ToString(),t=>t);
var unexpected=new List<string>();var removed=new List<string>();
foreach(var row in baseline["transforms"]) {
    var path=(string)row["path"];
    if(path.StartsWith("In-scene Objects/索道群/塔架/CablewayTower_11")) {removed.Add(path);continue;}
    if(path=="In-scene Objects/索道群/塔架/CablewayTower_10/CoalBucket") continue;
    if(!lookup.TryGetValue((string)row["id"],out var t)) {unexpected.Add("Missing "+path);continue;}
    if(Vector3.Distance(t.position,V(row["pos"]))>.0001f || Quaternion.Angle(t.rotation,Quaternion.Euler(V(row["rot"])))>.05f || Vector3.Distance(t.lossyScale,V(row["scale"]))>.0001f || t.gameObject.activeSelf!=(bool)row["active"] || Path(t)!=path) unexpected.Add("Changed "+path);
}
var terrainChanges=new List<string>();
foreach(var row in baseline["terrains"]) {
    var hash=BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(System.IO.File.ReadAllBytes((string)row["path"]))).Replace("-","");
    if(hash!=(string)row["hash"])terrainChanges.Add((string)row["path"]);
}
var badAssets=new List<string>();
foreach(var r in station.GetComponentsInChildren<Renderer>(true)) {
    if(r.sharedMaterials.Any(m=>m==null || m.shader==null || !m.shader.name.StartsWith("HDRP/"))) badAssets.Add(Path(r.transform));
    var mf=r.GetComponent<MeshFilter>();if(mf!=null && (mf.sharedMesh==null || mf.sharedMesh.vertexCount==0))badAssets.Add(Path(r.transform));
}
var buckets=all.Select(t=>t.GetComponent<MeshFilter>()).Where(m=>m!=null && m.sharedMesh!=null && AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("/CablewayCoalBucket.fbx")).ToArray();
var lines=new List<Vector3[]>();var cableRows=new List<object>();
var connection=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/Cableway/SummitStation/cable_connection.json"));
for(int line=0;line<2;line++) {
    var old=geometry["cables"][line];var asset=(string)old["asset"];
    var mf=all.Select(t=>t.GetComponent<MeshFilter>()).Single(m=>m!=null && AssetDatabase.GetAssetPath(m.sharedMesh)==asset);
    var vertices=mf.sharedMesh.vertices.Select(v=>mf.transform.TransformPoint(v)).ToArray();
    int prefix=(int)connection["meshes"][line]["preservedRings"]*8;
    float maxDelta=0;for(int i=0;i<prefix;i++)maxDelta=Mathf.Max(maxDelta,Vector3.Distance(vertices[i],V(old["vertices"][i])));
    var centers=new List<Vector3>();for(int k=0;k<vertices.Length;k+=8){var p=Vector3.zero;for(int j=0;j<8;j++)p+=vertices[k+j];centers.Add(p/8);}
    lines.Add(centers.ToArray());
    var rollerErrors=new List<float>();
    foreach(float z in new[]{-1.2f,0f,1.2f}){var target=terminal.TransformPoint(new Vector3(line==0?-2:2,4.85f,z));rollerErrors.Add(centers.Min(p=>Vector3.Distance(p,target)));}
    cableRows.Add(new {asset,preservedVertexCount=prefix,maximumPreservedVertexDisplacement=maxDelta,rollerErrors,vertexCount=vertices.Length});
}
float DistanceToLines(Vector3 p) {
    float min=float.PositiveInfinity;
    foreach(var line in lines) for(int i=1;i<line.Length;i++) {var a=line[i-1];var d=line[i]-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude);min=Mathf.Min(min,Vector3.Distance(p,a+d*t));}
    return min;
}
var bucketRows=buckets.Where(m=>m.name=="CoalBucket_Approach" || m.transform.parent.name=="CablewayTower_10").Select(m=>new {name=Path(m.transform),ropeOffset=DistanceToLines(m.transform.position)}).ToArray();
var originalBucketCount=baseline["transforms"].Count(row=>System.Text.RegularExpressions.Regex.IsMatch((string)row["path"],@"/CoalBucket[^/]*$"));
var missingScripts=station.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
var result=new {station=Path(station),oldTowerAbsent=!all.Any(t=>t.name=="CablewayTower_11"),towerCount=all.Count(t=>System.Text.RegularExpressions.Regex.IsMatch(t.name,@"^CablewayTower_\d+$")),originalBucketCount,bucketCount=buckets.Length,stationRenderers=station.GetComponentsInChildren<Renderer>(true).Length,missingScripts,unexpectedSceneChanges=unexpected,terrainAssetsChecked=baseline["terrains"].Count(),terrainChanges,badAssets,cables=cableRows,summitJoinDistance=Vector3.Distance(lines[0].Last(),lines[1].Last()),foothillJoinDistance=Vector3.Distance(lines[0].First(),lines[1].First()),buckets=bucketRows,sceneSaved=!scene.isDirty,playModeRun=false};
System.IO.File.WriteAllText("ArtSource/Cableway/SummitStation/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
