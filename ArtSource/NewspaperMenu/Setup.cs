if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play before installing the newspaper menu.");
var folder = "Assets/Art Resources/UI/NewspaperMenu";
var copy = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/Narrative/newspaper-menu.json");

TMPro.TMP_FontAsset BakeFont(string file, string name, int size, int padding, string chars)
{
    var path = folder + "/Fonts/" + name + ".asset";
    var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(path);
    if (existing != null) return existing;
    var source = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(folder + "/Fonts/" + file);
    var font = TMPro.TMP_FontAsset.CreateFontAsset(source, size, padding,
        UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDF16, 2048, 2048, TMPro.AtlasPopulationMode.Dynamic, false);
    font.name = name;
    string missing;
    if (!font.TryAddCharacters(chars, out missing)) throw new System.Exception("Missing glyphs " + missing);
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
    return font;
}

var latin = BakeFont("Bitter-Regular.ttf", "Bitter Newspaper SDF", 80, 8,
    new string(Enumerable.Range(32, 95).Select(n => (char)n).ToArray()));
var chinese = BakeFont("SourceHanSerifSC-Medium.otf", "Newspaper Chinese SDF", 48, 5,
    new string((copy.text + "厂区生活生产消息").Where(c => c >= 32).Distinct().ToArray()));

UnityEngine.Sprite ImportSprite(string name)
{
    string path = folder + "/" + name + ".png";
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Sprite;
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    importer.mipmapEnabled = false;
    importer.alphaIsTransparency = true;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.maxTextureSize = 2048;
    importer.SaveAndReimport();
    return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
}
var newsprint = ImportSprite("Newsprint");
var photo = ImportSprite("LoadingYard");
var cableway = ImportSprite("CablewayMark");
var root = UnityEngine.GameObject.Find("MainMenuUIRoot");
var presentation = root.transform.Find("MenuPresentation");
var menu = presentation.Find("MenuContent");
if (presentation.Find("Newspaper") != null) throw new System.InvalidOperationException("Newspaper already exists; edit the existing layout.");

UnityEngine.RectTransform Rect(UnityEngine.Transform parent, string name, float x, float y, float w, float h)
{
    var go = new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform));
    go.layer = 5;
    var r = (UnityEngine.RectTransform)go.transform;
    r.SetParent(parent, false);
    r.anchorMin = r.anchorMax = new UnityEngine.Vector2(0f, 1f);
    r.pivot = new UnityEngine.Vector2(0f, 1f);
    r.anchoredPosition = new UnityEngine.Vector2(x, -y);
    r.sizeDelta = new UnityEngine.Vector2(w, h);
    return r;
}

var paper = Rect(presentation, "Newspaper", 0, 0, 800, 960);
paper.anchorMin = paper.anchorMax = new UnityEngine.Vector2(0f, .5f);
paper.pivot = new UnityEngine.Vector2(.5f, .5f);
paper.anchoredPosition = new UnityEngine.Vector2(485f, 0f);
paper.localScale = UnityEngine.Vector3.one * .94f;
paper.localRotation = UnityEngine.Quaternion.Euler(0f, 0f, -2f);
var controller = paper.gameObject.AddComponent<NewspaperMenuUI>();
var ink = new UnityEngine.Color(.16f, .155f, .14f, .96f);
var lightInk = new UnityEngine.Color(.23f, .22f, .20f, .94f);
var paperColor = new UnityEngine.Color(.87f, .84f, .77f, 1f);

UnityEngine.UI.Image Image(string name, float x, float y, float w, float h, UnityEngine.Color color, UnityEngine.Sprite sprite = null)
{
    var r = Rect(paper, name, x, y, w, h);
    var image = r.gameObject.AddComponent<UnityEngine.UI.Image>();
    image.sprite = sprite;
    image.color = color;
    image.raycastTarget = false;
    image.useSpriteMesh = false;
    image.preserveAspect = false;
    r.gameObject.AddComponent<NewspaperGraphicBend>();
    return image;
}

TMPro.TMP_Text Text(string name, float x, float y, float w, float h, float size,
    TMPro.TMP_FontAsset font, string value, UnityEngine.Color color, bool centered = false)
{
    var r = Rect(paper, name, x, y, w, h);
    var t = r.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
    t.font = font;
    t.fontSize = size;
    t.enableAutoSizing = false;
    t.text = value;
    t.color = color;
    t.raycastTarget = false;
    t.alignment = centered ? TMPro.TextAlignmentOptions.Center : TMPro.TextAlignmentOptions.TopLeft;
    t.textWrappingMode = TMPro.TextWrappingModes.Normal;
    t.overflowMode = TMPro.TextOverflowModes.Overflow;
    t.lineSpacing = 1.5f;
    return t;
}

Image("PaperShadow", 7, 9, 800, 960, new UnityEngine.Color(0, 0, 0, .16f), newsprint);
Image("Newsprint", 0, 0, 800, 960, UnityEngine.Color.white, newsprint);
Image("MastheadInk", 68, 79, 662, 96, ink);
Image("CablewayMark", 85, 90, 76, 76, paperColor, cableway);
var masthead = Text("Masthead", 168, 62, 535, 132, 82, chinese, "", paperColor, true);
Text("LeftHeader", 68, 187, 120, 30, 18, chinese, "厂区生活", ink);
masthead.fontStyle = TMPro.FontStyles.Bold;
var english = Text("EnglishTitle", 195, 177, 422, 44, 31, latin, "", ink, true);
Text("RightHeader", 634, 187, 106, 30, 18, chinese, "生产消息", ink);
Image("HeaderRule", 64, 229, 674, 2, ink);
Image("LeftColumnRule", 253, 243, 1.2f, 620, lightInk);
Image("RightColumnRule", 562, 243, 1.2f, 620, lightInk);
Image("FactoryPhoto", 271, 250, 274, 218, new UnityEngine.Color(.91f, .88f, .82f, .96f), photo);
var caption = Text("PhotoCaption", 271, 480, 274, 32, 17, chinese, "", ink, true);

var headings = new TMPro.TMP_Text[3];
var subheads = new TMPro.TMP_Text[3];
var bodies = new TMPro.TMP_Text[3];
headings[0] = Text("TransportHeading", 68, 249, 172, 45, 28, chinese, "", ink);
subheads[0] = Text("TransportSubheading", 68, 305, 172, 51, 19, chinese, "", ink);
bodies[0] = Text("TransportBody", 68, 362, 172, 239, 19, chinese, "", lightInk);
Image("LeftArticleRule", 68, 619, 172, 1.4f, ink);
headings[2] = Text("CinemaHeading", 68, 628, 172, 62, 27, chinese, "", ink);
subheads[2] = Text("CinemaSubheading", 68, 674, 172, 44, 17, chinese, "", ink);
bodies[2] = Text("CinemaBody", 68, 708, 172, 170, 16, chinese, "", lightInk);
headings[1] = Text("CanteenHeading", 581, 249, 151, 116, 37, chinese, "", ink);
subheads[1] = Text("CanteenSubheading", 581, 369, 151, 64, 18, chinese, "", ink);
bodies[1] = Text("CanteenBody", 581, 443, 151, 237, 18, chinese, "", lightInk);
Image("RightArticleRule", 581, 685, 151, 1.4f, ink);
var noticeHeading = Text("LostAndFoundHeading", 581, 703, 151, 42, 26, chinese, "", ink);
var noticeBody = Text("LostAndFoundBody", 581, 756, 151, 109, 18, chinese, "", lightInk);

Image("MenuTopRule", 270, 537, 276, 3, ink);
Image("MenuLeftRule", 270, 539, 2, 317, ink);
Image("MenuRightRule", 544, 539, 2, 317, ink);
Image("MenuBottomRule", 270, 855, 276, 3, ink);
Image("StartPanel", 277, 546, 260, 113, ink);
Image("ContinueRule", 289, 745, 238, 1.4f, ink);
Image("FooterRule", 68, 884, 662, 1.4f, ink);
var footer = Text("Footer", 68, 894, 662, 25, 17, chinese, "", ink, true);

UnityEngine.UI.Button MoveButton(string name, float y, float height, float fontSize, UnityEngine.Color color)
{
    var button = menu.Find(name).GetComponent<UnityEngine.UI.Button>();
    var r = (UnityEngine.RectTransform)button.transform;
    r.SetParent(paper, false);
    r.anchorMin = r.anchorMax = new UnityEngine.Vector2(0, 1);
    r.pivot = new UnityEngine.Vector2(0, 1);
    r.anchoredPosition = new UnityEngine.Vector2(282, -y);
    r.sizeDelta = new UnityEngine.Vector2(250, height);
    r.localScale = UnityEngine.Vector3.one;
    var label = button.GetComponentInChildren<TMPro.TMP_Text>();
    label.font = latin;
    label.fontSize = fontSize;
    if (name == "StartGameButton") label.fontStyle = TMPro.FontStyles.Bold;
    label.enableAutoSizing = false;
    label.color = color;
    label.alignment = TMPro.TextAlignmentOptions.Center;
    label.rectTransform.anchorMin = UnityEngine.Vector2.zero;
    label.rectTransform.anchorMax = UnityEngine.Vector2.one;
    label.rectTransform.pivot = new UnityEngine.Vector2(.5f,.5f);
    label.rectTransform.anchoredPosition = UnityEngine.Vector2.zero;
    label.rectTransform.sizeDelta = UnityEngine.Vector2.zero;
    label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
    label.raycastTarget = false;
    button.targetGraphic = label;
    var colors = button.colors;
    colors.normalColor = UnityEngine.Color.white;
    colors.highlightedColor = new UnityEngine.Color(.8f,.75f,.65f,1f);
    colors.selectedColor = colors.highlightedColor;
    colors.pressedColor = new UnityEngine.Color(.65f,.58f,.49f,1f);
    colors.disabledColor = new UnityEngine.Color(1f,1f,1f,.4f);
    colors.fadeDuration = .12f;
    button.colors = colors;
    return button;
}
MoveButton("StartGameButton", 559, 84, 42, paperColor);
MoveButton("ContinueButton", 676, 58, 29, ink);
MoveButton("QuitButton", 775, 58, 34, ink);
menu.gameObject.SetActive(false);

var serialized = new UnityEditor.SerializedObject(controller);
void Reference(string name, UnityEngine.Object value) { serialized.FindProperty(name).objectReferenceValue = value; }
void References(string name, TMPro.TMP_Text[] values)
{
    var p = serialized.FindProperty(name);
    p.arraySize = values.Length;
    for (int i=0;i<values.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
}
Reference("copy",copy);
Reference("masthead",masthead);
Reference("englishTitle",english);
References("headings",headings);
References("subheadings",subheads);
References("bodies",bodies);
Reference("photoCaption",caption);
Reference("noticeHeading",noticeHeading);
Reference("noticeBody",noticeBody);
Reference("footer",footer);
serialized.ApplyModifiedPropertiesWithoutUndo();
controller.ApplyCopy();
var manager = new UnityEditor.SerializedObject(root.GetComponent<MainMenuUIManager>());
manager.FindProperty("newspaper").objectReferenceValue = controller;
manager.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssets();
UnityEngine.Canvas.ForceUpdateCanvases();
var checks = paper.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => {
    t.ForceMeshUpdate();
    return new {name=t.name, text=t.text, overflow=t.isTextOverflowing, preferred=t.GetPreferredValues().ToString(), size=t.rectTransform.rect.ToString()};
}).ToArray();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);
return checks;
