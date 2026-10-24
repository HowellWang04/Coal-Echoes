var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode before scene placement");
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var guard=all.First(t=>t.name=="Guard Post");
var gate=all.First(t=>t.name=="FactoryGate");
var wall=all.First(t=>t.name=="Wall_030_V06");
var footing=all.First(t=>t.name=="Wall Footing_030");
var pillar=all.First(t=>t.name=="Pillar_030");
if(guard.Find("Guardhouse_Rebuilt"))throw new System.Exception("Guardhouse already replaced");
var assetPath="Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/Guardhouse_Rebuilt.glb";
var returnPath="Assets/Art Resources/Environment/Gate Area/Guardhouse_Rebuilt/Guardhouse_WallReturns.glb";
UnityEditor.AssetDatabase.ImportAsset(assetPath,UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);
UnityEditor.AssetDatabase.ImportAsset(returnPath,UnityEditor.ImportAssetOptions.ForceSynchronousImport|UnityEditor.ImportAssetOptions.ForceUpdate);
var houseAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(assetPath);
var returnAsset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(returnPath);
if(!houseAsset||!returnAsset)throw new System.Exception("Missing imported models");
var wallMaterials=wall.GetComponentsInChildren<UnityEngine.Renderer>().SelectMany(r=>r.sharedMaterials).GroupBy(m=>m.name).ToDictionary(g=>g.Key,g=>g.First());
var footingMaterial=footing.GetComponent<UnityEngine.Renderer>().sharedMaterial;
System.Func<UnityEngine.Vector3,float[]> V=v=>new[]{v.x,v.y,v.z};
var before=all.Select(t=>new{id=t.GetInstanceID(),name=t.name,parent=t.parent?t.parent.GetInstanceID():0,position=V(t.localPosition),rotation=new[]{t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w},scale=V(t.localScale),active=t.gameObject.activeSelf}).ToArray();
var allowed=guard.GetComponentsInChildren<UnityEngine.Transform>(true).Concat(wall.GetComponentsInChildren<UnityEngine.Transform>(true)).Concat(footing.GetComponentsInChildren<UnityEngine.Transform>(true)).Select(t=>t.GetInstanceID()).ToArray();
System.IO.File.WriteAllText("Library/GuardhousePlacement/before-transforms.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{allowed,transforms=before}));
UnityEditor.Undo.IncrementCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("Install two-door guardhouse in perimeter");
int undo=UnityEditor.Undo.GetCurrentGroup();
foreach(var t in new[]{guard,wall})if(UnityEditor.PrefabUtility.IsPartOfPrefabInstance(t.gameObject))UnityEditor.PrefabUtility.UnpackPrefabInstance(t.gameObject,UnityEditor.PrefabUnpackMode.OutermostRoot,UnityEditor.InteractionMode.AutomatedAction);
UnityEditor.Undo.RecordObject(guard,"Align guardhouse to perimeter");
float pillarAlong=UnityEngine.Vector3.Dot(pillar.position-gate.position,gate.right);
float houseAlong=pillarAlong-3.9925f;
guard.position=gate.position+gate.right*houseAlong-gate.forward*1.65f;
guard.rotation=UnityEngine.Quaternion.LookRotation(-gate.forward,UnityEngine.Vector3.up);
guard.localScale=UnityEngine.Vector3.one;
foreach(var t in guard.Cast<UnityEngine.Transform>().ToArray())UnityEditor.Undo.DestroyObjectImmediate(t.gameObject);
var model=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(houseAsset,guard);
UnityEditor.Undo.RegisterCreatedObjectUndo(model,"Place new guardhouse");
model.name="Guardhouse_Rebuilt";
model.transform.localPosition=UnityEngine.Vector3.zero;model.transform.localRotation=UnityEngine.Quaternion.identity;model.transform.localScale=UnityEngine.Vector3.one;
foreach(var filter in model.GetComponentsInChildren<UnityEngine.MeshFilter>()){
    var collider=UnityEditor.Undo.AddComponent<UnityEngine.MeshCollider>(filter.gameObject);
    collider.sharedMesh=filter.sharedMesh;
    collider.convex=false;
}
foreach(var hinge in model.GetComponentsInChildren<UnityEngine.Transform>().Where(t=>t.name=="Door_Hinge_Game"||t.name=="RearDoor_Hinge_Game")){
    UnityEditor.Undo.RecordObject(hinge,"Open guardhouse entrance");
    hinge.localRotation=UnityEngine.Quaternion.Euler(0,100,0);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(hinge);
}
UnityEditor.Undo.RecordObject(wall,"Trim wall around guardhouse");
wall.localScale=UnityEngine.Vector3.one;
foreach(var child in wall.Cast<UnityEngine.Transform>().Where(t=>t.name.StartsWith("FactoryWall_")).ToArray())UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
foreach(var c in wall.GetComponents<UnityEngine.Collider>())UnityEditor.Undo.DestroyObjectImmediate(c);
var returns=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(returnAsset,wall);
UnityEditor.Undo.RegisterCreatedObjectUndo(returns,"Place cropped wall returns");
returns.name="Guardhouse Wall Returns";returns.transform.localPosition=UnityEngine.Vector3.zero;returns.transform.localRotation=UnityEngine.Quaternion.identity;returns.transform.localScale=UnityEngine.Vector3.one;
foreach(var r in returns.GetComponentsInChildren<UnityEngine.Renderer>()){
    string key=r.name.Contains("BrickWeathered")?"BrickWeathered":r.name.Contains("Brick")?"Brick":r.name.Contains("Mortar")?"Mortar":"Concrete";
    UnityEditor.Undo.RecordObject(r,"Retain perimeter materials");r.sharedMaterial=wallMaterials[key];
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);
}
foreach(float center in new[]{.76875f,7.21625f}){
    var c=UnityEditor.Undo.AddComponent<UnityEngine.BoxCollider>(wall.gameObject);
    c.center=new UnityEngine.Vector3(center,1.59f,0);c.size=new UnityEngine.Vector3(.8875f,3.18f,.62f);
}
UnityEditor.Undo.RecordObject(footing,"Trim footing around guardhouse");
float footingCenterY=footing.position.y-wall.position.y;
float footingHeight=footing.localScale.y;
footing.position=wall.position;footing.rotation=wall.rotation;footing.localScale=UnityEngine.Vector3.one;
foreach(var c in footing.GetComponents<UnityEngine.Component>().Where(c=>!(c is UnityEngine.Transform)).ToArray())UnityEditor.Undo.DestroyObjectImmediate(c);
foreach(float center in new[]{.76875f,7.21625f}){
    var child=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
    UnityEditor.Undo.RegisterCreatedObjectUndo(child,"Place short wall footing");
    child.name=center<2?"Guardhouse Footing Pillar Side":"Guardhouse Footing Gate Side";
    child.transform.SetParent(footing,false);child.transform.localPosition=new UnityEngine.Vector3(center,footingCenterY,0);child.transform.localScale=new UnityEngine.Vector3(.8875f,footingHeight,.62f);
    child.GetComponent<UnityEngine.Renderer>().sharedMaterial=footingMaterial;
}
UnityEngine.Physics.SyncTransforms();
UnityEditor.Undo.CollapseUndoOperations(undo);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.Selection.activeGameObject=guard.gameObject;
var view=UnityEditor.SceneView.lastActiveSceneView;
view.LookAt(guard.position+UnityEngine.Vector3.up*1.5f,UnityEngine.Quaternion.LookRotation(gate.forward-gate.right*.7f-UnityEngine.Vector3.up*.5f),13,false,true);
view.Repaint();
return new{houseAlong,pillarAlong,position=V(guard.position),frontOutside=true,backInside=true,doorOpeningDegrees=100,wallReturnLengths=new[]{.8875,.8875},houseWidth=5.6,colliders=model.GetComponentsInChildren<UnityEngine.Collider>().Length};
