using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off builder for the level 1 hint popup: imports the sprites, makes the row prefab,
// builds the popup under In-game UI Root and adds Level1HintFlowManager under Managers.
internal static class HintPopupSetup
{
    private const string SpriteDir = "Assets/Art Resources/UI/Hint Popup/";
    private const string RowPrefabPath = "Assets/Prefabs/HintTaskRow.prefab";
    private const string HintJsonPath = "Assets/Narrative/level1-hints.json";
    private const string FontPath = "Assets/Art Resources/UI/Fonts/Imprima 400 SDF.asset";
    private static readonly Color Ink = new Color32(92, 72, 50, 255);

    [MenuItem("Tools/Coal Echoes/Hint Popup/Build Level 1 Hint Popup")]
    private static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var uiRoot = FindInScene("In-game UI Root");
        var managers = FindInScene("Managers");
        if (uiRoot == null || managers == null)
        {
            Debug.LogError("[HintPopupSetup] Cannot find In-game UI Root or Managers in the active scene.");
            return;
        }
        if (uiRoot.Find("HintPopupUIRoot") != null)
        {
            Debug.LogError("[HintPopupSetup] HintPopupUIRoot already exists. Delete it first to rebuild.");
            return;
        }

        ImportSprite("HintPopup_BG.png", new Vector4(24, 24, 24, 24));
        ImportSprite("HintBox_Unfinished.png", Vector4.zero);
        ImportSprite("HintBox_Finished.png", Vector4.zero);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var rowPrefab = BuildRowPrefab(font);

        // UI root: full screen, holds the manager so it keeps running while the popup is hidden.
        var root = new GameObject("HintPopupUIRoot", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Build Level 1 Hint Popup");
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(uiRoot, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Popup panel, top right, below the echo / diary popups.
        var popup = new GameObject("HintPopup", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var popupRect = (RectTransform)popup.transform;
        popupRect.SetParent(rootRect, false);
        popupRect.anchorMin = popupRect.anchorMax = popupRect.pivot = Vector2.one;
        popupRect.anchoredPosition = new Vector2(-40f, -130f);
        popupRect.sizeDelta = new Vector2(460f, 0f);

        var background = popup.GetComponent<Image>();
        background.sprite = LoadSprite("HintPopup_BG.png");
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;

        var popupLayout = popup.GetComponent<VerticalLayoutGroup>();
        popupLayout.padding = new RectOffset(22, 22, 18, 18);
        popupLayout.spacing = 10f;
        popupLayout.childControlWidth = true;
        popupLayout.childControlHeight = true;
        popupLayout.childForceExpandWidth = true;
        popupLayout.childForceExpandHeight = false;
        popup.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var heading = CreateText("Heading", popupRect, font, 21f, FontStyles.Bold);

        var rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
        var rowsRect = (RectTransform)rows.transform;
        rowsRect.SetParent(popupRect, false);
        var rowsLayout = rows.GetComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 8f;
        rowsLayout.childControlWidth = true;
        rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;

        var manager = root.AddComponent<HintPopupUIManager>();
        var managerSo = new SerializedObject(manager);
        managerSo.FindProperty("hintJson").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(HintJsonPath);
        managerSo.FindProperty("popupRoot").objectReferenceValue = popup;
        managerSo.FindProperty("headingText").objectReferenceValue = heading;
        managerSo.FindProperty("rowsRoot").objectReferenceValue = rowsRect;
        managerSo.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
        managerSo.FindProperty("unfinishedBox").objectReferenceValue = LoadSprite("HintBox_Unfinished.png");
        managerSo.FindProperty("finishedBox").objectReferenceValue = LoadSprite("HintBox_Finished.png");
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        var flow = new GameObject("Level1HintFlowManager");
        Undo.RegisterCreatedObjectUndo(flow, "Build Level 1 Hint Popup");
        flow.transform.SetParent(managers, false);
        var flowSo = new SerializedObject(flow.AddComponent<Level1HintFlowManager>());
        flowSo.FindProperty("hintPopup").objectReferenceValue = manager;
        flowSo.FindProperty("playerPanel").objectReferenceValue = Object.FindAnyObjectByType<PlayerPanelUIManager>(FindObjectsInactive.Include);
        flowSo.FindProperty("cameraSystemManager").objectReferenceValue = Object.FindAnyObjectByType<CameraSystemManager>(FindObjectsInactive.Include);
        flowSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log("[HintPopupSetup] Built HintPopupUIRoot and Level1HintFlowManager. Save the scene to keep them.");
    }

    private static HintTaskRow BuildRowPrefab(TMP_FontAsset font)
    {
        var existing = AssetDatabase.LoadAssetAtPath<HintTaskRow>(RowPrefabPath);
        if (existing != null) return existing;

        var row = new GameObject("HintTaskRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        box.transform.SetParent(row.transform, false);
        var boxImage = box.GetComponent<Image>();
        boxImage.sprite = LoadSprite("HintBox_Unfinished.png");
        boxImage.preserveAspect = true;
        boxImage.raycastTarget = false;
        var boxLayout = box.GetComponent<LayoutElement>();
        boxLayout.minWidth = boxLayout.preferredWidth = 22f;
        boxLayout.minHeight = boxLayout.preferredHeight = 22f;

        var label = CreateText("Label", (RectTransform)row.transform, font, 19f, FontStyles.Normal);
        // Small preferred width so the label takes the space left by the box instead of its unwrapped length.
        var labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 1f;
        labelLayout.flexibleWidth = 1f;

        var rowComponent = row.AddComponent<HintTaskRow>();
        var so = new SerializedObject(rowComponent);
        so.FindProperty("box").objectReferenceValue = boxImage;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("layout").objectReferenceValue = layout;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(row, RowPrefabPath);
        Object.DestroyImmediate(row);
        return prefab.GetComponent<HintTaskRow>();
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Ink;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void ImportSprite(string fileName, Vector4 border)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(SpriteDir + fileName);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + fileName);
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
