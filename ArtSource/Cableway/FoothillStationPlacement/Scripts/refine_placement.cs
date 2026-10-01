var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var parent=all.Single(t=>t.name=="索道群");var root=all.Single(t=>t.name=="CablewayFoothillStation");
var first=parent.Cast<UnityEngine.Transform>().Single(t=>t.name=="CablewayTower_01");var second=parent.Cast<UnityEngine.Transform>().Single(t=>t.name=="CablewayTower_02");
var forward=second.position-first.position;forward.y=0;forward.Normalize();var right=UnityEngine.Vector3.Cross(UnityEngine.Vector3.up,forward);
var terrain=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Terrain>(true)).Single(t=>UnityEditor.AssetDatabase.GetAssetPath(t.terrainData)=="Assets/TerrainData/SheepRangeGenerated/Tile_0_-2_Main.asset");
float Ground(UnityEngine.Vector3 p)=>terrain.SampleHeight(p)+terrain.transform.position.y;
float[] V(UnityEngine.Vector3 v)=>new[]{v.x,v.y,v.z};
var shell=root.Cast<UnityEngine.Transform>().Single(t=>t.name=="CablewayStationShell");UnityEditor.Undo.RecordObject(shell,"Fit station beside the existing tower footing");
var center=first.position-forward*16+right*3;var heights=new System.Collections.Generic.List<float>();
for(int i=0;i<=24;i++)for(int j=0;j<=8;j++)heights.Add(Ground(center+forward*UnityEngine.Mathf.Lerp(-5.66f,5.66f,i/24f)+right*UnityEngine.Mathf.Lerp(-1.815f,1.815f,j/8f)));
center.y=heights.Max()-.25f;shell.position=center;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(shell);
var report=new System.Collections.Generic.List<object>();
foreach(UnityEngine.Transform tr in root){
 bool isShell=tr==shell;var hh=new System.Collections.Generic.List<float>();
 float halfLength=isShell?5.66f:1.81f;float halfWidth=isShell?1.815f:.44f;
 for(int i=0;i<=24;i++)for(int j=0;j<=8;j++)hh.Add(Ground(tr.position+forward*UnityEngine.Mathf.Lerp(-halfLength,halfLength,i/24f)+right*UnityEngine.Mathf.Lerp(-halfWidth,halfWidth,j/8f)));
 var rr=tr.GetComponentsInChildren<UnityEngine.Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
 var intersections=rr.SelectMany(r=>first.GetComponentsInChildren<UnityEngine.Renderer>().Where(t=>r.bounds.Intersects(t.bounds)).Select(t=>r.name+" / "+t.name)).ToArray();
 report.Add(new{name=tr.name,position=V(tr.position),rotation=V(tr.eulerAngles),scale=V(tr.lossyScale),boundsMin=V(b.min),boundsMax=V(b.max),groundMin=hh.Min(),groundMax=hh.Max(),groundSpan=hh.Max()-hh.Min(),maximumFoundationGap=b.min.y-hh.Min(),maximumFoundationEmbed=hh.Max()-b.min.y,firstTowerRendererBoundsIntersections=intersections,rendererCount=rr.Length});
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText("ArtSource/Cableway/FoothillStationPlacement/placement.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,group=UnityEditor.AnimationUtility.CalculateTransformPath(root,null),models=report,terrainModifiedDuringPlacement=false,cablesCreated=false},Newtonsoft.Json.Formatting.Indented));
var view=UnityEditor.SceneView.lastActiveSceneView;view.LookAtDirect(shell.position+UnityEngine.Vector3.up*2,UnityEngine.Quaternion.Euler(22,60,0),14);view.Repaint();return report;

