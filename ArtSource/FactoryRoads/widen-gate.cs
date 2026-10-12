var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var gate=all.First(t=>t.name=="FactoryGate");var road=all.First(t=>t.name=="Gate Dirt Road Test");
var names=new[]{"Left_GateLeaf","Right_GateLeaf","Left_Pier","Right_Pier","Left_WingWall","Pillar_044","Pillar Footing_044","Wall_043_V06","Wall_086_V06","Wall Footing_043","Wall Footing_086","Gate Concrete Threshold"};
var targets=all.Where(t=>names.Contains(t.name)&&(t.IsChildOf(gate)||UnityEngine.Vector3.Distance(t.position,gate.position)<20)).ToArray();
var backup=new{transforms=targets.Concat(road.GetComponentsInChildren<UnityEngine.Transform>()).Select(t=>new{name=t.name,id=t.GetInstanceID(),position=new[]{t.localPosition.x,t.localPosition.y,t.localPosition.z},scale=new[]{t.localScale.x,t.localScale.y,t.localScale.z}}).ToArray(),colliders=gate.GetComponents<UnityEngine.BoxCollider>().Select(c=>new{id=c.GetInstanceID(),center=new[]{c.center.x,c.center.y,c.center.z},size=new[]{c.size.x,c.size.y,c.size.z}}).ToArray(),decals=road.GetComponentsInChildren<UnityEngine.Rendering.HighDefinition.DecalProjector>().Select(d=>new{name=d.name,size=new[]{d.size.x,d.size.y,d.size.z},uvScale=new[]{d.uvScale.x,d.uvScale.y},uvBias=new[]{d.uvBias.x,d.uvBias.y}}).ToArray()};
System.IO.File.WriteAllText("ArtSource/FactoryRoads/gate-wide-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(backup,Newtonsoft.Json.Formatting.Indented));
UnityEditor.Undo.IncrementCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("Widen factory gate for two lanes");int group=UnityEditor.Undo.GetCurrentGroup();
float delta=.86f;
foreach(var t in targets){UnityEditor.Undo.RecordObject(t,"Widen factory gate");
if(t.name=="Left_GateLeaf"||t.name=="Right_GateLeaf"){var p=t.localPosition;p.x+=UnityEngine.Mathf.Sign(p.x)*delta;t.localPosition=p;var s=t.localScale;s.x*=3.56f/2.7f;t.localScale=s;}
else if(t.name=="Left_Pier"||t.name=="Left_WingWall"){t.localPosition+=UnityEngine.Vector3.right*delta;}
else if(t.name=="Right_Pier"){t.localPosition-=UnityEngine.Vector3.right*delta;}
else if(t.name=="Pillar_044"||t.name=="Pillar Footing_044"){t.position+=gate.right*delta;}
else if(t.name.StartsWith("Wall_")){var s=t.localScale;s.x-=delta/9;t.localScale=s;}
else if(t.name.StartsWith("Wall Footing_")){bool positive=t.name.EndsWith("043");t.position+=gate.right*(positive?delta*.5f:-delta*.5f);var s=t.localScale;s.x-=delta;t.localScale=s;}
else if(t.name=="Gate Concrete Threshold"){var s=t.localScale;s.x+=2*delta;t.localScale=s;}
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
foreach(var c in gate.GetComponents<UnityEngine.BoxCollider>()){UnityEditor.Undo.RecordObject(c,"Move gate pier collision");var p=c.center;p.x+=UnityEngine.Mathf.Sign(p.x)*delta;c.center=p;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
for(int i=0;i<8;i++){float start=-24+i*6,end=start+6;if(i==3)end=-1.35f;if(i==4)start=1.35f;float min=float.MaxValue,max=float.MinValue;
for(int z=0;z<=4;z++)for(int x=0;x<=4;x++){var p=gate.position+gate.forward*UnityEngine.Mathf.Lerp(start,end,z/4f)+gate.right*(x-2)*2;var h=UnityEngine.Physics.RaycastAll(p+UnityEngine.Vector3.up*50,UnityEngine.Vector3.down,100).Where(h=>h.collider is UnityEngine.TerrainCollider).OrderBy(h=>h.distance).First();min=UnityEngine.Mathf.Min(min,h.point.y);max=UnityEngine.Mathf.Max(max,h.point.y);}
var d=road.Find("Dirt Road "+(i+1)).GetComponent<UnityEngine.Rendering.HighDefinition.DecalProjector>();UnityEditor.Undo.RecordObject(d,"Widen gate road");UnityEditor.Undo.RecordObject(d.transform,"Fit wider road terrain");var center=d.transform.position;center.y=(min+max)*.5f;d.transform.position=center;d.size=new UnityEngine.Vector3(8,end-start,max-min+.4f);d.uvScale=new UnityEngine.Vector2(1,(end-start)/16);d.uvBias=new UnityEngine.Vector2(0,(start+24)/16);}
UnityEngine.Physics.SyncTransforms();UnityEditor.Undo.CollapseUndoOperations(group);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.Selection.activeGameObject=gate.gameObject;
return new{objects=targets.Length,pierShift=delta,hingeSpan=7.12,roadWidth=8,thresholdWidth=9.32};
