using UnityEngine;

public class GrandpaDiaryPageView : MonoBehaviour
{
    [SerializeField] private string pageId;
    [SerializeField] private GameObject pageRoot;
    [SerializeField] private GrandpaDiaryTextAreaBinding[] textAreas;

    public string PageId => pageId;

    private bool initialized;
    private RectTransform leftPage;
    private RectTransform rightPage;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void SetPageId(string id)
    {
        pageId = id;
    }

    public void Show(GrandpaDiaryPageData pageData)
    {
        EnsureInitialized();

        if (pageRoot != null) pageRoot.SetActive(true);

        ClearTextAreas();

        if (pageData == null) return;

        ShowTextBlocks(pageData);
    }

    public void Hide()
    {
        EnsureInitialized();

        if (pageRoot != null) pageRoot.SetActive(false);
    }

    public bool TryGetPageHalves(out RectTransform left, out RectTransform right)
    {
        EnsureInitialized();
        left = leftPage;
        right = rightPage;
        return left != null && right != null;
    }

    public void BringToFront()
    {
        EnsureInitialized();
        pageRoot.transform.SetAsLastSibling();
    }

    public void ResetPageHalfScales()
    {
        EnsureInitialized();
        if (leftPage != null) leftPage.localScale = Vector3.one;
        if (rightPage != null) rightPage.localScale = Vector3.one;
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        if (pageRoot == null)
            pageRoot = gameObject;

        if (textAreas == null || textAreas.Length == 0)
            textAreas = GetComponentsInChildren<GrandpaDiaryTextAreaBinding>(true);

        leftPage = pageRoot.transform.Find("LeftPage") as RectTransform;
        rightPage = pageRoot.transform.Find("RightPage") as RectTransform;

        if (textAreas == null || textAreas.Length == 0)
            Debug.LogWarning($"[GrandpaDiary] Page view '{name}' has no text area bindings.");

        initialized = true;
    }

    private void ShowTextBlocks(GrandpaDiaryPageData pageData)
    {
        if (pageData.textBlocks == null) return;

        for (int index = 0; index < pageData.textBlocks.Count; index++)
        {
            var block = pageData.textBlocks[index];
            if (block == null || string.IsNullOrEmpty(block.markerId)) continue;

            var textArea = FindTextArea(block.markerId);
            if (textArea == null && textAreas != null && index < textAreas.Length)
                textArea = textAreas[index];

            if (textArea != null)
                textArea.SetContent(block.content);
            else
                Debug.LogWarning($"[GrandpaDiary] Page '{pageData.pageId}' cannot find text area marker '{block.markerId}'.");
        }
    }

    private GrandpaDiaryTextAreaBinding FindTextArea(string markerId)
    {
        if (textAreas == null) return null;

        foreach (var textArea in textAreas)
            if (textArea != null && textArea.Matches(markerId))
                return textArea;

        return null;
    }

    private void ClearTextAreas()
    {
        if (textAreas == null) return;

        foreach (var textArea in textAreas)
            if (textArea != null)
                textArea.SetContent("");
    }
}
