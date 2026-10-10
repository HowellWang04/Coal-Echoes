// In-memory Editor diagnostic. Produces local alpha candidates without changing assets.
string dir="TerrainReferences/PeripheralMountainsPreview/Diagnosis/Circle2";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).First();
if(mm.IsGenerating())throw new System.Exception("MapMagic is generating");
var terrains=mm.GetComponentsInChildren<Terrain>(true).ToArray();
var cache=new System.Collections.Generic.Dictionary<Terrain,float[,]>();
foreach(var t in terrains)cache[t]=t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution);
System.Func<float,float,float> heightAt=(x,z)=>{
    var t=terrains.Where(t=>x>=t.transform.position.x-.002f&&x<=t.transform.position.x+1000.002f&&z>=t.transform.position.z-.002f&&z<=t.transform.position.z+1000.002f).OrderByDescending(t=>t.terrainData.heightmapResolution).First();
    var h=cache[t];int n=h.GetLength(0);float px=Mathf.Clamp((x-t.transform.position.x)*(n-1)/1000,0,n-1.001f),pz=Mathf.Clamp((z-t.transform.position.z)*(n-1)/1000,0,n-1.001f);int ix=(int)px,iz=(int)pz;
    return Mathf.Lerp(Mathf.Lerp(h[iz,ix],h[iz,ix+1],px-ix),Mathf.Lerp(h[iz+1,ix],h[iz+1,ix+1],px-ix),pz-iz);
};
var slopeNode=mm.graph.generators.OfType<MapMagic.Nodes.MatrixGenerators.Slope200>().Single();
var cavityNode=mm.graph.generators.OfType<MapMagic.Nodes.MatrixGenerators.Cavity200>().Single();
var textures=mm.graph.generators.OfType<MapMagic.Nodes.MatrixGenerators.TexturesOutput200>().Single();
var list=new System.Collections.Generic.List<object>();
foreach(var t in terrains.Where(t=>t.transform.position.x>=-2925&&t.transform.position.x<=-1923&&t.transform.position.z>=-2915&&t.transform.position.z<=-1913)){
    var td=t.terrainData;int n=td.heightmapResolution,ar=td.alphamapWidth;float spacing=1000f/(n-1);const int margin=3;int side=n+2*margin;
    var origin=t.transform.position-new Vector3(margin*spacing,0,margin*spacing);
    var matrix=new Den.Tools.Matrices.MatrixWorld(new Den.Tools.CoordRect(0,0,side,side),origin,new Vector3(1000+margin*spacing*2,1000,1000+margin*spacing*2));
    for(int z=0;z<side;z++)for(int x=0;x<side;x++)matrix.arr[z*side+x]=(x>=margin&&x<n+margin&&z>=margin&&z<n+margin)?cache[t][z-margin,x-margin]:heightAt(origin.x+x*spacing,origin.z+z*spacing);
    var slope=MapMagic.Nodes.MatrixGenerators.Slope200.Slope(matrix,1000,slopeNode.from,slopeNode.to,slopeNode.range);
    var cavity=new Den.Tools.Matrices.MatrixWorld(matrix.rect,matrix.worldPos,matrix.worldSize);
    MapMagic.Nodes.MatrixGenerators.Cavity200.Cavity(matrix,cavity,cavityNode.type,cavityNode.intensity,cavityNode.spread,spacing,new Den.Tools.Vector2D(1000,1000),null);
    var sand=new Den.Tools.Matrices.MatrixWorld(matrix.rect,matrix.worldPos,matrix.worldSize);sand.Fill(1);
    var layers=new Den.Tools.Matrices.Matrix[]{sand,slope,cavity};Den.Tools.Matrices.Matrix.BlendLayers(layers,textures.layers.Select(l=>l.Opacity).ToArray());
    var old=td.GetAlphamaps(0,0,ar,ar);var alpha=(float[,,])old.Clone();var expected=(float[,,])old.Clone();var mask=new float[ar,ar];int count=0;double oldError=0;int full=0;
    for(int z=0;z<ar;z++)for(int x=0;x<ar;x++){
        float wx=t.transform.position.x+(x+.5f)*1000/ar,wz=t.transform.position.z+(z+.5f)*1000/ar;
        float dx=wx+2000,dz=wz+1650,along=.5f*dx+.866025404f*dz,across=-.866025404f*dx+.5f*dz;
        float distance=new Vector2(Mathf.Max(Mathf.Abs(along)-272.5f,0),Mathf.Max(Mathf.Abs(across)-190,0)).magnitude;
        float w=1-Mathf.SmoothStep(0,1,(distance-95)/25);if(w<=0)continue;mask[z,x]=w;count++;
        float px=margin+(x+.5f)*(n-1)/ar,pz=margin+(z+.5f)*(n-1)/ar;
        for(int l=0;l<3;l++){float target=layers[l].GetInterpolated(px,pz);expected[z,x,l]=target;alpha[z,x,l]=Mathf.Lerp(old[z,x,l],target,w);}
        if(w==1){oldError+=Mathf.Abs(old[z,x,1]-expected[z,x,1]);full++;}
    }
    foreach(var pair in new[]{new {suffix="original",values=(System.Array)old},new {suffix="candidate",values=(System.Array)alpha},new {suffix="expected",values=(System.Array)expected},new {suffix="mask",values=(System.Array)mask},new {suffix="height",values=(System.Array)cache[t]}}){var bytes=new byte[pair.values.Length*4];System.Buffer.BlockCopy(pair.values,0,bytes,0,bytes.Length);System.IO.File.WriteAllBytes(dir+"/"+td.name+"."+pair.suffix+".f32",bytes);}
    list.Add(new{name=td.name,path=AssetDatabase.GetAssetPath(td),ar,n,p=new[]{t.transform.position.x,t.transform.position.y,t.transform.position.z},candidatePixels=count,fullyRecomputedPixels=full,oldRockMeanAbsoluteError=oldError/full});
}
var json=Newtonsoft.Json.JsonConvert.SerializeObject(list,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(dir+"/candidate.json",json);return json;
