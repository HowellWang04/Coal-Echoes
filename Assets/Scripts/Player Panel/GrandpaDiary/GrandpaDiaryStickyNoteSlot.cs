using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GrandpaDiaryPageSide
{
    Left,
    Right
}

public class GrandpaDiaryStickyNoteSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string slotId;
    [SerializeField] private GrandpaDiaryPageSide pageSide;
    [SerializeField] private GameObject noteRoot;
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private GrandpaDiaryStickyNoteStyleSet styleSet;
    [SerializeField] private Image noteImage;
    [SerializeField, Range(0, 3)] private int previewStyleIndex;
    [SerializeField, Range(0f, 1f)] private float normalAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float hoverAlpha = 0.35f;

    public string SlotId => slotId;
    public GrandpaDiaryPageSide PageSide => pageSide;

    private bool initialized;
    private CanvasGroup canvasGroup;
    private RectTransform noteRect;
    private Vector2 defaultAnchoredPosition;

    [ContextMenu("Apply Preview Style")]
    private void ApplyPreviewStyle()
    {
        ApplyStyle(previewStyleIndex);
    }

    private void Awake()
    {
        EnsureInitialized();
        Hide();
    }

    public bool Matches(string id)
    {
        return !string.IsNullOrEmpty(slotId) && slotId == id;
    }

    public bool MatchesPageSide(string side)
    {
        return pageSide == GrandpaDiaryPageSide.Left
            ? side == "left"
            : side == "right";
    }

    public void Show(string id, string content, bool useCustomAnchoredPosition, Vector2 customAnchoredPosition)
    {
        EnsureInitialized();
        int styleIndex = styleSet != null
            ? GrandpaDiaryStickyNoteStorage.GetOrCreateStyleIndex(id, styleSet.StyleCount)
            : previewStyleIndex;
        ApplyStyle(styleIndex);

        if (noteRect != null)
            noteRect.anchoredPosition = useCustomAnchoredPosition
                ? customAnchoredPosition
                : defaultAnchoredPosition;

        if (noteRoot != null) noteRoot.SetActive(true);
        if (noteText != null) noteText.text = content ?? "";
        SetAlpha(normalAlpha);
    }

    public void Hide()
    {
        EnsureInitialized();

        if (noteText != null) noteText.text = "";
        SetAlpha(normalAlpha);
        if (noteRoot != null) noteRoot.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetAlpha(hoverAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetAlpha(normalAlpha);
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        if (noteRoot == null)
            noteRoot = gameObject;

        noteRect = noteRoot.transform as RectTransform;
        if (noteRect != null)
            defaultAnchoredPosition = noteRect.anchoredPosition;

        if (noteText == null)
            noteText = GetComponent<TMP_Text>();

        if (noteText == null)
            noteText = GetComponentInChildren<TMP_Text>(true);

        if (noteImage == null)
            noteImage = noteRoot.GetComponent<Image>();

        canvasGroup = noteRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = noteRoot.AddComponent<CanvasGroup>();

        if (noteText == null)
            Debug.LogWarning($"[GrandpaDiary] Sticky note slot '{name}' has no TMP_Text target.");

        initialized = true;
    }

    private void ApplyStyle(int styleIndex)
    {
        GrandpaDiaryStickyNoteStyleSet.Style style = styleSet != null
            ? styleSet.GetStyle(styleIndex)
            : null;

        if (style == null)
            return;

        if (noteImage != null)
            noteImage.sprite = style.sprite;

        if (noteText == null)
            return;

        RectTransform textRect = noteText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = style.textAnchoredPosition;
        textRect.sizeDelta = style.textSize;
        textRect.localEulerAngles = new Vector3(0f, 0f, style.textRotation);
        textRect.localScale = Vector3.one;
        noteText.margin = style.textSafeMargins;
    }

    private void SetAlpha(float alpha)
    {
        EnsureInitialized();

        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }
}
