if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
UnityEditor.AssetDatabase.ImportAsset("Assets/Art Resources/Environment/WorkersDormitory/WorkersDormitory.glb",UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var dorm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Transform>(true)).First(t=>t.name=="WorkersDormitory");
foreach(var filter in dorm.GetComponentsInChildren<UnityEngine.MeshFilter>()){
    if(filter.name.Contains("Runoff")||filter.name=="SlopeFoundation")continue;
    var collider=filter.GetComponent<UnityEngine.MeshCollider>();
    if(collider==null)collider=UnityEditor.Undo.AddComponent<UnityEngine.MeshCollider>(filter.gameObject);
    UnityEditor.Undo.RecordObject(collider,"Update dormitory rear entrance collision");
    collider.sharedMesh=filter.sharedMesh;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
}
var leaf=dorm.GetComponentsInChildren<UnityEngine.Transform>().First(t=>t.name=="Dorm_RearDoorLeaf");
leaf.gameObject.layer=dorm.gameObject.layer;
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(leaf.gameObject);
var hinge=leaf.parent;
var checks=new System.Collections.Generic.List<object>();
try{
    foreach(float angle in new[]{0f,95f}){
        hinge.localRotation=UnityEngine.Quaternion.Euler(0,angle,0);
        UnityEngine.Physics.SyncTransforms();
        var blocked=new System.Collections.Generic.List<object>();
        for(int i=0;i<=13;i++){
            float z=-5.8f+i*.2f;
            var a=dorm.TransformPoint(new UnityEngine.Vector3(0,.84f,z));
            var b=dorm.TransformPoint(new UnityEngine.Vector3(0,2.04f,z));
            var hits=UnityEngine.Physics.OverlapCapsule(a,b,.29f,~0,UnityEngine.QueryTriggerInteraction.Ignore);
            if(hits.Length>0)blocked.Add(new{z,hits=hits.Select(c=>c.name).Distinct().ToArray()});
        }
        checks.Add(new{angle,blocked});
        if(angle>0&&blocked.Count>0)throw new System.Exception("Open doorway clearance is obstructed");
    }
}finally{
    hinge.localRotation=UnityEngine.Quaternion.identity;
    UnityEngine.Physics.SyncTransforms();
}
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(hinge);
var aging=UnityEngine.Object.FindFirstObjectByType<DormitoryMaterialAging>(UnityEngine.FindObjectsInactive.Include);
var pairs=new UnityEditor.SerializedObject(aging).FindProperty("materials");
var used=dorm.GetComponentsInChildren<UnityEngine.Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
for(int i=0;i<pairs.arraySize;i++){
    var p=pairs.GetArrayElementAtIndex(i);
    if(!used.Contains(p.FindPropertyRelative("aged").objectReferenceValue)||p.FindPropertyRelative("clean").objectReferenceValue==null)
        throw new System.Exception("Existing aging material reference is missing");
}
var view=UnityEditor.SceneView.lastActiveSceneView;
var target=dorm.TransformPoint(new UnityEngine.Vector3(0,1.7f,-4.8f));
var eye=dorm.TransformPoint(new UnityEngine.Vector3(-3,2.6f,-5.7f));
view.LookAt(target,UnityEngine.Quaternion.LookRotation(target-eye),UnityEngine.Vector3.Distance(target,eye)*UnityEngine.Mathf.Sin(view.camera.fieldOfView*UnityEngine.Mathf.Deg2Rad*.5f),false,true);
view.drawGizmos=false;
UnityEditor.Selection.activeObject=null;
view.Repaint();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
var report=new{checks,materialPairs=pairs.arraySize,connected=UnityEditor.PrefabUtility.GetPrefabInstanceStatus(dorm.gameObject).ToString(),saved=!scene.isDirty,defaultClosed=hinge.localRotation==UnityEngine.Quaternion.identity};
System.IO.File.WriteAllText("ArtSource/WorkersDormitory/rear_door_scene_checks.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
