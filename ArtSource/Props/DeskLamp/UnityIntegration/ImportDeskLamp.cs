// Method body executed through Unity MCP execute_code. Imports the desk lamp, creates its HDRP/Lit
// material and saves the prefab, without touching any scene. No collider: it sits out of reach on the desk.
const string Folder = "Assets/Art Resources/Props/DeskLamp";
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
    importer.maxTextureSize = 1024;
    importer.wrapMode = TextureWrapMode.Clamp;
    importer.SaveAndReimport();
}
string modelPath = Folder + "/Models/DeskLamp.fbx";
var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
modelImporter.SaveAndReimport();

string matPath = Folder + "/Materials/DeskLamp_ArmyGreen.mat";
var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
if (m == null) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, matPath); }
System.Func<string, Texture2D> tex = n => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/DeskLamp_" + n + ".png");
m.SetColor("_BaseColor", Color.white);
m.SetTexture("_BaseColorMap", tex("BaseColor"));
m.SetTexture("_NormalMap", tex("Normal")); m.SetFloat("_NormalScale", 1f);
m.SetTexture("_MaskMap", tex("HDRP_Mask"));
m.SetFloat("_MetallicRemapMin", 0); m.SetFloat("_MetallicRemapMax", 1);
m.SetFloat("_SmoothnessRemapMin", 0); m.SetFloat("_SmoothnessRemapMax", 1);
UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
EditorUtility.SetDirty(m);

var root = new GameObject("DeskLamp");
var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
model.name = "Model";
model.transform.SetParent(root.transform, false);
var r = model.GetComponentInChildren<MeshRenderer>();
r.sharedMaterial = m;
// Pivot at the centre of the base underside; the shade faces +Z and the cord trails off behind.
var b = r.bounds;
model.transform.position -= new Vector3(0, b.min.y, 0);
bool saved;
PrefabUtility.SaveAsPrefabAsset(root, Folder + "/DeskLamp.prefab", out saved);
UnityEngine.Object.DestroyImmediate(root);
AssetDatabase.SaveAssets();
return new { saved, bounds_min = b.min.ToString("F4"), bounds_max = b.max.ToString("F4") };
