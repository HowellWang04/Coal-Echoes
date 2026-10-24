using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off builder for the inner monologue subtitle: imports the sprites, makes the underlay
// material, builds the subtitle under In-game UI Root and hooks it into the level 1 hint flow.
internal static class InnerMonologueSetup
{
    private const string SpriteDir = "Assets/Art Resources/UI/Inner Monologue/";
    private const string MonologueJsonPath = "Assets/Narrative/inner-monologue.json";
    private const string FontPath = "Assets/Art Resources/UI/Fonts/Elegant Bloom SDF.asset";
    private const string UnderlayMaterialPath = "Assets/Art Resources/UI/Fonts/Elegant Bloom SDF - Monologue Underlay.mat";
    private static readonly Color PaperWhite = new Color32(232, 223, 205, 255);
    private static readonly Color CoalBlack = new Color32(41, 39, 36, 255);

    [MenuItem("Tools/Coal Echoes/Inner Monologue/Build Monologue Subtitle")]
    private static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var uiRoot = FindInScene("In-game UI Root");
        if (uiRoot == null)
        {
            Debug.LogError("[InnerMonologueSetup] Cannot find In-game UI Root in the active scene.");
            return;
        }
        if (uiRoot.Find("MonologueUIRoot") != null)
        {
            Debug.LogError("[InnerMonologueSetup] MonologueUIRoot already exists. Delete it first to rebuild.");
            return;
        }

        ImportSprite("ThoughtIcon_Cloud.png");
        ImportSprite("MonologueBand.png");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var underlay = CreateUnderlayMaterial(font);

        // UI root: full screen, holds the manager so it keeps running while the subtitle is hidden.
        // Drawn after the Player's Panel so a line triggered while reading stays visible.
        var root = new GameObject("MonologueUIRoot", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Build Monologue Subtitle");
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(uiRoot, false);
        Stretch(rootRect);
        var mainMenuRoot = uiRoot.Find("MainMenuUIRoot");
        if (mainMenuRoot != null) rootRect.SetSiblingIndex(mainMenuRoot.GetSiblingIndex());

        var subtitle = new GameObject("Subtitle", typeof(RectTransform), typeof(CanvasGroup));
        var subtitleRect = (RectTransform)subtitle.transform;
        subtitleRect.SetParent(rootRect, false);
        Stretch(subtitleRect);
        var group = subtitle.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var band = CreateImage("Band", subtitleRect, "MonologueBand.png", new Color(CoalBlack.r, CoalBlack.g, CoalBlack.b, 0.62f));
        band.rectTransform.sizeDelta = new Vector2(1200f, 170f);

        var icon = CreateImage("ThoughtIcon", subtitleRect, "ThoughtIcon_Cloud.png", PaperWhite);
        icon.rectTransform.sizeDelta = new Vector2(54f, 54f);
        icon.preserveAspect = true;
        var iconShadow = icon.gameObject.AddComponent<Shadow>();
        iconShadow.effectColor = new Color(CoalBlack.r, CoalBlack.g, CoalBlack.b, 0.6f);
        iconShadow.effectDistance = new Vector2(0f, -2f);

        // Text block centred at 86% of the screen height, as in the approved mockup.
        var body = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        var bodyRect = (RectTransform)body.transform;
        bodyRect.SetParent(subtitleRect, false);
        bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 0f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(0f, 151f);
        bodyRect.sizeDelta = new Vector2(1100f, 140f);
        var text = body.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        if (underlay != null) text.fontSharedMaterial = underlay;
        text.fontSize = 46f;
        text.color = PaperWhite;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.text = "Same gate, same spot. Just no one standing here anymore.";

        var manager = root.AddComponent<InnerMonologueUIManager>();
        var managerSo = new SerializedObject(manager);
        managerSo.FindProperty("monologueJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(MonologueJsonPath);
        managerSo.FindProperty("subtitleGroup").objectReferenceValue = group;
        managerSo.FindProperty("bodyText").objectReferenceValue = text;
        managerSo.FindProperty("thoughtIcon").objectReferenceValue = icon.rectTransform;
        managerSo.FindProperty("band").objectReferenceValue = band.rectTransform;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        var hintPopup = Object.FindAnyObjectByType<HintPopupUIManager>(FindObjectsInactive.Include);
        if (hintPopup != null) SetReference(hintPopup, "monologue", manager);
        var flow = Object.FindAnyObjectByType<Level1HintFlowManager>(FindObjectsInactive.Include);
        if (flow != null)
        {
            SetReference(flow, "monologue", manager);
            SetReference(flow, "mainMenu", Object.FindAnyObjectByType<MainMenuUIManager>(FindObjectsInactive.Include));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log("[InnerMonologueSetup] Built MonologueUIRoot and linked it to the hint flow. Save the scene to keep it.");
    }

    private static Material CreateUnderlayMaterial(TMP_FontAsset font)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(UnderlayMaterialPath);
        if (existing != null || font == null) return existing;

        // Soft coal-black shadow under the text, standing in for the mockup's blurred drop shadow.
        var material = new Material(font.material);
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", new Color(CoalBlack.r, CoalBlack.g, CoalBlack.b, 0.85f));
        material.SetFloat("_UnderlayOffsetX", 0f);
        material.SetFloat("_UnderlayOffsetY", -0.3f);
        material.SetFloat("_UnderlayDilate", 0.2f);
        material.SetFloat("_UnderlaySoftness", 0.6f);
        AssetDatabase.CreateAsset(material, UnderlayMaterialPath);
        return material;
    }

    private static Image CreateImage(string name, RectTransform parent, string spriteFile, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 151f);
        var image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + spriteFile);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetReference(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ImportSprite(string fileName)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(SpriteDir + fileName);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static Transform FindInScene(string name)
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
