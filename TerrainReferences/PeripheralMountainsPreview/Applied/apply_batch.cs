// Executed as an in-memory editor method body, not installed as a project script.
string folder="TerrainReferences/PeripheralMountainsPreview/Applied";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).First();
if(mm.IsGenerating())throw new System.Exception("MapMagic is generating");
var terrains=mm.GetComponentsInChildren<Terrain>(true).Where(t=>AssetDatabase.GetAssetPath(t.terrainData).Contains("SheepRangeGenerated")).ToArray();
var graph=mm.graph;
var slopeNode=graph.generators.OfType<MapMagic.Nodes.MatrixGenerators.Slope200>().Single();
var cavityNode=graph.generators.OfType<MapMagic.Nodes.MatrixGenerators.Cavity200>().Single();
var textureNode=graph.generators.OfType<MapMagic.Nodes.MatrixGenerators.TexturesOutput200>().Single();
var mask=graph.generators.OfType<MapMagic.Nodes.MatrixGenerators.Import200>().Single(g=>g.matrixAsset.name=="SheepRange_Peripheral_mask").matrixAsset.matrix;
var completed=System.IO.File.Exists(folder+"/completed.json")?Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<string>>(System.IO.File.ReadAllText(folder+"/completed.json")):new System.Collections.Generic.List<string>();
var batch=terrains.Where(t=>System.IO.File.Exists(folder+"/Prepared/"+t.terrainData.name+".f32")&&!completed.Contains(t.terrainData.name)).Take(4).ToArray();
var cache=new System.Collections.Generic.Dictionary<Terrain,float[,]>();
foreach(var t in terrains.OrderBy(t=>t.terrainData.heightmapResolution)){
    int n=t.terrainData.heightmapResolution;var values=new float[n,n];
    string file=folder+"/Prepared/"+t.terrainData.name+".f32";
    if(!System.IO.File.Exists(file))file=folder+"/Before/"+t.terrainData.name+".f32";
    var bytes=System.IO.File.ReadAllBytes(file);System.Buffer.BlockCopy(bytes,0,values,0,bytes.Length);cache[t]=values;
}
System.Func<float,float,float> worldHeight=(wx,wz)=>{
    Terrain chosen=null;int best=0;
    foreach(var candidate in terrains){var p=candidate.transform.position;var size=candidate.terrainData.size;if(wx>=p.x-.002f&&wx<=p.x+size.x+.002f&&wz>=p.z-.002f&&wz<=p.z+size.z+.002f&&candidate.terrainData.heightmapResolution>best){chosen=candidate;best=candidate.terrainData.heightmapResolution;}}
    if(chosen==null){chosen=terrains.OrderBy(t=>(t.transform.position+new Vector3(500,0,500)-new Vector3(wx,t.transform.position.y,wz)).sqrMagnitude).First();best=chosen.terrainData.heightmapResolution;}
    var a=cache[chosen];var tp=chosen.transform.position;var ts=chosen.terrainData.size;
    float fx=Mathf.Clamp((wx-tp.x)/ts.x*(best-1),0,best-1.001f),fz=Mathf.Clamp((wz-tp.z)/ts.z*(best-1),0,best-1.001f);int x=(int)fx,z=(int)fz;float u=fx-x,v=fz-z;
    return Mathf.Lerp(Mathf.Lerp(a[z,x],a[z,x+1],u),Mathf.Lerp(a[z+1,x],a[z+1,x+1],u),v);
};
var reports=new System.Collections.Generic.List<object>();
foreach(var terrain in batch){
    var td=terrain.terrainData;int n=td.heightmapResolution;int ar=td.alphamapResolution;
    var oldAlpha=td.GetAlphamaps(0,0,ar,ar);var alpha=(float[,,])oldAlpha.Clone();
    if(td.terrainLayers.Length!=3)throw new System.Exception("Unexpected layers "+td.name);
    for(int layer=0;layer<3;layer++)if(td.terrainLayers[layer]!=textureNode.layers[layer].prototype)throw new System.Exception("Layer order "+td.name);
    var oldH=td.GetHeights(0,0,n,n);var height=cache[terrain];
    Undo.RegisterCompleteObjectUndo(td,"Apply peripheral mountains");
    td.SetHeights(0,0,height);
    // Extend derivatives across tile boundaries using the complete prepared surface.
    const int margin=3;int side=n+margin*2;float spacing=td.size.x/(n-1);
    var origin=terrain.transform.position-new Vector3(margin*spacing,0,margin*spacing);
    var matrix=new Den.Tools.Matrices.MatrixWorld(new Den.Tools.CoordRect(0,0,side,side),origin,new Vector3(td.size.x+margin*spacing*2,1000,td.size.z+margin*spacing*2));
    for(int z=0;z<side;z++)for(int x=0;x<side;x++){
        if(x>=margin&&x<n+margin&&z>=margin&&z<n+margin)matrix.arr[z*side+x]=height[z-margin,x-margin];
        else matrix.arr[z*side+x]=worldHeight(origin.x+x*spacing,origin.z+z*spacing);
    }
    var slope=MapMagic.Nodes.MatrixGenerators.Slope200.Slope(matrix,1000,slopeNode.from,slopeNode.to,slopeNode.range);
    var cavity=new Den.Tools.Matrices.MatrixWorld(matrix.rect,matrix.worldPos,matrix.worldSize);
    MapMagic.Nodes.MatrixGenerators.Cavity200.Cavity(matrix,cavity,cavityNode.type,cavityNode.intensity,cavityNode.spread,spacing,new Den.Tools.Vector2D(1000,1000),null);
    var sand=new Den.Tools.Matrices.MatrixWorld(matrix.rect,matrix.worldPos,matrix.worldSize);sand.Fill(1);
    var layers=new Den.Tools.Matrices.Matrix[]{sand,slope,cavity};Den.Tools.Matrices.Matrix.BlendLayers(layers,textureNode.layers.Select(l=>l.Opacity).ToArray());
    int alteredAlpha=0;
    for(int z=0;z<ar;z++)for(int x=0;x<ar;x++){
        float wx=terrain.transform.position.x+(x+.5f)*1000/ar,wz=terrain.transform.position.z+(z+.5f)*1000/ar;
        float w=mask.GetInterpolated(Mathf.Clamp((wx+4924.00049f)*6144/6000,0,6143),Mathf.Clamp((wz+3914)*6144/6000,0,6143));
        if(w<=0)continue;
        w=Mathf.Clamp01(w*4);w=w*w*(3-2*w);
        float px=margin+(x+.5f)*(n-1)/ar,pz=margin+(z+.5f)*(n-1)/ar;
        for(int layer=0;layer<3;layer++)alpha[z,x,layer]=Mathf.Lerp(alpha[z,x,layer],layers[layer].GetInterpolated(px,pz),w);
        alteredAlpha++;
    }
    td.SetAlphamaps(0,0,alpha);
    terrain.Flush();EditorUtility.SetDirty(td);AssetDatabase.SaveAssetIfDirty(td);
    // Refresh serialized edge caches so LOD welding cannot restore old heights.
    var tile=terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==terrain?tile.main:tile.draft;
    detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));
    if(detail.edges.splatEdges!=null)for(int layer=0;layer<System.Math.Min(detail.edges.splatEdges.Length,3);layer++)detail.edges.splatEdges[layer].ReadFloats3D(td.GetAlphamaps(0,0,ar,ar),layer);
    EditorUtility.SetDirty(tile);
    var after=td.GetHeights(0,0,n,n);var alphaAfter=td.GetAlphamaps(0,0,ar,ar);
    int protectedChanged=0;float protectedMax=0;int unmaskedAlphaChanges=0;
    for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(height[z,x]==oldH[z,x]&&after[z,x]!=oldH[z,x]){protectedChanged++;protectedMax=Mathf.Max(protectedMax,Mathf.Abs(after[z,x]-oldH[z,x])*1000);}
    for(int z=0;z<ar;z++)for(int x=0;x<ar;x++)if(alpha[z,x,0]==oldAlpha[z,x,0]&&alpha[z,x,1]==oldAlpha[z,x,1]&&alpha[z,x,2]==oldAlpha[z,x,2])for(int layer=0;layer<3;layer++)if(alphaAfter[z,x,layer]!=oldAlpha[z,x,layer])unmaskedAlphaChanges++;
    reports.Add(new{name=td.name,alteredAlpha=alteredAlpha,unchangedHeightSamplesDrifted=protectedChanged,maxDriftM=protectedMax,unchangedAlphaChannelsDrifted=unmaskedAlphaChanges});
    completed.Add(td.name);System.IO.File.WriteAllText(folder+"/completed.json",Newtonsoft.Json.JsonConvert.SerializeObject(completed));
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
string report=Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(folder+"/batch_"+completed.Count+".json",report);
return report;
