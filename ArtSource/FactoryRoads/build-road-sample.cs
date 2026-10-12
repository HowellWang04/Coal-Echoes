var assetRoot = "Assets/Art Resources/Environment/FactoryRoads";
var folder = assetRoot + "/Sample48m";
System.IO.Directory.CreateDirectory(folder);
UnityEditor.AssetDatabase.Refresh();
var mask = new UnityEngine.Texture2D(512, 4096, UnityEngine.TextureFormat.RGBA32, false, true);
var pixels = new UnityEngine.Color32[512 * 4096];
for (int y = 0; y < 4096; y++)
{
    float z = y / 4095f * 48;
    for (int x = 0; x < 512; x++)
    {
        float across = x / 511f * 6;
        float leftWidth = .22f + .8f * UnityEngine.Mathf.PerlinNoise(3.7f, z * .21f);
        float rightWidth = .2f + .85f * UnityEngine.Mathf.PerlinNoise(18.1f, z * .19f);
        float noise = (UnityEngine.Mathf.PerlinNoise(across * 6, z * 6) - .5f) * .24f;
        float edge = UnityEngine.Mathf.Max(1 - across / leftWidth, 1 - (6 - across) / rightWidth);
        edge = UnityEngine.Mathf.SmoothStep(0, 1, UnityEngine.Mathf.Clamp01(edge * 1.8f + noise));
        float drift = UnityEngine.Mathf.PerlinNoise(across * .55f + 10, z * .32f);
        float dust = UnityEngine.Mathf.Clamp01((drift - .36f) * .48f);
        float amount = UnityEngine.Mathf.Clamp01(edge + dust);
        pixels[y * 512 + x] = new UnityEngine.Color32((byte)(amount * 255), 0, 0, 255);
    }
}
mask.SetPixels32(pixels); mask.Apply();
System.IO.File.WriteAllBytes(folder + "/DustBlend.png", UnityEngine.ImageConversion.EncodeToPNG(mask));
UnityEngine.Object.DestroyImmediate(mask);
UnityEditor.AssetDatabase.ImportAsset(folder + "/DustBlend.png");
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(folder + "/DustBlend.png");
importer.sRGBTexture = false; importer.wrapMode = UnityEngine.TextureWrapMode.Clamp; importer.maxTextureSize = 4096;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
var original = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(assetRoot + "/Asphalt02/Asphalt02.mat");
var plain = new UnityEngine.Material(original); plain.name = "Asphalt48m Plain";
plain.SetTextureScale("_BaseColorMap", new UnityEngine.Vector2(2, 16));
UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(plain);
UnityEditor.AssetDatabase.CreateAsset(plain, folder + "/Asphalt48m_Plain.mat");
var sandColor = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Art Resources/Environment/Terrain/MyTextures/gravelly_sand_diff_4k.jpg");
var sandNormal = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Art Resources/Environment/Terrain/MyTextures/gravelly_sand_nor_gl_4k.exr");
var road = new UnityEngine.Material(UnityEngine.Shader.Find("HDRP/LayeredLit")); road.name = "Asphalt48m Dust Edges";
road.SetFloat("_LayerCount", 2);
road.SetTexture("_BaseColorMap0", original.GetTexture("_BaseColorMap"));
road.SetTexture("_NormalMap0", original.GetTexture("_NormalMap"));
road.SetTexture("_MaskMap0", original.GetTexture("_MaskMap"));
road.SetFloat("_NormalScale0", .65f);
road.SetTextureScale("_BaseColorMap0", new UnityEngine.Vector2(2, 16));
road.SetTexture("_BaseColorMap1", sandColor); road.SetTexture("_NormalMap1", sandNormal);
road.SetColor("_BaseColor1", new UnityEngine.Color(.86f, .85f, .8f, 1));
road.SetFloat("_NormalScale1", .25f); road.SetFloat("_Smoothness1", .08f);
road.SetTextureScale("_BaseColorMap1", new UnityEngine.Vector2(2, 16));
road.SetTexture("_LayerMaskMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder + "/DustBlend.png"));
UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(road);
UnityEditor.AssetDatabase.CreateAsset(road, folder + "/Asphalt48m_DustEdges.mat");
var groundMat = new UnityEngine.Material(UnityEngine.Shader.Find("HDRP/Lit")); groundMat.name = "Sample Gravel Shoulder";
groundMat.SetTexture("_BaseColorMap", sandColor); groundMat.SetTexture("_NormalMap", sandNormal);
groundMat.SetFloat("_NormalScale", .35f); groundMat.SetFloat("_Smoothness", .08f);
groundMat.SetTextureScale("_BaseColorMap", new UnityEngine.Vector2(4, 18));
UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(groundMat);
UnityEditor.AssetDatabase.CreateAsset(groundMat, folder + "/Shoulder.mat");
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root = new UnityEngine.GameObject("Factory Road Sample 48m");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
try
{
    var surface = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane); surface.name = "Road 6m x 48m";
    surface.transform.SetParent(root.transform, false); surface.transform.localScale = new UnityEngine.Vector3(.6f, 1, 4.8f);
    surface.GetComponent<UnityEngine.Renderer>().sharedMaterial = road;
    var shoulder = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane); shoulder.name = "Gravel Shoulders";
    shoulder.transform.SetParent(root.transform, false); shoulder.transform.localPosition = new UnityEngine.Vector3(0, -.018f, 0);
    shoulder.transform.localScale = new UnityEngine.Vector3(1.2f, 1, 5.4f); shoulder.GetComponent<UnityEngine.Renderer>().sharedMaterial = groundMat;
    var details = new UnityEngine.GameObject("Cracks and Repairs"); details.transform.SetParent(root.transform, false);
    var random = new System.Random(4811);
    string[] repairs = {"G87 Patch Asphalt 2x1 Irregular", "G87 Patch Asphalt 2x2 Irregular", "G87 Patch Asphalt 2x3", "G87 Patch Asphalt 3x1 Irregular", "G87 Patch Asphalt 2x1 New Cut", "G87 Patch Asphalt 2x4 Irregular"};
    string[] cracks = {"Crack - Medium 1", "Crack - Medium 2", "Crack - Medium 3", "Cracks - XL", "Damage - Large 1", "Damage - Large 3"};
    for (int i = 0; i < 32; i++)
    {
        bool repair = i < 12;
        string name = repair ? repairs[i % repairs.Length] : cracks[(i - 12) % cracks.Length];
        string path = assetRoot + (repair ? "/G87RoadRepair/CustomDecals/RoadAssets/" : "/SullyCracksDamage/CustomDecals/RoadAssets/") + name + "/Decal.prefab";
        var obj = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path), preview);
        obj.transform.SetParent(details.transform, false);
        float z = repair ? -21f + i * 3.8f + (float)random.NextDouble() : -22f + (i - 12) * 2.25f + (float)random.NextDouble();
        obj.transform.localPosition = new UnityEngine.Vector3((float)random.NextDouble() * 3.2f - 1.6f, .035f, z);
        obj.transform.localRotation = UnityEngine.Quaternion.Euler(90, 0, (float)random.NextDouble() * (repair ? 25 : 360));
        var projector = obj.GetComponent<UnityEngine.Rendering.HighDefinition.DecalProjector>();
        float scale = repair ? .55f + (float)random.NextDouble() * .35f : .45f + (float)random.NextDouble() * .35f;
        projector.size = new UnityEngine.Vector3(UnityEngine.Mathf.Min(projector.size.x * scale, 3.4f), UnityEngine.Mathf.Min(projector.size.y * scale, 4.5f), .35f);
        projector.fadeFactor = repair ? .82f : .7f;
    }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, folder + "/FactoryRoad48m.prefab");
}
finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview); }
UnityEditor.AssetDatabase.SaveAssets();
return new { prefab = folder + "/FactoryRoad48m.prefab", width = 6, length = 48, decals = 32, sceneDirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty };
