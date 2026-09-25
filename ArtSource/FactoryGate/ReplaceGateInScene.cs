using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class ReplaceGateInScene
{
    const string OldPath = "Assets/Art Resources/Environment/Gate Area/FactoryGate.glb";
    const string NewPath = "Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt/FactoryGate_Rebuilt.glb";
    static Transform OldGate() => GameObject.Find("In-scene Objects/Factory Entrance/FactoryGate").transform;
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
    static float[] V(Vector3 v) => new [] {v.x,v.y,v.z};
    static object B(Bounds b) => new {center=V(b.center), size=V(b.size)};
    static object Describe(GameObject g) => new {
        name = g.name, path = PathOf(g.transform), id = g.GetEntityId().ToString(), active = g.activeSelf,
        position = V(g.transform.position), rotation = V(g.transform.eulerAngles),
        localPosition = V(g.transform.localPosition), localRotation = V(g.transform.localEulerAngles),
        scale = V(g.transform.localScale), worldScale = V(g.transform.lossyScale),
        components = g.GetComponents<Component>().Select(c => c ? c.GetType().FullName : "MISSING").ToArray(),
        renderers = g.GetComponentsInChildren<Renderer>(true).Select(r => new {
            path = PathOf(r.transform), bounds = B(r.bounds), localBounds = B(r.localBounds),
            materials = r.sharedMaterials.Select(m => m ? m.name + " | " + m.shader.name + " | supported=" + m.shader.isSupported : "MISSING").ToArray()
        }).ToArray(),
        colliders = g.GetComponentsInChildren<Collider>(true).Select(c => new {path=PathOf(c.transform), type=c.GetType().Name, bounds=B(c.bounds),trigger=c.isTrigger}).ToArray(),
        behaviours = g.GetComponentsInChildren<MonoBehaviour>(true).Select(c => c ? PathOf(c.transform)+" | "+c.GetType().FullName : "MISSING").ToArray()
    };
    public static object Inspect()
    {
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        var all = scenes.SelectMany(s => s.GetRootGameObjects()).SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var gates = all.Where(t => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == OldPath && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray();
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {
            scenes = scenes.Select(s => new {s.name,s.path,s.isDirty,s.isLoaded}).ToArray(),
            gates = gates.Select(t => new {gate=Describe(t.gameObject), parent=t.parent ? PathOf(t.parent) : null,
                siblings=t.parent ? t.parent.Cast<Transform>().Select(c => new {c.name,position=V(c.position),scale=V(c.localScale),components=c.GetComponents<Component>().Select(x=>x?x.GetType().Name:"MISSING").ToArray()}).ToArray():null}).ToArray(),
            newAsset = Describe(AssetDatabase.LoadAssetAtPath<GameObject>(NewPath))
        });
    }
    public static string Survey()
    {
        var gate = OldGate();
        var targets = gate.GetComponentsInChildren<Component>(true).Cast<UnityEngine.Object>().Concat(new UnityEngine.Object[]{gate.gameObject}).ToArray();
        var all = gate.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c).ToArray();
        var refs = new System.Collections.Generic.List<string>();
        foreach (var c in all.Where(c=>!(c is Transform) && !targets.Contains(c)))
        {
            var so = new SerializedObject(c);
            var p = so.GetIterator();
            while(p.Next(true))
                if(p.propertyType==SerializedPropertyType.ObjectReference && targets.Contains(p.objectReferenceValue))
                    refs.Add(PathOf(c.transform)+" | "+c.GetType().Name+" | "+p.propertyPath);
        }
        var b = gate.GetComponent<Renderer>().bounds;
        var view = SceneView.lastActiveSceneView;
        var front = Vector3.ProjectOnPlane(gate.up,Vector3.up).normalized;
        view.LookAt(b.center,Quaternion.LookRotation(-front+Vector3.down*.18f,Vector3.up),7f,false,true);
        view.Repaint();
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {
            references=refs, right=V(gate.right),up=V(gate.up),forward=V(gate.forward),
            nearbyColliders=all.OfType<Collider>().Where(c=>Vector3.Distance(c.bounds.ClosestPoint(b.center),b.center)<7).Select(c=>new {path=PathOf(c.transform),bounds=B(c.bounds),type=c.GetType().Name}).ToArray(),
            parentPosition=V(gate.parent.position),parentRotation=V(gate.parent.eulerAngles),parentScale=V(gate.parent.lossyScale)
        });
    }
    public static string Backup()
    {
        var scene = OldGate().gameObject.scene;
        var folder = "ArtSource/FactoryGate/Previous/Before_Unity_Replacement";
        Directory.CreateDirectory(folder);
        File.Copy(scene.path,folder+"/Main Game Scene.disk.unity",false);
        bool saved = EditorSceneManager.SaveScene(scene,folder+"/Main Game Scene.live.unity",true);
        if(!saved) throw new Exception("Could not back up the live scene");
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {saved,scene.path,scene.isDirty,folder});
    }
    public static string Replace()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode required");
        var old = OldGate();
        var scene = old.gameObject.scene;
        if(!File.Exists("ArtSource/FactoryGate/Previous/Before_Unity_Replacement/Main Game Scene.live.unity"))
            throw new Exception("Live scene backup required");
        var oldBounds = old.GetComponent<MeshFilter>().sharedMesh.bounds;
        var position = old.TransformPoint(new Vector3(oldBounds.center.x,oldBounds.center.y,oldBounds.max.z));
        var rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(old.up,Vector3.up).normalized,Vector3.up);
        Undo.IncrementCurrentGroup();
        var undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Replace factory gate model");
        var gate = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(NewPath),old.parent);
        Undo.RegisterCreatedObjectUndo(gate,"Create rebuilt factory gate");
        gate.name = old.name;
        gate.transform.SetPositionAndRotation(position,rotation);
        gate.transform.localScale = Vector3.one;
        gate.transform.SetSiblingIndex(old.GetSiblingIndex());
        foreach(var t in gate.GetComponentsInChildren<Transform>(true)) {
            t.gameObject.layer=old.gameObject.layer;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject,GameObjectUtility.GetStaticEditorFlags(old.gameObject));
        }
        gate.tag=old.tag;
        PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
        PrefabUtility.RecordPrefabInstancePropertyModifications(gate.transform);
        Undo.DestroyObjectImmediate(old.gameObject);
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
        var b=gate.GetComponentsInChildren<Renderer>()[0].bounds;
        foreach(var r in gate.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        var view=SceneView.lastActiveSceneView;
        view.LookAt(b.center,Quaternion.LookRotation(-gate.transform.forward+Vector3.down*.12f,Vector3.up),3.4f,false,true);
        Selection.activeGameObject=gate;
        view.Repaint();
        var result=Newtonsoft.Json.JsonConvert.SerializeObject(new {scene=scene.path,scene.isDirty,source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gate),gate=Describe(gate)},Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("ArtSource/FactoryGate/unity_replacement.json",result);
        return result;
    }
    public static string Verify()
    {
        var gate=OldGate().gameObject;
        var renderers=gate.GetComponentsInChildren<Renderer>();
        var b=renderers[0].bounds;
        foreach(var r in renderers) b.Encapsulate(r.bounds);
        var mats=renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var view=SceneView.lastActiveSceneView;
        view.LookAt(b.center,Quaternion.LookRotation(-gate.transform.forward+Vector3.down*.04f,Vector3.up),1.3f,false,true);
        view.Repaint();
        var result=Newtonsoft.Json.JsonConvert.SerializeObject(new {
            scene=gate.scene.path,gate.scene.isDirty,
            source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gate),
            triangles=gate.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3),
            materialCount=mats.Length,
            materials=mats.Select(m=>new {m.name,shader=m.shader.name,supported=m.shader.isSupported,textures=m.GetTexturePropertyNames().Select(p=>new {property=p,texture=m.GetTexture(p)?m.GetTexture(p).name:null,width=m.GetTexture(p)?m.GetTexture(p).width:0}).ToArray()}).ToArray(),
            externalConcreteTextures=new[]{"Concrete_Normal.png","Concrete_ORM.png"}.Select(n=>new {name=n,loaded=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art Resources/Environment/Gate Area/FactoryGate_Rebuilt/Textures/"+n)!=null}).ToArray(),
            colliderCount=gate.GetComponentsInChildren<Collider>().Length,
            missingScripts=gate.GetComponentsInChildren<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
            oldInstances=gate.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)&&PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==OldPath)
        },Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("ArtSource/FactoryGate/unity_verification.json",result);
        return result;
    }
}
