string dir="TerrainReferences/WholeMapGradient800";
string assetPath="Assets/TerrainData/SheepRangeDEM/SheepRange_WholeMap_800m_delta.asset";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).First();
if(mm.IsGenerating()||EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("Editor unavailable");
if(AssetDatabase.LoadAssetAtPath<Den.Tools.MatrixAsset>(assetPath)!=null)throw new System.Exception("Already applied");
var terrains=mm.GetComponentsInChildren<Terrain>(true);
var before=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/before.json"));
System.Func<System.Array,byte[]> bytes=a=>{var b=new byte[a.Length*4];System.Buffer.BlockCopy(a,0,b,0,b.Length);return b;};
System.Func<byte[],string> hash=b=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b)).Replace("-", "").ToLowerInvariant();
foreach(var item in before["terrains"]){var td=terrains.Single(t=>t.terrainData.name==(string)item["name"]).terrainData;if(hash(bytes(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution)))!=(string)item["heightHash"])throw new System.Exception("Terrain changed "+td.name);}
var edits=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(dir+"/prepared.json"));
var previous=(MapMagic.Nodes.IOutlet<Den.Tools.Matrices.MatrixWorld>)mm.graph.generators.Single(g=>g.id==5672710341184716803UL);
var consumers=mm.graph.links.Where(p=>ReferenceEquals(p.Value,previous)).Select(p=>p.Key).ToArray();
if(consumers.Length!=2)throw new System.Exception("Unexpected graph links");
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Apply outer transition 800m gradient terrain");
Undo.RegisterCompleteObjectUndo(mm.graph,"Apply outer transition terrain graph");
foreach(var item in edits){var terrain=terrains.Single(t=>t.terrainData.name==(string)item["name"]);Undo.RegisterCompleteObjectUndo(terrain.terrainData,"Apply outer transition terrain");Undo.RegisterCompleteObjectUndo(terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>(),"Update terrain edges");}
var asset=ScriptableObject.CreateInstance<Den.Tools.MatrixAsset>();asset.name="SheepRange_WholeMap_800m_delta";asset.source=Den.Tools.MatrixAsset.Source.New;asset.matrix=new Den.Tools.Matrices.Matrix(new Den.Tools.CoordRect(0,0,3073,3073));
var deltaBytes=System.IO.File.ReadAllBytes(dir+"/Prepared/gradient_delta.f32");System.Buffer.BlockCopy(deltaBytes,0,asset.matrix.arr,0,deltaBytes.Length);AssetDatabase.CreateAsset(asset,assetPath);
var node=(TerrainHeightDelta)MapMagic.Nodes.Generator.Create(typeof(TerrainHeightDelta));node.delta=asset;node.offset=new Vector2(-4996.00049f-mm.transform.position.x,-3986-mm.transform.position.z);node.size=new Vector2(6144,6144);node.guiPosition=previous.Gen.guiPosition+new Vector2(240,150);
mm.graph.Add(node);mm.graph.Link(previous,node);foreach(var inlet in consumers)mm.graph.Link(node,inlet);EditorUtility.SetDirty(mm.graph);
bool auto=EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true);EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",false);
var reports=new System.Collections.Generic.List<object>();
try{
foreach(var item in edits){string name=(string)item["name"];var terrain=terrains.Single(t=>t.terrainData.name==name);var td=terrain.terrainData;int n=td.heightmapResolution;var all=new float[n,n];var buf=System.IO.File.ReadAllBytes(dir+"/Prepared/"+name+".height.f32");System.Buffer.BlockCopy(buf,0,all,0,buf.Length);var r=item["height_rect"];int x0=(int)r[0],z0=(int)r[1],w=(int)r[2]-x0,h=(int)r[3]-z0;var patch=new float[h,w];for(int z=0;z<h;z++)for(int x=0;x<w;x++)patch[z,x]=all[z+z0,x+x0];td.SetHeights(x0,z0,patch);terrain.Flush();EditorUtility.SetDirty(td);var tile=terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==terrain?tile.main:tile.draft;detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));EditorUtility.SetDirty(tile);reports.Add(new{name,changedSamples=(int)item["changed"]});}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
}finally{EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",auto);}
System.IO.File.WriteAllText(dir+"/application.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{nodeId=node.id,assetPath,terrains=reports},Newtonsoft.Json.Formatting.Indented));return reports;
