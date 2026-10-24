using Controller.FinalCharacterController;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public sealed class MainMenuUIManager : MonoBehaviour
{
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private Camera menuCamera;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CanvasGroup gameplayUI;
    [SerializeField] private PlayerInputProvider inputProvider;
    [SerializeField] private PlayerMovementManager playerMovement;
    [SerializeField] private OpeningSequenceUI openingSequence;
    [SerializeField] private GameObject openingSceneToggle;
    [SerializeField] private NewspaperMenuUI newspaper;
    [SerializeField] private OpeningSceneState sceneState;
    [SerializeField] private Button continueButton;

    private Tween fadeTween;
    private bool isShowing;
    private bool isQuitting;
    private bool openingStarted;
    private bool previousCameraEnabled;
    private bool previousAllowControl;
    private bool previousGameplayEnabled;
    private float previousUIAlpha;
    private bool previousUIInteractable;
    private bool previousUIBlocksRaycasts;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    private void Start()
    {
        if (!showOnStart)
        {
            gameObject.SetActive(false);
            return;
        }

        // The menu objects may be switched off while editing; the menu itself must always come up.
        menuGroup.gameObject.SetActive(true);
        newspaper.gameObject.SetActive(true);

        previousCameraEnabled = playerCamera.enabled;
        previousAllowControl = playerMovement.AllowControl;
        previousGameplayEnabled = inputProvider.Controls.Gameplay.enabled;
        previousUIAlpha = gameplayUI.alpha;
        previousUIInteractable = gameplayUI.interactable;
        previousUIBlocksRaycasts = gameplayUI.blocksRaycasts;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        isShowing = true;

        inputProvider.Controls.Gameplay.Disable();
        playerMovement.SetPlayerMovementControl(false);
        gameplayUI.alpha = 0f;
        gameplayUI.interactable = false;
        gameplayUI.blocksRaycasts = false;
        playerCamera.enabled = false;
        menuCamera.enabled = true;
        menuCanvas.enabled = true;
        newspaper.Show(menuCamera);
        sceneState.Prepare();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current.SetSelectedGameObject(null);
        continueButton.interactable = GameSaveManager.HasSave;

        menuGroup.alpha = 0f;
        fadeTween = menuGroup.DOFade(1f, 0.6f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void StartGame()
    {
        if (!isShowing || openingStarted) return;

        // An old save is cleared in place; every system that loaded it resets itself.
        if (GameSaveManager.HasSave) GameSaveManager.ClearSaves();
        PlayOpening();
    }

    // Picks up the save where it is, without the opening.
    public void ContinueGame()
    {
        if (!isShowing || openingStarted) return;
        gameObject.SetActive(false);
    }

    private void PlayOpening()
    {
        if (!openingSceneToggle.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        openingStarted = true;
        menuGroup.interactable = false;
        menuGroup.blocksRaycasts = false;
        EventSystem.current.SetSelectedGameObject(null);
        fadeTween?.Complete();
        fadeTween?.Kill();
        newspaper.EnterPhoto(() =>
        {
            menuGroup.gameObject.SetActive(false);
            openingSequence.Play(() => gameObject.SetActive(false));
        });
    }

    public void QuitGame()
    {
        isQuitting = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDisable()
    {
        fadeTween?.Kill();
        if (!isShowing || isQuitting) return;

        isShowing = false;
        menuCanvas.enabled = false;
        menuCamera.enabled = false;
        playerCamera.enabled = previousCameraEnabled;
        playerMovement.SetPlayerMovementControl(previousAllowControl);
        if (previousGameplayEnabled) inputProvider.Controls.Gameplay.Enable();
        gameplayUI.alpha = previousUIAlpha;
        gameplayUI.interactable = previousUIInteractable;
        gameplayUI.blocksRaycasts = previousUIBlocksRaycasts;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }
}
