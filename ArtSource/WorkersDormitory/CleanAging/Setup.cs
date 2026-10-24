// Run through Unity CLI eval_file in Edit Mode.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Use Edit Mode.");
const string folder = "Assets/Art Resources/Environment/WorkersDormitory/Clean";
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var dorm = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
    .Single(t => t.name == "WorkersDormitory");
foreach (string name in new[] { "BuffBrick", "Concrete" })
{
    string path = folder + "/Dorm_" + name + "_Clean_BaseColor.png";
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.sRGBTexture = true;
    importer.mipmapEnabled = true;
    importer.wrapMode = TextureWrapMode.Repeat;
    importer.filterMode = FilterMode.Trilinear;
    importer.maxTextureSize = 2048;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.textureCompression = UnityEditor.TextureImporterCompression.CompressedHQ;
    importer.SaveAndReimport();
}
var pairs = new System.Collections.Generic.List<DormitoryMaterialAging.MaterialPair>();
foreach (var aged in dorm.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
{
    bool brick = aged.name == "Dorm_BuffBrick";
    bool concrete = aged.name == "Dorm_Concrete" || aged.name == "Dorm_Roof";
    bool frame = aged.name.StartsWith("Dorm_Frame");
    bool runoff = aged.name.StartsWith("Dorm_SillRunoff");
    if (!brick && !concrete && !frame && !runoff) continue;
    string name = aged.name.Replace(".002", "") + "_Clean";
    string path = folder + "/" + name + ".mat";
    var clean = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
    if (clean == null)
    {
        clean = new Material(aged) { name = name };
        UnityEditor.AssetDatabase.CreateAsset(clean, path);
    }
    if (brick || concrete)
    {
        string texture = brick ? "BuffBrick" : "Concrete";
        clean.SetTexture("baseColorTexture", UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
            folder + "/Dorm_" + texture + "_Clean_BaseColor.png"));
        clean.SetFloat("normalTexture_scale", brick ? .45f : .06f);
    }
    if (frame)
    {
        var color = aged.GetColor("baseColorFactor");
        clean.SetColor("baseColorFactor", new Color(color.r * 1.08f, color.g * 1.08f, color.b * 1.08f, color.a));
        clean.SetFloat("roughnessFactor", .58f);
    }
    if (runoff)
    {
        var color = aged.GetColor("baseColorFactor");
        color.a = 0f;
        clean.SetColor("baseColorFactor", color);
    }
    UnityEditor.EditorUtility.SetDirty(clean);
    pairs.Add(new DormitoryMaterialAging.MaterialPair { aged = aged, clean = clean });
}
var menu = UnityEngine.Object.FindFirstObjectByType<NewspaperMenuUI>(UnityEngine.FindObjectsInactive.Include);
var aging = menu.GetComponent<DormitoryMaterialAging>();
if (aging == null) aging = UnityEditor.Undo.AddComponent<DormitoryMaterialAging>(menu.gameObject);
var settings = new UnityEditor.SerializedObject(aging);
settings.FindProperty("dormitory").objectReferenceValue = dorm;
settings.FindProperty("blendShader").objectReferenceValue = Shader.Find("Hidden/Coal Echoes/Dormitory Texture Blend");
var list = settings.FindProperty("materials");
list.arraySize = pairs.Count;
for (int i = 0; i < pairs.Count; i++)
{
    list.GetArrayElementAtIndex(i).FindPropertyRelative("aged").objectReferenceValue = pairs[i].aged;
    list.GetArrayElementAtIndex(i).FindPropertyRelative("clean").objectReferenceValue = pairs[i].clean;
}
settings.ApplyModifiedProperties();
var menuSettings = new UnityEditor.SerializedObject(menu);
menuSettings.FindProperty("dormitoryAging").objectReferenceValue = aging;
menuSettings.ApplyModifiedProperties();
UnityEditor.AssetDatabase.SaveAssets();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return new { pairs = pairs.Select(p => new { aged = p.aged.name, clean = p.clean.name }).ToArray(),
    target = UnityEditor.AnimationUtility.CalculateTransformPath(dorm, null), saved = false };
