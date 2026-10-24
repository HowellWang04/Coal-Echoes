var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var wall=all.First(t=>t.name=="Wall_031_V06");var footing=all.First(t=>t.name=="Wall Footing_031");
var materials=wall.GetComponentsInChildren<UnityEngine.Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToDictionary(m=>m.name);
var path="Assets/Art Resources/Environment/Gate Area/FactoryBoundary/Models/FactoryWall_GateShort.glb";
UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
if(!prefab)throw new System.Exception("Missing trimmed wall");
var allowed=Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.HashSet<int>>(System.IO.File.ReadAllText("Library/RoadGateAlignment/allowed.json"));
foreach(var t in wall.GetComponentsInChildren<UnityEngine.Transform>(true))allowed.Add(t.GetInstanceID());allowed.Add(footing.GetInstanceID());
UnityEditor.Undo.IncrementCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Shorten wall beside widened gate");int undo=UnityEditor.Undo.GetCurrentGroup();
if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(wall.gameObject))UnityEditor.PrefabUtility.UnpackPrefabInstance(wall.gameObject,UnityEditor.PrefabUnpackMode.OutermostRoot,UnityEditor.InteractionMode.AutomatedAction);
UnityEditor.Undo.RecordObject(wall,"Use baked wall dimensions");wall.localScale=UnityEngine.Vector3.one;
foreach(var child in wall.Cast<UnityEngine.Transform>().Where(t=>t.name.StartsWith("FactoryWall_")).ToArray())UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
var model=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,wall);
UnityEditor.Undo.RegisterCreatedObjectUndo(model,"Install shortened entrance wall");model.name="FactoryWall_GateShort";
foreach(var renderer in model.GetComponentsInChildren<UnityEngine.Renderer>()){
    var key=renderer.name.Contains("BrickWeathered")?"BrickWeathered":renderer.name.Contains("Brick")?"Brick":renderer.name.Contains("Mortar")?"Mortar":"Concrete";
    renderer.sharedMaterial=materials[key];UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
}
foreach(var c in wall.GetComponents<UnityEngine.BoxCollider>()){
    UnityEditor.Undo.RecordObject(c,"Shorten wall collision");var p=c.center;p.x=7.38f;c.center=p;var size=c.size;size.x=1.8f;c.size=size;
}
var start=wall.Find("Snap_Start");var end=wall.Find("Snap_End");
if(start){UnityEditor.Undo.RecordObject(start,"Update shortened wall snap");start.localPosition=new UnityEngine.Vector3(6.12f,0,0);}
if(end){UnityEditor.Undo.RecordObject(end,"Update shortened wall snap");end.localPosition=new UnityEngine.Vector3(8.586f,0,0);}
UnityEditor.Undo.RecordObject(footing,"Shorten gate-side wall footing");
var fp=wall.TransformPoint(new UnityEngine.Vector3(7.38f,0,0));fp.y=footing.position.y;footing.position=fp;
var fs=footing.localScale;fs.x=1.8f;footing.localScale=fs;
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(footing);
System.IO.File.WriteAllText("Library/RoadGateAlignment/allowed.json",Newtonsoft.Json.JsonConvert.SerializeObject(allowed));
UnityEditor.Undo.CollapseUndoOperations(undo);UnityEngine.Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return new{wall=wall.name,length=1.8,materials=model.GetComponentsInChildren<UnityEngine.Renderer>().Select(r=>r.sharedMaterial.name).ToArray()};
