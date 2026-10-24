using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Controller.FinalCharacterController;

// Sidebar tabs, as flags so the level flow can unlock several at once.
[Flags]
public enum PlayerPanelTabs
{
    None = 0,
    Album = 1,
    ScrapBook = 2,
    GrandpaDiary = 4,
    Backpack = 8,
    All = Album | ScrapBook | GrandpaDiary | Backpack
}

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
    [FormerlySerializedAs("grandpaDiaryMarkdown")]
    [SerializeField] private TextAsset grandpaDiaryJson;
    [SerializeField] private GrandpaDiaryPageView grandpaDiaryPagePrefab;
    [SerializeField] private RectTransform grandpaDiaryPagesRoot;

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

    [Header("Tab Guidance")]
    [SerializeField, Range(0f, 1f)] private float lockedTabAlpha = .35f;
    [SerializeField] private float guidePulseScale = 1.15f;
    

    public bool IsOpen { get; private set; }
    public bool IsGrandpaDiaryTabActive => activeTabTransition != null && activeTabTransition == grandpaDiaryTabTransition;
    public bool IsBackpackTabActive => activeTabTransition != null && activeTabTransition == backpackTabTransition;
    public int CurrentGrandpaDiaryPageIndex => currentGrandpaDiaryPageIndex;
    // The level flow keeps the panel shut until the player has been handed something to look at.
    public bool OpenAllowed { get; set; } = true;

    private CursorLockMode prevLockState;
    private bool prevCursorVisible;
    private bool cachedCursor;
    private int currentGrandpaDiaryPageIndex;
    private GrandpaDiaryPageData[] grandpaDiaryPages;
    private GrandpaDiaryPageView[] grandpaDiaryPageViews;
    private UITabTransition albumTabTransition;
    private UITabTransition scrapBookTabTransition;
    private UITabTransition grandpaDiaryTabTransition;
    private UITabTransition backpackTabTransition;
    private UITabTransition activeTabTransition;
    private UIPanelSlideTransition panelTransition;
    private Canvas grandpaDiaryCanvas;
    private GrandpaDiaryPageTurnTransition grandpaDiaryPageTurnTransition;
    private PlayerPanelTabs unlockedTabs = PlayerPanelTabs.All;
    private PlayerPanelTabs guideTab = PlayerPanelTabs.None;
    private Tween guidePulse;

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
        ConfigureTabLocks();
        ApplyTabAccess();
    }


    public void Open()  => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!IsOpen);

    public void SetOpen(bool open)
    {
        if (open == IsOpen) return;
        if (open && !OpenAllowed) return;
        if (open)
        {
            RefreshGrandpaDiaryAvailability();
            if (!IsUnlocked(ActiveTab)) ShowTabImmediate(FallbackTab());
        }
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
        // The button stays in the sidebar; without the diary it is simply locked.
        if (!GrandpaDiaryProgress.HasDiary) GrandpaDiaryTab.SetActive(false);
        ApplyTabAccess();
    }

    public void ShowGrandpaDiaryPage(int pageIndex)
    {
        int pageCount = GetGrandpaDiaryPageCount();
        if (pageCount <= 0)
        {
            if (grandpaDiaryPageText != null) grandpaDiaryPageText.text = "0 / 0";
            SetGrandpaDiaryPageButtons(false, false);
            HideAllGrandpaDiaryPageViews();
            return;
        }

        currentGrandpaDiaryPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        var pageData = grandpaDiaryPages[currentGrandpaDiaryPageIndex];

        ShowGrandpaDiaryPageView(pageData, currentGrandpaDiaryPageIndex);

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
        RefreshGuidePulse();
    }

    // The level flow decides which sidebar tabs can be opened and which one the player should open next.
    // Locked buttons stay in place, faded and unclickable; the guided one pulses until it is opened.
    public void SetTabAccess(PlayerPanelTabs unlocked, PlayerPanelTabs guide)
    {
        if (unlocked == unlockedTabs && guide == guideTab) return;
        unlockedTabs = unlocked;
        guideTab = guide;
        ApplyTabAccess();
        if (IsOpen && !IsUnlocked(ActiveTab)) ShowTab(FallbackTab());
    }

    private static readonly PlayerPanelTabs[] SidebarOrder =
        { PlayerPanelTabs.Album, PlayerPanelTabs.ScrapBook, PlayerPanelTabs.GrandpaDiary, PlayerPanelTabs.Backpack };

    private bool IsUnlocked(PlayerPanelTabs tab)
    {
        if (tab == PlayerPanelTabs.GrandpaDiary && !GrandpaDiaryProgress.HasDiary) return false;
        return (unlockedTabs & tab) != 0;
    }

    private PlayerPanelTabs ActiveTab =>
        activeTabTransition == null ? PlayerPanelTabs.None
        : activeTabTransition == albumTabTransition ? PlayerPanelTabs.Album
        : activeTabTransition == scrapBookTabTransition ? PlayerPanelTabs.ScrapBook
        : activeTabTransition == grandpaDiaryTabTransition ? PlayerPanelTabs.GrandpaDiary
        : PlayerPanelTabs.Backpack;

    private PlayerPanelTabs FallbackTab()
    {
        if (guideTab != PlayerPanelTabs.None && IsUnlocked(guideTab)) return guideTab;
        foreach (var tab in SidebarOrder)
            if (IsUnlocked(tab)) return tab;
        return ActiveTab;
    }

    private Button ButtonFor(PlayerPanelTabs tab) => tab switch
    {
        PlayerPanelTabs.Album => albumButton,
        PlayerPanelTabs.ScrapBook => scrapBookButton,
        PlayerPanelTabs.GrandpaDiary => grandpaDiaryButton,
        PlayerPanelTabs.Backpack => backpackButton,
        _ => null
    };

    private UITabTransition TransitionFor(PlayerPanelTabs tab) => tab switch
    {
        PlayerPanelTabs.Album => albumTabTransition,
        PlayerPanelTabs.ScrapBook => scrapBookTabTransition,
        PlayerPanelTabs.GrandpaDiary => grandpaDiaryTabTransition,
        PlayerPanelTabs.Backpack => backpackTabTransition,
        _ => null
    };

    private void ShowTab(PlayerPanelTabs tab)
    {
        if (tab == PlayerPanelTabs.Album) ShowAlbum();
        else if (tab == PlayerPanelTabs.ScrapBook) ShowScrapBook();
        else if (tab == PlayerPanelTabs.GrandpaDiary) ShowGrandpaDiary();
        else if (tab == PlayerPanelTabs.Backpack) ShowBackpack();
    }

    // Used while the panel is closed, where tab animations would run on inactive objects.
    private void ShowTabImmediate(PlayerPanelTabs tab)
    {
        var next = TransitionFor(tab);
        if (next == null || next == activeTabTransition) return;
        activeTabTransition?.HideImmediate();
        next.ShowImmediate();
        activeTabTransition = next;
        SetActiveSidebarButton(ButtonFor(tab));
        if (tab == PlayerPanelTabs.GrandpaDiary) ShowGrandpaDiaryPage(currentGrandpaDiaryPageIndex);
        RefreshGuidePulse();
    }

    private void ConfigureTabLocks()
    {
        foreach (var tab in SidebarOrder)
        {
            var button = ButtonFor(tab);
            var relay = button.GetComponent<SidebarLockedClick>();
            if (relay == null) relay = button.gameObject.AddComponent<SidebarLockedClick>();
            relay.Clicked += () => ShakeLockedButton(button);
        }
    }

    private void ApplyTabAccess()
    {
        foreach (var tab in SidebarOrder)
        {
            var button = ButtonFor(tab);
            if (button == null) continue;
            bool unlocked = IsUnlocked(tab);
            button.interactable = unlocked;
            var group = button.GetComponent<CanvasGroup>();
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = unlocked ? 1f : lockedTabAlpha;
        }
        RefreshGuidePulse();
    }

    private void RefreshGuidePulse()
    {
        guidePulse?.Kill();
        guidePulse = null;
        foreach (var tab in SidebarOrder)
        {
            var icon = ButtonFor(tab)?.transform.Find("Icon");
            if (icon != null) icon.localScale = Vector3.one;
        }

        if (guideTab == PlayerPanelTabs.None || guideTab == ActiveTab || !IsUnlocked(guideTab)) return;
        var guideIcon = ButtonFor(guideTab).transform.Find("Icon");
        if (guideIcon == null) return;
        guidePulse = guideIcon.DOScale(guidePulseScale, .55f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private static void ShakeLockedButton(Button button)
    {
        var icon = button.transform.Find("Icon");
        if (icon == null) return;
        icon.DOComplete();
        icon.DOPunchPosition(new Vector3(8f, 0f, 0f), .35f, 12, .6f).SetUpdate(true);
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
            pageView.Hide();
            grandpaDiaryPageViews[index] = pageView;
        }
    }

    private void ShowGrandpaDiaryPageView(GrandpaDiaryPageData pageData, int pageIndex)
    {
        HideAllGrandpaDiaryPageViews();

        var pageView = FindGrandpaDiaryPageView(pageData, pageIndex);
        if (pageView == null)
        {
            Debug.LogWarning($"[GrandpaDiary] Cannot find page view for page '{pageData?.pageId}' at index {pageIndex}.");
            return;
        }

        pageView.Show(pageData);
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

    private void SetGrandpaDiaryPageButtons(bool canGoPrevious, bool canGoNext)
    {
        if (grandpaDiaryPrevPageButton != null) grandpaDiaryPrevPageButton.interactable = canGoPrevious;
        if (grandpaDiaryNextPageButton != null) grandpaDiaryNextPageButton.interactable = canGoNext;
    }
}
