using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using System.IO;

// Camera controls and photo preview must initialize before their roots are hidden.
[DefaultExecutionOrder(10)]
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
    // Capture reads the whole screen, so every in-game HUD (hints included) is hidden while it runs.
    [SerializeField] private Canvas inGameUICanvas;
    // Frames to wait after hiding the handheld model or turning on the flash, so TAA rebuilds its history before the screen is read.
    [FormerlySerializedAs("handheldCaptureSettleFrames")]
    [SerializeField] private int captureSettleFrames = 8;

    [Header("Flash")]
    // Light under Main Camera; lit for the settle frames and the capture, so the photo includes it.
    [SerializeField] private GameObject cameraFlash;

    [Header("Echo System")]
    [FormerlySerializedAs("highlightObjectDetector")]
    [SerializeField] private EchoTargetDetector echoTargetDetector;
    [SerializeField] private EchoTargetingUI echoTargetingUI;

    public event Action OnNewEchoDiscovered;
    public event Action OnCaptureStarted;


    // Initialize the state machine to manage photo mode states
    private CameraStateMachine stateMachine;
    // Expose the current state for external reference
    public CameraModeState CurrentState => stateMachine != null ? stateMachine.State : CameraModeState.Disabled;
    public CameraViewMode CurrentViewMode { get; private set; } = CameraViewMode.Handheld;
    public bool UsesAnimatedViewTransition =>
        viewTransitionHandler != null && viewTransitionHandler.IsConfigured;
    public bool IsViewTransitioning =>
        UsesAnimatedViewTransition && viewTransitionHandler.IsTransitioning;
    public bool FlashEnabled { get; private set; }

    private Coroutine captureRoutine;
    private EchoTrigger captureSnapshot;


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

        echoTargetingUI.HideImmediate();
        echoTargetDetector.ClearTarget();

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
        if (next != CameraModeState.Active)
        {
            echoTargetingUI.HideImmediate();
            echoTargetDetector.ClearTarget();
        }
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

    public void ToggleFlash()
    {
        FlashEnabled = !FlashEnabled;
    }

    public void Capture()
    {
        if (IsViewTransitioning) return;
        if (CurrentState != CameraModeState.Active) return;
        captureSnapshot = echoTargetDetector.CaptureTarget();
        if (!stateMachine.RequestCapture()) return;

        OnCaptureStarted?.Invoke();

        if (cameraPhotoCapture == null) { stateMachine.FinishCaptureToActive(); return; }

        if (captureRoutine != null) StopCoroutine(captureRoutine);
        captureRoutine = StartCoroutine(CaptureFlow());
    }

    private IEnumerator CaptureFlow()
    {
        bool capturingFromHandheld =
            CurrentViewMode == CameraViewMode.Handheld && UsesAnimatedViewTransition;
        bool useFlash = FlashEnabled && cameraFlash != null;
        if (capturingFromHandheld)
        {
            viewTransitionHandler.SetCameraModelVisibleImmediate(false);
            SetFullscreenPhotoVolume(true);
        }
        if (useFlash) cameraFlash.SetActive(true);

        if (cameraTweakingUIManager != null) CameraUIRoot.SetActive(false);
        if (inGameUICanvas != null) inGameUICanvas.enabled = false;

        if (capturingFromHandheld || useFlash)
        {
            // TAA history still holds the frame before the model was hidden or the flash lit.
            HDCamera.GetOrCreate(Camera.main).Reset();
            for (int i = 0; i < captureSettleFrames; i++)
                yield return null;
        }

        Texture2D captured = null;
        yield return cameraPhotoCapture.Capture(tex => captured = tex);

        if (useFlash) cameraFlash.SetActive(false);

        if (capturingFromHandheld)
        {
            SetFullscreenPhotoVolume(false);
            viewTransitionHandler.SetCameraModelVisibleImmediate(true);
        }

        if (cameraTweakingUIManager != null) CameraUIRoot.SetActive(true);
        if (inGameUICanvas != null) inGameUICanvas.enabled = true;

        if (captured == null)
        {
            stateMachine.FinishCaptureToActive();
            yield break;
        }

        if (cameraPhotoPreviewUIManager != null) cameraPhotoPreviewUIManager.ShowCapturedPreview(captured);

        string fileName = null;
        if (cameraPhotoAlbumUIManager != null) fileName = cameraPhotoAlbumUIManager.AddPhotoFromTexture(captured);

        // Only a saved photo can discover the one target shown by the yellow frame.
        if (!string.IsNullOrEmpty(fileName) && captureSnapshot != null && GrandpaDiaryProgress.HasDiary)
        {
            try
            {
                EchoPhotoStorage.Save(fileName, new List<string> { captureSnapshot.scrapbookContent }, captureSnapshot.noteLayout);
                bool wasDiscovered = EchoDiscoveryStorage.IsDiscovered(captureSnapshot);
                EchoDiscoveryStorage.Record(captureSnapshot);
                if (!wasDiscovered) OnNewEchoDiscovered?.Invoke();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogError($"Could not save the captured echo: {error.Message}");
            }
            captureSnapshot.NotifyCaptured();
        }

        if (cameraPhotoAlbumUIManager != null) cameraPhotoAlbumUIManager.Refresh();

        captureSnapshot = null;

        stateMachine.FinishCaptureToReviewing();
        captureRoutine = null;
    }

    public void CloseReview()
    {
        if (!stateMachine.CloseReview()) return;
        if (cameraPhotoPreviewUIManager != null) cameraPhotoPreviewUIManager.ClosePreview();
    }
}
