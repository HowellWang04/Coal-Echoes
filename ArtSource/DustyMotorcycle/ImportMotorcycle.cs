// Run this body through Unity CLI eval in the open editor. No runtime scripts are added.
var folder = "Assets/Art Resources/Environment/Gate Area/DustyMotorcycle/";
var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(folder + "Dusty_Red_Motorcycle.fbx");
importer.globalScale = 1;
importer.materialImportMode = UnityEditor.ModelImporterMaterialImportMode.None;
importer.importAnimation = false;
importer.addCollider = false;
importer.importNormals = UnityEditor.ModelImporterNormals.Import;
importer.importTangents = UnityEditor.ModelImporterTangents.CalculateMikk;
importer.SaveAndReimport();
foreach (var name in new[] { "Motorcycle_BaseColor", "Motorcycle_Normal", "Motorcycle_HDRPMask", "Motorcycle_ORM" })
{
    var textureImporter = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(folder + "Textures/" + name + ".png");
    textureImporter.textureType = name == "Motorcycle_Normal" ? UnityEditor.TextureImporterType.NormalMap : UnityEditor.TextureImporterType.Default;
    textureImporter.sRGBTexture = name == "Motorcycle_BaseColor";
    textureImporter.maxTextureSize = 4096;
    textureImporter.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
    textureImporter.mipmapEnabled = true;
    textureImporter.filterMode = UnityEngine.FilterMode.Trilinear;
    textureImporter.anisoLevel = 4;
    textureImporter.SaveAndReimport();
}
var materialPath = folder + "Motorcycle_Dusty_HDRP.mat";
var material = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
if (material == null)
{
    material = new UnityEngine.Material(UnityEngine.Shader.Find("HDRP/Lit"));
    UnityEditor.AssetDatabase.CreateAsset(material, materialPath);
}
material.SetColor("_BaseColor", UnityEngine.Color.white);
material.SetTexture("_BaseColorMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder + "Textures/Motorcycle_BaseColor.png"));
material.SetTexture("_NormalMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder + "Textures/Motorcycle_Normal.png"));
material.SetTexture("_MaskMap", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder + "Textures/Motorcycle_HDRPMask.png"));
material.SetFloat("_NormalScale", 1);
material.SetFloat("_MetallicRemapMin", 0);
material.SetFloat("_MetallicRemapMax", 1);
material.SetFloat("_SmoothnessRemapMin", 0);
material.SetFloat("_SmoothnessRemapMax", 1);
UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(material);
UnityEditor.EditorUtility.SetDirty(material);
UnityEditor.AssetDatabase.SaveAssetIfDirty(material);

var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var root = new UnityEngine.GameObject("Dusty_Red_Motorcycle");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
try
{
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder + "Dusty_Red_Motorcycle.fbx");
    var visual = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source, scene);
    visual.transform.SetParent(root.transform, false);
    visual.name = "Visual";
    // FBX converts Blender's positive X forward to Unity negative X.
    visual.transform.localRotation = UnityEngine.Quaternion.Euler(0, 180, 0) * visual.transform.localRotation;
    foreach (var renderer in visual.GetComponentsInChildren<UnityEngine.Renderer>()) renderer.sharedMaterial = material;
    var centers = new[] {
        new UnityEngine.Vector3(-.18f,.66f,0),
        new UnityEngine.Vector3(.75f,.34f,0),
        new UnityEngine.Vector3(-.68f,.34f,0),
        new UnityEngine.Vector3(-1.055f,1.015f,0),
        new UnityEngine.Vector3(.39f,1.13f,0)
    };
    var sizes = new[] {
        new UnityEngine.Vector3(1.37f,.61f,.37f),
        new UnityEngine.Vector3(.62f,.64f,.15f),
        new UnityEngine.Vector3(.62f,.64f,.17f),
        new UnityEngine.Vector3(.33f,.25f,.37f),
        new UnityEngine.Vector3(.19f,.11f,.78f)
    };
    for (var i = 0; i < centers.Length; i++)
    {
        var collider = root.AddComponent<UnityEngine.BoxCollider>();
        collider.center = centers[i];
        collider.size = sizes[i];
    }
    var diaryAnchor = new UnityEngine.GameObject("DiaryPromptAnchor").transform;
    diaryAnchor.SetParent(root.transform, false);
    diaryAnchor.localPosition = new UnityEngine.Vector3(-.75f, 1.20f, 0);
    var pickup = root.AddComponent<GrandpaDiaryPickup>();
    var pickupSettings = new UnityEditor.SerializedObject(pickup);
    pickupSettings.FindProperty("promptAnchor").objectReferenceValue = diaryAnchor;
    pickupSettings.ApplyModifiedPropertiesWithoutUndo();
    var prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, folder + "Dusty_Red_Motorcycle.prefab");
    var bounds = visual.GetComponentInChildren<UnityEngine.Renderer>().bounds;
    return "Prefab saved " + UnityEditor.AssetDatabase.GetAssetPath(prefab) + " world size " + bounds.size.ToString("F4") + " center " + bounds.center.ToString("F4");
}
finally { UnityEngine.Object.DestroyImmediate(root); }
