using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class CameraSystemManager : MonoBehaviour
{


    [Header("UI Root Ref")]
    [SerializeField] private GameObject CameraUIRoot;

    [Header("View Transition Ref")]
    [SerializeField] private CameraViewTransitionHandler viewTransitionHandler;

    [Header("UI Manager Refs")]
    [SerializeField] private CameraTweakUIManager cameraTweakingUIManager;
    [SerializeField] private CameraPhotoPreviewUIManager cameraPhotoPreviewUIManager;
    [SerializeField] private CameraPhotoAlbumUIManager cameraPhotoAlbumUIManager;

    [Header("Fullscreen Photo Volume Ref")]
    [SerializeField] private Volume fullscreenPhotoVolume;

    [Header("Photo Capture Function Provider")]
    [SerializeField] private CameraPhotoCapture cameraPhotoCapture;

    [Header("Echo System")]
    [SerializeField] private CameraHighlightObjectDetector highlightObjectDetector;

    public event Action<IReadOnlyCollection<CameraHighlightObject>> OnPhotoCapturedWithEchoes;


    // Initialize the state machine to manage photo mode states
    private CameraStateMachine stateMachine;
    // Expose the current state for external reference
    public CameraModeState CurrentState => stateMachine.State;
    public CameraViewMode CurrentViewMode { get; private set; } = CameraViewMode.Handheld;
    public bool UsesAnimatedViewTransition =>
        viewTransitionHandler != null && viewTransitionHandler.IsConfigured;
    public bool IsViewTransitioning =>
        UsesAnimatedViewTransition && viewTransitionHandler.IsTransitioning;

    private Coroutine captureRoutine;
    private List<CameraHighlightObject> captureSnapshot;


    private void Awake()
    {
        stateMachine = new CameraStateMachine();
        stateMachine.OnStateChanged += HandleStateChanged;

        HideCameraUIImmediate();
        SetFullscreenPhotoVolume(false);
        if (cameraPhotoPreviewUIManager != null) cameraPhotoPreviewUIManager.HidePreviewImmediate();
    }

    private void Start()
    {
        if (cameraPhotoAlbumUIManager != null)
            StartCoroutine(cameraPhotoAlbumUIManager.PrewarmThumbnails());
    }

    public void EnterCameraMode()
    {
        if (stateMachine.State != CameraModeState.Disabled) return;
        CurrentViewMode = CameraViewMode.Handheld;
        stateMachine.EnterCameraMode();
    }

    public void ExitCameraMode()
    {
        if (!stateMachine.ExitCameraMode()) return;
    }

    public bool ToggleViewMode()
    {
        if (CurrentState != CameraModeState.Active) return false;
        if (IsViewTransitioning) return false;

        CameraViewMode targetMode = CurrentViewMode == CameraViewMode.Handheld
            ? CameraViewMode.Fullscreen
            : CameraViewMode.Handheld;

        if (UsesAnimatedViewTransition)
        {
            if (!viewTransitionHandler.PlayTransition(targetMode, () =>
                {
                    SetFullscreenPhotoVolume(targetMode == CameraViewMode.Fullscreen);
                }))
                return false;
            CurrentViewMode = targetMode;
        }
        else
        {
            CurrentViewMode = targetMode;
            ApplyActiveViewMode();
        }
        return true;
    }

    private void ApplyActiveViewMode()
    {
        bool fullscreen = CurrentViewMode == CameraViewMode.Fullscreen;
        if (UsesAnimatedViewTransition)
            viewTransitionHandler.SetImmediate(CurrentViewMode);
        else if (CameraUIRoot != null)
            CameraUIRoot.SetActive(fullscreen);
        SetFullscreenPhotoVolume(fullscreen);
    }

    private void HideCameraUIImmediate()
    {
        if (UsesAnimatedViewTransition)
            viewTransitionHandler.HideFullscreenUIImmediate();
        else if (CameraUIRoot != null)
            CameraUIRoot.SetActive(false);
    }

    private void SetFullscreenPhotoVolume(bool enabled)
    {
        if (fullscreenPhotoVolume != null)
            fullscreenPhotoVolume.weight = enabled ? 1f : 0f;
    }

    private void HandleStateChanged(CameraModeState prev, CameraModeState next)
    {
        switch (next)
        {
            case CameraModeState.Disabled:
                HideCameraUIImmediate();
                SetFullscreenPhotoVolume(false);
                if (cameraPhotoPreviewUIManager != null) cameraPhotoPreviewUIManager.HidePreviewImmediate();
                if (cameraTweakingUIManager != null) cameraTweakingUIManager.ResetCameraTweaks();
                break;

            case CameraModeState.Active:
                ApplyActiveViewMode();
                if (cameraPhotoPreviewUIManager != null) cameraPhotoPreviewUIManager.HidePreviewImmediate();
                break;

            case CameraModeState.Capturing:
                ApplyActiveViewMode();
                break;

            case CameraModeState.Reviewing:
                if (UsesAnimatedViewTransition)
                    viewTransitionHandler.ShowFullscreenUIImmediate();
                else if (CameraUIRoot != null)
                    CameraUIRoot.SetActive(true);
                SetFullscreenPhotoVolume(false);
                break;
        }
    }

    public void Capture()
    {
        if (IsViewTransitioning) return;
        if (!stateMachine.RequestCapture()) return;

        if (highlightObjectDetector != null)
        {
            var raw = new List<CameraHighlightObject>(highlightObjectDetector.HighlightedObjects);
            captureSnapshot = highlightObjectDetector.FilterConfirmedObjects(raw);
        }
        else
            captureSnapshot = null;

        if (cameraPhotoCapture == null) { stateMachine.FinishCaptureToActive(); return; }

        if (captureRoutine != null) StopCoroutine(captureRoutine);
        captureRoutine = StartCoroutine(CaptureFlow());
    }

    private IEnumerator CaptureFlow()
    {
        bool capturingFromHandheld =
            CurrentViewMode == CameraViewMode.Handheld && UsesAnimatedViewTransition;
        if (capturingFromHandheld)
        {
            viewTransitionHandler.SetCameraModelVisibleImmediate(false);
            SetFullscreenPhotoVolume(true);
        }

        if (cameraTweakingUIManager != null) CameraUIRoot.SetActive(false);

        Texture2D captured = null;
        yield return cameraPhotoCapture.Capture(tex => captured = tex);

        if (capturingFromHandheld)
        {
            SetFullscreenPhotoVolume(false);
            viewTransitionHandler.SetCameraModelVisibleImmediate(true);
        }

        if (cameraTweakingUIManager != null) CameraUIRoot.SetActive(true);

        if (captured == null)
        {
            stateMachine.FinishCaptureToActive();
            yield break;
        }

        if (cameraPhotoPreviewUIManager != null) cameraPhotoPreviewUIManager.ShowCapturedPreview(captured);

        string fileName = null;
        if (cameraPhotoAlbumUIManager != null) fileName = cameraPhotoAlbumUIManager.AddPhotoFromTexture(captured);

        // Bind the captured Echo texts to this photo's fileName for later scrapbook display.
        if (!string.IsNullOrEmpty(fileName) && captureSnapshot != null)
        {
            var scrapbookContents = new List<string>();
            foreach (var obj in captureSnapshot)
                if (obj.TryGetComponent<EchoTrigger>(out var echo))
                {
                    scrapbookContents.Add(echo.scrapbookContent);

                    if (!string.IsNullOrEmpty(echo.stickyNoteSlotId))
                        GrandpaDiaryStickyNoteStorage.Unlock(echo.stickyNoteSlotId);
                }

            EchoPhotoStorage.Save(fileName, scrapbookContents);
        }

        if (cameraPhotoAlbumUIManager != null) cameraPhotoAlbumUIManager.Refresh();

        OnPhotoCapturedWithEchoes?.Invoke(captureSnapshot);

        stateMachine.FinishCaptureToReviewing();
        captureRoutine = null;
    }

    public void CloseReview()
    {
        if (!stateMachine.CloseReview()) return;
        if (cameraPhotoPreviewUIManager != null) cameraPhotoPreviewUIManager.ClosePreview();
    }
}
