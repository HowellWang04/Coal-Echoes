// Integrate the approved spread in the loaded main scene through Unity APIs.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before installation.");
const string art = "Assets/Art Resources/UI/NewspaperMenu";
var root = UnityEngine.GameObject.Find("MainMenuUIRoot");
var manager = root.GetComponent<MainMenuUIManager>();
var presentation = root.transform.Find("MenuPresentation");
var oldPaper = presentation.Find("Newspaper");
if (oldPaper.Find("Spread") != null) throw new System.InvalidOperationException("Spread already installed.");
var draft = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/NewspaperMenu/Spread/copy.zh-CN.json"));
var content = new Newtonsoft.Json.Linq.JObject
{
    ["status"] = "draft",
    ["period"] = "1980s",
    ["issueDate"] = "",
    ["issueNumber"] = "",
    ["masthead"] = draft["masthead"].DeepClone(),
    ["englishTitle"] = draft["englishTitle"].DeepClone(),
    ["sections"] = new Newtonsoft.Json.Linq.JArray(new[] { "lead", "canteen", "cinema", "tools" }.Select(k => draft[k].DeepClone())),
    ["notices"] = new Newtonsoft.Json.Linq.JArray(new[] { "repairs", "library", "lost" }.Select(k => draft[k].DeepClone())),
    ["photoHeading"] = draft["photoHeading"].DeepClone(),
    ["photoCaption"] = draft["photoCaption"].DeepClone(),
    ["footer"] = draft["footer"].DeepClone(),
    ["menu"] = draft["menu"].DeepClone()
};
content["notices"][2]["heading"] = "失\n物\n招\n领";
const string copyPath = "Assets/Narrative/newspaper-menu.json";
System.IO.File.WriteAllText(copyPath, content.ToString(Newtonsoft.Json.Formatting.Indented));
UnityEditor.AssetDatabase.ImportAsset(copyPath);

var material = new UnityEngine.Material(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(art + "/Halftone/NewspaperHalftone.mat"));
material.name = "NewspaperSpread";
material.SetVector("_PaperUV", new UnityEngine.Vector4(.91f, .9f, .046f, .054f));
material.SetVector("_PrintRect", new UnityEngine.Vector4(1008f / 1920f, (1080f - 158f - 479.25f) / 1080f, 852f / 1920f, 479.25f / 1080f));
material.SetVector("_PaperSize", new UnityEngine.Vector4(1920, 1080, 0, 0));
material.SetFloat("_InkWear", .09f);
UnityEditor.AssetDatabase.CreateAsset(material, art + "/Halftone/NewspaperSpread.mat");
UnityEditor.AssetDatabase.SaveAssetIfDirty(material);

oldPaper.name = "PreviousNewspaper";
var panel = new UnityEngine.GameObject("Newspaper", typeof(UnityEngine.RectTransform));
panel.layer = 5;
panel.transform.SetParent(presentation, false);
var rect = (UnityEngine.RectTransform)panel.transform;
rect.anchorMin = UnityEngine.Vector2.zero;
rect.anchorMax = UnityEngine.Vector2.one;
rect.offsetMin = rect.offsetMax = UnityEngine.Vector2.zero;

var background = new UnityEngine.GameObject("PaperBackdrop", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.RawImage));
background.layer = 5;
background.transform.SetParent(panel.transform, false);
var backgroundRect = (UnityEngine.RectTransform)background.transform;
backgroundRect.anchorMin = UnityEngine.Vector2.zero;
backgroundRect.anchorMax = UnityEngine.Vector2.one;
backgroundRect.offsetMin = backgroundRect.offsetMax = UnityEngine.Vector2.zero;
var backgroundImage = background.GetComponent<UnityEngine.UI.RawImage>();
backgroundImage.texture = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(art + "/SpreadPreview/SpreadPaper.png");
backgroundImage.uvRect = new UnityEngine.Rect(.1f, .15f, .8f, .7f);
backgroundImage.raycastTarget = false;

var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(art + "/SpreadPreview/NewspaperSpreadPreview.prefab");
var layout = UnityEngine.Object.Instantiate(prefab);
var spread = (UnityEngine.RectTransform)layout.transform.Find("Spread");
spread.SetParent(panel.transform, false);
spread.anchorMin = spread.anchorMax = spread.pivot = new UnityEngine.Vector2(.5f, .5f);
spread.anchoredPosition = UnityEngine.Vector2.zero;
spread.localScale = UnityEngine.Vector3.one;
UnityEngine.Object.DestroyImmediate(layout);

// Keep the fine photo border attached to its adaptive image rectangle.
var photoRect = spread.Find("FactoryPhoto").GetComponent<UnityEngine.RectTransform>();
foreach (string side in new[] { "Top", "Bottom", "Left", "Right" })
{
    var border = (UnityEngine.RectTransform)spread.Find("PhotoFrame" + side);
    border.SetParent(photoRect, false);
    bool horizontal = side == "Top" || side == "Bottom";
    float edge = side == "Top" || side == "Right" ? 1 : 0;
    border.anchorMin = horizontal ? new UnityEngine.Vector2(0, edge) : new UnityEngine.Vector2(edge, 0);
    border.anchorMax = horizontal ? new UnityEngine.Vector2(1, edge) : new UnityEngine.Vector2(edge, 1);
    border.pivot = new UnityEngine.Vector2(.5f, .5f);
    border.anchoredPosition = horizontal ? new UnityEngine.Vector2(0, edge == 1 ? .6f : -.6f) : new UnityEngine.Vector2(edge == 1 ? .6f : -.6f, 0);
    border.sizeDelta = horizontal ? new UnityEngine.Vector2(2, .8f) : new UnityEngine.Vector2(.8f, 2);
}

foreach (string name in new[] { "StartGameButton", "ContinueButton", "QuitButton" })
{
    var oldButton = oldPaper.Find(name).GetComponent<UnityEngine.UI.Button>();
    var button = spread.Find(name).GetComponent<UnityEngine.UI.Button>();
    var oldSerialized = new UnityEditor.SerializedObject(oldButton);
    var buttonSerialized = new UnityEditor.SerializedObject(button);
    buttonSerialized.CopyFromSerializedProperty(oldSerialized.FindProperty("m_OnClick"));
    buttonSerialized.ApplyModifiedPropertiesWithoutUndo();
    var label = button.GetComponentInChildren<TMPro.TMP_Text>();
    var colors = button.colors;
    colors.normalColor = label.color;
    colors.highlightedColor = new UnityEngine.Color(.64f, .18f, .09f, 1f);
    colors.selectedColor = colors.highlightedColor;
    colors.pressedColor = new UnityEngine.Color(.48f, .10f, .045f, 1f);
    colors.disabledColor = new UnityEngine.Color(label.color.r, label.color.g, label.color.b, .36f);
    button.colors = colors;
    label.color = UnityEngine.Color.white;
    var oldFeedback = oldButton.GetComponent<UIButtonTweenFeedback>();
    var feedback = button.gameObject.AddComponent<UIButtonTweenFeedback>();
    UnityEditor.EditorUtility.CopySerialized(oldFeedback, feedback);
    var feedbackSerialized = new UnityEditor.SerializedObject(feedback);
    feedbackSerialized.FindProperty("visualRoot").objectReferenceValue = label.rectTransform;
    feedbackSerialized.ApplyModifiedPropertiesWithoutUndo();
}

var volume = panel.AddComponent<UnityEngine.Rendering.HighDefinition.CustomPassVolume>();
volume.enabled = false;
volume.injectionPoint = UnityEngine.Rendering.HighDefinition.CustomPassInjectionPoint.AfterPostProcess;
volume.customPasses.Clear();
var controller = panel.AddComponent<NewspaperMenuUI>();
var serialized = new UnityEditor.SerializedObject(controller);
void Reference(string name, UnityEngine.Object value) { serialized.FindProperty(name).objectReferenceValue = value; }
void TextReference(string field, string name) { Reference(field, spread.Find(name).GetComponent<TMPro.TMP_Text>()); }
void TextReferences(string field, string[] names)
{
    var property = serialized.FindProperty(field);
    property.arraySize = names.Length;
    for (int i = 0; i < names.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = spread.Find(names[i]).GetComponent<TMPro.TMP_Text>();
}
Reference("paper", spread);
Reference("photo", spread.Find("FactoryPhoto").GetComponent<UnityEngine.UI.RawImage>());
Reference("printMaterial", material);
Reference("captureVolume", volume);
Reference("copy", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(copyPath));
TextReference("masthead", "Masthead");
TextReference("englishTitle", "EnglishTitle");
TextReferences("headings", new[] { "LeadHeading", "CanteenHeading", "CinemaHeading", "ToolsHeading" });
TextReferences("subheadings", new[] { "LeadSubheading", "CanteenSubheading", "CinemaSubheading" });
TextReferences("bodies", new[] { "leadColumn1", "leadColumn2", "leadColumn3", "canteenColumn1", "canteenColumn2", "canteenColumn3", "cinemaColumn1", "cinemaColumn2", "toolsColumn1", "toolsColumn2", "toolsColumn3" });
TextReferences("noticeHeadings", new[] { "RepairsHeading", "LibraryHeading", "LostHeading" });
TextReferences("noticeBodies", new[] { "RepairsBody", "LibraryBody", "LostBody" });
TextReference("photoHeading", "PhotoPageHeading");
TextReference("photoCaption", "PhotoCaption");
TextReference("footer", "LeftFooter");
serialized.ApplyModifiedPropertiesWithoutUndo();
controller.ApplyCopy();
var managerSerialized = new UnityEditor.SerializedObject(manager);
managerSerialized.FindProperty("newspaper").objectReferenceValue = controller;
managerSerialized.ApplyModifiedPropertiesWithoutUndo();
UnityEngine.Object.DestroyImmediate(oldPaper.gameObject);
presentation.Find("BackdropShade").gameObject.SetActive(false);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
UnityEngine.Canvas.ForceUpdateCanvases();
return new { controller = controller.name, labels = spread.GetComponentsInChildren<TMPro.TMP_Text>().Length, passDisabledInEditMode = !volume.enabled,
    buttons = spread.GetComponentsInChildren<UnityEngine.UI.Button>().Select(b => new { b.name, b.interactable, events = Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Select(i => b.onClick.GetPersistentMethodName(i)).ToArray() }).ToArray(),
    missingScripts = root.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t => UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) };
