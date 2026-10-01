var root="ArtSource/Cableway/FoothillStationPlacement/";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).Single();
if(UnityEditor.EditorApplication.isPlaying || mm.IsGenerating()) throw new System.Exception("Editor must be idle");
if(!System.IO.File.Exists(root+"Before/MainGameScene.unity")) throw new System.Exception("Missing scene backup");
var plan=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(root+"restoration_plan.json"));
string Sha(byte[] b)=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b)).Replace("-","").ToLowerInvariant();
var rawPath="Assets/TerrainData/SheepRangeDEM/SheepRange_Height.raw";
if(Sha(System.IO.File.ReadAllBytes(rawPath))!=(string)plan["raw_before_sha256"]) throw new System.Exception("Input changed since backup");
if(Sha(System.IO.File.ReadAllBytes(root+"RestoredFactory_Height.raw"))!=(string)plan["raw_after_sha256"]) throw new System.Exception("Prepared input hash mismatch");
var changes=new System.Collections.Generic.Dictionary<UnityEngine.TerrainData,float[,]>();
foreach(var patch in plan["patches"]){
 var td=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainData>((string)patch["path"]);int n=td.heightmapResolution;
 var h=td.GetHeights(0,0,n,n);var bytes=new byte[n*n*4];System.Buffer.BlockCopy(h,0,bytes,0,bytes.Length);
 if(Sha(bytes)!=((string)patch["sha256"]).ToLowerInvariant()) throw new System.Exception("Terrain changed since backup "+td.name);
 var after=System.IO.File.ReadAllBytes(root+(string)patch["patch"]);if(after.Length!=bytes.Length)throw new System.Exception("Patch size mismatch");
 System.Buffer.BlockCopy(after,0,h,0,after.Length);changes.Add(td,h);
}
UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Restore original factory terrain");
foreach(var pair in changes){UnityEditor.Undo.RegisterCompleteObjectUndo(pair.Key,"Restore original factory terrain");pair.Key.SetHeights(0,0,pair.Value);UnityEditor.EditorUtility.SetDirty(pair.Key);UnityEditor.AssetDatabase.SaveAssetIfDirty(pair.Key);}
foreach(var tile in mm.tiles.All()){
 bool touched=false;
 foreach(var detail in new[]{tile.main,tile.draft})if(detail!=null && detail.terrain!=null && changes.ContainsKey(detail.terrain.terrainData)){
  if(!touched)UnityEditor.Undo.RecordObject(tile,"Update restored terrain edges");
  var td=detail.terrain.terrainData;detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution));detail.terrain.Flush();touched=true;
 }
 if(touched)UnityEditor.EditorUtility.SetDirty(tile);
}
System.IO.File.Copy(root+"RestoredFactory_Height.raw",rawPath,true);
var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<Den.Tools.MatrixAsset>("Assets/TerrainData/SheepRangeDEM/SheepRange_Height.asset");
Den.Tools.MatrixAsset.ImportRaw(ref asset.matrix,rawPath);UnityEditor.EditorUtility.SetDirty(asset);UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
foreach(var node in mm.graph.GeneratorsOfType<MapMagic.Nodes.MatrixGenerators.Import200>())if(node.matrixAsset==asset)node.version++;
UnityEditor.EditorUtility.SetDirty(mm.graph);UnityEditor.AssetDatabase.SaveAssetIfDirty(mm.graph);
UnityEditor.Undo.CollapseUndoOperations(group);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed");
System.IO.File.WriteAllText(root+"restoration_applied.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{terrainAssets=changes.Count,rawHash=Sha(System.IO.File.ReadAllBytes(rawPath)),sceneSaved=true,generationRequested=false},Newtonsoft.Json.Formatting.Indented));
return new{terrainAssets=changes.Count,sceneSaved=true,generationRequested=false};
