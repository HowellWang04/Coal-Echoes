string folder="TerrainReferences/Circle1GradientPreview/Applied800";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).First();
if(mm.IsGenerating()||EditorApplication.isPlaying||EditorApplication.isCompiling)throw new System.Exception("Editor unavailable");
var terrains=mm.GetComponentsInChildren<Terrain>(true);
System.Func<System.Array,byte[]> bytes=a=>{var b=new byte[a.Length*4];System.Buffer.BlockCopy(a,0,b,0,b.Length);return b;};
System.Func<byte[],string> hash=b=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b)).Replace("-", "").ToLowerInvariant();
var preview=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("TerrainReferences/Circle1GradientPreview/live.json"));
foreach(var item in preview["terrains"]){var t=terrains.Single(t=>t.terrainData.name==(string)item["name"]);var td=t.terrainData;if(hash(bytes(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution)))!=(string)item["sha256"])throw new System.Exception("Terrain changed since preview "+td.name);}
if(System.IO.File.Exists(folder+"/before.json"))throw new System.Exception("Backup already exists");
System.IO.Directory.CreateDirectory(folder+"/Backup");System.IO.Directory.CreateDirectory(folder+"/Before");System.IO.Directory.CreateDirectory(folder+"/After");
System.IO.File.Copy(scene.path,folder+"/Backup/Main Game Scene.disk.unity",false);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,folder+"/Backup/Main Game Scene.live.unity",true))throw new System.Exception("Scene backup failed");
var reports=new System.Collections.Generic.List<object>();
foreach(var t in terrains){var d=t.terrainData;var p=t.transform.position;var sz=d.size;int n=d.heightmapResolution;var h=bytes(d.GetHeights(0,0,n,n));System.IO.File.WriteAllBytes(folder+"/Before/"+d.name+".height.f32",h);string path=AssetDatabase.GetAssetPath(d);bool overlaps=p.x<-2100&&p.x+sz.x> -4148&&p.z< -1786&&p.z+sz.z> -3834;
if(overlaps){System.IO.File.Copy(path,folder+"/Backup/"+d.name+".asset",false);System.IO.File.Copy(path+".meta",folder+"/Backup/"+d.name+".asset.meta",false);System.IO.File.WriteAllBytes(folder+"/Before/"+d.name+".alpha.f32",bytes(d.GetAlphamaps(0,0,d.alphamapResolution,d.alphamapResolution)));}
reports.Add(new{name=d.name,path,res=n,alpha=d.alphamapResolution,p=new[]{p.x,p.y,p.z},size=new[]{sz.x,sz.y,sz.z},heightHash=hash(h),overlaps});}
foreach(string path in new[]{AssetDatabase.GetAssetPath(mm.graph),"Assets/Settings/HeatHaze/FactoryGroundHeight.asset","Assets/Settings/HeatHaze/M_FactoryHeatHaze.mat"}){System.IO.File.Copy(path,folder+"/Backup/"+System.IO.Path.GetFileName(path),false);}
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).OrderBy(t=>t.GetInstanceID()).Select(t=>new{id=t.GetInstanceID(),p=new[]{t.position.x,t.position.y,t.position.z},q=new[]{t.rotation.x,t.rotation.y,t.rotation.z,t.rotation.w},s=new[]{t.localScale.x,t.localScale.y,t.localScale.z}}).ToArray();
string transformJson=Newtonsoft.Json.JsonConvert.SerializeObject(transforms);System.IO.File.WriteAllText(folder+"/transforms-before.json",transformJson);
var report=new{terrains=reports,graph=AssetDatabase.GetAssetPath(mm.graph),transformHash=hash(System.Text.Encoding.UTF8.GetBytes(transformJson)),transformCount=transforms.Length,auto=EditorPrefs.GetBool("ProjectCoal.AutoAlignTerrainTextures",true),locks=Newtonsoft.Json.JsonConvert.SerializeObject(mm.locks.Select(l=>new{l.locked,l.guiName,l.relativeHeight,l.worldRadius,l.worldTransition,p=new[]{l.worldPos.x,l.worldPos.y,l.worldPos.z}}))};
System.IO.File.WriteAllText(folder+"/before.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return new{terrainCount=reports.Count,transformCount=transforms.Length,backup=folder};
