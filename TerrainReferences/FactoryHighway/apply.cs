var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();
if(EditorApplication.isPlaying || mm.IsGenerating()) throw new System.Exception("Editor must be idle");
var src=mm.graph.generators.OfType<TerrainHeightSource>().Single();
string root="TerrainReferences/FactoryHighway";
var route=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(root+"/route.json"));
var bounds=route["bounds"];float minX=(float)bounds[0][0],minZ=(float)bounds[0][1],maxX=(float)bounds[1][0],maxZ=(float)bounds[1][1];
var oldMain=(float[])src.main.matrix.arr.Clone();var oldDraft=(float[])src.draft.matrix.arr.Clone();
foreach(var p in new[]{(asset:src.main,old:oldMain,name:"main"),(asset:src.draft,old:oldDraft,name:"draft")}){
 var before=System.IO.File.ReadAllBytes(root+"/Before/"+p.name+".f32");var current=new byte[p.old.Length*4];System.Buffer.BlockCopy(p.old,0,current,0,current.Length);
 if(!before.SequenceEqual(current)) throw new System.Exception("Source changed since survey");
}
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Create factory highway and approach roadbed");
foreach(var p in new[]{(asset:src.main,name:"main"),(asset:src.draft,name:"draft")}){
 Undo.RegisterCompleteObjectUndo(p.asset,"Grade road corridor");
 var b=System.IO.File.ReadAllBytes(root+"/Prepared/"+p.name+".f32");System.Buffer.BlockCopy(b,0,p.asset.matrix.arr,0,b.Length);EditorUtility.SetDirty(p.asset);
}
System.Func<float[],int,float,float,float> sample=(a,n,x,z)=>{
 double px=System.Math.Clamp((x-mm.transform.position.x-src.offset.x)/src.size.x*(n-1),0,n-1.00001);
 double pz=System.Math.Clamp((z-mm.transform.position.z-src.offset.y)/src.size.y*(n-1),0,n-1.00001);
 int ix=(int)px,iz=(int)pz;double tx=px-ix,tz=pz-iz;
 return (float)((a[iz*n+ix]*(1-tx)+a[iz*n+ix+1]*tx)*(1-tz)+(a[(iz+1)*n+ix]*(1-tx)+a[(iz+1)*n+ix+1]*tx)*tz);
};
var edits=new System.Collections.Generic.List<object>();
bool auto=EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true);EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",false);
try{
 foreach(var terrain in mm.GetComponentsInChildren<Terrain>(true)){
  var td=terrain.terrainData;var pos=terrain.transform.position;var size=td.size;int n=td.heightmapResolution;
  if(pos.x>maxX||pos.z>maxZ||pos.x+size.x<minX||pos.z+size.z<minZ)continue;
  int x0=Mathf.Clamp(Mathf.FloorToInt((minX-pos.x)/size.x*(n-1)),0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt((minZ-pos.z)/size.z*(n-1)),0,n-1);
  int x1=Mathf.Clamp(Mathf.CeilToInt((maxX-pos.x)/size.x*(n-1)),0,n-1),z1=Mathf.Clamp(Mathf.CeilToInt((maxZ-pos.z)/size.z*(n-1)),0,n-1);
  var tile=terrain.GetComponentInParent<MapMagic.Terrains.TerrainTile>();bool draft=tile.draft!=null&&tile.draft.terrain==terrain;
  var previous=draft?oldDraft:oldMain;int sn=draft?src.draft.matrix.rect.size.x:src.main.matrix.rect.size.x;
  var height=td.GetHeights(x0,z0,x1-x0+1,z1-z0+1);int changed=0;
  for(int z=0;z<height.GetLength(0);z++)for(int x=0;x<height.GetLength(1);x++){
   float wx=pos.x+(x+x0)*size.x/(n-1),wz=pos.z+(z+z0)*size.z/(n-1);
   float delta=src.SampleHeight(wx-mm.transform.position.x,wz-mm.transform.position.z,draft)-sample(previous,sn,wx,wz);
   if(Mathf.Abs(delta)<0.00000008f)continue;height[z,x]+=delta;changed++;
  }
  if(changed==0)continue;
  System.IO.File.Copy(AssetDatabase.GetAssetPath(td),root+"/Before/"+td.name+".asset",true);
  Undo.RegisterCompleteObjectUndo(td,"Grade road corridor");td.SetHeights(x0,z0,height);terrain.Flush();EditorUtility.SetDirty(td);
  var detail=draft?tile.draft:tile.main;detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));detail.data?.Clear(inSubs:true);EditorUtility.SetDirty(tile);
  edits.Add(new{name=terrain.name,path=AssetDatabase.GetAssetPath(td),changed,x0,z0,width=x1-x0+1,height=z1-z0+1});
 }
}finally{EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",auto);}
System.IO.File.WriteAllText(root+"/application.json",Newtonsoft.Json.JsonConvert.SerializeObject(edits,Newtonsoft.Json.Formatting.Indented));
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
AssetDatabase.SaveAssets();return edits;
