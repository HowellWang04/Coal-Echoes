string dir="TerrainReferences/Circle1GradientPreview/Applied800";
string assetPath="Assets/TerrainData/SheepRangeDEM/SheepRange_Circle1_800m_delta.asset";
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
var previous=(MapMagic.Nodes.IOutlet<Den.Tools.Matrices.MatrixWorld>)mm.graph.generators.Single(g=>g.id==12165584888434524163UL);
var consumers=mm.graph.links.Where(p=>ReferenceEquals(p.Value,previous)).Select(p=>p.Key).ToArray();
if(consumers.Length!=2)throw new System.Exception("Unexpected graph links");
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Apply circle 1 800m gradient terrain");
Undo.RegisterCompleteObjectUndo(mm.graph,"Apply circle 1 terrain graph");
foreach(var item in edits){var terrain=terrains.Single(t=>t.terrainData.name==(string)item["name"]);Undo.RegisterCompleteObjectUndo(terrain.terrainData,"Apply circle 1 terrain");Undo.RegisterCompleteObjectUndo(terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>(),"Update terrain edges");}
var asset=ScriptableObject.CreateInstance<Den.Tools.MatrixAsset>();asset.name="SheepRange_Circle1_800m_delta";asset.source=Den.Tools.MatrixAsset.Source.New;asset.matrix=new Den.Tools.Matrices.Matrix(new Den.Tools.CoordRect(0,0,1025,1025));
var deltaBytes=System.IO.File.ReadAllBytes(dir+"/Prepared/gradient_delta.f32");System.Buffer.BlockCopy(deltaBytes,0,asset.matrix.arr,0,deltaBytes.Length);AssetDatabase.CreateAsset(asset,assetPath);
var node=(TerrainHeightDelta)MapMagic.Nodes.Generator.Create(typeof(TerrainHeightDelta));node.delta=asset;node.offset=new Vector2(-4148-mm.transform.position.x,-3834-mm.transform.position.z);node.size=new Vector2(2048,2048);node.guiPosition=previous.Gen.guiPosition+new Vector2(240,150);
mm.graph.Add(node);mm.graph.Link(previous,node);foreach(var inlet in consumers)mm.graph.Link(node,inlet);EditorUtility.SetDirty(mm.graph);
bool auto=EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true);EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",false);
var reports=new System.Collections.Generic.List<object>();
try{
foreach(var item in edits){string name=(string)item["name"];var terrain=terrains.Single(t=>t.terrainData.name==name);var td=terrain.terrainData;int n=td.heightmapResolution;var all=new float[n,n];var buf=System.IO.File.ReadAllBytes(dir+"/Prepared/"+name+".height.f32");System.Buffer.BlockCopy(buf,0,all,0,buf.Length);var r=item["height_rect"];int x0=(int)r[0],z0=(int)r[1],w=(int)r[2]-x0,h=(int)r[3]-z0;var patch=new float[h,w];for(int z=0;z<h;z++)for(int x=0;x<w;x++)patch[z,x]=all[z+z0,x+x0];td.SetHeights(x0,z0,patch);terrain.Flush();EditorUtility.SetDirty(td);var tile=terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==terrain?tile.main:tile.draft;detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));EditorUtility.SetDirty(tile);}
foreach(var item in edits){string name=(string)item["name"];var terrain=terrains.Single(t=>t.terrainData.name==name);var td=terrain.terrainData;int a=td.alphamapResolution;var old=new float[a,a,td.alphamapLayers];var oldBytes=System.IO.File.ReadAllBytes(dir+"/Before/"+name+".alpha.f32");System.Buffer.BlockCopy(oldBytes,0,old,0,oldBytes.Length);var mask=System.IO.File.ReadAllBytes(dir+"/Prepared/"+name+".paint.u8");int x0=a,z0=a,x1=0,z1=0;for(int z=0;z<a;z++)for(int x=0;x<a;x++)if(mask[z*a+x]!=0){x0=Mathf.Min(x0,x);z0=Mathf.Min(z0,z);x1=Mathf.Max(x1,x+1);z1=Mathf.Max(z1,z+1);}var pos=terrain.transform.position;var region=Rect.MinMaxRect(pos.x+x0*td.size.x/a,pos.z+z0*td.size.z/a,pos.x+x1*td.size.x/a,pos.z+z1*td.size.z/a);int painted=MapMagicTerrainTextureSync.RefreshTerrain(terrain,region,false);var alpha=td.GetAlphamaps(0,0,a,a);for(int z=0;z<a;z++)for(int x=0;x<a;x++)if(mask[z*a+x]==0)for(int l=0;l<td.alphamapLayers;l++)alpha[z,x,l]=old[z,x,l];td.SetAlphamaps(0,0,alpha);terrain.Flush();EditorUtility.SetDirty(td);var tile=terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==terrain?tile.main:tile.draft;var actual=td.GetAlphamaps(0,0,a,a);if(detail.edges.splatEdges!=null)for(int l=0;l<detail.edges.splatEdges.Length;l++)detail.edges.splatEdges[l].ReadFloats3D(actual,l);EditorUtility.SetDirty(tile);reports.Add(new{name,painted});}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
}finally{EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",auto);}
System.IO.File.WriteAllText(dir+"/application.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{nodeId=node.id,assetPath,terrains=reports},Newtonsoft.Json.Formatting.Indented));return reports;
