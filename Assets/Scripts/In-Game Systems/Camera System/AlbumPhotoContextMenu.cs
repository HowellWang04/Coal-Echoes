using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AlbumPhotoContextMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button addToScrapBookButton;

    private string currentFileName;
    private Texture2D currentTexture;
    private RectTransform currentSourceRect;
    private ScrapbookFlyFeedback scrapbookFlyFeedback;

    private void Awake()
    {
        scrapbookFlyFeedback = GetComponentInParent<UIAnimationScope>()
            .GetComponentInChildren<ScrapbookFlyFeedback>(true);
        Hide();
        if (addToScrapBookButton != null)
            addToScrapBookButton.onClick.AddListener(OnAddToScrapBook);
    }

    public void Show(string fileName, Texture2D texture, RectTransform sourceRect, Vector2 screenPosition)
    {
        currentFileName = fileName;
        currentTexture = texture;
        currentSourceRect = sourceRect;

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            menuPanel.GetComponent<RectTransform>().position = screenPosition;
        }
    }

    public void Hide()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        currentFileName = null;
        currentTexture = null;
        currentSourceRect = null;
    }

    private void OnAddToScrapBook()
    {
        if (string.IsNullOrEmpty(currentFileName)) return;

        bool added = ScrapBookDataManager.Instance.RegisterPhoto(currentFileName);
        if (added)
            scrapbookFlyFeedback.Play(currentTexture, currentSourceRect);
        Hide();
    }

    private void Update()
    {
        // Click anywhere else to close the menu
        if (menuPanel != null && menuPanel.activeSelf)
        {
            var mouse = Mouse.current;
            if (mouse != null
                && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            {
                // Check if click is outside the menu
                RectTransform rt = menuPanel.GetComponent<RectTransform>();
                if (!RectTransformUtility.RectangleContainsScreenPoint(rt, mouse.position.ReadValue()))
                    Hide();
            }
        }
    }
}
