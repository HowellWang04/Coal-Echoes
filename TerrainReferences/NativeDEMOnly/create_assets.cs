string dir="TerrainReferences/NativeDEMOnly";
var atlas=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(dir+"/atlas.json"));
var results=new System.Collections.Generic.List<object>();
foreach(var row in atlas["atlases"]){
    string lod=(string)row["lod"],path="Assets/TerrainData/SheepRangeDEM/SheepRange_Native_"+lod+".asset";
    if(AssetDatabase.LoadAssetAtPath<Den.Tools.MatrixAsset>(path)!=null)throw new System.Exception("Source asset already exists");
    int n=(int)row["resolution"];
    var bytes=System.IO.File.ReadAllBytes(dir+"/Prepared/"+lod.ToLowerInvariant()+"_height.f32");
    if(bytes.Length!=n*n*4)throw new System.Exception("Incomplete atlas");
    var asset=ScriptableObject.CreateInstance<Den.Tools.MatrixAsset>();asset.source=Den.Tools.MatrixAsset.Source.New;
    asset.matrix=new Den.Tools.Matrices.Matrix(new Den.Tools.CoordRect(0,0,n,n));
    System.Buffer.BlockCopy(bytes,0,asset.matrix.arr,0,bytes.Length);
    AssetDatabase.CreateAsset(asset,path);EditorUtility.SetDirty(asset);
    results.Add(new{path,resolution=n,bytes=bytes.Length});
}
AssetDatabase.SaveAssets();return results;
