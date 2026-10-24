var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var root=GameObject.Find("Factory Road Tunnel").transform;
var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();
if(EditorApplication.isPlaying || mm.IsGenerating())throw new System.Exception("Editor is busy");
var origin=root.position;
var reliefBytes=System.IO.File.ReadAllBytes("ArtSource/FactoryRoadTunnel/ridge-relief.f32");
var relief=new float[300*300];System.Buffer.BlockCopy(reliefBytes,0,relief,0,reliefBytes.Length);
float Detail(float x,float z){
 float sx=Mathf.Clamp(x+150,0,298.999f),sz=Mathf.Clamp(z+60,0,298.999f);int ix=(int)sx,iz=(int)sz;
 return Mathf.Lerp(Mathf.Lerp(relief[iz*300+ix],relief[iz*300+ix+1],sx-ix),Mathf.Lerp(relief[(iz+1)*300+ix],relief[(iz+1)*300+ix+1],sx-ix),sz-iz);
}
var cfg=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/FactoryRoadTunnel/terrain-shape.json"));
var hull=cfg["hull"].Select(p=>new Vector2((float)p[0],(float)p[1])).ToArray();
float S(float a,float b,float v)=>Mathf.SmoothStep(0,1,(v-a)/(b-a));
float FactoryMask(float x,float z){
 var p=new Vector2(x,z);bool inside=true;float distance=float.PositiveInfinity;
 for(int i=0;i<hull.Length;i++){
  var a=hull[i];var ab=hull[(i+1)%hull.Length]-a;var ap=p-a;
  if(ab.x*ap.y-ab.y*ap.x<0)inside=false;
  distance=Mathf.Min(distance,Vector2.Distance(p,a+ab*Mathf.Clamp01(Vector2.Dot(ap,ab)/ab.sqrMagnitude)));
 }
 return inside?0:S(8,32,distance);
}
float Shape(float wx,float wz,float baseline){
 var p=root.InverseTransformPoint(new Vector3(wx,origin.y,wz));float x=p.x,z=p.z;
 float radius=Mathf.Sqrt(Mathf.Pow(x/170,2)+Mathf.Pow((z-65)/190,2));
 float weight=(1-S(.25f,1,radius))*S(-36,-8,z)*FactoryMask(wx,wz);
 if(weight<=0)return baseline;
 float broad=10.3f-.04f*Mathf.Max(0,z-10)-.018f*Mathf.Abs(x);
 float top=origin.y+broad+(Detail(x,z)-Detail(0,0))*1.15f;
 float portalDistance=Mathf.Sqrt(x*x+(z-4)*(z-4));
 top=Mathf.Lerp(top,Mathf.Max(top,origin.y+10),1-S(10,30,portalDistance));
 float h=Mathf.Lerp(baseline,Mathf.Max(baseline,top),weight);
 // The short road cutting is concealed by the arch and its rock shoulders.
 float approach=(1-S(5.8f,24,Mathf.Abs(x)))*(1-S(-.5f,2,z));
 h=Mathf.Lerp(h,origin.y-.06f+Mathf.Clamp01(-z/40)*.1f,approach);
 return h;
}
var region=Rect.MinMaxRect(origin.x-300,origin.z-300,origin.x+330,origin.z+300);
var terrains=mm.GetComponentsInChildren<Terrain>(true).Where(t=>new Rect(t.transform.position.x,t.transform.position.z,t.terrainData.size.x,t.terrainData.size.z).Overlaps(region)).ToArray();
System.IO.Directory.CreateDirectory("Temp/TunnelNativeTerrain");
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Sculpt native tunnel terrain");
bool automatic=EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true);
EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",false);
var report=new System.Collections.Generic.List<object>();
try{
 foreach(var t in terrains){
  var td=t.terrainData;int n=td.heightmapResolution;var pos=t.transform.position;var size=td.size;
  string backup="Temp/TunnelNativeTerrain/"+td.name+".f32";
  var h=td.GetHeights(0,0,n,n);
  if(!System.IO.File.Exists(backup)){var bytes=new byte[h.Length*4];System.Buffer.BlockCopy(h,0,bytes,0,bytes.Length);System.IO.File.WriteAllBytes(backup,bytes);}
  var original=new float[n,n];var data=System.IO.File.ReadAllBytes(backup);System.Buffer.BlockCopy(data,0,original,0,data.Length);
  int changed=0,minX=n,minZ=n,maxX=0,maxZ=0;float maximum=0;
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){
   float wx=pos.x+x*size.x/(n-1),wz=pos.z+z*size.z/(n-1);
   float old=original[z,x]*size.y+pos.y,next=Shape(wx,wz,old);
   if(Mathf.Abs(next-old)<.0001f && Mathf.Abs(h[z,x]-original[z,x])<.000001f)continue;
   h[z,x]=(next-pos.y)/size.y;changed++;maximum=Mathf.Max(maximum,next-old);
   minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minZ=Mathf.Min(minZ,z);maxZ=Mathf.Max(maxZ,z);
  }
  if(changed==0)continue;
  Undo.RegisterCompleteObjectUndo(td,"Sculpt native tunnel terrain");
  int w=maxX-minX+1,d=maxZ-minZ+1;var patch=new float[d,w];for(int z=0;z<d;z++)for(int x=0;x<w;x++)patch[z,x]=h[z+minZ,x+minX];
  td.SetHeights(minX,minZ,patch);
  int hn=td.holesResolution;var holes=td.GetHoles(0,0,hn,hn);
  string holesBackup="Temp/TunnelNativeTerrain/"+td.name+".holes";
  if(!System.IO.File.Exists(holesBackup)){var bytes=new byte[holes.Length];for(int z=0;z<hn;z++)for(int x=0;x<hn;x++)bytes[z*hn+x]=holes[z,x]?(byte)1:(byte)0;System.IO.File.WriteAllBytes(holesBackup,bytes);}
  var originalHoles=System.IO.File.ReadAllBytes(holesBackup);int cut=0;
  for(int z=0;z<hn;z++)for(int x=0;x<hn;x++){
   var p=root.InverseTransformPoint(new Vector3(pos.x+(x+.5f)*size.x/hn,origin.y,pos.z+(z+.5f)*size.z/hn));
   bool hole=Mathf.Abs(p.x)<4.65f && p.z> -1.5f && p.z<3.8f;
   holes[z,x]=originalHoles[z*hn+x]!=0&&!hole;if(hole)cut++;
  }
  td.SetHoles(0,0,holes);t.Flush();EditorUtility.SetDirty(td);
  var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var lod=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;
  lod.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));lod.data?.Clear(inSubs:true);EditorUtility.SetDirty(tile);
  int painted=MapMagicTerrainTextureSync.RefreshTerrain(t,region,true);
  report.Add(new{td.name,changed,maximum,cut,painted});
 }
 // Keep the fixed height source consistent with the edited native Terrain.
 var source=mm.graph.generators.OfType<TerrainHeightSource>().Single();
 foreach(var asset in new[]{source.main,source.draft}){
  var matrix=asset.matrix;int nx=matrix.rect.size.x,nz=matrix.rect.size.z;
  string backup="Temp/TunnelNativeTerrain/"+asset.name+".source.f32";
  if(!System.IO.File.Exists(backup)){var bytes=new byte[matrix.arr.Length*4];System.Buffer.BlockCopy(matrix.arr,0,bytes,0,bytes.Length);System.IO.File.WriteAllBytes(backup,bytes);}
  var bytesBefore=System.IO.File.ReadAllBytes(backup);var before=new float[matrix.arr.Length];System.Buffer.BlockCopy(bytesBefore,0,before,0,bytesBefore.Length);
  Undo.RegisterCompleteObjectUndo(asset,"Preserve native tunnel hill");
  int x0=Mathf.Max(0,Mathf.FloorToInt((region.xMin-mm.transform.position.x-source.offset.x)/source.size.x*(nx-1)));
  int x1=Mathf.Min(nx-1,Mathf.CeilToInt((region.xMax-mm.transform.position.x-source.offset.x)/source.size.x*(nx-1)));
  int z0=Mathf.Max(0,Mathf.FloorToInt((region.yMin-mm.transform.position.z-source.offset.y)/source.size.y*(nz-1)));
  int z1=Mathf.Min(nz-1,Mathf.CeilToInt((region.yMax-mm.transform.position.z-source.offset.y)/source.size.y*(nz-1)));
  int count=0;
  for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
   float wx=mm.transform.position.x+source.offset.x+x*source.size.x/(nx-1),wz=mm.transform.position.z+source.offset.y+z*source.size.y/(nz-1);
   float old=before[z*nx+x]*mm.globals.height+mm.transform.position.y,next=Shape(wx,wz,old);
   if(Mathf.Abs(next-old)<.0001f && Mathf.Abs(matrix.arr[z*nx+x]-before[z*nx+x])<.000001f)continue;
   matrix.arr[z*nx+x]=(next-mm.transform.position.y)/mm.globals.height;count++;
  }
  if(count>0){EditorUtility.SetDirty(asset);report.Add(new{source=asset.name,changed=count});}
 }
}finally{EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",automatic);}
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return Newtonsoft.Json.JsonConvert.SerializeObject(report);
