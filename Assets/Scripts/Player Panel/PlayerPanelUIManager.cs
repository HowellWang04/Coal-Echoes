using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Controller.FinalCharacterController;

// Let child UI initialize before this manager hides the panel in Awake.
[DefaultExecutionOrder(10)]
public sealed class PlayerPanelUIManager : MonoBehaviour
{
    [Header("Root UI Ref")]
    [SerializeField] private GameObject playerPanelUI;
    
    [Header("Manager Refs")]
    [SerializeField] private PlayerMovementManager playerMovementManager;
    [SerializeField] private CameraPhotoAlbumUIManager cameraPhotoAlbumUIManager;

    [Header("Tabs")]
    [SerializeField] private GameObject Albumtab;
    [SerializeField] private GameObject ScrapBooktab;
    [FormerlySerializedAs("GrandpaNotesTab")]
    [SerializeField] private GameObject GrandpaDiaryTab;
    [SerializeField] private GameObject BackpackTab;

    [Header("Grandpa's Diary")]
    [FormerlySerializedAs("grandpaNotesText")]
    [SerializeField] private TMP_Text grandpaDiaryText;
    [FormerlySerializedAs("grandpaDiaryMarkdown")]
    [SerializeField] private TextAsset grandpaDiaryJson;
    [SerializeField] private GrandpaDiaryPageData[] grandpaDiaryPages;
    [SerializeField] private GrandpaDiaryPageView grandpaDiaryPagePrefab;
    [SerializeField] private RectTransform grandpaDiaryPagesRoot;
    [SerializeField] private GrandpaDiaryPageView[] grandpaDiaryPageViews;

    [Header("Grandpa Diary Sticky Note Positions")]
    [SerializeField] private GrandpaDiaryStickyNotePositionOverride[] grandpaDiaryStickyNotePositionOverrides;

    [FormerlySerializedAs("grandpaNotesPrevPageButton")]
    [SerializeField] private Button grandpaDiaryPrevPageButton;
    [FormerlySerializedAs("grandpaNotesNextPageButton")]
    [SerializeField] private Button grandpaDiaryNextPageButton;
    [FormerlySerializedAs("grandpaNotesPageText")]
    [SerializeField] private TMP_Text grandpaDiaryPageText;

    [Header("Buttons")]
    [SerializeField] private Button albumButton;
    [SerializeField] private Button scrapBookButton;
    [FormerlySerializedAs("grandpaNotesButton")]
    [SerializeField] private Button grandpaDiaryButton;
    [SerializeField] private Button backpackButton;
    

    public bool IsOpen { get; private set; }

    private CursorLockMode prevLockState;
    private bool prevCursorVisible;
    private bool cachedCursor;
    private int currentGrandpaDiaryPageIndex;
    private UITabTransition albumTabTransition;
    private UITabTransition scrapBookTabTransition;
    private UITabTransition grandpaDiaryTabTransition;
    private UITabTransition backpackTabTransition;
    private UITabTransition activeTabTransition;
    private UIPanelSlideTransition panelTransition;
    private Canvas grandpaDiaryCanvas;
    private GrandpaDiaryPageTurnTransition grandpaDiaryPageTurnTransition;

    private void Awake()
    {
        RefreshGrandpaDiaryAvailability();
        LoadGrandpaDiaryJson();
        CreateGrandpaDiaryPageViews();
        ConfigureGrandpaDiaryPageTurnTransition();
        ConfigurePanelTransition();
        IsOpen = false;
        ApplyOpenState();
    }

    private void Start()
    {
        InitBtnListeners();
        ConfigureSidebarButtonFeedback();
        ConfigureGrandpaDiarySorting();
        ConfigureTabTransitions();
        ShowInitialAlbum();
    }


    public void Open()  => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!IsOpen);

    public void SetOpen(bool open)
    {
        if (open == IsOpen) return;
        if (open) RefreshGrandpaDiaryAvailability();
        IsOpen = open;
        ApplyOpenState();
    }

    private void InitBtnListeners()
    {
        if (albumButton != null) albumButton.onClick.AddListener(ShowAlbum);
        if (scrapBookButton != null) scrapBookButton.onClick.AddListener(ShowScrapBook);
        if (grandpaDiaryButton != null) grandpaDiaryButton.onClick.AddListener(ShowGrandpaDiary);
        if (backpackButton != null) backpackButton.onClick.AddListener(ShowBackpack);
        if (grandpaDiaryPrevPageButton != null) grandpaDiaryPrevPageButton.onClick.AddListener(ShowPreviousGrandpaDiaryPage);
        if (grandpaDiaryNextPageButton != null) grandpaDiaryNextPageButton.onClick.AddListener(ShowNextGrandpaDiaryPage);
    }


    private void ApplyOpenState()
    {
        bool open = IsOpen;

        if (!open && activeTabTransition != null)
            CompleteTabTransitionState();

        if (open)
        {
            playerPanelUI.SetActive(true);
            panelTransition.Show();
        }
        else
        {
            panelTransition.HideImmediate();
            playerPanelUI.SetActive(false);
        }
        if (playerMovementManager != null) playerMovementManager.SetPlayerMovementControl(!open);
        if (!open && cameraPhotoAlbumUIManager != null) cameraPhotoAlbumUIManager.OnPanelClosed();

        if (open && !cachedCursor)
        {
            prevLockState = Cursor.lockState;
            prevCursorVisible = Cursor.visible;
            cachedCursor = true;
        }

        if (open)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else
        {
            if (cachedCursor)
            {
                Cursor.lockState = prevLockState;
                Cursor.visible = prevCursorVisible;
                cachedCursor = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public void ShowAlbum()
    {
        SwitchTab(albumTabTransition, albumButton);
    }

    public void ShowScrapBook()
    {
        SwitchTab(scrapBookTabTransition, scrapBookButton);
    }

    public void ShowGrandpaDiary()
    {
        if (!GrandpaDiaryProgress.HasDiary) return;
        SwitchTab(grandpaDiaryTabTransition, grandpaDiaryButton);
        ShowGrandpaDiaryPage(currentGrandpaDiaryPageIndex);
    }

    public void RefreshGrandpaDiaryAvailability()
    {
        bool available = GrandpaDiaryProgress.HasDiary;
        grandpaDiaryButton.gameObject.SetActive(available);
        if (!available) GrandpaDiaryTab.SetActive(false);
    }

    public void ShowGrandpaDiary(string content)
    {
        if (grandpaDiaryText != null) grandpaDiaryText.text = content;
        ShowGrandpaDiary();
    }

    public void ShowGrandpaDiaryPage(int pageIndex)
    {
        int pageCount = GetGrandpaDiaryPageCount();
        if (pageCount <= 0)
        {
            if (grandpaDiaryText != null) grandpaDiaryText.text = "";
            if (grandpaDiaryPageText != null) grandpaDiaryPageText.text = "0 / 0";
            SetGrandpaDiaryPageButtons(false, false);
            HideAllGrandpaDiaryPageViews();
            return;
        }

        currentGrandpaDiaryPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        var pageData = grandpaDiaryPages[currentGrandpaDiaryPageIndex];

        if (!ShowGrandpaDiaryPageView(pageData, currentGrandpaDiaryPageIndex))
            ShowGrandpaDiaryFallbackText(pageData);

        if (grandpaDiaryPageText != null)
            grandpaDiaryPageText.text = $"{currentGrandpaDiaryPageIndex + 1} / {pageCount}";

        SetGrandpaDiaryPageButtons(currentGrandpaDiaryPageIndex > 0, currentGrandpaDiaryPageIndex < pageCount - 1);
    }

    private void ShowPreviousGrandpaDiaryPage()
    {
        TurnGrandpaDiaryPage(currentGrandpaDiaryPageIndex - 1);
    }

    private void ShowNextGrandpaDiaryPage()
    {
        TurnGrandpaDiaryPage(currentGrandpaDiaryPageIndex + 1);
    }

    public void ShowBackpack()
    {
        SwitchTab(backpackTabTransition, backpackButton);
    }

    private void ConfigurePanelTransition()
    {
        var animationScope = GetComponentInParent<UIAnimationScope>();
        var sidebarRoot = albumButton.transform.parent as RectTransform;
        var tabsRoot = Albumtab.transform.parent as RectTransform;
        panelTransition = animationScope.RegisterPanel(playerPanelUI, sidebarRoot, tabsRoot);
    }

    private void ConfigureGrandpaDiaryPageTurnTransition()
    {
        grandpaDiaryPageTurnTransition = GetComponent<GrandpaDiaryPageTurnTransition>();
        if (grandpaDiaryPageTurnTransition == null)
            grandpaDiaryPageTurnTransition = gameObject.AddComponent<GrandpaDiaryPageTurnTransition>();
    }

    private void ConfigureTabTransitions()
    {
        var animationScope = GetComponentInParent<UIAnimationScope>();
        albumTabTransition = animationScope.RegisterTab(Albumtab);
        scrapBookTabTransition = animationScope.RegisterTab(ScrapBooktab);
        grandpaDiaryTabTransition = animationScope.RegisterTab(GrandpaDiaryTab);
        backpackTabTransition = animationScope.RegisterTab(BackpackTab);

        albumTabTransition.HideImmediate();
        scrapBookTabTransition.HideImmediate();
        grandpaDiaryTabTransition.HideImmediate();
        backpackTabTransition.HideImmediate();
    }

    private void ShowInitialAlbum()
    {
        albumTabTransition.ShowImmediate();
        activeTabTransition = albumTabTransition;
        SetActiveSidebarButton(albumButton);
    }

    private void SwitchTab(UITabTransition nextTab, Button activeButton)
    {
        if (activeTabTransition == nextTab) return;

        activeTabTransition?.Hide();
        nextTab.Show();
        activeTabTransition = nextTab;
        SetActiveSidebarButton(activeButton);
    }

    private void CompleteTabTransitionState()
    {
        albumTabTransition.HideImmediate();
        scrapBookTabTransition.HideImmediate();
        grandpaDiaryTabTransition.HideImmediate();
        backpackTabTransition.HideImmediate();
        activeTabTransition.ShowImmediate();
    }

    private void ConfigureSidebarButtonFeedback()
    {
        UseSidebarSettings(albumButton);
        UseSidebarSettings(scrapBookButton);
        UseSidebarSettings(grandpaDiaryButton);
        UseSidebarSettings(backpackButton);
    }

    private static void UseSidebarSettings(Button button)
    {
        if (button != null)
            button.GetComponent<UIButtonTweenFeedback>()?.UseSidebarSettings();
    }

    private void SetActiveSidebarButton(Button activeButton)
    {
        SetTabActive(albumButton, albumButton == activeButton);
        SetTabActive(scrapBookButton, scrapBookButton == activeButton);
        SetTabActive(grandpaDiaryButton, grandpaDiaryButton == activeButton);
        SetTabActive(backpackButton, backpackButton == activeButton);
        KeepGrandpaDiaryAboveSidebarButtons();
    }

    private void ConfigureGrandpaDiarySorting()
    {
        grandpaDiaryCanvas = GrandpaDiaryTab.GetComponent<Canvas>();
        if (grandpaDiaryCanvas == null)
            grandpaDiaryCanvas = GrandpaDiaryTab.AddComponent<Canvas>();

        grandpaDiaryCanvas.overrideSorting = true;
        grandpaDiaryCanvas.sortingLayerID = albumButton.GetComponent<Canvas>().sortingLayerID;

        if (GrandpaDiaryTab.GetComponent<GraphicRaycaster>() == null)
            GrandpaDiaryTab.AddComponent<GraphicRaycaster>();
    }

    private void KeepGrandpaDiaryAboveSidebarButtons()
    {
        int highestButtonOrder = Mathf.Max(
            albumButton.GetComponent<Canvas>().sortingOrder,
            scrapBookButton.GetComponent<Canvas>().sortingOrder,
            grandpaDiaryButton.GetComponent<Canvas>().sortingOrder,
            backpackButton.GetComponent<Canvas>().sortingOrder);
        grandpaDiaryCanvas.sortingOrder = highestButtonOrder + 1;
    }

    private static void SetTabActive(Button button, bool active)
    {
        if (button != null)
            button.GetComponent<UIButtonTweenFeedback>()?.SetTabActive(active);
    }

    private int GetGrandpaDiaryPageCount()
    {
        return grandpaDiaryPages != null ? grandpaDiaryPages.Length : 0;
    }

    private void LoadGrandpaDiaryJson()
    {
        if (grandpaDiaryJson == null)
        {
            Debug.LogWarning("[GrandpaDiary] Grandpa Diary Json is not assigned.");
            return;
        }

        var parsedPages = GrandpaDiaryJsonParser.Parse(grandpaDiaryJson.text);
        if (parsedPages.Length > 0)
        {
            grandpaDiaryPages = parsedPages;
            Debug.Log($"[GrandpaDiary] Loaded {grandpaDiaryPages.Length} diary pages from JSON.");
        }
        else
        {
            Debug.LogWarning("[GrandpaDiary] Diary JSON parsed 0 pages.");
        }
    }

    private void CreateGrandpaDiaryPageViews()
    {
        if (grandpaDiaryPagePrefab == null || grandpaDiaryPagesRoot == null
            || grandpaDiaryPages == null || grandpaDiaryPages.Length == 0)
            return;

        HideAllGrandpaDiaryPageViews();
        grandpaDiaryPageViews = new GrandpaDiaryPageView[grandpaDiaryPages.Length];

        for (int index = 0; index < grandpaDiaryPages.Length; index++)
        {
            var pageData = grandpaDiaryPages[index];
            var pageView = Instantiate(grandpaDiaryPagePrefab, grandpaDiaryPagesRoot, false);
            pageView.name = $"DiaryPage_Runtime_{pageData.pageId}";
            pageView.SetPageId(pageData.pageId);
            pageView.SetStickyNotePositionOverrides(grandpaDiaryStickyNotePositionOverrides);
            pageView.Hide();
            grandpaDiaryPageViews[index] = pageView;
        }
    }

    private bool ShowGrandpaDiaryPageView(GrandpaDiaryPageData pageData, int pageIndex)
    {
        HideAllGrandpaDiaryPageViews();

        var pageView = FindGrandpaDiaryPageView(pageData, pageIndex);
        if (pageView == null)
        {
            Debug.LogWarning($"[GrandpaDiary] Cannot find page view for page '{pageData?.pageId}' at index {pageIndex}.");
            return false;
        }

        pageView.Show(pageData);
        return true;
    }

    private void TurnGrandpaDiaryPage(int pageIndex)
    {
        int pageCount = GetGrandpaDiaryPageCount();
        if (pageCount <= 0)
            return;

        int targetPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        if (targetPageIndex == currentGrandpaDiaryPageIndex)
            return;

        var currentPageView = FindGrandpaDiaryPageView(
            grandpaDiaryPages[currentGrandpaDiaryPageIndex],
            currentGrandpaDiaryPageIndex);
        var targetPageView = FindGrandpaDiaryPageView(
            grandpaDiaryPages[targetPageIndex],
            targetPageIndex);

        if (currentPageView == null || targetPageView == null)
        {
            ShowGrandpaDiaryPage(targetPageIndex);
            return;
        }

        targetPageView.Show(grandpaDiaryPages[targetPageIndex]);
        SetGrandpaDiaryPageButtons(false, false);

        bool started = grandpaDiaryPageTurnTransition.Play(
            currentPageView,
            targetPageView,
            targetPageIndex > currentGrandpaDiaryPageIndex,
            () =>
            {
                currentGrandpaDiaryPageIndex = targetPageIndex;
                if (grandpaDiaryPageText != null)
                    grandpaDiaryPageText.text = $"{currentGrandpaDiaryPageIndex + 1} / {pageCount}";
                SetGrandpaDiaryPageButtons(false, false);
            },
            () =>
            {
                currentPageView.Hide();
                SetGrandpaDiaryPageButtons(
                    currentGrandpaDiaryPageIndex > 0,
                    currentGrandpaDiaryPageIndex < pageCount - 1);
            });

        if (!started)
            ShowGrandpaDiaryPage(targetPageIndex);
    }

    private GrandpaDiaryPageView FindGrandpaDiaryPageView(GrandpaDiaryPageData pageData, int pageIndex)
    {
        if (grandpaDiaryPageViews == null || grandpaDiaryPageViews.Length == 0)
            return null;

        if (pageData != null && !string.IsNullOrEmpty(pageData.pageId))
        {
            foreach (var pageView in grandpaDiaryPageViews)
                if (pageView != null && pageView.PageId == pageData.pageId)
                    return pageView;
        }

        if (pageIndex >= 0 && pageIndex < grandpaDiaryPageViews.Length)
            return grandpaDiaryPageViews[pageIndex];

        return null;
    }

    private void HideAllGrandpaDiaryPageViews()
    {
        if (grandpaDiaryPageViews == null) return;

        foreach (var pageView in grandpaDiaryPageViews)
            if (pageView != null)
                pageView.Hide();
    }

    private void ShowGrandpaDiaryFallbackText(GrandpaDiaryPageData pageData)
    {
        if (grandpaDiaryText == null) return;

        if (pageData == null || pageData.textBlocks == null || pageData.textBlocks.Count == 0)
        {
            grandpaDiaryText.text = "";
            return;
        }

        var content = "";
        foreach (var block in pageData.textBlocks)
        {
            if (block == null || string.IsNullOrEmpty(block.content)) continue;
            if (!string.IsNullOrEmpty(content)) content += "\n\n";
            content += block.content;
        }

        grandpaDiaryText.text = content;
    }

    private void SetGrandpaDiaryPageButtons(bool canGoPrevious, bool canGoNext)
    {
        if (grandpaDiaryPrevPageButton != null) grandpaDiaryPrevPageButton.interactable = canGoPrevious;
        if (grandpaDiaryNextPageButton != null) grandpaDiaryNextPageButton.interactable = canGoNext;
    }
}
