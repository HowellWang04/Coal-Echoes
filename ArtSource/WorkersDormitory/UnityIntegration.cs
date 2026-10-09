using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class DormitoryIntegration
{
    const string TargetName="SM_AbandonedFactoryBuilding_Pack__C_1";
    const string Folder="ArtSource/WorkersDormitory/UnityIntegration";
    const string AssetPath="Assets/Art Resources/Environment/WorkersDormitory/WorkersDormitory.glb";
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static Transform[] All()=>Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s.isLoaded).SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static object Describe(GameObject g)=>new{
        path=PathOf(g.transform),scene=g.scene.path,active=g.activeSelf,position=V(g.transform.position),rotation=V(g.transform.eulerAngles),scale=V(g.transform.localScale),worldScale=V(g.transform.lossyScale),
        components=g.GetComponentsInChildren<Component>(true).Select(c=>c?PathOf(c.transform)+" | "+c.GetType().FullName:"MISSING").ToArray(),
        renderers=g.GetComponentsInChildren<Renderer>(true).Select(r=>new{path=PathOf(r.transform),center=V(r.bounds.center),size=V(r.bounds.size),localCenter=V(r.localBounds.center),localSize=V(r.localBounds.size),materials=r.sharedMaterials.Select(m=>m?m.name+" | "+m.shader.name:"MISSING").ToArray()}).ToArray(),
        mesh=g.GetComponentsInChildren<MeshFilter>(true).Select(f=>new{path=PathOf(f.transform),asset=AssetDatabase.GetAssetPath(f.sharedMesh),center=V(f.sharedMesh.bounds.center),size=V(f.sharedMesh.bounds.size)}).ToArray()
    };
    static string Save(string name,object value){Directory.CreateDirectory(Folder);var text=Newtonsoft.Json.JsonConvert.SerializeObject(value,Newtonsoft.Json.Formatting.Indented);File.WriteAllText(Folder+"/"+name+".json",text);return text;}
    public static string Inspect(){
        var matches=All().Where(t=>t.name==TargetName).ToArray();
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);
        return Save("inspection",new{scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(s=>new{s.name,s.path,s.isDirty}).ToArray(),matches=matches.Select(t=>Describe(t.gameObject)).ToArray(),newAsset=asset?Describe(asset):null});
    }
    public static string Survey(){
        var t=All().Single(o=>o.name==TargetName);
        var targets=new HashSet<UnityEngine.Object>(t.GetComponentsInChildren<Component>(true).Cast<UnityEngine.Object>().Concat(t.GetComponentsInChildren<Transform>(true).Select(x=>(UnityEngine.Object)x.gameObject)));
        var refs=new List<string>();
        foreach(var c in All().SelectMany(x=>x.GetComponents<Component>()).Where(c=>c && !(c is Transform) && !targets.Contains(c))){
            var so=new SerializedObject(c);var p=so.GetIterator();
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && targets.Contains(p.objectReferenceValue))refs.Add(PathOf(c.transform)+" | "+c.GetType().Name+" | "+p.propertyPath);
        }
        var b=BoundsOf(t.gameObject);
        var sv=SceneView.lastActiveSceneView;
        sv.LookAt(b.center,Quaternion.LookRotation(new Vector3(.8f,-.35f,1f)),b.size.magnitude*.6f,false,true);sv.Repaint();
        return Save("survey",new{references=refs,parent=t.parent?PathOf(t.parent):null,parentScale=t.parent?V(t.parent.lossyScale):null,tag=t.tag,layer=t.gameObject.layer,staticFlags=GameObjectUtility.GetStaticEditorFlags(t.gameObject).ToString(),right=V(t.right),up=V(t.up),forward=V(t.forward),bounds=new{center=V(b.center),size=V(b.size)},nearby=All().Where(x=>x!=t&&x.GetComponent<Renderer>()&&Vector3.Distance(x.GetComponent<Renderer>().bounds.center,b.center)<70).Select(x=>new{path=PathOf(x),center=V(x.GetComponent<Renderer>().bounds.center),size=V(x.GetComponent<Renderer>().bounds.size)}).Take(35).ToArray()});
    }
    public static string ImportAndLocate(){
        Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
        if(!File.Exists(AssetPath)) File.Copy("ArtSource/WorkersDormitory/Exports/WorkersDormitory.glb",AssetPath);
        AssetDatabase.ImportAsset(AssetPath,ImportAssetOptions.ForceSynchronousImport);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);
        var t=All().Single(o=>o.name==TargetName);
        var renderers=t.GetComponentsInChildren<Renderer>(true).Where(r=>!r.name.Contains("LOD")||r.name.Contains("LOD0")).ToArray();
        var points=new List<Vector3>();
        foreach(var r in renderers){var b=r.localBounds;for(int i=0;i<8;i++)points.Add(t.InverseTransformPoint(r.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)))));}
        var local=new Bounds(points[0],Vector3.zero);foreach(var p in points)local.Encapsulate(p);
        var baseCenter=t.TransformPoint(new Vector3(local.center.x,local.min.y,local.center.z));
        Physics.SyncTransforms();
        var ground=Physics.RaycastAll(baseCenter+Vector3.up*100,Vector3.down,250).Where(h=>!h.transform.IsChildOf(t)).OrderByDescending(h=>h.point.y).Select(h=>new{path=PathOf(h.transform),point=V(h.point),normal=V(h.normal)}).ToArray();
        return Save("placement",new{asset=asset?Describe(asset):null,localBounds=new{center=V(local.center),size=V(local.size)},baseCenter=V(baseCenter),ground,terrain=Terrain.activeTerrains.Select(x=>new{x.name,position=V(x.transform.position),size=V(x.terrainData.size),height=x.SampleHeight(baseCenter)+x.transform.position.y}).ToArray(),triggers=t.GetComponentsInChildren<Collider>(true).Count(c=>c.isTrigger)});
    }
    public static string GroundSamples(){
        var t=All().Single(o=>o.name==TargetName);
        var origin=new Vector3(-2220.4397f,0,-1711.8595f);
        var points=new List<object>();
        foreach(float x in new[]{-16.35f,0,16.35f})foreach(float z in new[]{-5f,0,6.57f}){
            var p=origin+t.rotation*new Vector3(x,0,z);
            var hits=Physics.RaycastAll(p+Vector3.up*100,Vector3.down,1000).Where(h=>!h.transform.IsChildOf(t)).OrderByDescending(h=>h.point.y).ToArray();
            points.Add(new{x,z,hits=hits.Select(h=>new{path=PathOf(h.transform),point=V(h.point)}).ToArray()});
        }
        return Save("ground_samples",points);
    }
    public static string Replace(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var old=All().Single(t=>t.name==TargetName);
        var scene=old.gameObject.scene;
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);
        if(!asset)throw new Exception("Imported model missing");
        var backup=Folder+"/BeforeReplacement";
        Directory.CreateDirectory(backup);
        File.Copy(scene.path,backup+"/Main Game Scene.disk.unity",false);
        if(!EditorSceneManager.SaveScene(scene,backup+"/Main Game Scene.live.unity",true))throw new Exception("Backup failed");
        var unaffected=All().Where(t=>!t.IsChildOf(old)).Select(t=>new{path=PathOf(t),id=t.GetEntityId().ToString(),position=V(t.position),rotation=V(t.eulerAngles),scale=V(t.localScale)}).ToArray();
        Save("unaffected_before",unaffected);
        Undo.IncrementCurrentGroup();var group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Replace factory building with workers dormitory");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,old.parent);
        Undo.RegisterCreatedObjectUndo(go,"Create workers dormitory");
        go.name="WorkersDormitory";
        go.transform.SetPositionAndRotation(new Vector3(-2220.4397f,-454.68f,-1711.8595f),old.rotation*Quaternion.Euler(0,180,0));
        go.transform.localScale=Vector3.one;
        go.transform.SetSiblingIndex(old.GetSiblingIndex());
        go.tag=old.tag;
        foreach(var t in go.GetComponentsInChildren<Transform>(true)){
            t.gameObject.layer=old.gameObject.layer;
            GameObjectUtility.SetStaticEditorFlags(t.gameObject,GameObjectUtility.GetStaticEditorFlags(old.gameObject));
        }
        foreach(var f in go.GetComponentsInChildren<MeshFilter>()){
            if(f.name.Contains("Runoff"))continue;
            var col=Undo.AddComponent<MeshCollider>(f.gameObject);col.sharedMesh=f.sharedMesh;
        }
        var foundation=GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(foundation,"Add slope foundation");
        foundation.name="SlopeFoundation";foundation.transform.SetParent(go.transform,false);
        foundation.transform.localPosition=new Vector3(0,-.79f,0);
        foundation.transform.localScale=new Vector3(32.3f,1.6f,9.85f);
        foundation.GetComponent<Renderer>().sharedMaterial=go.transform.Find("Dorm_Concrete").GetComponent<Renderer>().sharedMaterial;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        Undo.DestroyObjectImmediate(old.gameObject);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
        Save("unaffected_after",All().Where(t=>!t.IsChildOf(go.transform)).Select(t=>new{path=PathOf(t),id=t.GetEntityId().ToString(),position=V(t.position),rotation=V(t.eulerAngles),scale=V(t.localScale)}).ToArray());
        Selection.activeGameObject=go;
        var b=BoundsOf(go);
        SceneView.lastActiveSceneView.LookAt(b.center,Quaternion.LookRotation(-go.transform.forward-go.transform.right*.22f+Vector3.down*.22f),22f,false,true);
        SceneView.lastActiveSceneView.Repaint();
        return Save("replacement",new{scene=scene.path,scene.isDirty,backup,go=Describe(go)});
    }
    public static string Verify(){
        var go=All().Single(t=>t.name=="WorkersDormitory"&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).gameObject;
        var mats=go.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        return Save("verification",new{scene=go.scene.path,go.scene.isDirty,source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go),position=V(go.transform.position),rotation=V(go.transform.eulerAngles),scale=V(go.transform.lossyScale),oldCount=All().Count(t=>t.name==TargetName),rendererCount=go.GetComponentsInChildren<Renderer>().Length,colliderCount=go.GetComponentsInChildren<Collider>().Length,missingScripts=go.GetComponentsInChildren<Transform>().Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),materials=mats.Select(m=>new{m.name,shader=m.shader.name,supported=m.shader.isSupported,textures=m.GetTexturePropertyNames().Where(p=>m.GetTexture(p)).Select(p=>new{property=p,name=m.GetTexture(p).name,width=m.GetTexture(p).width}).ToArray()}).ToArray(),scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Select(s=>new{s.path,s.isDirty}).ToArray()});
    }
    public static string FrameAndCheckCollision(){
        var t=All().Single(t=>t.name=="WorkersDormitory"&&PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject));
        var sv=SceneView.lastActiveSceneView;
        sv.LookAt(t.position+Vector3.up*5.1f,Quaternion.LookRotation(-t.forward-t.right*.18f+Vector3.down*.13f),14f,false,true);sv.Repaint();
        Physics.SyncTransforms();
        var start=t.TransformPoint(new Vector3(0,1.6f,9f));
        var hits=Physics.RaycastAll(start,-t.forward,8f).Where(h=>h.transform.IsChildOf(t)).OrderBy(h=>h.distance).Select(h=>new{path=PathOf(h.transform),h.distance,point=V(h.point)}).ToArray();
        return Save("collision_check",new{entranceRayHits=hits,colliders=t.GetComponentsInChildren<Collider>().Select(c=>new{c.name,c.enabled,type=c.GetType().Name,c.isTrigger}).ToArray()});
    }
}
