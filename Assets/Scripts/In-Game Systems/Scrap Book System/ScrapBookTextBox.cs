using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrapBookTextBox : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text displayText;

    [Header("Placeholder")]
    // Shown faded while the box is empty; cleared when editing starts and never saved.
    [SerializeField] private string placeholderText = "Feel free to write something here";
    [SerializeField] private float placeholderAlpha = 0.5f;

    public string Text => displayText == null || showingPlaceholder ? "" : displayText.text;

    private bool showingPlaceholder;
    private bool isEditing;
    private int clickCount;
    private float lastClickTime;
    private const float DoubleClickThreshold = 0.3f;

    private void Awake()
    {
        if (inputField != null)
        {
            inputField.gameObject.SetActive(false);
            inputField.onEndEdit.AddListener(OnEndEdit);
            inputField.onValidateInput = KeepInsideTextArea;
        }
    }

    /// <summary>
    /// Called by ScrapBookCanvas when creating text box from code (no prefab).
    /// </summary>
    public void InitRefs(TMP_InputField input, TMP_Text display)
    {
        inputField = input;
        displayText = display;

        if (inputField != null)
        {
            inputField.gameObject.SetActive(false);
            inputField.onEndEdit.AddListener(OnEndEdit);
            inputField.onValidateInput = KeepInsideTextArea;
        }
    }

    public void Init(string text = "")
    {
        SetDisplayText(text);
        if (inputField != null) inputField.text = text;
    }

    private void SetDisplayText(string text)
    {
        showingPlaceholder = string.IsNullOrEmpty(text);
        if (displayText == null) return;

        displayText.text = showingPlaceholder ? placeholderText : text;
        displayText.alpha = showingPlaceholder ? placeholderAlpha : 1f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Double-click detection
        if (Time.unscaledTime - lastClickTime < DoubleClickThreshold)
        {
            EnterEditMode();
        }
        lastClickTime = Time.unscaledTime;
    }

    private void EnterEditMode()
    {
        if (isEditing) return;
        isEditing = true;

        if (inputField != null)
        {
            inputField.text = Text; // empty while the placeholder shows
            inputField.gameObject.SetActive(true);
            inputField.ActivateInputField();
        }

        if (displayText != null) displayText.gameObject.SetActive(false);
    }

    // Runs before each typed or pasted character is inserted (TMP has already removed any selection from text).
    // Lays out the text as it would be with the character added; returns '\0' to drop it if that overflows the text area.
    private char KeepInsideTextArea(string text, int charIndex, char addedChar)
    {
        TMP_Text field = inputField.textComponent;
        Rect area = field.rectTransform.rect;
        string candidate = text.Insert(charIndex, addedChar.ToString());
        return field.GetPreferredValues(candidate, area.width, 0f).y <= area.height ? addedChar : '\0';
    }

    private void OnEndEdit(string text)
    {
        isEditing = false;

        SetDisplayText(text);
        if (displayText != null) displayText.gameObject.SetActive(true);

        if (inputField != null) inputField.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (inputField != null) inputField.onEndEdit.RemoveAllListeners();
    }
}
