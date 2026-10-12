var folder = "Assets/Art Resources/Environment/FactoryRoads/Sample48m";
var road = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(folder + "/Asphalt48m_DustEdges.mat");
var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art Resources/Environment/FactoryRoads/Asphalt02/Asphalt02.mat");
road.SetFloat("_LayerCount", 3);
road.SetTexture("_BaseColorMap2", road.GetTexture("_BaseColorMap1"));
road.SetTexture("_NormalMap2", road.GetTexture("_NormalMap1"));
road.SetColor("_BaseColor2", road.GetColor("_BaseColor1"));
road.SetFloat("_NormalScale2", .25f); road.SetFloat("_Smoothness2", .08f);
road.SetTextureScale("_BaseColorMap2", new UnityEngine.Vector2(2, 16));
road.SetTexture("_BaseColorMap1", source.GetTexture("_BaseColorMap"));
road.SetTexture("_NormalMap1", source.GetTexture("_NormalMap"));
road.SetTexture("_MaskMap1", source.GetTexture("_MaskMap"));
road.SetColor("_BaseColor1", UnityEngine.Color.white); road.SetFloat("_NormalScale1", .65f);
road.SetFloat("_UVBase1", 1); road.SetVector("_UVMappingMask1", new UnityEngine.Vector4(0, 1, 0, 0));
UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(road);
UnityEditor.EditorUtility.SetDirty(road);
var mask = new UnityEngine.Texture2D(512, 4096, UnityEngine.TextureFormat.RGBA32, false, true);
var pixels = new UnityEngine.Color32[512 * 4096];
for (int y = 0; y < 4096; y++)
{
    float z = y / 4095f * 48;
    for (int x = 0; x < 512; x++)
    {
        float across = x / 511f * 6;
        float leftWidth = .25f + 1.1f * UnityEngine.Mathf.PerlinNoise(3.7f, z * .21f);
        float rightWidth = .25f + 1.05f * UnityEngine.Mathf.PerlinNoise(18.1f, z * .19f);
        float noise = (UnityEngine.Mathf.PerlinNoise(across * 6, z * 6) - .5f) * .3f;
        float edge = UnityEngine.Mathf.Max(1 - across / leftWidth, 1 - (6 - across) / rightWidth);
        edge = UnityEngine.Mathf.SmoothStep(0, 1, UnityEngine.Mathf.Clamp01(edge * 1.8f + noise));
        float drift = UnityEngine.Mathf.PerlinNoise(across * .55f + 10, z * .32f);
        float dust = UnityEngine.Mathf.Clamp01((drift - .36f) * .6f);
        float blend = UnityEngine.Mathf.SmoothStep(0, 1, UnityEngine.Mathf.Clamp01((UnityEngine.Mathf.PerlinNoise(across * .36f + 5, z * .26f) - .35f) * 3.4f));
        pixels[y * 512 + x] = new UnityEngine.Color32((byte)(blend * 255), (byte)(UnityEngine.Mathf.Clamp01(edge + dust) * 255), 0, 255);
    }
}
mask.SetPixels32(pixels); mask.Apply();
System.IO.File.WriteAllBytes(folder + "/DustBlend.png", UnityEngine.ImageConversion.EncodeToPNG(mask));
UnityEngine.Object.DestroyImmediate(mask); UnityEditor.AssetDatabase.ImportAsset(folder + "/DustBlend.png");
var root = UnityEditor.PrefabUtility.LoadPrefabContents(folder + "/FactoryRoad48m.prefab");
try
{
    var filter = root.transform.Find("Road 6m x 48m").GetComponent<UnityEngine.MeshFilter>();
    var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh); mesh.name = "Road UV Blend";
    mesh.uv2 = mesh.uv.Select(uv => new UnityEngine.Vector2(uv.y * 8 + .173f, uv.x / 8 + .021f)).ToArray();
    UnityEditor.AssetDatabase.CreateAsset(mesh, folder + "/RoadUVBlend.asset"); filter.sharedMesh = mesh;
    root.transform.Find("Road 6m x 48m").GetComponent<UnityEngine.MeshCollider>().sharedMesh = mesh;
    float[] positions = {-20.8f, -17.5f, -12.1f, -11f, -4.7f, -1.4f, 5.2f, 7.4f, 13.8f, 16, 16.7f, 21.2f};
    var details = root.transform.Find("Cracks and Repairs");
    for (int i = 0; i < 12; i++) { var item = details.GetChild(i); var position = item.localPosition; position.z = positions[i]; item.localPosition = position; }
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, folder + "/FactoryRoad48m.prefab");
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
UnityEditor.AssetDatabase.SaveAssets();
return new { layers = road.GetFloat("_LayerCount"), meshIsText = System.IO.File.ReadAllText(folder + "/RoadUVBlend.asset").StartsWith("%YAML") };
