var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var factory = scene.GetRootGameObjects().Single(x => x.name == "In-scene Objects").transform.Find("工厂建筑群");
var buildings = factory.Find("01 建筑与构筑物");
var terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene && t.isActiveAndEnabled && t.terrainData != null && AssetDatabase.GetAssetPath(t.terrainData).EndsWith("_Main.asset")).ToArray();
var rows = new Newtonsoft.Json.Linq.JArray();
foreach (Transform category in buildings)
{
    if (category.name == "立柱与支架") continue;
    foreach (Transform building in category)
    {
        var renderers = building.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0) continue;
        var first = building.InverseTransformPoint(renderers[0].bounds.center);
        var local = new Bounds(first, Vector3.zero);
        float bottom = float.PositiveInfinity;
        foreach (var renderer in renderers)
        {
            bottom = Mathf.Min(bottom, renderer.bounds.min.y);
            var b = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                var p = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                local.Encapsulate(building.InverseTransformPoint(renderer.transform.TransformPoint(p)));
            }
        }
        var width = building.TransformVector(new Vector3(local.size.x, 0, 0)).magnitude;
        var depth = building.TransformVector(new Vector3(0, 0, local.size.z)).magnitude;
        int nx = Mathf.Clamp(Mathf.CeilToInt(width / 2f), 2, 100);
        int nz = Mathf.Clamp(Mathf.CeilToInt(depth / 2f), 2, 100);
        float low = float.PositiveInfinity, high = float.NegativeInfinity;
        int missing = 0;
        for (int ix = 0; ix <= nx; ix++)
        for (int iz = 0; iz <= nz; iz++)
        {
            var point = building.TransformPoint(new Vector3(Mathf.Lerp(local.min.x, local.max.x, (float)ix / nx), local.min.y, Mathf.Lerp(local.min.z, local.max.z, (float)iz / nz)));
            var terrain = terrains.FirstOrDefault(t => point.x >= t.transform.position.x && point.x <= t.transform.position.x + t.terrainData.size.x && point.z >= t.transform.position.z && point.z <= t.transform.position.z + t.terrainData.size.z);
            if (terrain == null) { missing++; continue; }
            float height = terrain.SampleHeight(point) + terrain.transform.position.y;
            low = Mathf.Min(low, height);
            high = Mathf.Max(high, height);
        }
        bool eligible = missing == 0 && high - low <= 0.3f;
        rows.Add(Newtonsoft.Json.Linq.JObject.FromObject(new { name = building.name, instanceId = building.gameObject.GetInstanceID(), globalId = GlobalObjectId.GetGlobalObjectIdSlow(building.gameObject).ToString(), path = "In-scene Objects/工厂建筑群/01 建筑与构筑物/" + category.name + "/" + building.name, x = building.position.x, y = building.position.y, z = building.position.z, rotation = new float[] { building.rotation.x, building.rotation.y, building.rotation.z, building.rotation.w }, scale = new float[] { building.localScale.x, building.localScale.y, building.localScale.z }, bottom, groundMin = low, groundMax = high, range = high - low, samples = (nx+1)*(nz+1), missing, eligible, deltaY = eligible ? low - 0.02f - bottom : 0f }));
    }
}
var road = factory.Find("02 道路与围栏/路面与路沿/SM_Road");
var result = new Newtonsoft.Json.Linq.JObject { ["scene"] = scene.path, ["thresholdMetres"] = 0.3, ["sampling"] = "Oriented building bounds sampled at <=2m spacing on loaded Main Terrain; base 2cm below minimum sampled ground", ["buildings"] = rows, ["roadInstanceId"] = road.gameObject.GetInstanceID(), ["roadChildren"] = road.childCount, ["roadRenderers"] = road.GetComponentsInChildren<Renderer>(true).Length };
System.IO.File.WriteAllText("ArtSource/FactoryGrounding/survey.json", result.ToString());
return new { buildings = rows.Count, eligible = rows.Count(r => (bool)r["eligible"]), rows = rows.Select(r => new { name = (string)r["name"], range = (float)r["range"], eligible = (bool)r["eligible"], deltaY = (float)r["deltaY"] }).ToArray() };
