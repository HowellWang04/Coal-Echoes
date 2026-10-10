string dir="TerrainReferences/WholeMapGradient800";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).First();
var asset=ScriptableObject.CreateInstance<Den.Tools.MatrixAsset>();asset.source=Den.Tools.MatrixAsset.Source.New;asset.matrix=new Den.Tools.Matrices.Matrix(new Den.Tools.CoordRect(0,0,3073,3073));
var bytes=System.IO.File.ReadAllBytes(dir+"/Prepared/gradient_delta.f32");System.Buffer.BlockCopy(bytes,0,asset.matrix.arr,0,bytes.Length);
var import=(TerrainHeightDelta)MapMagic.Nodes.Generator.Create(typeof(TerrainHeightDelta));import.delta=asset;import.size=new Vector2(6144,6144);import.offset=new Vector2(-4996.00049f-mm.transform.position.x,-3986-mm.transform.position.z);import.LinkedOutletId=123UL;
var tiles=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/before.json"))["terrains"];
var results=new System.Collections.Generic.List<object>();
foreach(var t in tiles){if(!(bool)t["overlaps"])continue;int n=(int)t["res"];string name=(string)t["name"];float px=(float)t["p"][0],pz=(float)t["p"][2];var data=new MapMagic.Products.TileData{area=new MapMagic.Terrains.Area(new Den.Tools.Vector2D(px-mm.transform.position.x,pz-mm.transform.position.z),new Den.Tools.Vector2D(1000,1000),n,n==129?mm.draftMargins:mm.tileMargins),globals=mm.globals};data.StoreProduct(123UL,new Den.Tools.Matrices.MatrixWorld(data.area.full.rect,data.area.full.worldPos,data.area.full.worldSize,mm.globals.height));import.Generate(data,new MapMagic.Products.StopToken());var matrix=data.ReadOutletProduct(import);var delta=new float[n,n];var r=data.area.active.rect;
for(int z=0;z<n;z++)for(int x=0;x<n;x++)delta[z,x]=matrix[x+r.offset.x,z+r.offset.z];
var output=new byte[n*n*4];System.Buffer.BlockCopy(delta,0,output,0,output.Length);System.IO.File.WriteAllBytes(dir+"/Prepared/"+name+".delta.f32",output);results.Add(new{name,res=n,min=delta.Cast<float>().Min(),max=delta.Cast<float>().Max()});}
UnityEngine.Object.DestroyImmediate(asset);System.IO.File.WriteAllText(dir+"/import_trials.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));return results;
