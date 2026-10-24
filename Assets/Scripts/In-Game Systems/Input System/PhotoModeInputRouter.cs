using UnityEngine;
using UnityEngine.InputSystem;
using Controller.FinalCharacterController;

public sealed class PhotoModeInputRouter : MonoBehaviour
{
    [Header("Object Refs")]
    [SerializeField] private PlayerInputProvider inputProvider;
    [SerializeField] private CameraSystemManager cameraSystemManager;
    [SerializeField] private PlayerPanelUIManager playerPanel;
    [SerializeField] private PlayerMovementManager playerMovementManager;
    [SerializeField] private CameraRaiseAnimationHandler cameraRaiseAnimationHandler;
    
    private InputAction cameraToggle;
    private InputAction cameraViewToggle;
    private InputAction shutter;
    private InputAction flashToggle;
    private InputAction cursorToggle;

    private bool cursorMode = false;
    private float enterTime;

    private void OnEnable()
    {
        if (inputProvider == null) return;
        if (cameraSystemManager == null) return;
        if (playerPanel == null) return;

        var map = inputProvider.Controls.Gameplay;

        cameraToggle = map.CameraModeToggle;
        cameraViewToggle = map.CameraViewToggle;
        shutter = map.PhotoShutter;
        flashToggle = map.FlashToggle;
        cursorToggle = map.CursorToggle;
        cameraToggle.performed += OnCameraTogglePerformed;
        cameraViewToggle.performed += OnCameraViewTogglePerformed;
        shutter.performed += OnShutterPerformed;
        flashToggle.performed += OnFlashTogglePerformed;
        cursorToggle.performed += OnCursorToggle;
        shutter.Disable();
    }

    private void OnDisable()
    {
        if (cameraToggle != null) cameraToggle.performed -= OnCameraTogglePerformed;
        if (cameraViewToggle != null) cameraViewToggle.performed -= OnCameraViewTogglePerformed;
        if (shutter != null) shutter.performed -= OnShutterPerformed;
        if (flashToggle != null) flashToggle.performed -= OnFlashTogglePerformed;
        if (cursorToggle != null)
            cursorToggle.performed -= OnCursorToggle;
    }

    private void OnCameraTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (cameraSystemManager == null) return;
        if (cameraSystemManager.IsViewTransitioning) return;
        if (playerPanel.IsOpen) return;

        if (cameraSystemManager.CurrentState == CameraModeState.Reviewing) return;

        if (cameraSystemManager.CurrentState == CameraModeState.Disabled)
        {
            if (cameraRaiseAnimationHandler != null)
            {
                cameraRaiseAnimationHandler.PlayRaise(() =>
                {
                    cameraSystemManager.EnterCameraMode();
                    enterTime = Time.time;
                    shutter.Enable();
                });
            }
            else
            {
                cameraSystemManager.EnterCameraMode();
                enterTime = Time.time;
                shutter.Enable();
            }
        }
        else
        {
            if (cursorMode) ExitCursorMode();
            shutter.Disable();
            cameraSystemManager.ExitCameraMode();
            if (cameraRaiseAnimationHandler != null)
                cameraRaiseAnimationHandler.PlayLower(null);
        }
    }

    private void OnCameraViewTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (!cameraSystemManager.ToggleViewMode()) return;
        if (cursorMode) ExitCursorMode();
        if (!cameraSystemManager.UsesAnimatedViewTransition && cameraRaiseAnimationHandler != null)
            cameraRaiseAnimationHandler.SetCameraModelVisible(
                cameraSystemManager.CurrentViewMode == CameraViewMode.Handheld);
    }

    private void OnShutterPerformed(InputAction.CallbackContext ctx)
    {
        if (cameraSystemManager == null) return;
        if (playerPanel.IsOpen) return;
        if (cameraSystemManager.CurrentState == CameraModeState.Disabled) return; 
        if (cursorMode) return;

        var currentState = cameraSystemManager.CurrentState;

        // In REVIEWING, shutter button exits review (back to ACTIVE) instead of attempting another capture
        if (currentState == CameraModeState.Reviewing)
        {
            cameraSystemManager.CloseReview();
            return;
        }

        // In ACTIVE, shutter button initiates capture
        if (currentState == CameraModeState.Active)
        {
            cameraSystemManager.Capture();
        }
    }

    private void OnFlashTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (playerPanel.IsOpen) return;
        if (cameraSystemManager.CurrentState != CameraModeState.Active) return;

        cameraSystemManager.ToggleFlash();
    }

    private void OnCursorToggle(InputAction.CallbackContext ctx)
    {
        if (cameraSystemManager.IsViewTransitioning) return;
        if (cameraSystemManager.CurrentState != CameraModeState.Active) return;
        if (cameraSystemManager.CurrentViewMode != CameraViewMode.Fullscreen) return;

        if (!cursorMode)
        {
            cursorMode = true;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            if (playerMovementManager != null) playerMovementManager.SetPlayerMovementControl(false);
        }
        else
        {
            ExitCursorMode();
        }
    }

    private void ExitCursorMode()
    {
        if (!cursorMode) return;

        cursorMode = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerMovementManager != null) playerMovementManager.SetPlayerMovementControl(true);
    }
}
