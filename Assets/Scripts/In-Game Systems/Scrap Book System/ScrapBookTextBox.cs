using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrapBookTextBox : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text displayText;

    public string Text => displayText != null ? displayText.text : "";

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
        }
    }

    public void Init(string defaultText = "Text")
    {
        if (displayText != null) displayText.text = defaultText;
        if (inputField != null) inputField.text = defaultText;
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
            inputField.text = displayText != null ? displayText.text : "";
            inputField.gameObject.SetActive(true);
            inputField.ActivateInputField();
        }

        if (displayText != null) displayText.gameObject.SetActive(false);
    }

    private void OnEndEdit(string text)
    {
        isEditing = false;

        if (displayText != null)
        {
            displayText.text = text;
            displayText.gameObject.SetActive(true);
        }

        if (inputField != null) inputField.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (inputField != null) inputField.onEndEdit.RemoveAllListeners();
    }
}
