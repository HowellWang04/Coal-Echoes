// Method body executed through Unity MCP execute_code. Imports the guardhouse desk and chair,
// creates HDRP/Lit materials and saves prefabs with box colliders, without touching any scene.
const string Folder = "Assets/Art Resources/Props/GuardhouseFurniture";
if (EditorApplication.isPlaying) return "Exit Play Mode first.";
AssetDatabase.Refresh();
foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[]{Folder + "/Textures"}))
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
    string name = System.IO.Path.GetFileNameWithoutExtension(path);
    importer.textureType = name.EndsWith("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
    importer.sRGBTexture = name.EndsWith("_BaseColor");
    importer.alphaSource = TextureImporterAlphaSource.FromInput;
    importer.maxTextureSize = 2048;
    importer.wrapMode = TextureWrapMode.Clamp;
    importer.SaveAndReimport();
}
var result = new System.Collections.Generic.List<object>();
foreach (var piece in new[]{"GuardDesk", "GuardChair"})
{
    string modelPath = Folder + "/Models/" + piece + ".fbx";
    var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
    modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
    modelImporter.SaveAndReimport();

    string matPath = Folder + "/Materials/" + piece + "_PaintedWood.mat";
    var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
    if (m == null) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, matPath); }
    System.Func<string, Texture2D> tex = n => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/" + piece + "_" + n + ".png");
    m.SetColor("_BaseColor", Color.white);
    m.SetTexture("_BaseColorMap", tex("BaseColor"));
    m.SetTexture("_NormalMap", tex("Normal")); m.SetFloat("_NormalScale", 1f);
    m.SetTexture("_MaskMap", tex("HDRP_Mask"));
    m.SetFloat("_MetallicRemapMin", 0); m.SetFloat("_MetallicRemapMax", 1);
    m.SetFloat("_SmoothnessRemapMin", 0); m.SetFloat("_SmoothnessRemapMax", 1);
    UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
    EditorUtility.SetDirty(m);

    var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
    var root = new GameObject(piece);
    var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
    model.name = "Model";
    model.transform.SetParent(root.transform, false);
    var renderers = model.GetComponentsInChildren<MeshRenderer>();
    foreach (var r in renderers) r.sharedMaterial = m;
    var b = renderers[0].bounds;
    foreach (var r in renderers) b.Encapsulate(r.bounds);
    // Pivot at the centre of the carcass footprint so the piece stands on whatever it is placed on.
    // Front faces +Z. Desk pulls stick out past the front and the chair back leans past the seat,
    // so the depth centre is measured from the desk back face and from the chair seat front.
    float centreZ = piece == "GuardDesk" ? b.min.z + .30f : b.max.z - .20f;
    model.transform.position -= new Vector3(b.center.x, b.min.y, centreZ);
    // Solid collision for the player capsule; small parts are left out.
    var col = root.AddComponent<BoxCollider>();
    if (piece == "GuardDesk") { col.center = new Vector3(0, .39f, 0); col.size = new Vector3(1.2f, .78f, .6f); }
    else { col.center = new Vector3(0, .45f, b.center.z - centreZ); col.size = new Vector3(b.size.x, .9f, b.size.z); }
    bool saved;
    PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + piece + ".prefab", out saved);
    result.Add(new { piece, saved, bounds_min = b.min.ToString("F4"), bounds_max = b.max.ToString("F4"), model_rotation = source.transform.rotation.eulerAngles.ToString("F2") });
    UnityEngine.Object.DestroyImmediate(root);
}
AssetDatabase.SaveAssets();
return result;
