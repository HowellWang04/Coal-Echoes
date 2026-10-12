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

    [Header("Echo Label")]
    [SerializeField] private float echoLabelGap = 8f;       // distance from photo edge
    [SerializeField] private float echoLabelWidth = 0f;     // 0 = match photo width
    [SerializeField] private float echoLabelFontSize = 14f;
    [SerializeField] private Color echoLabelColor = Color.black;

    private RectTransform rect;

    private GameObject photoElementPrefab;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void SetPhotoElementPrefab(GameObject prefab)
    {
        photoElementPrefab = prefab;
    }

    public GameObject AddPhotoElement(Texture2D tex, Vector2 localPos, string fileName)
    {
        GameObject go = Instantiate(photoElementPrefab, transform);
        go.name = "ScrapElement";

        var elRect = go.GetComponent<RectTransform>();
        elRect.anchorMin = new Vector2(0.5f, 0.5f);
        elRect.anchorMax = new Vector2(0.5f, 0.5f);
        elRect.pivot = new Vector2(0.5f, 0.5f);
        elRect.anchoredPosition = localPos;
        Vector2 photoSize = new Vector2(defaultPhotoSize.x, defaultPhotoSize.x / AlbumPhotoAspect);
        ScrapBookPhotoVisual.Configure(go, tex, photoSize);

        var element = go.GetComponent<ScrapBookElement>();
        if (element == null) element = go.AddComponent<ScrapBookElement>();
        element.Init(rect);

        var meta = go.AddComponent<ScrapBookElementMeta>();
        meta.type = "photo";
        meta.id = fileName;

        AddEchoLabel(go, fileName);

        return go;
    }

    // Read-only Echo text block, child of the photo, derived from EchoPhotoStorage.
    // Placed on the side away from the canvas edge; height grows from the photo outward.
    private void AddEchoLabel(GameObject photoGo, string fileName)
    {
        var texts = EchoPhotoStorage.Load(fileName);
        if (texts == null || texts.Count == 0) return;

        var photoRect = photoGo.GetComponent<RectTransform>();
        float photoW = photoRect.sizeDelta.x;
        float photoH = photoRect.sizeDelta.y;

        GameObject labelGo = new GameObject("EchoLabel");
        labelGo.transform.SetParent(photoGo.transform, false);

        var rt = labelGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(echoLabelWidth > 0f ? echoLabelWidth : photoW, 0f);

        // Photo in lower half -> place above; otherwise below. Pivot pins the near edge to the photo.
        bool placeAbove = photoRect.anchoredPosition.y < 0f;
        if (placeAbove)
        {
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, photoH * 0.5f + echoLabelGap);
        }
        else
        {
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(photoH * 0.5f + echoLabelGap));
        }

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = string.Join("\n\n", texts);
        tmp.fontSize = echoLabelFontSize;
        tmp.color = echoLabelColor;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false; // visual only; the photo handles all interaction

        var fitter = labelGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
        element.Init(rect);

        var textBox = go.GetComponent<ScrapBookTextBox>();
        if (textBox != null) textBox.Init("Text");

        var meta = go.AddComponent<ScrapBookElementMeta>();
        meta.type = "textbox";
        meta.id = preset.presetName;

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
        textRect.offsetMin = new Vector2(8, 4);
        textRect.offsetMax = new Vector2(-8, -4);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Text";
        tmp.fontSize = 18;
        tmp.color = Color.black;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        // Input field (hidden by default)
        GameObject inputGo = new GameObject("InputField");
        inputGo.transform.SetParent(go.transform, false);
        var inputRect = inputGo.AddComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = new Vector2(8, 4);
        inputRect.offsetMax = new Vector2(-8, -4);

        // TextArea child for TMP_InputField
        GameObject textAreaGo = new GameObject("TextArea");
        textAreaGo.transform.SetParent(inputGo.transform, false);
        var textAreaRect = textAreaGo.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = Vector2.zero;
        textAreaRect.offsetMax = Vector2.zero;
        textAreaGo.AddComponent<RectMask2D>();

        GameObject inputTextGo = new GameObject("Text");
        inputTextGo.transform.SetParent(textAreaGo.transform, false);
        var inputTextRect = inputTextGo.AddComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = Vector2.zero;
        inputTextRect.offsetMax = Vector2.zero;

        var inputTmp = inputTextGo.AddComponent<TextMeshProUGUI>();
        inputTmp.fontSize = 18;
        inputTmp.color = Color.black;
        inputTmp.alignment = TextAlignmentOptions.Center;

        var inputField = inputGo.AddComponent<TMP_InputField>();
        inputField.textComponent = inputTmp;
        inputField.textViewport = textAreaRect;

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
        element.Init(rect);

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
                text = ""
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
    }
}
