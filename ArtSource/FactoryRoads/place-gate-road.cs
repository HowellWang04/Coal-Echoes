var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var gate=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).First(t=>t.name=="FactoryGate");
var root=new UnityEngine.GameObject("Gate Dirt Road Test");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);UnityEditor.Undo.RegisterCreatedObjectUndo(root,"Place gate dirt road");
root.transform.position=gate.position;root.transform.rotation=UnityEngine.Quaternion.Euler(0,gate.eulerAngles.y,0);
var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art Resources/Environment/FactoryRoads/DirtSample48m/DirtRoad.mat");
var record=new System.Collections.Generic.List<object>();
for(int i=0;i<8;i++){float start=-24+i*6,end=start+6;if(i==3)end=-1.35f;if(i==4)start=1.35f;float minimum=float.MaxValue,maximum=float.MinValue;
for(int z=0;z<=4;z++)for(int x=0;x<=2;x++){var p=gate.position+gate.forward*UnityEngine.Mathf.Lerp(start,end,z/4f)+gate.right*(x-1)*3;var hits=UnityEngine.Physics.RaycastAll(p+UnityEngine.Vector3.up*50,UnityEngine.Vector3.down,100);var terrain=hits.Where(h=>h.collider is UnityEngine.TerrainCollider).OrderBy(h=>h.distance).First();minimum=UnityEngine.Mathf.Min(minimum,terrain.point.y);maximum=UnityEngine.Mathf.Max(maximum,terrain.point.y);}
var obj=new UnityEngine.GameObject("Dirt Road "+(i+1));obj.transform.SetParent(root.transform,false);
var center=gate.position+gate.forward*((start+end)*.5f);center.y=(minimum+maximum)*.5f;obj.transform.position=center;obj.transform.rotation=root.transform.rotation*UnityEngine.Quaternion.Euler(90,0,0);
var dp=obj.AddComponent<UnityEngine.Rendering.HighDefinition.DecalProjector>();dp.material=mat;dp.size=new UnityEngine.Vector3(6,end-start,maximum-minimum+.4f);dp.uvScale=new UnityEngine.Vector2(1,(end-start)/12);dp.uvBias=new UnityEngine.Vector2(0,(start+24)/12);dp.drawDistance=180;dp.startAngleFade=40;dp.endAngleFade=75;
record.Add(new{start=start,end=end,minY=minimum,maxY=maximum,depth=dp.size.z,uvV=dp.uvBias.y,uvScaleV=dp.uvScale.y});}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.Selection.activeGameObject=root;
System.IO.File.WriteAllText("ArtSource/FactoryRoads/gate-road-placement.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{gate=new[]{gate.position.x,gate.position.y,gate.position.z},yaw=gate.eulerAngles.y,width=6,length=48,thresholdGap=2.7,segments=record},Newtonsoft.Json.Formatting.Indented));
return new{root=root.GetInstanceID(),segments=8,terrainChanged=false};
