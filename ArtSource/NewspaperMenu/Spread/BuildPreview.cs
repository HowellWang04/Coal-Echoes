// One-off layout authoring in Edit Mode. Run through Unity CLI eval_file.
// Creates a review prefab and screenshots without changing the loaded scene.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Stop Play Mode before building the layout preview.");

const string output = "ArtSource/NewspaperMenu/Spread";
const string art = "Assets/Art Resources/UI/NewspaperMenu";
const string assets = art + "/SpreadPreview";
const int width = 1920, height = 1080;
const float photoX = 1008, photoY = 158, photoWidth = 852, photoHeight = 479.25f;
var copyText = System.IO.File.ReadAllText(output + "/copy.zh-CN.json");
var copy = Newtonsoft.Json.Linq.JObject.Parse(copyText);
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool wasDirty = scene.isDirty;
if (!UnityEditor.AssetDatabase.IsValidFolder(assets)) UnityEditor.AssetDatabase.CreateFolder(art, "SpreadPreview");
UnityEditor.AssetDatabase.ImportAsset(art + "/Fonts/MaShanZheng-Regular.ttf");
UnityEditor.AssetDatabase.ImportAsset(art + "/Fonts/MaShanZheng-OFL.txt");
UnityEditor.AssetDatabase.ImportAsset("Assets/Scripts/MainMenu/NewspaperHalftone.shader");

TMPro.TMP_FontAsset Bake(string sourceFile, string name, string characters, int size, int atlas)
{
    string path = assets + "/" + name + ".asset";
    var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
    if (existing != null) return existing;
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(art + "/Fonts/" + sourceFile);
    var font = TMPro.TMP_FontAsset.CreateFontAsset(source, size, 6,
        UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDF16, atlas, atlas, TMPro.AtlasPopulationMode.Dynamic, false);
    font.name = name;
    string missing;
    if (!font.TryAddCharacters(characters, out missing)) throw new System.Exception("Missing glyphs " + missing);
    font.atlasPopulationMode = TMPro.AtlasPopulationMode.Static;
    UnityEditor.AssetDatabase.CreateAsset(font, path);
    font.material.name = name + " Material";
    UnityEditor.AssetDatabase.AddObjectToAsset(font.material, font);
    foreach (var texture in font.atlasTextures)
    {
        texture.name = name + " Atlas";
        UnityEditor.AssetDatabase.AddObjectToAsset(texture, font);
    }
    UnityEditor.EditorUtility.SetDirty(font);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(font);
    return font;
}
var allCopy = copyText + "厂区通讯生活生产消息文体活动山间索道厂房与站台便民小栏目录一二";
var glyphs = new string(allCopy.Where(c => c >= 32).Distinct().ToArray());
var chinese = Bake("SourceHanSerifSC-Medium.otf", "Spread Chinese SDF", glyphs, 44, 2048);
var mastheadFont = Bake("MaShanZheng-Regular.ttf", "Spread Masthead SDF", "煤火回声", 160, 1024);
var latin = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(art + "/Fonts/Bitter Newspaper SDF.asset");
var sourceCamera = UnityEngine.GameObject.Find("Gameplay Root/MainMenuCamera").GetComponent<UnityEngine.Camera>();
var cameraObject = UnityEngine.Object.Instantiate(sourceCamera.gameObject);
cameraObject.name = "Spread Preview Camera";
cameraObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var camera = cameraObject.GetComponent<UnityEngine.Camera>();
camera.enabled = false;
cameraObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().antialiasing = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None;
var canvasObject = new UnityEngine.GameObject("NewspaperSpreadPreview", typeof(UnityEngine.RectTransform), typeof(UnityEngine.Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
canvasObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
canvasObject.layer = 5;
var canvas = canvasObject.GetComponent<UnityEngine.Canvas>();
canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
canvas.worldCamera = camera;
canvas.planeDistance = 1;
var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
scaler.referenceResolution = new UnityEngine.Vector2(width, height);
scaler.matchWidthOrHeight = .5f;

var material = new UnityEngine.Material(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(art + "/Halftone/NewspaperHalftone.mat"));
var sourceRT = new UnityEngine.RenderTexture(width, height, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var printRT = new UnityEngine.RenderTexture(width, height, 0, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var capture = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
var previousActive = UnityEngine.RenderTexture.active;
bool previousSRGB = UnityEngine.GL.sRGBWrite;
var ink = new UnityEngine.Color(.075f, .064f, .05f, .99f);
var red = new UnityEngine.Color(.64f, .18f, .09f, .96f);
var muted = new UnityEngine.Color(.105f, .095f, .08f, .99f);
var rules = new UnityEngine.Color(.17f, .16f, .14f, .62f);

UnityEngine.RectTransform Rect(UnityEngine.Transform parent, string name, float x, float y, float w, float h)
{
    var go = new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform));
    go.layer = 5;
    var r = (UnityEngine.RectTransform)go.transform;
    r.SetParent(parent, false);
    r.anchorMin = r.anchorMax = new UnityEngine.Vector2(0, 1);
    r.pivot = new UnityEngine.Vector2(0, 1);
    r.anchoredPosition = new UnityEngine.Vector2(x, -y);
    r.sizeDelta = new UnityEngine.Vector2(w, h);
    return r;
}
var paper = Rect(canvas.transform, "Spread", 0, 0, width, height);
paper.anchorMin = paper.anchorMax = paper.pivot = new UnityEngine.Vector2(.5f, .5f);
paper.anchoredPosition = UnityEngine.Vector2.zero;

UnityEngine.UI.Image Rule(string name, float x, float y, float w, float h, UnityEngine.Color color)
{
    var image = Rect(paper, name, x, y, w, h).gameObject.AddComponent<UnityEngine.UI.Image>();
    image.color = color;
    image.raycastTarget = false;
    return image;
}
TMPro.TMP_Text Text(string name, float x, float y, float w, float h, float size, string value,
    TMPro.TMP_FontAsset font = null, bool centered = false)
{
    var t = Rect(paper, name, x, y, w, h).gameObject.AddComponent<TMPro.TextMeshProUGUI>();
    t.font = font ?? chinese;
    t.fontSize = size;
    t.enableAutoSizing = false;
    t.text = value;
    t.color = ink;
    t.raycastTarget = false;
    t.alignment = centered ? TMPro.TextAlignmentOptions.Top : TMPro.TextAlignmentOptions.TopLeft;
    t.textWrappingMode = TMPro.TextWrappingModes.Normal;
    t.overflowMode = TMPro.TextOverflowModes.Overflow;
    t.lineSpacing = 1;
    return t;
}
void Columns(string key, float x, float y, float w, float h, float size, float gap)
{
    var columns = (Newtonsoft.Json.Linq.JArray)copy[key]["columns"];
    float columnWidth = (w - gap * (columns.Count - 1)) / columns.Count;
    for (int i = 0; i < columns.Count; i++)
    {
        var t = Text(key + "Column" + (i + 1), x + i * (columnWidth + gap), y, columnWidth, h, size, (string)columns[i]);
        t.color = muted;
        t.alignment = TMPro.TextAlignmentOptions.TopJustified;
    }
}
void Frame(string name, float x, float y, float w, float h, float thickness, UnityEngine.Color color)
{
    Rule(name + "Top", x, y, w, thickness, color);
    Rule(name + "Bottom", x, y + h - thickness, w, thickness, color);
    Rule(name + "Left", x, y, thickness, h, color);
    Rule(name + "Right", x + w - thickness, y, thickness, h, color);
}
void Save(UnityEngine.RenderTexture target, string path)
{
    UnityEngine.RenderTexture.active = target;
    capture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
    capture.Apply();
    System.IO.File.WriteAllBytes(path, capture.EncodeToPNG());
}
UnityEngine.Texture2D ImportTexture(string path)
{
    UnityEditor.AssetDatabase.ImportAsset(path);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Default;
    importer.mipmapEnabled = false;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.npotScale = UnityEditor.TextureImporterNPOTScale.None;
    importer.maxTextureSize = 2048;
    importer.SaveAndReimport();
    return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
}
UnityEngine.UI.RawImage Picture(string name, float x, float y, float w, float h, UnityEngine.Texture texture)
{
    var image = Rect(paper, name, x, y, w, h).gameObject.AddComponent<UnityEngine.UI.RawImage>();
    image.texture = texture;
    image.raycastTarget = false;
    return image;
}
var reports = new System.Collections.Generic.List<object>();
try
{
    camera.targetTexture = sourceRT;
    camera.aspect = (float)width / height;
    camera.Render();
    Save(sourceRT, output + "/camera-original.png");
    var original = capture.GetPixels32();
    UnityEngine.GL.sRGBWrite = true;
    material.SetFloat("_EffectStrength", 0);
    UnityEngine.Graphics.Blit(sourceRT, printRT, material);
    Save(printRT, output + "/effect-zero.png");
    var bypass = capture.GetPixels32();
    int maxBypassDifference = 0;
    for (int i = 0; i < original.Length; i++)
    {
        maxBypassDifference = System.Math.Max(maxBypassDifference, System.Math.Abs(original[i].r - bypass[i].r));
        maxBypassDifference = System.Math.Max(maxBypassDifference, System.Math.Abs(original[i].g - bypass[i].g));
        maxBypassDifference = System.Math.Max(maxBypassDifference, System.Math.Abs(original[i].b - bypass[i].b));
    }
    material.SetFloat("_EffectStrength", 1);
    material.SetFloat("_InkWear", .09f);
    material.SetVector("_PaperUV", new UnityEngine.Vector4(.91f, .9f, .046f, .054f));
    material.SetVector("_PrintRect", new UnityEngine.Vector4(0, 0, 1, 1));
    // A white source produces the unprinted paper using the same paper shader.
    UnityEngine.Graphics.Blit(UnityEngine.Texture2D.whiteTexture, printRT, material);
    Save(printRT, assets + "/SpreadPaper.png");
    var paperTexture = ImportTexture(assets + "/SpreadPaper.png");
    Picture("Paper", 0, 0, width, height, paperTexture);

    material.SetVector("_PrintRect", new UnityEngine.Vector4(photoX / width, (height - photoY - photoHeight) / height, photoWidth / width, photoHeight / height));
    UnityEngine.Graphics.Blit(sourceRT, printRT, material);
    Save(printRT, assets + "/CameraPhoto.png");
    var photoTexture = ImportTexture(assets + "/CameraPhoto.png");

    Text("Masthead", 64, 44, 640, 140, 127, (string)copy["masthead"], mastheadFont).color = red;
    var english = Text("EnglishTitle", 80, 185, 590, 38, 29, (string)copy["englishTitle"], latin);
    english.characterSpacing = 10;
    Frame("InfoFrame", 738, 52, 174, 154, 2.4f, rules);
    Frame("InfoInnerFrame", 743, 57, 164, 144, .7f, rules);
    Text("InfoHeading", 751, 66, 148, 38, 25, "厂区通讯", null, true);
    Rule("InfoRule", 755, 111, 140, 1, rules);
    Text("InfoBody", 753, 122, 145, 68, 18, "生活　生产\n文体　消息", null, true);
    Rule("HeaderRule", 60, 235, 860, 2, ink);
    Rule("HeaderFineRule", 60, 240, 860, .7f, rules);
    Rule("LeadStrip", 60, 253, 860, 24, ink);
    Text("LeadStripLabel", 69, 256, 842, 23, 14, "生产消息　　生活简讯　　文体活动", null, true).color = new UnityEngine.Color(.88f, .86f, .82f, .98f);
    Text("LeadHeading", 60, 287, 860, 62, 42, (string)copy["lead"]["heading"]).fontStyle = TMPro.FontStyles.Bold;
    Text("LeadSubheading", 61, 352, 858, 34, 19, (string)copy["lead"]["subheading"]);
    Columns("lead", 60, 395, 500, 309, 14.1f, 15);
    Rule("MenuColumnRule", 576, 392, .8f, 291, rules);
    Rule("MenuTopRule", 599, 397, 304, 2, ink);
    Rule("MenuTopFineRule", 599, 402, 304, .7f, rules);
    Text("MenuHeading", 599, 411, 304, 28, 17, "目　录", null, true);

    void Button(string name, float y, float h, float size, string label, bool enabled)
    {
        var r = Rect(paper, name, 607, y, 292, h);
        var hit = r.gameObject.AddComponent<UnityEngine.UI.Image>();
        hit.color = new UnityEngine.Color(0, 0, 0, 0);
        var button = r.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.interactable = enabled;
        var text = Text(name + "Label", 618, y + 4, 270, h - 8, size, label, latin, true);
        text.rectTransform.SetParent(r, true);
        text.alignment = TMPro.TextAlignmentOptions.Center;
        button.targetGraphic = text;
        var colors = button.colors;
        colors.normalColor = UnityEngine.Color.white;
        colors.highlightedColor = new UnityEngine.Color(.8f, .26f, .12f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new UnityEngine.Color(.6f, .15f, .06f);
        colors.disabledColor = new UnityEngine.Color(1, 1, 1, .36f);
        button.colors = colors;
    }
    Button("StartGameButton", 443, 69, 40, (string)copy["menu"]["start"], true);
    Rule("StartUnderline", 620, 510, 264, 2, red);
    Button("ContinueButton", 527, 62, 29, (string)copy["menu"]["continue"], false);
    Rule("MenuMiddleRule", 620, 591, 264, .7f, rules);
    Button("QuitButton", 607, 61, 31, (string)copy["menu"]["quit"], true);
    Rule("MenuBottomRule", 599, 680, 304, 2, ink);

    Rule("LeftLowerRule", 60, 710, 860, 1.4f, rules);
    Text("CanteenHeading", 60, 729, 527, 47, 31, (string)copy["canteen"]["heading"]);
    Text("CanteenSubheading", 60, 775, 527, 29, 18, (string)copy["canteen"]["subheading"]);
    Columns("canteen", 60, 815, 527, 195, 14.2f, 15);
    Rule("CinemaColumnRule", 604, 732, .8f, 280, rules);
    Text("CinemaHeading", 626, 733, 290, 44, 29, (string)copy["cinema"]["heading"]);
    Text("CinemaSubheading", 626, 779, 290, 30, 18, (string)copy["cinema"]["subheading"]);
    Columns("cinema", 626, 820, 290, 194, 14.1f, 16);

    Text("PhotoPageHeading", 1008, 56, 450, 76, 51, (string)copy["photoHeading"]);
    Text("PhotoPageDeck", 1505, 89, 355, 29, 17, "山间索道　厂房与站台", null, true);
    Rule("RightHeaderRule", 1008, 134, 852, 1.5f, rules);
    Picture("FactoryPhoto", photoX, photoY, photoWidth, photoHeight, photoTexture);
    Frame("PhotoFrame", photoX - 1, photoY - 1, photoWidth + 2, photoHeight + 2, .8f, rules);
    Text("PhotoCaption", 1009, 650, 850, 31, 16, (string)copy["photoCaption"]);
    Rule("RightLowerRule", 1008, 696, 852, 1.4f, rules);
    Text("ToolsHeading", 1008, 718, 640, 46, 29, (string)copy["tools"]["heading"]);
    Columns("tools", 1008, 778, 644, 150, 14.2f, 19);
    Rule("ServicesRule", 1008, 938, 644, .8f, rules);
    Text("RepairsHeading", 1008, 943, 306, 38, 25, (string)copy["repairs"]["heading"]);
    Text("RepairsBody", 1008, 981, 307, 62, 14.1f, (string)copy["repairs"]["body"]);
    Text("LibraryHeading", 1343, 943, 309, 38, 25, (string)copy["library"]["heading"]);
    Text("LibraryBody", 1343, 981, 309, 62, 14.1f, (string)copy["library"]["body"]);

    Frame("NoticeFrame", 1680, 715, 180, 316, .8f, rules);
    Text("NoticeLabel", 1690, 729, 160, 35, 23, "便民小栏", null, true);
    Rule("NoticeRule", 1696, 768, 148, 1, rules);
    Text("LostHeading", 1699, 784, 38, 181, 25, "失\n物\n招\n领", null, true);
    Text("LostBody", 1748, 786, 97, 222, 14.5f, (string)copy["lost"]["body"]);
    Rule("LeftFooterRule", 60, 1045, 860, .8f, rules);
    Rule("RightFooterRule", 1008, 1045, 852, .8f, rules);
    Text("LeftFooter", 60, 1050, 860, 23, 12.3f, (string)copy["footer"], null, true);
    Text("RightFooter", 1008, 1050, 852, 23, 12.3f, "煤火回声　　生活　生产　文体", null, true);

    // A narrow layered shadow suggests the valley of the open spread.
    for (int i = 0; i < 24; i++)
    {
        float x = 936 + i * 2;
        float a = .008f + .12f * UnityEngine.Mathf.Exp(-UnityEngine.Mathf.Abs(x - 959) / 7f);
        Rule("FoldShade" + i, x, 0, 2, height, new UnityEngine.Color(.15f, .12f, .09f, a));
    }
    Rule("FoldCrease", 959, 0, 1, height, new UnityEngine.Color(.15f, .12f, .09f, .15f));
    Rule("FoldHighlight", 963, 0, 1, height, new UnityEngine.Color(1, .99f, .95f, .2f));
    camera.targetTexture = sourceRT;
    UnityEngine.Canvas.ForceUpdateCanvases();
    foreach (var text in paper.GetComponentsInChildren<TMPro.TMP_Text>()) text.ForceMeshUpdate();
    UnityEngine.Canvas.ForceUpdateCanvases();
    camera.Render();
    Save(sourceRT, output + "/menu-spread-1920.png");

    var texts = paper.GetComponentsInChildren<TMPro.TMP_Text>();
    var overflowing = texts.Where(t => t.isTextOverflowing).Select(t => new { t.name, t.preferredHeight, rectHeight = t.rectTransform.rect.height }).ToArray();
    var missingGlyphs = texts.SelectMany(t => t.text.Where(c => !char.IsWhiteSpace(c) && !t.font.HasCharacter(c)).Select(c => new { t.name, character = c.ToString() })).ToArray();
    // Save a standalone Canvas prefab. Its static photo is explicitly a preview.
    canvas.worldCamera = null;
    canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
    canvasObject.hideFlags = UnityEngine.HideFlags.None;
    bool saved;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(canvasObject, assets + "/NewspaperSpreadPreview.prefab", out saved);
    reports.Add(new { saved, prefab = assets + "/NewspaperSpreadPreview.prefab", textCount = texts.Length,
        overflowing, missingGlyphs, sourceCamera = sourceCamera.name, sourcePosition = sourceCamera.transform.position.ToString(),
        photoAspect = photoWidth / photoHeight, maxBypassDifference, paperCoordinatesShared = true,
        shaderMessages = UnityEditor.ShaderUtil.GetShaderMessages(material.shader).Select(m => m.message).ToArray(),
        wasDirty, sceneDirty = scene.isDirty, playing = UnityEditor.EditorApplication.isPlaying,
        runtimeIntegrated = false });
    System.IO.File.WriteAllText(output + "/checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(reports, Newtonsoft.Json.Formatting.Indented));
}
finally
{
    camera.targetTexture = null;
    UnityEngine.RenderTexture.active = previousActive;
    UnityEngine.GL.sRGBWrite = previousSRGB;
    UnityEngine.Object.DestroyImmediate(canvasObject);
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEngine.Object.DestroyImmediate(capture);
    UnityEngine.Object.DestroyImmediate(sourceRT);
    UnityEngine.Object.DestroyImmediate(printRT);
    UnityEngine.Object.DestroyImmediate(material);
}
return reports;
