var dir = "Assets/Art Resources/Environment/Cableway/Tajia01_Rebuilt";
var report = new System.Collections.Generic.List<object>();
foreach (var item in new[] { "Tower", "CoalBucket" })
{
    foreach (var map in new[] { "BaseColor", "Normal", "HDRP_Mask" })
    {
        var path = dir + "/Textures/" + item + "_" + map + ".png";
        var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
        importer.textureType = map == "Normal" ? UnityEditor.TextureImporterType.NormalMap : UnityEditor.TextureImporterType.Default;
        importer.sRGBTexture = map == "BaseColor";
        importer.maxTextureSize = 4096;
        importer.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
        importer.mipmapEnabled = true;
        importer.alphaSource = map == "HDRP_Mask" ? UnityEditor.TextureImporterAlphaSource.FromInput : UnityEditor.TextureImporterAlphaSource.None;
        importer.SaveAndReimport();
    }
    var matPath = dir + "/Materials/Tajia01_" + item + ".mat";
    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(matPath);
    if (mat == null)
    {
        mat = new UnityEngine.Material(UnityEngine.Shader.Find("HDRP/Lit"));
        UnityEditor.AssetDatabase.CreateAsset(mat, matPath);
    }
    var oldTower = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/Art Resources/Environment/Cableway/Material/Material.007.mat");
    mat.SetColor("_BaseColor", item == "Tower" ? oldTower.GetColor("_BaseColor") : UnityEngine.Color.white);
    mat.SetTexture("_BaseColorMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(dir + "/Textures/" + item + "_BaseColor.png"));
    mat.SetTexture("_NormalMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(dir + "/Textures/" + item + "_Normal.png"));
    mat.SetTexture("_MaskMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(dir + "/Textures/" + item + "_HDRP_Mask.png"));
    mat.SetFloat("_NormalScale", .45f);
    mat.SetFloat("_MetallicRemapMin", 0f);
    mat.SetFloat("_MetallicRemapMax", item == "Tower" ? .414f : 1f);
    mat.SetFloat("_SmoothnessRemapMin", 0f);
    mat.SetFloat("_SmoothnessRemapMax", item == "Tower" ? 0f : 1f);
    mat.SetFloat("_AORemapMin", 0f);
    mat.SetFloat("_AORemapMax", 1f);
    UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(mat);
    UnityEditor.EditorUtility.SetDirty(mat);
    var fbx = dir + "/Tajia01_" + item + "_v01.fbx";
    var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(fbx);
    var sourceMaterial = model.GetComponentInChildren<UnityEngine.Renderer>().sharedMaterial;
    var mi = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
    mi.importAnimation = false;
    mi.importCameras = false;
    mi.importLights = false;
    mi.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material), sourceMaterial.name), mat);
    mi.SaveAndReimport();
    report.Add(new { item, material = matPath, shader = mat.shader.name, keywords = mat.shaderKeywords });
}
UnityEditor.AssetDatabase.SaveAssets();
return Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
