using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerPanelInputRouter : MonoBehaviour
{
    [Header("Object Refs")]
    [SerializeField] private PlayerInputProvider inputProvider;
    [SerializeField] private CameraSystemManager cameraSystemManager;
    [SerializeField] private PlayerPanelUIManager playerPanel;

    private InputAction playerPanelToggle;

    private void OnEnable()
    {
        if (inputProvider == null) return;
        if (playerPanel == null) return;
        if (cameraSystemManager == null) return;
        
        var gameplay = inputProvider.Controls.Gameplay;

        playerPanelToggle = gameplay.PlayerPanelToggle;

        playerPanelToggle.performed += OnPanelToggle;
    }

    private void OnDisable()
    {
        if (playerPanelToggle != null) playerPanelToggle.performed -= OnPanelToggle;
    }

    private void OnPanelToggle(InputAction.CallbackContext ctx)
    {
        if (playerPanel == null) return;
        if (!CanTogglePlayerPanel()) return;

        playerPanel.Toggle();
    }

    // The PlayerPanel can only be toggled when not in the middle of capturing or reviewing photos.
    private bool CanTogglePlayerPanel()
    {
        if (cameraSystemManager == null) return true;

        return cameraSystemManager.CurrentState == CameraModeState.Disabled;
    }
}
