var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Transform>(true)).ToArray();
var house=all.First(t=>t.name=="Guard Post");
var hinges=house.GetComponentsInChildren<UnityEngine.Transform>().Where(t=>t.name.Contains("Hinge_Game")).ToArray();
UnityEngine.Physics.SyncTransforms();
var closedHits=new System.Collections.Generic.List<string>();
foreach(float sign in new[]{-1f,1f}){
    var origin=house.TransformPoint(new UnityEngine.Vector3(-1.4f,1.2f,sign*3.2f));
    var hits=UnityEngine.Physics.RaycastAll(origin,house.TransformDirection(new UnityEngine.Vector3(0,0,-sign)),1.5f,~0,UnityEngine.QueryTriggerInteraction.Ignore);
    closedHits.AddRange(hits.Where(h=>h.collider.transform.IsChildOf(house)&&h.collider.name.Contains("DoorLeaf")).Select(h=>h.collider.name));
}
var rotations=hinges.Select(t=>t.localRotation).ToArray();
var blocked=new System.Collections.Generic.List<float>();
try{
    foreach(var hinge in hinges)hinge.localRotation=UnityEngine.Quaternion.Euler(0,100,0);
    UnityEngine.Physics.SyncTransforms();
    for(int i=0;i<=80;i++){
        float z=UnityEngine.Mathf.Lerp(-4,4,i/80f);
        var low=house.TransformPoint(new UnityEngine.Vector3(-1.4f,.53f,z));
        var high=house.TransformPoint(new UnityEngine.Vector3(-1.4f,1.73f,z));
        if(UnityEngine.Physics.OverlapCapsule(low,high,.3f,~0,UnityEngine.QueryTriggerInteraction.Ignore).Any(c=>c.enabled))blocked.Add(z);
    }
}finally{
    for(int i=0;i<hinges.Length;i++)hinges[i].localRotation=rotations[i];
    UnityEngine.Physics.SyncTransforms();
}
var current=all.ToDictionary(t=>t.GetInstanceID());
var baseline=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("Library/GuardhousePlacement/before-surfaces.json"));
var changed=new System.Collections.Generic.List<string>();
foreach(var old in baseline){
    UnityEngine.Transform t;
    if(!current.TryGetValue((int)old["id"],out t)){changed.Add((string)old["name"]+" removed");continue;}
    var p=old["position"].Values<float>().ToArray();var q=old["rotation"].Values<float>().ToArray();var s=old["scale"].Values<float>().ToArray();
    if(UnityEngine.Vector3.Distance(t.localPosition,new UnityEngine.Vector3(p[0],p[1],p[2]))>.00001f||UnityEngine.Mathf.Abs(t.localRotation.x-q[0])+UnityEngine.Mathf.Abs(t.localRotation.y-q[1])+UnityEngine.Mathf.Abs(t.localRotation.z-q[2])+UnityEngine.Mathf.Abs(t.localRotation.w-q[3])>.00001f||UnityEngine.Vector3.Distance(t.localScale,new UnityEngine.Vector3(s[0],s[1],s[2]))>.00001f)changed.Add(t.name);
}
bool collisionMatches=house.GetComponentsInChildren<UnityEngine.MeshFilter>().All(f=>f.GetComponent<UnityEngine.MeshCollider>().sharedMesh==f.sharedMesh);
bool bothClosed=hinges.Length==2&&hinges.All(t=>UnityEngine.Quaternion.Angle(t.localRotation,UnityEngine.Quaternion.identity)<.001f);
var result=new{bothClosed,closedDoorCollision=closedHits.Distinct().ToArray(),openedCorridorSamples=81,blocked,collisionMatches,unrelatedChecked=baseline.Count,unrelatedChanged=changed};
System.IO.File.WriteAllText("Library/GuardhousePlacement/surface-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
// Report concurrent scene edits separately; this script never writes to them.
if(!bothClosed||closedHits.Distinct().Count()!=2||!collisionMatches||blocked.Count!=0)throw new System.Exception(Newtonsoft.Json.JsonConvert.SerializeObject(result));
return result;
