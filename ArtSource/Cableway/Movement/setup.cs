var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(!scene.isLoaded || EditorApplication.isPlaying || AnimationMode.InAnimationMode())throw new Exception("Loaded Main scene in edit mode required.");
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var parent=all.Single(t=>t.name=="索道群");
if(parent.GetComponentInChildren<CablewayMovementManager>(true)!=null)throw new Exception("Movement manager already exists.");
var dir="ArtSource/Cableway/Movement/Before";
System.IO.Directory.CreateDirectory(dir);
if(System.IO.File.Exists(dir+"/Main Game Scene.live.unity"))throw new Exception("Backup already exists.");
System.IO.File.Copy(scene.path,dir+"/Main Game Scene.disk.unity");
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,dir+"/Main Game Scene.live.unity",true))throw new Exception("Live backup failed.");
var cables=new List<Vector3[]>();
foreach(string name in new[]{"Cable_0.asset","Cable_1.asset"}) {
    var mf=parent.GetComponentsInChildren<MeshFilter>(true).Single(m=>AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("/"+name));
    var vertices=mf.sharedMesh.vertices;
    if(vertices.Length%8!=0)throw new Exception("Expected eight vertices per rope cross-section.");
    var centers=new List<Vector3>();
    for(int k=0;k<vertices.Length;k+=8){var p=Vector3.zero;for(int j=0;j<8;j++)p+=mf.transform.TransformPoint(vertices[k+j]);centers.Add(p/8);}
    cables.Add(centers.ToArray());
}
if(Vector3.Distance(cables[0].First(),cables[1].First())>.002f || Vector3.Distance(cables[0].Last(),cables[1].Last())>.002f)throw new Exception("Rope routes are not closed at both stations.");
var points=new List<Vector3>();
foreach(var p in cables[0].Concat(cables[1].Reverse()))
    if(points.Count==0 || Vector3.Distance(points.Last(),p)>.001f)points.Add(p);
if(Vector3.Distance(points.First(),points.Last())<.001f)points.RemoveAt(points.Count-1);
var buckets=parent.GetComponentsInChildren<MeshFilter>(true).Where(m=>AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("/CablewayCoalBucket.fbx")).Select(m=>m.transform).OrderBy(t=>AnimationUtility.CalculateTransformPath(t,null)).ToArray();
float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
var before=buckets.Select(t=>new {path=AnimationUtility.CalculateTransformPath(t,null),position=V(t.position),rotation=V(t.eulerAngles),localPosition=V(t.localPosition),localRotation=new[]{t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w},staticFlags=(int)GameObjectUtility.GetStaticEditorFlags(t.gameObject)}).ToArray();
System.IO.File.WriteAllText(dir+"/buckets.json",Newtonsoft.Json.JsonConvert.SerializeObject(before,Newtonsoft.Json.Formatting.Indented));
Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Configure cableway movement");
var root=new GameObject("索道移动管理");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);root.transform.SetParent(parent,false);
Undo.RegisterCreatedObjectUndo(root,"Configure cableway movement");
var manager=Undo.AddComponent<CablewayMovementManager>(root);
var serialized=new SerializedObject(manager);
var route=serialized.FindProperty("routePoints");route.arraySize=points.Count;
for(int i=0;i<points.Count;i++)route.GetArrayElementAtIndex(i).vector3Value=root.transform.InverseTransformPoint(points[i]);
var bindings=serialized.FindProperty("buckets");bindings.arraySize=buckets.Length;
for(int i=0;i<buckets.Length;i++)bindings.GetArrayElementAtIndex(i).objectReferenceValue=buckets[i];
serialized.FindProperty("startAutomatically").boolValue=true;
serialized.FindProperty("speed").floatValue=1f;
serialized.ApplyModifiedProperties();
foreach(var bucket in buckets)foreach(var t in bucket.GetComponentsInChildren<Transform>(true)) {
    Undo.RecordObject(t.gameObject,"Make moving bucket non-static");
    GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
    PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
}
if(!manager.PrepareMovement())throw new Exception("Route preparation failed.");
EditorUtility.SetDirty(manager);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
Selection.activeGameObject=root;
var report=new {hierarchy=AnimationUtility.CalculateTransformPath(root.transform,null),routePoints=points.Count,routeLength=manager.RouteLength,buckets=buckets.Length,speed=manager.Speed,automaticStart=true,saved=!scene.isDirty,editorPreviewRunning=AnimationMode.InAnimationMode()};
System.IO.File.WriteAllText("ArtSource/Cableway/Movement/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
