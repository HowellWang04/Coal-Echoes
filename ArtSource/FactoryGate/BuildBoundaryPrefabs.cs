using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class BuildBoundaryPrefabs
{
    const string Folder="Assets/Art Resources/Environment/Gate Area/FactoryBoundary";
    const string GatePath="Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt/FactoryGate_Rebuilt.glb";
    public static string Inspect()
    {
        var entrance=GameObject.Find("In-scene Objects/Factory Entrance");
        var gate=entrance.transform.Find("FactoryGate");
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {
            gate=gate.position.ToString(),
            neighbours=entrance.transform.Cast<Transform>().Select(t=>new {t.name,localToGate=gate.InverseTransformPoint(t.position).ToString("F3")}).ToArray(),
            wings=gate.Cast<Transform>().Where(t=>t.name.Contains("WingWall")).Select(t=>new {t.name,center=gate.InverseTransformPoint(t.GetComponentInChildren<Renderer>().bounds.center).ToString("F3")}).ToArray()
        });
    }
    static void Socket(Transform parent,string name,Vector3 position)
    {
        var obj=new GameObject(name);
        SceneManager.MoveGameObjectToScene(obj,parent.gameObject.scene);
        obj.transform.SetParent(parent,false);
        obj.transform.localPosition=position;
    }
    public static string Validate()
    {
        var assets=new[]{"FactoryGate","FactoryPillar","FactoryWall_3m"}.Select(name=>
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Prefabs/"+name+".prefab");
            var meshes=prefab.GetComponentsInChildren<MeshFilter>();
            return new {name,
                meshes=meshes.Select(m=>new {m.name,vertices=m.sharedMesh.vertexCount,
                    uv=m.sharedMesh.uv.Length,normals=m.sharedMesh.normals.Length,tangents=m.sharedMesh.tangents.Length,
                    colors=m.sharedMesh.colors.Length,
                    materials=m.GetComponent<Renderer>().sharedMaterials.Select(s=>new {s.name,shader=s.shader.name,s.shader.isSupported}).ToArray()}).ToArray(),
                colliders=prefab.GetComponentsInChildren<BoxCollider>().Select(c=>new {c.name,center=c.center.ToString("F3"),size=c.size.ToString("F3")}).ToArray()};
        }).ToArray();
        var gate=GameObject.Find("In-scene Objects/Factory Entrance/FactoryGate");
        var result=Newtonsoft.Json.JsonConvert.SerializeObject(new {assets,
            liveGate=new {position=gate.transform.position.ToString("F3"),scale=gate.transform.localScale.ToString(),
                removedWingAbsent=gate.transform.Find("Right_WingWall")==null,
                guardhouseWingPresent=gate.transform.Find("Left_WingWall")!=null},
            playMode=EditorApplication.isPlaying},Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("ArtSource/FactoryBoundary/unity_import_verification.json",result);
        return result;
    }
    static void Box(GameObject obj,Vector3 center,Vector3 size)
    {
        var collider=obj.AddComponent<BoxCollider>();
        collider.center=center;
        collider.size=size;
    }
    public static string Build()
    {
        if(EditorApplication.isPlaying) throw new Exception("Edit mode required");
        Directory.CreateDirectory(Folder+"/Prefabs");
        AssetDatabase.Refresh();
        foreach(var path in new[]{GatePath,Folder+"/Models/FactoryPillar.glb",Folder+"/Models/FactoryWall_3m.glb"})
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var gateAsset=AssetDatabase.LoadAssetAtPath<GameObject>(GatePath);
        var materials=gateAsset.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToDictionary(m=>m.name);
        var results=new System.Collections.Generic.List<object>();
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var name in new[]{"FactoryGate","FactoryPillar","FactoryWall_3m"})
            {
                var path=name=="FactoryGate"?GatePath:Folder+"/Models/"+name+".glb";
                var root=new GameObject(name);
                SceneManager.MoveGameObjectToScene(root,scene);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
                model.transform.SetParent(root.transform,false);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>materials[m.name]).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                if(name=="FactoryPillar")
                {
                    Box(root,new Vector3(0,2.245f,0),new Vector3(.78f,3.73f,.77f));
                    Box(root,new Vector3(0,.19f,0),new Vector3(.96f,.38f,.95f));
                    Box(root,new Vector3(0,4.245f,0),new Vector3(1.05f,.27f,.99f));
                    Socket(root.transform,"Snap_Centre",Vector3.zero);
                }
                else if(name=="FactoryWall_3m")
                {
                    Box(root,new Vector3(1.5f,1.84f,0),new Vector3(2.25f,3.68f,.5f));
                    Box(root,new Vector3(1.5f,.14f,0),new Vector3(2.25f,.28f,.62f));
                    Socket(root.transform,"Snap_Start",Vector3.zero);
                    Socket(root.transform,"Snap_End",new Vector3(3,0,0));
                }
                else
                {
                    Box(root,new Vector3(-3.17f,2.195f,0),new Vector3(.96f,4.39f,.95f));
                    Box(root,new Vector3(3.17f,2.195f,0),new Vector3(.96f,4.39f,.95f));
                    Box(root,new Vector3(3.9025f,1.84f,0),new Vector3(.685f,3.68f,.5f));
                    foreach(var leafName in new[]{"Left_GateLeaf","Right_GateLeaf"})
                    {
                        var leaf=model.transform.Find(leafName);
                        var x=leafName.StartsWith("Left")?1.359f:-1.359f;
                        Box(leaf.gameObject,leaf.InverseTransformPoint(model.transform.TransformPoint(new Vector3(x,2.07f,0))),new Vector3(2.682f,3.86f,.12f));
                    }
                    Socket(root.transform,"Snap_OuterPillar",new Vector3(-3.17f,0,0));
                    Socket(root.transform,"Snap_GuardWallEnd",new Vector3(4.245f,0,0));
                }
                var prefabPath=Folder+"/Prefabs/"+name+".prefab";
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath,out bool success);
                if(!success) throw new Exception("Could not save "+prefabPath);
                var meshFilters=prefab.GetComponentsInChildren<MeshFilter>();
                results.Add(new {name,path=prefabPath,triangles=meshFilters.Sum(f=>f.sharedMesh.triangles.Length/3),
                    colliderCount=prefab.GetComponentsInChildren<Collider>().Length,
                    allMaterialsShared=prefab.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>materials.Values.Contains(m))),
                    missingScripts=prefab.GetComponentsInChildren<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
                    sockets=prefab.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Snap_")).Select(t=>new {t.name,position=t.localPosition.ToString()}).ToArray()});
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        var result=Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("ArtSource/FactoryBoundary/unity_prefab_report.json",result);
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Prefabs/FactoryWall_3m.prefab"));
        return result;
    }
}
