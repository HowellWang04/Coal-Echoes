string dir="TerrainReferences/Tile22CliffRepairPreview/Applied";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).Single();
var previous=mm.graph.generators.OfType<TerrainHeightDelta>().Single(n=>n.delta.name=="SheepRange_WholeMap_800m_delta");
var asset=ScriptableObject.CreateInstance<Den.Tools.MatrixAsset>();
asset.source=Den.Tools.MatrixAsset.Source.New;asset.matrix=new Den.Tools.Matrices.Matrix(new Den.Tools.CoordRect(0,0,3073,3073));
var source=System.IO.File.ReadAllBytes("TerrainReferences/Tile22CliffRepairPreview/Prepared/gradient_delta.f32");
System.Buffer.BlockCopy(source,0,asset.matrix.arr,0,source.Length);
var updated=(TerrainHeightDelta)MapMagic.Nodes.Generator.Create(typeof(TerrainHeightDelta));
updated.delta=asset;updated.size=previous.size;updated.offset=previous.offset;updated.LinkedOutletId=123UL;
var baseline=(TerrainHeightDelta)MapMagic.Nodes.Generator.Create(typeof(TerrainHeightDelta));
baseline.delta=previous.delta;baseline.size=previous.size;baseline.offset=previous.offset;baseline.LinkedOutletId=123UL;
System.IO.Directory.CreateDirectory(dir+"/Prepared");
var records=new System.Collections.Generic.List<object>();
foreach(var t in Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/before.json"))["terrains"]){
    int n=(int)t["res"];string name=(string)t["name"];
    var data=new MapMagic.Products.TileData{area=new MapMagic.Terrains.Area(new Den.Tools.Vector2D((float)t["p"][0]-mm.transform.position.x,(float)t["p"][2]-mm.transform.position.z),new Den.Tools.Vector2D(1000,1000),n,n==129?mm.draftMargins:mm.tileMargins),globals=mm.globals};
    data.StoreProduct(123UL,new Den.Tools.Matrices.MatrixWorld(data.area.full.rect,data.area.full.worldPos,data.area.full.worldSize,mm.globals.height));
    updated.Generate(data,new MapMagic.Products.StopToken());baseline.Generate(data,new MapMagic.Products.StopToken());
    var a=data.ReadOutletProduct(baseline);var b=data.ReadOutletProduct(updated);var r=data.area.active.rect;
    var repair=new float[n,n];var total=new float[n,n];
    for(int z=0;z<n;z++)for(int x=0;x<n;x++){total[z,x]=b[x+r.offset.x,z+r.offset.z];repair[z,x]=total[z,x]-a[x+r.offset.x,z+r.offset.z];}
    foreach(var pair in new[]{new{suffix="delta",values=repair},new{suffix="total",values=total}}){var bytes=new byte[n*n*4];System.Buffer.BlockCopy(pair.values,0,bytes,0,bytes.Length);System.IO.File.WriteAllBytes(dir+"/Prepared/"+name+"."+pair.suffix+".f32",bytes);}
    records.Add(new{name,min=repair.Cast<float>().Min(),max=repair.Cast<float>().Max()});
}
UnityEngine.Object.DestroyImmediate(asset);
System.IO.File.WriteAllText(dir+"/sampling.json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
return new{terrains=records.Count};
