string dir="TerrainReferences/Tile22CliffRepairPreview/Applied";
var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();
var graph=UnityEngine.Object.Instantiate(mm.graph);
if(ReferenceEquals(graph.generators[0],mm.graph.generators[0]))throw new System.Exception("Clone shares generators");
var asset=ScriptableObject.CreateInstance<Den.Tools.MatrixAsset>();asset.source=Den.Tools.MatrixAsset.Source.New;asset.matrix=new Den.Tools.Matrices.Matrix(new Den.Tools.CoordRect(0,0,3073,3073));
var bytes=System.IO.File.ReadAllBytes("TerrainReferences/Tile22CliffRepairPreview/Prepared/gradient_delta.f32");System.Buffer.BlockCopy(bytes,0,asset.matrix.arr,0,bytes.Length);
var node=graph.generators.OfType<TerrainHeightDelta>().Single(n=>n.delta.name=="SheepRange_WholeMap_800m_delta");
var previous=(MapMagic.Nodes.IOutlet<Den.Tools.Matrices.MatrixWorld>)graph.links[node];node.delta=asset;
foreach(var pair in graph.links){pair.Key.LinkedOutletId=pair.Value.Id;pair.Key.LinkedGenId=pair.Value.Gen.id;}
var records=new System.Collections.Generic.List<object>();
foreach(var t in Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/before.json"))["terrains"]){
string name=(string)t["name"];if(!new[]{"Tile_-1_-2_Main","Tile_-1_-2_Draft","Tile_0_-1_Main","Tile_1_2_Main","Tile_1_2_Draft","Tile_1_-1_Main","Tile_1_-1_Draft","Tile_2_2_Main","Tile_2_2_Draft","Tile_2_1_Main","Tile_2_1_Draft"}.Contains(name))continue;
int n=(int)t["res"];var data=new MapMagic.Products.TileData{area=new MapMagic.Terrains.Area(new Den.Tools.Vector2D((float)t["p"][0]-mm.transform.position.x,(float)t["p"][2]-mm.transform.position.z),new Den.Tools.Vector2D(1000,1000),n,n==129?mm.draftMargins:mm.tileMargins),globals=mm.globals,random=graph.random,isDraft=n==129};
graph.GenerateRecursive(node,data,graph.defaults,new MapMagic.Products.StopToken());
var old=data.ReadOutletProduct(previous);var updated=data.ReadOutletProduct(node);var deltaBytes=System.IO.File.ReadAllBytes(dir+"/Prepared/"+name+".total.f32");var expected=new float[n,n];System.Buffer.BlockCopy(deltaBytes,0,expected,0,deltaBytes.Length);float error=0;int unchanged=0;var rect=data.area.active.rect;
for(int z=0;z<n;z++)for(int x=0;x<n;x++){float a=old[x+rect.offset.x,z+rect.offset.z],b=updated[x+rect.offset.x,z+rect.offset.z];error=Mathf.Max(error,Mathf.Abs((b-a)-expected[z,x]));if(expected[z,x]==0&&a!=b)unchanged++;}
if(error>.000001f||unchanged!=0||updated.arr.Min()<0||updated.arr.Max()>1)throw new System.Exception("Graph validation failed "+name);
records.Add(new{name,error_m=error*1000,unchangedDifferences=unchanged,min=updated.arr.Min(),max=updated.arr.Max()});}
UnityEngine.Object.DestroyImmediate(graph);UnityEngine.Object.DestroyImmediate(asset);
System.IO.File.WriteAllText(dir+"/graph-dry-run.json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));return new{tested=records.Count};
