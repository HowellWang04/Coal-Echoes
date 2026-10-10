var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var before=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/FactoryHeightReview/transforms-before.json")).ToDictionary(r=>(int)r["id"]);
var plan=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/FactoryHeightReview/plan.json"));
var expected=plan.SelectMany(r=>r["members"].Select(id=>((GameObject)EditorUtility.InstanceIDToObject((int)id)).transform.GetInstanceID())).ToHashSet();
var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var unexpected=new System.Collections.Generic.List<string>();int changed=0;
foreach(var t in all) {
    var b=before[t.GetInstanceID()];var p=b["position"];var q=b["rotation"];var s=b["scale"];
    bool y=t.localPosition.y!=(float)p[1];if(y)changed++;
    if(t.localPosition.x!=(float)p[0]||t.localPosition.z!=(float)p[2]||(t.localRotation.x!=(float)q[0]||t.localRotation.y!=(float)q[1]||t.localRotation.z!=(float)q[2]||t.localRotation.w!=(float)q[3])||t.localScale!=new Vector3((float)s[0],(float)s[1],(float)s[2])||(t.parent==null?0:t.parent.GetInstanceID())!=(int)b["parent"]||(y&&!expected.Contains(t.GetInstanceID())))unexpected.Add(AnimationUtility.CalculateTransformPath(t,null));
}
var terrains=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Where(t=>t.isActiveAndEnabled&&AssetDatabase.GetAssetPath(t.terrainData).EndsWith("_Main.asset")).ToArray();
var survey=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/FactoryHeightReview/survey.json"));
float terrainError=0,positionError=0;int samples=0;
foreach(var r in survey) foreach(var c in r["contacts"]) {
    var p=c["point"];var v=new Vector3((float)p[0],(float)p[1],(float)p[2]);
    var terrain=terrains.First(t=>v.x>=t.transform.position.x&&v.x<=t.transform.position.x+t.terrainData.size.x&&v.z>=t.transform.position.z&&v.z<=t.transform.position.z+t.terrainData.size.z);
    terrainError=Mathf.Max(terrainError,Mathf.Abs(terrain.SampleHeight(v)+terrain.transform.position.y-(float)c["ground"]));samples++;
}
foreach(var r in plan) {var t=((GameObject)EditorUtility.InstanceIDToObject((int)r["id"])).transform;var p=r["before"];positionError=Mathf.Max(positionError,Mathf.Abs(t.position.y-(float)p[1]-(float)r["deltaY"]));}
var result=new {scene=scene.path,objects=plan.Count,buildingCount=26,beforeTransforms=before.Count,afterTransforms=all.Length,changedTransforms=changed,expectedTransforms=expected.Count,unexpectedChanges=unexpected,terrainSamples=samples,maxTerrainHeightChange=terrainError,maxPlacementError=positionError,dirtyTerrainAssets=terrains.Count(t=>EditorUtility.IsDirty(t.terrainData)),compilationFailed=EditorUtility.scriptCompilationFailed,playModeTested=false};
System.IO.File.WriteAllText("ArtSource/FactoryHeightReview/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
