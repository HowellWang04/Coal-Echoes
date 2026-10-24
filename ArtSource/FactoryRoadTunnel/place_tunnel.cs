var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
const string folder="Assets/Art Resources/Environment/FactoryRoadTunnel";
var layout=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/FactoryRoadTunnel/layout.json"));
Vector3 V(Newtonsoft.Json.Linq.JToken a)=>new Vector3((float)a[0],(float)a[1],(float)a[2]);
var origin=V(layout["origin"]);var direction=V(layout["direction"]);var right=V(layout["right"]);
var world=scene.GetRootGameObjects().Single(g=>g.name=="场景内Object").transform;
if(world.Find("Factory Road Tunnel")!=null)throw new System.Exception("Tunnel already exists");
var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art Resources/Environment/Cableway/CablewayStationShell/Materials/Station_Concrete.mat");
Material Save(Material m,string name){m.name=name;UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");return m;}
var rock=new Material(Shader.Find("HDRP/Lit"));rock.SetColor("_BaseColor",Color.white);rock.SetFloat("_Smoothness",.05f);rock.SetFloat("_NormalScale",.45f);
rock.SetTexture("_BaseColorMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/MapMagic/Demo/LandTextures/CliffDark.tif"));rock.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/MapMagic/Demo/LandTextures/CliffDark_nrm.tif"));rock=Save(rock,"TunnelCutRock");
var portalMaterial=Save(new Material(concrete),"TunnelPortalConcrete");portalMaterial.SetColor("_BaseColor",new Color(.72f,.69f,.63f,1));EditorUtility.SetDirty(portalMaterial);
var interiorMaterials=new Material[12];interiorMaterials[0]=portalMaterial;
var entrance=new Material(portalMaterial);entrance.SetColor("_BaseColor",new Color(.3f,.29f,.26f,1));interiorMaterials[1]=Save(entrance,"TunnelInnerConcrete");
float[] shades={.045f,.018f,.005f,.001f,0};Material black=null;
for(int i=2;i<12;i++){
 if(i>6){interiorMaterials[i]=black;continue;}
 var material=new Material(Shader.Find("HDRP/Unlit"));material.SetColor("_UnlitColor",new Color(shades[i-2],shades[i-2],shades[i-2],1));material.SetColor("_EmissiveColor",Color.black);
 material.SetFloat("_SurfaceType",1);material.SetFloat("_BlendMode",0);material.SetFloat("_TransparentZWrite",1);material.SetFloat("_EnableFogOnTransparent",0);
 if(i<6)material.SetTexture("_UnlitColorMap",concrete.GetTexture("_BaseColorMap"));
 interiorMaterials[i]=Save(material,i==6?"TunnelBlack":"TunnelDark"+i);if(i==6)black=interiorMaterials[i];
}
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Create factory road tunnel");
var root=new GameObject("Factory Road Tunnel");Undo.RegisterCreatedObjectUndo(root,"Create factory road tunnel");root.transform.SetParent(world,false);root.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(direction));root.isStatic=true;
foreach(var file in new[]{"TunnelRockCollar.obj","TunnelEntranceApron.obj","ConcreteTunnelPortal.obj","TunnelInterior.obj"}){
 var model=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+file);var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,root.transform);instance.transform.localPosition=Vector3.zero;instance.transform.localRotation=Quaternion.identity;instance.transform.localScale=Vector3.one;
 foreach(var mf in instance.GetComponentsInChildren<MeshFilter>(true)){
  mf.gameObject.isStatic=true;var renderer=mf.GetComponent<MeshRenderer>();
  renderer.sharedMaterial=file=="TunnelRockCollar.obj"?rock:file=="ConcreteTunnelPortal.obj"||file=="TunnelEntranceApron.obj"?portalMaterial:mf.name=="Black_End"?black:interiorMaterials[int.Parse(mf.name.Substring(9))];
  if(mf.name!="Black_End"){var collider=mf.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=mf.sharedMesh;}
  PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
 }
}
var blocker=new GameObject("Tunnel End Blocker");blocker.transform.SetParent(root.transform,false);blocker.transform.localPosition=new Vector3(0,4,8);var box=blocker.AddComponent<BoxCollider>();box.size=new Vector3(9.3f,8,.6f);blocker.isStatic=true;
// Grade the approach before sculpting the surrounding native Terrain with rebuild_terrain.cs.
var mm=UnityEngine.Object.FindFirstObjectByType<MapMagic.Core.MapMagicObject>();var source=mm.graph.generators.OfType<TerrainHeightSource>().Single();
float Weight(float x,float z){var p=new Vector3(x-origin.x,0,z-origin.z);float lateral=Vector3.Dot(p,right),forward=Vector3.Dot(p,direction);return (1-Mathf.SmoothStep(0,1,(Mathf.Abs(lateral)-5.8f)/12)) * Mathf.SmoothStep(0,1,(forward+45)/10)*(1-Mathf.SmoothStep(0,1,(forward-18)/12));}
float RoadHeight(float x,float z){float forward=Vector3.Dot(new Vector3(x-origin.x,0,z-origin.z),direction);return origin.y+Mathf.Clamp01(-forward/40)*.1f-.06f;}
System.IO.Directory.CreateDirectory("Temp/FactoryRoadTunnel");
var changed=new System.Collections.Generic.List<string>();bool automatic=EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true);EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",false);
try{
 foreach(var t in mm.GetComponentsInChildren<Terrain>(true)){
  var td=t.terrainData;var p=t.transform.position;var size=td.size;if(origin.x<p.x-100||origin.x>p.x+size.x+100||origin.z<p.z-100||origin.z>p.z+size.z+100)continue;
  int n=td.heightmapResolution;var h=td.GetHeights(0,0,n,n);var before=new byte[h.Length*4];System.Buffer.BlockCopy(h,0,before,0,before.Length);System.IO.File.WriteAllBytes("Temp/FactoryRoadTunnel/"+td.name+".f32",before);int count=0;
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){float wx=p.x+x*size.x/(n-1),wz=p.z+z*size.z/(n-1),w=Weight(wx,wz);if(w<=0)continue;h[z,x]=Mathf.Lerp(h[z,x],(RoadHeight(wx,wz)-p.y)/size.y,w);count++;}
  if(count==0)continue;Undo.RegisterCompleteObjectUndo(td,"Grade tunnel approach");td.SetHeights(0,0,h);t.Flush();EditorUtility.SetDirty(td);changed.Add(td.name);
  var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;detail.edges.heightEdges.ReadFloats2D(td.GetHeights(0,0,n,n));detail.data?.Clear(inSubs:true);EditorUtility.SetDirty(tile);
 }
 foreach(var asset in new[]{source.main,source.draft}){
  var m=asset.matrix;int nx=m.rect.size.x,nz=m.rect.size.z;Undo.RegisterCompleteObjectUndo(asset,"Preserve tunnel approach in source");
  int x0=Mathf.Max(0,Mathf.FloorToInt((origin.x-100-mm.transform.position.x-source.offset.x)/source.size.x*(nx-1))),x1=Mathf.Min(nx-1,Mathf.CeilToInt((origin.x+100-mm.transform.position.x-source.offset.x)/source.size.x*(nx-1)));
  int z0=Mathf.Max(0,Mathf.FloorToInt((origin.z-100-mm.transform.position.z-source.offset.y)/source.size.y*(nz-1))),z1=Mathf.Min(nz-1,Mathf.CeilToInt((origin.z+100-mm.transform.position.z-source.offset.y)/source.size.y*(nz-1)));
  for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){float wx=mm.transform.position.x+source.offset.x+x*source.size.x/(nx-1),wz=mm.transform.position.z+source.offset.y+z*source.size.y/(nz-1),w=Weight(wx,wz);if(w>0)m.arr[z*nx+x]=Mathf.Lerp(m.arr[z*nx+x],(RoadHeight(wx,wz)-mm.transform.position.y)/mm.globals.height,w);}
  EditorUtility.SetDirty(asset);
 }
}finally{EditorPrefs.SetBool("ProjectCoal.AutoAlignTerrainTextures",automatic);}
var roadTemplate=scene.GetRootGameObjects().Single(g=>g.name=="Gate Dirt Road Test").transform.GetChild(0).gameObject;
var roadParent=new GameObject("Tunnel Approach Road");roadParent.transform.SetParent(root.transform,false);
for(int i=0;i<4;i++){
 var decal=UnityEngine.Object.Instantiate(roadTemplate,roadParent.transform);decal.name="Tunnel Road "+(i+1);var p=origin+direction*(-15+i*6);p.y=RoadHeight(p.x,p.z)+.065f;decal.transform.position=p;decal.transform.rotation=roadTemplate.transform.rotation;decal.transform.localScale=roadTemplate.transform.lossyScale;
}
Physics.SyncTransforms();AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return new{root=root.name,position=origin.ToString(),changedTerrains=changed,meshes=root.GetComponentsInChildren<MeshFilter>().Length,blocker=box.size.ToString(),saved=false};
