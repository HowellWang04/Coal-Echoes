using UnityEngine;

// Reports level 1 progress to the hint popup and plays the matching inner monologue lines.
// State checks run every frame; the popup ignores tasks that are not part of the hint
// currently showing, and the monologue ignores lines it has already played.
// Steps done on a game object are reported from a UnityEvent on that object:
// HintPopupUIManager.CompleteTask for T4_match_photo (the gate's echo), T6_open_guardhouse,
// T8_cut_gate_chain, InnerMonologueUIManager.Play for M4 (photo retaken), M5 (duty log), M7 (gate open).
public sealed class Level1HintFlowManager : MonoBehaviour
{
    // The Group Photo goes into the Photo Album under this fixed file name.
    public const string GroupPhotoFileName = "GroupPhoto_1980.png";
    private const string LockPickItemId = "lock_pick";
    private const string BoltCutterItemId = "bolt_cutter";

    [SerializeField] private HintPopupUIManager hintPopup;
    [SerializeField] private InnerMonologueUIManager monologue;
    [SerializeField] private MainMenuUIManager mainMenu;
    [SerializeField] private PlayerPanelUIManager playerPanel;
    [SerializeField] private CameraSystemManager cameraSystemManager;
    [SerializeField] private CameraPhotoAlbumUIManager photoAlbum;
    [SerializeField] private ItemUsePoint guardhouseLock;
    [SerializeField] private ItemUsePoint gateChain;
    [SerializeField] private EchoTrigger groupPhotoRetake;
    [SerializeField] private GameObject retakeCircle;

    private int scrapBookPhotoCount;
    private bool levelStarted;
    private bool diaryFirstPageSeen;

    private void Start()
    {
        scrapBookPhotoCount = ScrapBookDataManager.Instance.Photos.Count;
        ScrapBookDataManager.Instance.OnPhotosChanged += HandleScrapBookPhotosChanged;
        ScrapBookDraggable.OnPhotoPlaced += HandlePhotoPlaced;
    }

    private void OnDestroy()
    {
        if (ScrapBookDataManager.Instance != null)
            ScrapBookDataManager.Instance.OnPhotosChanged -= HandleScrapBookPhotosChanged;
        ScrapBookDraggable.OnPhotoPlaced -= HandlePhotoPlaced;
    }

    private void Update()
    {
        if (!levelStarted)
        {
            // The main menu switches itself off when the opening hands control to the player.
            if (mainMenu != null && mainMenu.isActiveAndEnabled) return;
            levelStarted = true;
            monologue.Play("M1");
            hintPopup.Begin();
        }

        if (GrandpaDiaryProgress.HasDiary) hintPopup.CompleteTask("T1_grandpa_diary");
        if (photoAlbum.HasPhoto(GroupPhotoFileName)) hintPopup.CompleteTask("T1_group_photo");
        if (ItemBackpackManager.Instance.HasItem(LockPickItemId))
        {
            hintPopup.CompleteTask("T1_lock_pick");
            monologue.Play("M2");
        }
        if (ItemBackpackManager.Instance.HasItem(BoltCutterItemId)) monologue.Play("M6");

        bool onDiaryFirstPage = playerPanel.IsOpen && playerPanel.IsGrandpaDiaryTabActive
            && playerPanel.CurrentGrandpaDiaryPageIndex == 0;
        if (playerPanel.IsOpen)
        {
            hintPopup.CompleteTask("T2_open_panel");
            if (onDiaryFirstPage) hintPopup.CompleteTask("T3_diary_first_page");
            if (playerPanel.IsBackpackTabActive) hintPopup.CompleteTask("T5_view_lock_pick");
        }
        else
        {
            hintPopup.CompleteTask("T4_close_panel");
        }

        // The first page counts as read once the player has looked at it and moved on.
        if (onDiaryFirstPage) diaryFirstPageSeen = true;
        else if (diaryFirstPageSeen) monologue.Play("M3");

        if (cameraSystemManager.CurrentState != CameraModeState.Disabled)
            hintPopup.CompleteTask("T4_raise_camera");

        UpdatePanelAccess();

        // A tool works only once its hint shows; used any earlier, the hint could never tick it.
        guardhouseLock.Allowed = hintPopup.HasReached("T6");
        gateChain.Allowed = hintPopup.HasReached("T8");

        // The gate is the Group Photo echo, and its circle glows, only while T4 waits for the retake,
        // so loading a save past T4 leaves the circle gone without a separate record.
        bool retakeOpen = hintPopup.CurrentHintId == "T4" && !hintPopup.IsTaskDone("T4_match_photo");
        groupPhotoRetake.enabled = retakeOpen;
        retakeCircle.SetActive(retakeOpen);
    }

    // Player's Panel tabs open one at a time in the order the hints teach them: the panel itself
    // after the storage box, then the diary, the album, the scrapbook, and the backpack at T5.
    private void UpdatePanelAccess()
    {
        playerPanel.OpenAllowed = GrandpaDiaryProgress.HasDiary;

        const PlayerPanelTabs diary = PlayerPanelTabs.GrandpaDiary;
        const PlayerPanelTabs album = PlayerPanelTabs.Album;
        const PlayerPanelTabs scrapBook = PlayerPanelTabs.ScrapBook;
        string hint = hintPopup.Finished ? null : hintPopup.CurrentHintId ?? "T1";
        switch (hint)
        {
            case "T1":
            case "T2":
                playerPanel.SetTabAccess(diary, diary);
                break;
            case "T3":
                if (hintPopup.IsTaskDone("T3_add_to_scrapbook"))
                    playerPanel.SetTabAccess(diary | album | scrapBook, scrapBook);
                else if (hintPopup.IsTaskDone("T3_diary_first_page"))
                    playerPanel.SetTabAccess(diary | album, album);
                else
                    playerPanel.SetTabAccess(diary, diary);
                break;
            case "T4":
                playerPanel.SetTabAccess(diary | album | scrapBook, PlayerPanelTabs.None);
                break;
            case "T5":
                playerPanel.SetTabAccess(PlayerPanelTabs.All, PlayerPanelTabs.Backpack);
                break;
            default:
                playerPanel.SetTabAccess(PlayerPanelTabs.All, PlayerPanelTabs.None);
                break;
        }
    }

    private void HandleScrapBookPhotosChanged()
    {
        int count = ScrapBookDataManager.Instance.Photos.Count;
        if (count > scrapBookPhotoCount) hintPopup.CompleteTask("T3_add_to_scrapbook");
        scrapBookPhotoCount = count;
    }

    private void HandlePhotoPlaced()
    {
        hintPopup.CompleteTask("T3_place_in_scrapbook");
    }
}
