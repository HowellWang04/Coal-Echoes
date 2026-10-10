var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if (EditorApplication.isPlaying || !scene.isLoaded) throw new System.Exception("Scene not ready");
var world = scene.GetRootGameObjects().Single(x => x.name == "场景内Object").transform;
var factory = world.Find("工厂建筑群");
var terrains = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Terrain>(true)).Where(t => t.isActiveAndEnabled && AssetDatabase.GetAssetPath(t.terrainData).EndsWith("_Main.asset")).ToArray();
float[] V(Vector3 v) => new[] {v.x, v.y, v.z};
float Ground(Vector3 p) {
    var t = terrains.FirstOrDefault(t => p.x >= t.transform.position.x && p.x <= t.transform.position.x + t.terrainData.size.x && p.z >= t.transform.position.z && p.z <= t.transform.position.z + t.terrainData.size.z);
    return t == null ? float.NaN : t.SampleHeight(p) + t.transform.position.y;
}
var roots = factory.Cast<Transform>().Where(x => !x.name.StartsWith("08")).SelectMany(x => x.Cast<Transform>()).SelectMany(x => x.Cast<Transform>()).Concat(world.Find("工厂大门").Cast<Transform>()).ToArray();
var assemblies = roots.ToDictionary(t=>t.gameObject.GetInstanceID(),t=>new[]{t});
var air = (GameObject)EditorUtility.InstanceIDToObject(101058);
assemblies.Remove(101058);
foreach(Transform t in air.transform) assemblies[t.gameObject.GetInstanceID()]=new[]{t};
var silos = ((GameObject)EditorUtility.InstanceIDToObject(82896)).transform;
assemblies.Remove(82896);
foreach(var group in silos.Cast<Transform>().GroupBy(t=>t.name.Split(new[]{"_SM_"},System.StringSplitOptions.None)[0])) {
    var members=group.ToArray(); var anchor=members.Single(t=>t.name.Contains("HugeSiloBase"));
    assemblies[anchor.gameObject.GetInstanceID()]=members;
}
var rows = new System.Collections.Generic.List<object>();
var cache = new System.Collections.Generic.Dictionary<Mesh, Vector3[]>();
foreach (var assembly in assemblies) {
    var root=((GameObject)EditorUtility.InstanceIDToObject(assembly.Key)).transform;
    var filters = assembly.Value.SelectMany(t=>t.GetComponentsInChildren<MeshFilter>(true)).Where(m => m.sharedMesh != null && m.gameObject.activeInHierarchy && !System.Text.RegularExpressions.Regex.IsMatch(m.name, @"LOD[1-9]", System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToArray();
    if (filters.Length == 0) continue;
    var points = new System.Collections.Generic.List<Vector3>();
    foreach (var m in filters) {
        if (!cache.ContainsKey(m.sharedMesh)) cache[m.sharedMesh] = m.sharedMesh.vertices;
        points.AddRange(cache[m.sharedMesh].Select(v => m.transform.TransformPoint(v)));
    }
    if (points.Count == 0) continue;
    var bounds = new Bounds(points[0], Vector3.zero);
    foreach (var p in points) bounds.Encapsulate(p);
    var bottom = points.Where(p => p.y <= bounds.min.y + 0.12f).GroupBy(p => (Mathf.RoundToInt(p.x * 10), Mathf.RoundToInt(p.z * 10))).Select(g => g.OrderBy(p => p.y).First()).ToArray();
    var contacts = bottom.Select(p => new {point=V(p),ground=Ground(p),offset=Ground(p)-p.y}).ToArray();
    var lowerEnvelope=points.GroupBy(p=>(Mathf.RoundToInt(p.x*5),Mathf.RoundToInt(p.z*5))).Select(g=>g.OrderBy(p=>p.y).First()).ToArray();
    float clearDelta=lowerEnvelope.Max(p=>Ground(p)-p.y);
    var footprint=new System.Collections.Generic.List<float>();
    if(AnimationUtility.CalculateTransformPath(root,null).Contains("厂房") || root.name.Contains("ModulFloor")) {
        var local=new Bounds(root.InverseTransformPoint(points[0]),Vector3.zero);
        foreach(var p in points)local.Encapsulate(root.InverseTransformPoint(p));
        int nx=Mathf.Clamp(Mathf.CeilToInt(bounds.size.x),2,100),nz=Mathf.Clamp(Mathf.CeilToInt(bounds.size.z),2,100);
        for(int x=0;x<=nx;x++)for(int z=0;z<=nz;z++)footprint.Add(Ground(root.TransformPoint(new Vector3(Mathf.Lerp(local.min.x,local.max.x,(float)x/nx),local.min.y,Mathf.Lerp(local.min.z,local.max.z,(float)z/nz)))));
    }
    rows.Add(new {id=root.gameObject.GetInstanceID(),members=assembly.Value.Select(t=>t.gameObject.GetInstanceID()).ToArray(),globalId=GlobalObjectId.GetGlobalObjectIdSlow(root.gameObject).ToString(),path=AnimationUtility.CalculateTransformPath(root,null),name=root.name,position=V(root.position),localPosition=V(root.localPosition),rotation=new[]{root.localRotation.x,root.localRotation.y,root.localRotation.z,root.localRotation.w},scale=V(root.localScale),min=V(bounds.min),max=V(bounds.max),size=V(bounds.size),meshCount=filters.Length,contacts,groundCenter=Ground(bounds.center),clearDelta,footprintMin=footprint.Count==0?0:footprint.Min(),footprintMax=footprint.Count==0?0:footprint.Max()});
}
var transforms = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Select(t => new {id=t.GetInstanceID(),path=AnimationUtility.CalculateTransformPath(t,null),parent=t.parent == null ? 0 : t.parent.GetInstanceID(),position=V(t.localPosition),rotation=new[]{t.localRotation.x,t.localRotation.y,t.localRotation.z,t.localRotation.w},scale=V(t.localScale)}).ToArray();
System.IO.File.WriteAllText("ArtSource/FactoryHeightReview/survey.json", Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
System.IO.File.WriteAllText("ArtSource/FactoryHeightReview/transforms-before.json", Newtonsoft.Json.JsonConvert.SerializeObject(transforms));
return new {objects=rows.Count,transforms=transforms.Length,meshes=cache.Count,sceneDirty=scene.isDirty};
