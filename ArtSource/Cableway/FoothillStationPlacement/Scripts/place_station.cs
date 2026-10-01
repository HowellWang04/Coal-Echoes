var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var parent=all.Single(t=>t.name=="索道群");
if(all.Any(t=>t.name=="CablewayFoothillStation"))throw new System.Exception("Station already exists");
var first=parent.Cast<UnityEngine.Transform>().Single(t=>t.name=="CablewayTower_01");
var second=parent.Cast<UnityEngine.Transform>().Single(t=>t.name=="CablewayTower_02");
var forward=second.position-first.position;forward.y=0;forward.Normalize();var right=UnityEngine.Vector3.Cross(UnityEngine.Vector3.up,forward);
var terrain=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Terrain>(true)).Single(t=>UnityEditor.AssetDatabase.GetAssetPath(t.terrainData)=="Assets/TerrainData/SheepRangeGenerated/Tile_0_-2_Main.asset");
float Ground(UnityEngine.Vector3 p)=>terrain.SampleHeight(p)+terrain.transform.position.y;
float[] V(UnityEngine.Vector3 v)=>new[]{v.x,v.y,v.z};
var root=new UnityEngine.GameObject("CablewayFoothillStation");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);root.transform.SetParent(parent,false);root.transform.position=first.position-forward*23;
UnityEditor.Undo.RegisterCreatedObjectUndo(root,"Place foothill departure station");
var report=new System.Collections.Generic.List<object>();
foreach(var name in new[]{"CablewayStationShell","CablewayStationRollerFrame"}){
 bool shell=name=="CablewayStationShell";var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art Resources/Environment/Cableway/"+name+"/"+name+".fbx");
 var go=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset,scene);go.name=name;go.transform.SetParent(root.transform,true);UnityEditor.Undo.RegisterCreatedObjectUndo(go,"Place foothill station model");
 go.transform.rotation=UnityEngine.Quaternion.LookRotation(shell?-forward:forward,UnityEngine.Vector3.up);go.transform.localScale=UnityEngine.Vector3.one;
 var center=first.position-forward*(shell?23:15)+(shell?right*3:UnityEngine.Vector3.zero);center.y=0;go.transform.position=center;
 var heights=new System.Collections.Generic.List<float>();
 float halfLength=shell?5.66f:1.81f;float halfWidth=shell?1.815f:.44f;
 for(int i=0;i<=24;i++)for(int j=0;j<=8;j++)heights.Add(Ground(center+forward*UnityEngine.Mathf.Lerp(-halfLength,halfLength,i/24f)+right*UnityEngine.Mathf.Lerp(-halfWidth,halfWidth,j/8f)));
 // Only place the existing model. Keep its floor above the highest natural ground.
 center.y=heights.Max()-(shell?.25f:.12f);go.transform.position=center;
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
 var rs=go.GetComponentsInChildren<UnityEngine.Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 report.Add(new{name,position=V(go.transform.position),rotation=V(go.transform.eulerAngles),scale=V(go.transform.lossyScale),boundsMin=V(b.min),boundsMax=V(b.max),groundMin=heights.Min(),groundMax=heights.Max(),groundSpan=heights.Max()-heights.Min(),maximumFoundationGap=b.min.y-heights.Min(),maximumFoundationEmbed=heights.Max()-b.min.y,rendererCount=rs.Length});
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Scene save failed");
System.IO.File.WriteAllText("ArtSource/Cableway/FoothillStationPlacement/placement.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,group=UnityEditor.AnimationUtility.CalculateTransformPath(root.transform,null),models=report,terrainModifiedDuringPlacement=false,cablesCreated=false},Newtonsoft.Json.Formatting.Indented));
UnityEditor.Selection.activeGameObject=root;var view=UnityEditor.SceneView.lastActiveSceneView;view.LookAtDirect(root.transform.position+UnityEngine.Vector3.up*3,UnityEngine.Quaternion.Euler(27,235,0),19);view.Repaint();return report;
