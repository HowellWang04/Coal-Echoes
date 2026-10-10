// Only Terrain alphamaps and their serialized splat edge caches are changed.
string dir="TerrainReferences/PeripheralMountainsPreview/Diagnosis/Circle2";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var mm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapMagic.Core.MapMagicObject>(true)).First();
if(mm.IsGenerating()||EditorApplication.isPlaying)throw new System.Exception("Editor unavailable");
var tiles=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText(dir+"/candidate.json"));
var terrains=mm.GetComponentsInChildren<Terrain>(true);
System.Func<System.Array,byte[]> bytes=a=>{var b=new byte[a.Length*4];System.Buffer.BlockCopy(a,0,b,0,b.Length);return b;};
System.Func<byte[],string> hash=b=>System.BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(b));
var allHeightHashes=terrains.ToDictionary(t=>t.terrainData.name,t=>hash(bytes(t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution))));
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).OrderBy(t=>t.GetInstanceID()).ToArray();
System.Func<string> transformHash=()=>hash(System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(transforms.Select(t=>new{id=t.GetInstanceID(),p=new[]{t.position.x,t.position.y,t.position.z},q=new[]{t.rotation.x,t.rotation.y,t.rotation.z,t.rotation.w},s=new[]{t.localScale.x,t.localScale.y,t.localScale.z}}))));
string beforeTransforms=transformHash();
string locks=Newtonsoft.Json.JsonConvert.SerializeObject(mm.locks.Select(l=>new{l.locked,l.guiName,l.relativeHeight,l.worldRadius,l.worldTransition,p=new[]{l.worldPos.x,l.worldPos.y,l.worldPos.z}}));
// Check every input before the first mutation.
foreach(var item in tiles){string name=(string)item["name"];var td=terrains.Single(t=>t.terrainData.name==name).terrainData;
    if(!bytes(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution)).SequenceEqual(System.IO.File.ReadAllBytes(dir+"/"+name+".height.f32")))throw new System.Exception("Height changed since preparation "+name);
    if(!bytes(td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight)).SequenceEqual(System.IO.File.ReadAllBytes(dir+"/"+name+".original.f32")))throw new System.Exception("Paint changed since preparation "+name);
}
System.IO.Directory.CreateDirectory(dir+"/Backup");
System.IO.File.Copy(scene.path,dir+"/Backup/Main Game Scene.unity",false);
foreach(var item in tiles){string path=(string)item["path"];System.IO.File.Copy(path,dir+"/Backup/"+System.IO.Path.GetFileName(path),false);System.IO.File.Copy(path+".meta",dir+"/Backup/"+System.IO.Path.GetFileName(path)+".meta",false);}
var reports=new System.Collections.Generic.List<object>();
foreach(var item in tiles){string name=(string)item["name"];var t=terrains.Single(t=>t.terrainData.name==name);var td=t.terrainData;int ar=td.alphamapWidth;var alpha=new float[ar,ar,3];var data=System.IO.File.ReadAllBytes(dir+"/"+name+".candidate.f32");System.Buffer.BlockCopy(data,0,alpha,0,data.Length);
    Undo.RegisterCompleteObjectUndo(td,"Remove stale factory platform paint");td.SetAlphamaps(0,0,alpha);t.Flush();EditorUtility.SetDirty(td);AssetDatabase.SaveAssetIfDirty(td);
    var actual=td.GetAlphamaps(0,0,ar,ar);System.IO.File.WriteAllBytes(dir+"/"+name+".actual.f32",bytes(actual));
    var tile=t.GetComponentInParent<MapMagic.Terrains.TerrainTile>();var detail=tile.main!=null&&tile.main.terrain==t?tile.main:tile.draft;
    Undo.RecordObject(tile,"Refresh repaired terrain paint edges");if(detail.edges.splatEdges!=null)for(int i=0;i<System.Math.Min(3,detail.edges.splatEdges.Length);i++)detail.edges.splatEdges[i].ReadFloats3D(actual,i);EditorUtility.SetDirty(tile);
    reports.Add(new{name,heightUnchanged=allHeightHashes[name]==hash(bytes(td.GetHeights(0,0,td.heightmapResolution,td.heightmapResolution)))});
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
var verification=new{tiles=reports,allTerrainHeightHashesUnchanged=terrains.All(t=>allHeightHashes[t.terrainData.name]==hash(bytes(t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution)))),transformsUnchanged=beforeTransforms==transformHash(),transformCount=transforms.Length,lockState=locks};
var json=Newtonsoft.Json.JsonConvert.SerializeObject(verification,Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(dir+"/apply_verification.json",json);return new{repaired=reports,verification.allTerrainHeightHashesUnchanged,verification.transformsUnchanged,verification.transformCount};
