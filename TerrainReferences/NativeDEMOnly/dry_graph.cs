string dir="TerrainReferences/NativeDEMOnly";
var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();
var graph=UnityEngine.Object.Instantiate(mm.graph);
var old=graph.generators.OfType<TerrainHeightDelta>().Single(n=>n.delta.name=="SheepRange_WholeMap_800m_delta");
var consumers=graph.links.Where(p=>ReferenceEquals(p.Value,old)).Select(p=>p.Key).ToArray();
var node=new TerrainHeightSource{id=Den.Tools.Id.Generate(),main=AssetDatabase.LoadAssetAtPath<Den.Tools.MatrixAsset>("Assets/TerrainData/SheepRangeDEM/SheepRange_Native_Main.asset"),draft=AssetDatabase.LoadAssetAtPath<Den.Tools.MatrixAsset>("Assets/TerrainData/SheepRangeDEM/SheepRange_Native_Draft.asset"),offset=new Vector2(-2031.25f,-3031.25f),size=new Vector2(6062.5f,6062.5f)};
string[] remove={"Noise200","Curve200","Erosion200","Levels200","Import200","Mask200","TerrainHeightDelta"};
foreach(var gen in graph.generators.Where(g=>remove.Contains(g.GetType().Name)).ToArray())graph.Remove(gen);
graph.Add(node);foreach(var inlet in consumers)graph.Link(node,inlet);
foreach(var pair in graph.links){pair.Key.LinkedOutletId=pair.Value.Id;pair.Key.LinkedGenId=pair.Value.Gen.id;}
if(graph.generators.Length!=7||consumers.Length!=2)throw new System.Exception("Unexpected graph structure");
var globals=mm.globals.Clone();globals.height=1100;
var records=new System.Collections.Generic.List<object>();
foreach(var t in Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/before.json"))["terrains"]){
    string name=(string)t["name"];int n=(int)t["res"];
    // Use exact tile coordinates, as MapMagic does when preparing a real tile.
    var parts=name.Split('_');var coord=new Den.Tools.Coord(int.Parse(parts[1]),int.Parse(parts[2]));
    var data=new MapMagic.Products.TileData{area=new MapMagic.Terrains.Area(coord,n,n==129?mm.draftMargins:mm.tileMargins,mm.tileSize),globals=globals,random=graph.random,isDraft=n==129};
    graph.GenerateRecursive(node,data,graph.defaults,new MapMagic.Products.StopToken());
    var generated=data.ReadOutletProduct(node);var rect=data.area.active.rect;
    var b=System.IO.File.ReadAllBytes(dir+"/Prepared/"+name+".height.f32");var expected=new float[n,n];System.Buffer.BlockCopy(b,0,expected,0,b.Length);
    float error=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++)error=Mathf.Max(error,Mathf.Abs(generated[x+rect.offset.x,z+rect.offset.z]-expected[z,x]));
    if(error>.000001f||generated.arr.Min()<0||generated.arr.Max()>1)throw new System.Exception("Atlas mismatch "+name+" "+error);
    records.Add(new{name,error_m=error*1100});
}
UnityEngine.Object.DestroyImmediate(graph);
System.IO.File.WriteAllText(dir+"/graph-dry-run.json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
return new{tested=records.Count,proceduralNodes=0};
