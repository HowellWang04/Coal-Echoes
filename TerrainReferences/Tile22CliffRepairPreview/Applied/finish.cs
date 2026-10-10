string dir="TerrainReferences/Tile22CliffRepairPreview/Applied";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var mm=all.Select(t=>t.GetComponent<MapMagic.Core.MapMagicObject>()).First(m=>m!=null);
if(mm.IsGenerating()||EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("Editor unavailable");
var edits=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(dir+"/prepared.json"));
var terrains=mm.GetComponentsInChildren<Terrain>(true);
System.Func<MapMagic.Terrains.Edges,MapMagic.Terrains.Edges,bool> same=(a,b)=>a.arr_x.SequenceEqual(b.arr_x)&&a.arr_X.SequenceEqual(b.arr_X)&&a.arr_z.SequenceEqual(b.arr_z)&&a.arr_Z.SequenceEqual(b.arr_Z);
foreach(var item in edits){
    var t=terrains.Single(t=>t.terrainData.name==(string)item["name"]);var d=t.terrainData;
    var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;
    var h=new MapMagic.Terrains.Edges(0,0);h.ReadFloats2D(d.GetHeights(0,0,d.heightmapResolution,d.heightmapResolution));
    if(!same(h,detail.edges.heightEdges))throw new System.Exception("Height cache mismatch "+d.name);
    var a=d.GetAlphamaps(0,0,d.alphamapResolution,d.alphamapResolution);
    for(int l=0;l<detail.edges.splatEdges.Length;l++){var e=new MapMagic.Terrains.Edges(0,0);e.ReadFloats3D(a,l);if(!same(e,detail.edges.splatEdges[l]))throw new System.Exception("Material cache mismatch "+d.name);}
}
var sandstorm=all.Single(t=>t.name=="Factory Sandstorm").gameObject;
var heat=all.Single(t=>t.name=="Factory Heat Haze").gameObject;
bool sandActive=sandstorm.activeSelf,heatActive=heat.activeSelf;
try{sandstorm.SetActive(true);FactoryHeatHazeSetup.Refresh();}
finally{sandstorm.SetActive(sandActive);heat.SetActive(heatActive);}
var saved=UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
if(!saved)throw new System.Exception("Scene save failed");
var asset=mm.graph.generators.OfType<TerrainHeightDelta>().Single(n=>n.delta.name=="SheepRange_WholeMap_800m_delta").delta;
var b=new byte[asset.matrix.arr.Length*4];System.Buffer.BlockCopy(asset.matrix.arr,0,b,0,b.Length);
if(!b.SequenceEqual(System.IO.File.ReadAllBytes("TerrainReferences/Tile22CliffRepairPreview/Prepared/gradient_delta.f32")))throw new System.Exception("Corrected delta mismatch");
var report=new{saved,scene.isDirty,heightAndMaterialCachesChecked=edits.Count,sandActive,heatActive,deltaSource=asset.source.ToString(),deltaMatchesCandidate=true,graphNodes=mm.graph.generators.Length,otherSceneDirty=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/SmokePortalTest/SmokePortal_Test.unity").isDirty};
System.IO.File.WriteAllText(dir+"/persistence.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
