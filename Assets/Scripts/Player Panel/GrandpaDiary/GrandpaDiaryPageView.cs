using UnityEngine;

public class GrandpaDiaryPageView : MonoBehaviour
{
    [SerializeField] private string pageId;
    [SerializeField] private GameObject pageRoot;
    [SerializeField] private GrandpaDiaryTextAreaBinding[] textAreas;
    [SerializeField] private GrandpaDiaryStickyNoteSlot[] stickyNoteSlots;

    public string PageId => pageId;

    private bool initialized;
    private RectTransform leftPage;
    private RectTransform rightPage;
    private GrandpaDiaryStickyNotePositionOverride[] stickyNotePositionOverrides;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void SetPageId(string id)
    {
        pageId = id;
    }

    public void SetStickyNotePositionOverrides(GrandpaDiaryStickyNotePositionOverride[] overrides)
    {
        stickyNotePositionOverrides = overrides;
    }

    public void Show(GrandpaDiaryPageData pageData)
    {
        EnsureInitialized();

        if (pageRoot != null) pageRoot.SetActive(true);

        ClearTextAreas();
        HideAllStickyNoteSlots();

        if (pageData == null) return;

        ShowTextBlocks(pageData);
        ShowUnlockedStickyNotes(pageData);
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

        if (stickyNoteSlots == null || stickyNoteSlots.Length == 0)
            stickyNoteSlots = GetComponentsInChildren<GrandpaDiaryStickyNoteSlot>(true);

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

    private void ShowUnlockedStickyNotes(GrandpaDiaryPageData pageData)
    {
        if (pageData.stickyNoteSlots == null) return;

        for (int index = 0; index < pageData.stickyNoteSlots.Count; index++)
        {
            var slotData = pageData.stickyNoteSlots[index];
            if (slotData == null || string.IsNullOrEmpty(slotData.slotId)) continue;
            if (!slotData.unlockedByDefault && !GrandpaDiaryStickyNoteStorage.IsUnlocked(slotData.slotId)) continue;

            var stickyNoteSlot = FindStickyNoteSlot(slotData.slotId);
            if (stickyNoteSlot == null && !string.IsNullOrEmpty(slotData.pageSide))
                stickyNoteSlot = FindStickyNoteSlotByPageSide(slotData.pageSide);

            if (stickyNoteSlot == null && stickyNoteSlots != null && index < stickyNoteSlots.Length)
                stickyNoteSlot = stickyNoteSlots[index];

            if (stickyNoteSlot != null)
            {
                bool hasCustomPosition = TryGetStickyNotePosition(
                    pageData.pageId,
                    stickyNoteSlot.PageSide,
                    out Vector2 customPosition);
                stickyNoteSlot.Show(
                    slotData.slotId,
                    slotData.noteContent,
                    hasCustomPosition,
                    customPosition);
            }
            else
                Debug.LogWarning($"[GrandpaDiary] Page '{pageData.pageId}' cannot find sticky note slot '{slotData.slotId}'.");
        }
    }

    private bool TryGetStickyNotePosition(
        string id,
        GrandpaDiaryPageSide pageSide,
        out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;
        if (stickyNotePositionOverrides == null)
            return false;

        foreach (var positionOverride in stickyNotePositionOverrides)
        {
            if (positionOverride == null || positionOverride.pageId != id
                || positionOverride.pageSide != pageSide)
                continue;

            anchoredPosition = positionOverride.anchoredPosition;
            return true;
        }

        return false;
    }

    private GrandpaDiaryTextAreaBinding FindTextArea(string markerId)
    {
        if (textAreas == null) return null;

        foreach (var textArea in textAreas)
            if (textArea != null && textArea.Matches(markerId))
                return textArea;

        return null;
    }

    private GrandpaDiaryStickyNoteSlot FindStickyNoteSlot(string slotId)
    {
        if (stickyNoteSlots == null) return null;

        foreach (var stickyNoteSlot in stickyNoteSlots)
            if (stickyNoteSlot != null && stickyNoteSlot.Matches(slotId))
                return stickyNoteSlot;

        return null;
    }

    private GrandpaDiaryStickyNoteSlot FindStickyNoteSlotByPageSide(string pageSide)
    {
        if (stickyNoteSlots == null) return null;

        foreach (var stickyNoteSlot in stickyNoteSlots)
            if (stickyNoteSlot != null && stickyNoteSlot.MatchesPageSide(pageSide))
                return stickyNoteSlot;

        return null;
    }

    private void ClearTextAreas()
    {
        if (textAreas == null) return;

        foreach (var textArea in textAreas)
            if (textArea != null)
                textArea.SetContent("");
    }

    private void HideAllStickyNoteSlots()
    {
        if (stickyNoteSlots == null) return;

        foreach (var stickyNoteSlot in stickyNoteSlots)
            if (stickyNoteSlot != null)
                stickyNoteSlot.Hide();
    }
}
