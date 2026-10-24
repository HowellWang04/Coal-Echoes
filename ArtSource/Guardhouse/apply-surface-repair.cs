if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode before applying the surface repair");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var assetPath="Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/Guardhouse_Rebuilt.glb";
UnityEditor.AssetDatabase.ImportAsset(assetPath,UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);
var house=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).First(t=>t.name=="Guard Post");
UnityEditor.Undo.IncrementCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("Repair guardhouse surfaces and close both doors");
int group=UnityEditor.Undo.GetCurrentGroup();
foreach(var filter in house.GetComponentsInChildren<UnityEngine.MeshFilter>()){
    var collider=filter.GetComponent<UnityEngine.MeshCollider>();
    UnityEditor.Undo.RecordObject(collider,"Refresh guardhouse collision geometry");
    collider.sharedMesh=filter.sharedMesh;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
}
foreach(var hinge in house.GetComponentsInChildren<UnityEngine.Transform>().Where(t=>t.name=="Door_Hinge_Game"||t.name=="RearDoor_Hinge_Game")){
    UnityEditor.Undo.RecordObject(hinge,"Close guardhouse door");
    hinge.localRotation=UnityEngine.Quaternion.identity;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(hinge);
}
UnityEditor.Undo.CollapseUndoOperations(group);
UnityEngine.Physics.SyncTransforms();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneView.RepaintAll();
return new{materials=house.GetComponentsInChildren<UnityEngine.Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Select(m=>new{m.name,shader=m.shader.name,supported=m.shader.isSupported}).ToArray(),doors=house.GetComponentsInChildren<UnityEngine.Transform>().Where(t=>t.name.Contains("Hinge_Game")).Select(t=>new{t.name,angle=t.localEulerAngles.y}).ToArray(),colliders=house.GetComponentsInChildren<UnityEngine.MeshCollider>().Select(c=>new{c.name,mesh=c.sharedMesh.name}).ToArray()};
