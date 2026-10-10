string dir="TerrainReferences/NativeDEMOnly";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).First();
if(mm.IsGenerating()||EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("Editor unavailable");
if(System.IO.File.Exists(dir+"/application.json"))throw new System.Exception("Already applied");
var terrains=mm.GetComponentsInChildren<Terrain>(true);
var before=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/before.json"));
System.Func<System.Array,byte[]> bytes=a=>{var b=new byte[a.Length*4];System.Buffer.BlockCopy(a,0,b,0,b.Length);return b;};
System.Func<byte[],string> hash=b=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b)).Replace("-", "").ToLowerInvariant();
foreach(var t in terrains){var td=t.terrainData;var row=before["terrains"].Single(r=>(string)r["name"]==td.name);if(hash(bytes(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution)))!=(string)row["heightHash"])throw new System.Exception("Baseline changed "+td.name);}
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,dir+"/Backup/Main Game Scene.pre-apply.live.unity",true))throw new System.Exception("Latest scene backup failed");
var old=mm.graph.generators.OfType<TerrainHeightDelta>().Single(n=>n.delta.name=="SheepRange_WholeMap_800m_delta");
var consumers=mm.graph.links.Where(p=>ReferenceEquals(p.Value,old)).Select(p=>p.Key).ToArray();
var node=new TerrainHeightSource{id=Den.Tools.Id.Generate(),main=AssetDatabase.LoadAssetAtPath<Den.Tools.MatrixAsset>("Assets/TerrainData/SheepRangeDEM/SheepRange_Native_Main.asset"),draft=AssetDatabase.LoadAssetAtPath<Den.Tools.MatrixAsset>("Assets/TerrainData/SheepRangeDEM/SheepRange_Native_Draft.asset"),offset=new Vector2(-2031.25f,-3031.25f),size=new Vector2(6062.5f,6062.5f),guiPosition=old.guiPosition};
if(node.main==null||node.draft==null||consumers.Length!=2)throw new System.Exception("Missing sources or unexpected graph");
var dry=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(dir+"/graph-dry-run.json"));if(dry.Count!=58)throw new System.Exception("Incomplete dry run");
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Replace procedural terrain with native DEM");
Undo.RegisterCompleteObjectUndo(mm,"Update terrain height range");Undo.RegisterCompleteObjectUndo(mm.graph,"Use native DEM height source");
foreach(var t in terrains)Undo.RegisterCompleteObjectUndo(t.terrainData,"Apply native DEM");
bool auto=EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true);EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",false);
try{
    string[] remove={"Noise200","Curve200","Erosion200","Levels200","Import200","Mask200","TerrainHeightDelta"};
    foreach(var gen in mm.graph.generators.Where(g=>remove.Contains(g.GetType().Name)).ToArray())mm.graph.Remove(gen);
    mm.graph.Add(node);foreach(var inlet in consumers)mm.graph.Link(node,inlet);
    foreach(var pair in mm.graph.links){pair.Key.LinkedOutletId=pair.Value.Id;pair.Key.LinkedGenId=pair.Value.Gen.id;}
    mm.globals.height=1100;mm.tiles.generateInfinite=false;EditorUtility.SetDirty(mm);EditorUtility.SetDirty(mm.graph);
    foreach(var t in terrains){
        var td=t.terrainData;int n=td.heightmapResolution;
        var h=new float[n,n];var b=System.IO.File.ReadAllBytes(dir+"/Prepared/"+td.name+".height.f32");System.Buffer.BlockCopy(b,0,h,0,b.Length);
        td.size=new Vector3(1000,1100,1000);td.SetHeights(0,0,h);t.Flush();EditorUtility.SetDirty(td);
        var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;
        detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));
        var alpha=td.GetAlphamaps(0,0,td.alphamapResolution,td.alphamapResolution);
        if(detail.edges.splatEdges!=null)for(int l=0;l<detail.edges.splatEdges.Length;l++)detail.edges.splatEdges[l].ReadFloats3D(alpha,l);
        detail.data?.Clear(inSubs:true);EditorUtility.SetDirty(tile);
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
}finally{EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",auto);}
var report=new{nodeId=node.id.ToString(),terrainCount=terrains.Length,graphNodes=mm.graph.generators.Length,proceduralNodes=0,heightRange=mm.globals.height};
System.IO.File.WriteAllText(dir+"/application.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
