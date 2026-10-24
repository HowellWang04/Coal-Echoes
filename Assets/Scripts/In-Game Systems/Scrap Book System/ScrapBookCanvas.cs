using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1)]
public class ScrapBookCanvas : MonoBehaviour
{
    private const float AlbumPhotoAspect = 16f / 9f;

    [Header("Prefabs")]
    [SerializeField] private GameObject textBoxPrefab;

    [Header("Default Element Size")]
    [SerializeField] private Vector2 defaultPhotoSize = new Vector2(120, 120);

    [Header("Echo Note")]
    [SerializeField] private Sprite echoNotePaper;
    [SerializeField] private Sprite echoNoteTape;
    [SerializeField] private TMP_FontAsset echoNoteFont;
    [SerializeField] private float echoNoteWidth = 130f;
    [SerializeField] private Vector4 echoNotePadding = new Vector4(9, 9, 8, 13); // left, right, top, bottom; bottom leaves room for the torn edge
    [SerializeField] private float echoNoteOverlap = 6f; // how far the note tucks under the photo edge
    [SerializeField] private Vector2 echoTapeSize = new Vector2(46f, 13f);
    [SerializeField] private float echoNoteFontSize = 12f;
    [SerializeField] private Color echoNoteTextColor = new Color(0.361f, 0.282f, 0.196f);
    [SerializeField] private Color echoTagColor = new Color(0.659f, 0.263f, 0.173f); // same red ink as the ECHO stamp

    [Header("Text Box")]
    // Keeps text inside the TextArea frame lines; ScrapBookTextBox blocks input that would overflow this area.
    [SerializeField] private Vector2 textBoxPadding = new Vector2(16, 12);
    [SerializeField] private Color textBoxTextColor = new Color(0.361f, 0.282f, 0.196f); // matches the task hint text
    [SerializeField] private float textBoxFontSize = 14f;

    [Header("Context Menu")]
    [SerializeField] private ScrapBookElementContextMenu elementContextMenu;

    private RectTransform rect;

    private GameObject photoElementPrefab;

    // Every page's elements live under this canvas; only the current page's are active.
    public int CurrentPage { get; private set; }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void ShowPage(int page)
    {
        CurrentPage = page;
        if (elementContextMenu != null) elementContextMenu.Hide();

        foreach (Transform child in transform)
        {
            var meta = child.GetComponent<ScrapBookElementMeta>();
            if (meta != null) child.gameObject.SetActive(meta.page == page);
        }
    }

    public void ClearElements()
    {
        if (elementContextMenu != null) elementContextMenu.Hide();
        foreach (Transform child in transform)
            if (child.GetComponent<ScrapBookElementMeta>() != null) Destroy(child.gameObject);
        CurrentPage = 0;
    }

    public void SetPhotoElementPrefab(GameObject prefab)
    {
        photoElementPrefab = prefab;
    }

    public GameObject AddPhotoElement(Texture2D tex, Vector2 localPos, string fileName)
    {
        GameObject go = CreatePhotoVisual(tex, fileName, transform);
        go.name = "ScrapElement";
        go.GetComponent<RectTransform>().anchoredPosition = localPos;

        var element = go.GetComponent<ScrapBookElement>();
        if (element == null) element = go.AddComponent<ScrapBookElement>();
        element.Init(rect, elementContextMenu);

        var meta = go.AddComponent<ScrapBookElementMeta>();
        meta.type = "photo";
        meta.id = fileName;
        meta.page = CurrentPage;

        return go;
    }

    // The photo as it looks on the canvas, with its Echo mark and note, at scale 1.
    // AddPhotoElement and the top bar's drag ghost both build from this, so the ghost matches what gets placed.
    public GameObject CreatePhotoVisual(Texture tex, string fileName, Transform parent)
    {
        GameObject go = Instantiate(photoElementPrefab, parent);

        var elRect = go.GetComponent<RectTransform>();
        elRect.anchorMin = new Vector2(0.5f, 0.5f);
        elRect.anchorMax = new Vector2(0.5f, 0.5f);
        elRect.pivot = new Vector2(0.5f, 0.5f);
        elRect.anchoredPosition = Vector2.zero;
        Vector2 photoSize = new Vector2(defaultPhotoSize.x, defaultPhotoSize.x / AlbumPhotoAspect);
        ScrapBookPhotoVisual.Configure(go, tex, photoSize);

        // A canvas photo is not a button. If its Button stays, UIAnimationScope can give it the album's hover zoom
        // (it scans all Buttons when it wakes, which can happen after a restore), and that zoom fights ScrapBookElement's scale.
        DestroyImmediate(go.GetComponent<Button>());

        var echo = EchoPhotoStorage.Find(fileName);
        if (echo != null)
        {
            go.GetComponent<EchoPhotoMark>().Show();
            AddEchoNote(go, echo);
        }

        return go;
    }

    // Read-only handwritten note taped to the photo, one entry per Echo text, placed by the Echo's saved layout.
    private void AddEchoNote(GameObject photoGo, EchoPhotoEntry echo)
    {
        GameObject noteGo = new GameObject("EchoNote", typeof(RectTransform));
        noteGo.transform.SetParent(photoGo.transform, false);
        noteGo.transform.SetSiblingIndex(photoGo.GetComponent<EchoPhotoMark>().Stamp.GetSiblingIndex()); // stamp stays on top
        var noteRect = (RectTransform)noteGo.transform;
        noteRect.sizeDelta = new Vector2(echoNoteWidth, 0f);

        // The paper takes raycasts so pressing on the note drags, rotates or deletes the photo (events bubble to its ScrapBookElement).
        var paper = noteGo.AddComponent<Image>();
        paper.sprite = echoNotePaper;
        paper.type = Image.Type.Sliced;

        var shadow = noteGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.24f, 0.16f, 0.08f, 0.3f);
        shadow.effectDistance = new Vector2(1.5f, -2f);

        var layout = noteGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(
            (int)echoNotePadding.x, (int)echoNotePadding.y, (int)echoNotePadding.z, (int)echoNotePadding.w);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var fitter = noteGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(noteGo.transform, false);

        string tag = $"<size=70%><cspace=0.15em><color=#{ColorUtility.ToHtmlStringRGB(echoTagColor)}>ECHO</color></cspace></size>";
        string separator = $"\n<size=60%><color=#{ColorUtility.ToHtmlStringRGB(echoNoteTextColor)}59>- - - - - - - - - - - -</color></size>\n";
        var entries = new List<string>();
        foreach (string text in echo.scrapbookContents) entries.Add(tag + "\n" + text.Trim());

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Join(separator, entries);
        if (echoNoteFont != null) tmp.font = echoNoteFont;
        tmp.fontSize = echoNoteFontSize;
        tmp.color = echoNoteTextColor;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;

        GameObject tapeGo = new GameObject("Tape", typeof(RectTransform));
        tapeGo.transform.SetParent(noteGo.transform, false);
        var tapeRect = (RectTransform)tapeGo.transform;
        tapeRect.sizeDelta = echoTapeSize;
        tapeGo.AddComponent<LayoutElement>().ignoreLayout = true;
        var tape = tapeGo.AddComponent<Image>();
        tape.sprite = echoNoteTape;
        tape.raycastTarget = false;

        PlaceEchoNote(noteRect, tapeRect, ((RectTransform)photoGo.transform).sizeDelta, echo.noteLayout);

        // Size the note now, so bounds checks in the same frame (drag ghost, canvas clamp) already include it.
        LayoutRebuilder.ForceRebuildLayoutImmediate(noteRect);
    }

    // The note's pivot is the middle of its edge that touches the photo; that edge tucks a little under the photo.
    private void PlaceEchoNote(RectTransform noteRect, RectTransform tapeRect, Vector2 photoSize, EchoNoteLayout layout)
    {
        Vector2 half = photoSize * 0.5f;
        switch (layout.side)
        {
            case EchoNoteSide.Below:
                noteRect.pivot = new Vector2(0.5f, 1f);
                noteRect.anchoredPosition = new Vector2(layout.shift * photoSize.x, -(half.y - echoNoteOverlap));
                break;
            case EchoNoteSide.Above:
                noteRect.pivot = new Vector2(0.5f, 0f);
                noteRect.anchoredPosition = new Vector2(layout.shift * photoSize.x, half.y - echoNoteOverlap);
                break;
            case EchoNoteSide.Left:
                noteRect.pivot = new Vector2(1f, 0.5f);
                noteRect.anchoredPosition = new Vector2(-(half.x - echoNoteOverlap), layout.shift * photoSize.y);
                break;
            case EchoNoteSide.Right:
                noteRect.pivot = new Vector2(0f, 0.5f);
                noteRect.anchoredPosition = new Vector2(half.x - echoNoteOverlap, layout.shift * photoSize.y);
                break;
        }
        noteRect.localRotation = Quaternion.Euler(0f, 0f, layout.angle);

        // Tape across that same edge, tilted against the note.
        tapeRect.anchorMin = tapeRect.anchorMax = noteRect.pivot;
        tapeRect.anchoredPosition = Vector2.zero;
        tapeRect.localRotation = Quaternion.Euler(0f, 0f, layout.angle < 0f ? 12f : -12f);
    }

    public void AddDecoElement(ScrapBookDecoPreset preset, Vector2 localPos)
    {
        if (preset.decoType == DecoType.Sticker)
            AddStickerElement(preset, localPos);
        else if (preset.decoType == DecoType.TextBox)
            AddTextBoxElement(preset, localPos);
    }

    private GameObject AddStickerElement(ScrapBookDecoPreset preset, Vector2 localPos)
    {
        GameObject go = CreateElementBase(localPos);

        var image = go.AddComponent<Image>();
        image.sprite = preset.icon;
        image.preserveAspect = true;
        image.raycastTarget = true;

        go.GetComponent<RectTransform>().sizeDelta = preset.defaultSize;

        var meta = go.AddComponent<ScrapBookElementMeta>();
        meta.type = "sticker";
        meta.id = preset.presetName;
        meta.page = CurrentPage;

        return go;
    }

    private GameObject AddTextBoxElement(ScrapBookDecoPreset preset, Vector2 localPos)
    {
        GameObject go;

        if (textBoxPrefab != null)
        {
            go = Instantiate(textBoxPrefab, transform);
            go.GetComponent<RectTransform>().anchoredPosition = localPos;
        }
        else
        {
            go = CreateTextBoxFromCode(localPos);
        }

        go.GetComponent<RectTransform>().sizeDelta = preset.defaultSize;

        // Use preset icon as text box background
        var bg = go.GetComponent<Image>();
        if (bg != null && preset.icon != null)
        {
            bg.sprite = preset.icon;
            bg.type = Image.Type.Sliced;
        }

        var element = go.GetComponent<ScrapBookElement>();
        if (element == null) element = go.AddComponent<ScrapBookElement>();
        element.Init(rect, elementContextMenu);

        var textBox = go.GetComponent<ScrapBookTextBox>();
        if (textBox != null) textBox.Init(); // starts on the placeholder

        var meta = go.AddComponent<ScrapBookElementMeta>();
        meta.type = "textbox";
        meta.id = preset.presetName;
        meta.page = CurrentPage;

        return go;
    }

    private GameObject CreateTextBoxFromCode(Vector2 localPos)
    {
        // Root object with background
        GameObject go = new GameObject("TextBoxElement");
        go.transform.SetParent(transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = localPos;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.9f);
        bg.raycastTarget = true;

        // Display text
        GameObject textGo = new GameObject("DisplayText");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textBoxPadding;
        textRect.offsetMax = -textBoxPadding;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Text";
        tmp.fontSize = textBoxFontSize;
        tmp.color = textBoxTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        // Input field (hidden by default)
        GameObject inputGo = new GameObject("InputField");
        inputGo.transform.SetParent(go.transform, false);
        var inputRect = inputGo.AddComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = textBoxPadding;
        inputRect.offsetMax = -textBoxPadding;

        // TextArea child for TMP_InputField
        GameObject textAreaGo = new GameObject("TextArea");
        textAreaGo.transform.SetParent(inputGo.transform, false);
        var textAreaRect = textAreaGo.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = Vector2.zero;
        textAreaRect.offsetMax = Vector2.zero;
        // No RectMask2D: it clips axis-aligned and cuts the text once the box is rotated.
        // ScrapBookTextBox already keeps text inside this area.

        GameObject inputTextGo = new GameObject("Text");
        inputTextGo.transform.SetParent(textAreaGo.transform, false);
        var inputTextRect = inputTextGo.AddComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = Vector2.zero;
        inputTextRect.offsetMax = Vector2.zero;

        var inputTmp = inputTextGo.AddComponent<TextMeshProUGUI>();
        inputTmp.fontSize = textBoxFontSize;
        inputTmp.color = textBoxTextColor;
        inputTmp.alignment = TextAlignmentOptions.Center;
        inputTmp.textWrappingMode = TextWrappingModes.Normal;

        var inputField = inputGo.AddComponent<TMP_InputField>();
        inputField.textComponent = inputTmp;
        inputField.textViewport = textAreaRect;
        inputField.lineType = TMP_InputField.LineType.MultiLineNewline; // Enter adds a line break instead of ending the edit

        inputGo.SetActive(false);

        // ScrapBookTextBox component
        var textBox = go.AddComponent<ScrapBookTextBox>();
        // Use reflection-free approach: assign via serialized fields in prefab,
        // or use GetComponentInChildren at runtime
        // For code-created version, we use a setup method
        textBox.InitRefs(inputField, tmp);

        return go;
    }

    private GameObject CreateElementBase(Vector2 localPos)
    {
        GameObject go = new GameObject("ScrapElement");
        go.transform.SetParent(transform, false);

        var elRect = go.AddComponent<RectTransform>();
        elRect.anchoredPosition = localPos;
        elRect.sizeDelta = defaultPhotoSize;

        var element = go.AddComponent<ScrapBookElement>();
        element.Init(rect, elementContextMenu);

        return go;
    }

    // --- Save / Restore ---

    // Walk all placed elements and read their saved state.
    public List<ScrapBookElementData> GetAllElementData()
    {
        var list = new List<ScrapBookElementData>();

        foreach (Transform child in transform)
        {
            var meta = child.GetComponent<ScrapBookElementMeta>();
            if (meta == null) continue;

            var childRect = child.GetComponent<RectTransform>();
            var data = new ScrapBookElementData
            {
                type = meta.type,
                id = meta.id,
                position = childRect.anchoredPosition,
                rotation = childRect.localEulerAngles.z,
                scale = childRect.localScale.x,
                text = "",
                page = meta.page
            };

            if (meta.type == "textbox")
            {
                var textBox = child.GetComponent<ScrapBookTextBox>();
                if (textBox != null) data.text = textBox.Text;
            }

            list.Add(data);
        }

        return list;
    }

    // Recreate one element from saved data. preset is required for sticker/textbox, null for photo.
    public void RebuildElement(ScrapBookElementData data, ScrapBookDecoPreset preset)
    {
        GameObject go = null;

        if (data.type == "photo")
        {
            Texture2D tex = CameraPhotoStorage.LoadTexture(data.id);
            if (tex == null) return;
            go = AddPhotoElement(tex, data.position, data.id);
        }
        else if (data.type == "sticker")
        {
            if (preset == null) return;
            go = AddStickerElement(preset, data.position);
        }
        else if (data.type == "textbox")
        {
            if (preset == null) return;
            go = AddTextBoxElement(preset, data.position);
            var textBox = go.GetComponent<ScrapBookTextBox>();
            if (textBox != null) textBox.Init(data.text);
        }

        if (go == null) return;

        var rt = go.GetComponent<RectTransform>();
        rt.localRotation = Quaternion.Euler(0, 0, data.rotation);

        var element = go.GetComponent<ScrapBookElement>();
        if (element != null) element.SetScale(data.scale);

        go.GetComponent<ScrapBookElementMeta>().page = data.page;
    }
}
