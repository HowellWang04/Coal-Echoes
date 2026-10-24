// Method body executed through Unity MCP execute_code. Imports the lock pick textures,
// creates HDRP/Lit materials and saves the prefab without touching any scene.
const string Folder = "Assets/Art Resources/Props/LockPick";
if (EditorApplication.isPlaying) return "Exit Play Mode first.";
foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[]{Folder + "/Textures"}))
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
    string name = System.IO.Path.GetFileNameWithoutExtension(path);
    importer.textureType = name.EndsWith("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
    importer.sRGBTexture = name.Contains("BaseColor");
    importer.alphaSource = TextureImporterAlphaSource.FromInput;
    importer.maxTextureSize = 1024;
    importer.wrapMode = TextureWrapMode.Repeat;
    importer.SaveAndReimport();
}
System.Func<string, Texture2D> tex = n => n == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/" + n + ".png");
System.Func<string, string, string, string, float, float, Material> make = (name, color, normal, mask, smooth, metal) =>
{
    string path = Folder + "/Materials/" + name + ".mat";
    var m = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (m == null) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, path); }
    m.SetColor("_BaseColor", Color.white);
    m.SetTexture("_BaseColorMap", tex(color));
    m.SetFloat("_Smoothness", smooth);
    m.SetFloat("_Metallic", metal);
    if (normal != null) { m.SetTexture("_NormalMap", tex(normal)); m.SetFloat("_NormalScale", .5f); }
    if (mask != null) { m.SetTexture("_MaskMap", tex(mask)); m.SetFloat("_MetallicRemapMin", 0); m.SetFloat("_MetallicRemapMax", 1); m.SetFloat("_SmoothnessRemapMin", 0); m.SetFloat("_SmoothnessRemapMax", 1); }
    UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
    EditorUtility.SetDirty(m);
    return m;
};
var mats = new System.Collections.Generic.Dictionary<string, Material>();
mats["Steel_WornLockPick"] = make("Steel_WornLockPick", "Steel_BaseColor", "Steel_Normal", "Steel_HDRP_Mask", .64f, 1f);
mats["Grip_FrictionTape"] = make("Grip_FrictionTape", "Tape_BaseColor", "Tape_Normal", "Tape_HDRP_Mask", .18f, 0f);
mats["RubberBand_Aged"] = make("RubberBand_Aged", "Rubber_BaseColor", null, null, .22f, 0f);

var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Models/LockPick.fbx");
var root = new GameObject("LockPick");
var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
model.name = "Model";
model.transform.SetParent(root.transform, false);
var renderers = model.GetComponentsInChildren<MeshRenderer>();
var used = new System.Collections.Generic.List<string>();
foreach (var r in renderers)
{
    r.sharedMaterials = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(r.sharedMaterials, m => mats[m.name]));
    used.Add(r.name + ":" + string.Join(",", System.Linq.Enumerable.Select(r.sharedMaterials, m => m.name)));
}
var b = renderers[0].bounds;
foreach (var r in renderers) b.Encapsulate(r.bounds);
// Pivot at the centre of the underside so the set rests on whatever it is placed on.
model.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
bool saved;
PrefabUtility.SaveAsPrefabAsset(root, Folder + "/LockPick.prefab", out saved);
UnityEngine.Object.DestroyImmediate(root);
AssetDatabase.SaveAssets();
return new { saved, size = b.size.ToString("F4"), renderers = used };
