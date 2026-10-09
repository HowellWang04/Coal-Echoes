var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if (!scene.isLoaded || EditorApplication.isPlaying) throw new Exception("Main scene must be in edit mode.");
var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
var station = all.Single(t => t.name == "CablewaySummitStation");
var terminal = station.Find("CablewayReturnTerminal");
var oldTower = all.Single(t => t.name == "CablewayTower_11");
var previous = all.Single(t => t.name == "CablewayTower_10");
var cableFilters = all.Select(t=>t.GetComponent<MeshFilter>()).Where(m=>m!=null && m.sharedMesh!=null && AssetDatabase.GetAssetPath(m.sharedMesh).Contains("/Cable_")).OrderBy(m=>AssetDatabase.GetAssetPath(m.sharedMesh)).ToArray();
if(cableFilters.Length!=2) throw new Exception("Expected two cable meshes.");
var terrains=Terrain.activeTerrains.Where(t=>AssetDatabase.GetAssetPath(t.terrainData).EndsWith("_Main.asset")).ToArray();
float Ground(Vector3 p) {
    var t=terrains.First(t=>p.x>=t.transform.position.x && p.x<=t.transform.position.x+t.terrainData.size.x && p.z>=t.transform.position.z && p.z<=t.transform.position.z+t.terrainData.size.z);
    return t.SampleHeight(p)+t.transform.position.y;
}
float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
var newPaths=new List<Vector3[]>();
var keepRings=new List<int>();
var spanStarts=new List<Vector3>();
var spanEnds=new List<Vector3>();
var sags=new List<float>();
var minGroundClearance=float.PositiveInfinity;
for(int line=0;line<2;line++) {
    var mf=cableFilters[line];var vertices=mf.sharedMesh.vertices;
    var centers=new List<Vector3>();
    for(int k=0;k<vertices.Length;k+=8) {var p=Vector3.zero;for(int j=0;j<8;j++)p+=mf.transform.TransformPoint(vertices[k+j]);centers.Add(p/8);}
    var support=previous.TransformPoint(new Vector3(line==0?2f:-2f,15.02f,0));
    int last=Enumerable.Range(0,centers.Count).OrderBy(k=>(centers[k]-support).sqrMagnitude).First();
    if(Vector3.Distance(centers[last],support)>.03f) throw new Exception("Tower 10 rope anchor mismatch.");
    keepRings.Add(last+1);
    var points=centers.Take(last+1).ToList();
    var start=points.Last();var end=terminal.TransformPoint(new Vector3(line==0?-2f:2f,4.85f,-1.2f));
    float horizontal=Vector3.ProjectOnPlane(end-start,Vector3.up).magnitude;
    float sag=Mathf.Clamp(horizontal*.022f,.3f,3.5f);
    spanStarts.Add(start);spanEnds.Add(end);sags.Add(sag);
    int steps=Mathf.CeilToInt(Vector3.Distance(start,end)/.5f);
    for(int k=1;k<=steps;k++) {
        float u=k/(float)steps;var p=Vector3.Lerp(start,end,u)-Vector3.up*(4*sag*u*(1-u));
        minGroundClearance=Mathf.Min(minGroundClearance,p.y-Ground(p));points.Add(p);
    }
    // Three station rollers followed by the straight rope tangent to the return wheel.
    for(int k=1;k<=46;k++) points.Add(terminal.TransformPoint(new Vector3(line==0?-2f:2f,4.85f,Mathf.Lerp(-1.2f,8f,k/46f))));
    // One mesh carries the full outer semicircle; both lines meet at the other tangent.
    if(line==0) for(int k=1;k<=48;k++) {
        float angle=Mathf.PI*k/48;points.Add(terminal.TransformPoint(new Vector3(-2*Mathf.Cos(angle),4.85f,8+2*Mathf.Sin(angle))));
    }
    newPaths.Add(points.ToArray());
}
if(minGroundClearance<3.0f) throw new Exception("Insufficient last-span bucket clearance " + minGroundClearance);
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Connect summit station cables and replace Tower 11");
var meshReport=new List<object>();
for(int line=0;line<2;line++) {
    var mf=cableFilters[line];var mesh=mf.sharedMesh;var points=newPaths[line];int keep=keepRings[line]*8;
    var oldVertices=mesh.vertices;var vertices=oldVertices.Take(keep).ToList();var normals=mesh.normals.Take(keep).ToList();
    var triangles=mesh.triangles.Take((keepRings[line]-1)*8*6).ToList();
    for(int k=keepRings[line];k<points.Length;k++) {
        var tangent=(points[Mathf.Min(k+1,points.Length-1)]-points[k-1]).normalized;
        var side=Vector3.Cross(tangent,Vector3.up).normalized;var up=Vector3.Cross(side,tangent).normalized;
        for(int j=0;j<8;j++) {
            float angle=j*Mathf.PI/4;var normal=side*Mathf.Cos(angle)+up*Mathf.Sin(angle);
            vertices.Add(mf.transform.InverseTransformPoint(points[k]+normal*.02f));normals.Add(mf.transform.InverseTransformDirection(normal));
        }
        for(int j=0;j<8;j++) {int a=(k-1)*8+j,b=k*8+j,c=(k-1)*8+(j+1)%8,d=k*8+(j+1)%8;triangles.AddRange(new[]{a,b,c,c,b,d});}
    }
    Undo.RegisterCompleteObjectUndo(mesh,"Connect summit station cable");
    mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
    EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
    if(!mesh.vertices.Take(keep).SequenceEqual(oldVertices.Take(keep))) throw new Exception("Existing cable prefix changed.");
    meshReport.Add(new {asset=AssetDatabase.GetAssetPath(mesh),preservedRings=keepRings[line],rings=points.Length,unchangedPrefix=true,first=V(points.First()),last=V(points.Last())});
}
Vector3 OnSpan(int line,float u)=>Vector3.Lerp(spanStarts[line],spanEnds[line],u)-Vector3.up*(4*sags[line]*u*(1-u));
var bucket10=previous.Find("CoalBucket");
var oldBucket=oldTower.Cast<Transform>().Single(t=>t.name.StartsWith("CoalBucket"));
var bucket11=UnityEngine.Object.Instantiate(oldBucket.gameObject,station).transform;
Undo.RegisterCreatedObjectUndo(bucket11.gameObject,"Preserve summit bucket as independent object");
Undo.RecordObject(bucket10,"Align bucket to summit approach");
float u10=8f/Vector3.Distance(spanStarts[0],spanEnds[0]);
bucket10.position=OnSpan(0,u10);bucket10.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(spanEnds[0]-spanStarts[0],Vector3.up));
PrefabUtility.RecordPrefabInstancePropertyModifications(bucket10);
bucket11.name="CoalBucket_Approach";
bucket11.position=OnSpan(0,1f-u10);bucket11.rotation=bucket10.rotation;
PrefabUtility.RecordPrefabInstancePropertyModifications(bucket11);
Undo.DestroyObjectImmediate(oldTower.gameObject);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
var report=new {removedTower="CablewayTower_11",remainingTowers=all.Count(t=>t!=null && t.name.StartsWith("CablewayTower_")),minimumLastSpanRopeGroundClearance=minGroundClearance,minimumLastSpanBucketGroundClearance=minGroundClearance-2.617f, summitJoinDistance=Vector3.Distance(newPaths[0].Last(),newPaths[1].Last()),meshes=meshReport,bucket10=V(bucket10.position),bucket11=V(bucket11.position)};
System.IO.File.WriteAllText("ArtSource/Cableway/SummitStation/cable_connection.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
System.IO.File.WriteAllText("ArtSource/Cableway/SummitStation/cable_paths.json",Newtonsoft.Json.JsonConvert.SerializeObject(newPaths.Select(p=>p.Select(V).ToArray()).ToArray()));
return report;
