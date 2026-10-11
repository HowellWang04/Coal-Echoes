string dir="TerrainReferences/FactoryOnlyProtection";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).Single();
if(mm.IsGenerating()||EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("Editor unavailable");
if(System.IO.File.Exists(dir+"/application.json"))throw new System.Exception("Already applied");
var terrains=mm.GetComponentsInChildren<Terrain>(true);
var before=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/before.json"));
var check=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/prepared-verification.json"));
if((int)check["terrains"]!=terrains.Length || !(bool)check["seams_not_worsened"] || (float)check["lod_max_m"]>.001f)throw new System.Exception("Candidate verification incomplete");
System.Func<System.Array,byte[]> bytes=a=>{var b=new byte[a.Length*4];System.Buffer.BlockCopy(a,0,b,0,b.Length);return b;};
System.Func<byte[],string> hash=b=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b)).Replace("-", "").ToLowerInvariant();
foreach(var t in terrains){var td=t.terrainData;var row=before["terrains"].Single(r=>(string)r["name"]==td.name);if(hash(bytes(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution)))!=(string)row["heightHash"])throw new System.Exception("Baseline changed "+td.name);}
var source=mm.graph.generators.OfType<TerrainHeightSource>().Single();
foreach(var pair in new[]{(asset:source.main,lod:"main"),(asset:source.draft,lod:"draft")})
 if(!bytes(pair.asset.matrix.arr).SequenceEqual(System.IO.File.ReadAllBytes(dir+"/Before/"+pair.lod+"_height.f32")))throw new System.Exception("Source baseline changed");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,dir+"/Backup/Main Game Scene.pre-apply.live.unity",true))throw new System.Exception("Scene backup failed");
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Keep factory protection and smooth former boundary");
foreach(var asset in new[]{source.main,source.draft})Undo.RegisterCompleteObjectUndo(asset,"Update fixed DEM source");
var edits=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(dir+"/prepared.json")).Where(r=>(int)r["changed"]>0).ToArray();
bool auto=EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true);EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",false);
try{
 foreach(var pair in new[]{(asset:source.main,lod:"main"),(asset:source.draft,lod:"draft")}){
  var b=System.IO.File.ReadAllBytes(dir+"/Prepared/"+pair.lod+"_height.f32");if(b.Length!=pair.asset.matrix.arr.Length*4)throw new System.Exception("Matrix size changed");
  System.Buffer.BlockCopy(b,0,pair.asset.matrix.arr,0,b.Length);EditorUtility.SetDirty(pair.asset);
 }
 foreach(var row in edits){
  var t=terrains.Single(t=>t.terrainData.name==(string)row["name"]);var td=t.terrainData;int n=td.heightmapResolution;
  Undo.RegisterCompleteObjectUndo(td,"Apply factory-only terrain protection");
  var h=new float[n,n];var b=System.IO.File.ReadAllBytes(dir+"/Prepared/"+td.name+".height.f32");System.Buffer.BlockCopy(b,0,h,0,b.Length);
  td.SetHeights(0,0,h);t.Flush();EditorUtility.SetDirty(td);
  var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;
  detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));detail.data?.Clear(inSubs:true);EditorUtility.SetDirty(tile);
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
}finally{EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",auto);}
var report=new{terrainCount=edits.Length,graphNodes=mm.graph.generators.Length,locks=mm.locks.Length,generating=mm.IsGenerating()};
System.IO.File.WriteAllText(dir+"/application.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;

