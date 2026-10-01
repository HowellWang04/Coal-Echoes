var root="ArtSource/Cableway/FoothillStationPlacement/";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var station=all.Single(t=>t.name=="CablewayFoothillStation");var survey=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(root+"survey.json"));
UnityEngine.Vector3 Vec(Newtonsoft.Json.Linq.JToken v)=>new UnityEngine.Vector3((float)v[0],(float)v[1],(float)v[2]);
var transformChecks=new System.Collections.Generic.List<object>();
foreach(var row in survey["towers"].Concat(survey["routes"]).Concat(survey["factory"])){
 var tr=all.Single(t=>UnityEditor.AnimationUtility.CalculateTransformPath(t,null)==(string)row["path"]);
 transformChecks.Add(new{name=tr.name,positionDelta=UnityEngine.Vector3.Distance(tr.position,Vec(row["pos"])),rotationDelta=UnityEngine.Quaternion.Angle(tr.rotation,UnityEngine.Quaternion.Euler(Vec(row["rotation"]))),scaleDelta=UnityEngine.Vector3.Distance(tr.lossyScale,Vec(row["scale"]))});
}
var beforePlacement=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(root+"terrain_after.json"));
string Sha(byte[] b)=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b)).Replace("-","");
var terrainChecks=new System.Collections.Generic.List<object>();
foreach(var r in beforePlacement){var td=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainData>((string)r["path"]);int n=td.heightmapResolution;var h=td.GetHeights(0,0,n,n);var bytes=new byte[n*n*4];System.Buffer.BlockCopy(h,0,bytes,0,bytes.Length);terrainChecks.Add(new{path=(string)r["path"],matchesRestoredSnapshot=Sha(bytes)==(string)r["sha256"]});}
var renderers=station.GetComponentsInChildren<UnityEngine.Renderer>();
var assetChecks=renderers.Select(r=>new{name=r.name,hasMesh=r.GetComponent<UnityEngine.MeshFilter>()?.sharedMesh!=null,materials=r.sharedMaterials.Select(m=>new{name=m?.name,shader=m?.shader?.name,valid=m!=null && m.shader!=null && !m.shader.name.Contains("Error")}).ToArray()}).ToArray();
if(scene.isDirty && !UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed");
var result=new{scene=scene.path,saved=!scene.isDirty,activeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,stationRootCount=all.Count(t=>t.name=="CablewayFoothillStation"),stationChildren=station.childCount,terrainChecks,transformChecks,assetChecks,playModeTested=false,collidersAdded=false,cablesAdded=false};
System.IO.File.WriteAllText(root+"verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
var captureDir="Assets/ArtSource/Cableway/FoothillStationPlacement/Previews";
foreach(var name in new[]{"FactoryBefore.png","FactoryRestored.png","StationPlaced.png","StationFinal.png"}){var p=captureDir+"/"+name;if(System.IO.File.Exists(p))System.IO.File.Copy(p,root+"Previews/"+name,true);}
return new{sceneSaved=!scene.isDirty,stationChildren=station.childCount,renderers=renderers.Length,verificationWritten=true};
