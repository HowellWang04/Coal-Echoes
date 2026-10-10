string dir="TerrainReferences/NativeDEMOnly";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var mm=all.Select(t=>t.GetComponent<MapMagic.Core.MapMagicObject>()).First(m=>m!=null);
if(mm.IsGenerating()||EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("Editor unavailable");
var terrains=mm.GetComponentsInChildren<Terrain>(true);
System.Func<MapMagic.Terrains.Edges,MapMagic.Terrains.Edges,bool> same=(a,b)=>a.arr_x.SequenceEqual(b.arr_x)&&a.arr_X.SequenceEqual(b.arr_X)&&a.arr_z.SequenceEqual(b.arr_z)&&a.arr_Z.SequenceEqual(b.arr_Z);
foreach(var t in terrains){
    var d=t.terrainData;var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;
    var h=new MapMagic.Terrains.Edges(0,0);h.ReadFloats2D(d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution));
    if(!same(h,detail.edges.heightEdges))throw new System.Exception("Height cache mismatch "+d.name);
    var a=d.GetAlphamaps(0,0,d.alphamapResolution,d.alphamapResolution);
    for(int l=0;l<detail.edges.splatEdges.Length;l++){var e=new MapMagic.Terrains.Edges(0,0);e.ReadFloats3D(a,l);if(!same(e,detail.edges.splatEdges[l]))throw new System.Exception("Material cache mismatch "+d.name);}
}
var node=mm.graph.generators.OfType<TerrainHeightSource>().Single();
foreach(var pair in new[]{(asset:node.main,lod:"main"),(asset:node.draft,lod:"draft")}){
    var b=new byte[pair.asset.matrix.arr.Length*4];System.Buffer.BlockCopy(pair.asset.matrix.arr,0,b,0,b.Length);
    if(!b.SequenceEqual(System.IO.File.ReadAllBytes(dir+"/Prepared/"+pair.lod+"_height.f32")))throw new System.Exception("Source matrix mismatch");
}
if(mm.graph.generators.Length!=7||mm.graph.links.Count(p=>ReferenceEquals(p.Value,node))!=2)throw new System.Exception("Unexpected graph");
var sandstorm=all.Single(t=>t.name=="Factory Sandstorm").gameObject;var heat=all.Single(t=>t.name=="Factory Heat Haze").gameObject;
bool sandActive=sandstorm.activeSelf,heatActive=heat.activeSelf;
try{sandstorm.SetActive(true);FactoryHeatHazeSetup.Refresh();}
finally{sandstorm.SetActive(sandActive);heat.SetActive(heatActive);}
AssetDatabase.SaveAssets();
var saved=UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);if(!saved)throw new System.Exception("Scene save failed");
var report=new{saved,scene.isDirty,heightAndMaterialCachesChecked=terrains.Length,sandActive,heatActive,sourceMatricesMatch=true,graphNodes=mm.graph.generators.Length,proceduralNodes=0,infiniteGeneration=mm.tiles.generateInfinite,heightRange=mm.globals.height,otherSceneDirty=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/SmokePortalTest/SmokePortal_Test.unity").isDirty};
System.IO.File.WriteAllText(dir+"/persistence.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
