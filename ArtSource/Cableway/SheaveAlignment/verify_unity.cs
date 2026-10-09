var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(!scene.isLoaded || EditorApplication.isPlaying)throw new Exception("Main scene must be loaded in edit mode.");
var expected=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/Cableway/SheaveAlignment/after.json"));
var names=new[]{"CablewayTower_Metric15","CablewayEntryTower"};
var rows=new List<object>();
for(int i=0;i<names.Length;i++) {
    var path="Assets/Art Resources/Environment/Cableway/CablewayTower_Metric15/"+names[i]+".fbx";
    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var mesh=model.GetComponentInChildren<MeshFilter>().sharedMesh;
    var vertices=mesh.vertices;float error=0;
    foreach(var sheave in expected[i]["sheaves"])foreach(var p in sheave["unity_vertices"]) {
        var point=new Vector3((float)p[0],(float)p[1],(float)p[2]);error=Mathf.Max(error,vertices.Min(v=>Vector3.Distance(v,point)));
    }
    if(error>.001f)throw new Exception("Imported sheave geometry does not match repaired source " + names[i]+" error "+error);
    rows.Add(new {asset=path,guid=AssetDatabase.AssetPathToGUID(path),vertices=mesh.vertexCount,triangles=mesh.triangles.Length/3,maximumWheelVertexError=error});
}
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var towers=all.Where(t=>System.Text.RegularExpressions.Regex.IsMatch(t.name,@"^CablewayTower_\d+$")).ToArray();
var bad=new List<string>();
foreach(var tower in towers) {
    var body=tower.Find("CablewayTower");var mf=body.GetComponent<MeshFilter>();var renderer=body.GetComponent<Renderer>();
    if(mf.sharedMesh==null || renderer.sharedMaterials.Any(m=>m==null || m.shader==null || !m.shader.name.StartsWith("HDRP/")))bad.Add(tower.name);
}
var target=towers.Single(t=>t.name=="CablewayTower_02");
var view=SceneView.lastActiveSceneView;
Selection.activeGameObject=target.gameObject;
view.LookAtDirect(target.position+Vector3.up*14.7f,Quaternion.LookRotation((target.forward*.7f-target.right+Vector3.down*.22f).normalized),2.7f);
view.camera.ResetAspect();view.camera.ResetProjectionMatrix();view.Repaint();
var report=new {assets=rows,towerCount=towers.Length,badTowerAssets=bad,mainSceneDirty=scene.isDirty,playModeRun=false};
System.IO.File.WriteAllText("ArtSource/Cableway/SheaveAlignment/unity_verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
