var root="ArtSource/Cableway/FoothillStationPlacement/";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var terrains=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Terrain>(true)).ToArray();
string Sha(byte[] b)=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b)).Replace("-","");
var rows=new System.Collections.Generic.List<object>();
foreach(var t in terrains){
 var td=t.terrainData;var p=t.transform.position;var size=td.size;var path=UnityEditor.AssetDatabase.GetAssetPath(td);int n=td.heightmapResolution;
 var h=td.GetHeights(0,0,n,n);var bytes=new byte[n*n*4];System.Buffer.BlockCopy(h,0,bytes,0,bytes.Length);
 bool affected=p.x<-1550 && p.x+size.x>-2450 && p.z<-1200 && p.z+size.z>-2150;
 string file=System.IO.Path.GetFileNameWithoutExtension(path);
 if(affected){System.IO.File.WriteAllBytes(root+"Before/"+file+".f32",bytes);System.IO.File.Copy(path,root+"Before/"+file+".asset",true);}
 rows.Add(new{path,pos=new[]{p.x,p.y,p.z},size=new[]{size.x,size.y,size.z},resolution=n,affected,sha256=Sha(bytes),file});
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,root+"Before/MainGameScene.unity",true);
foreach(var path in new[]{"Assets/TerrainData/SheepRangeDEM/SheepRange_Height.raw","Assets/TerrainData/SheepRangeDEM/SheepRange_Height.asset","Assets/MapMagic Graph.asset"})System.IO.File.Copy(path,root+"Before/"+System.IO.Path.GetFileName(path),true);
System.IO.File.WriteAllText(root+"terrain_before.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
return new { terrains=rows.Count,sceneBackup=true };
