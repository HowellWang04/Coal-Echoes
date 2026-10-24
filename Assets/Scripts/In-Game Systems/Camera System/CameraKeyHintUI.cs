using TMPro;
using UnityEngine;

// Key hints (V fullscreen, R flash) shown while the camera is held up in handheld mode.
public sealed class CameraKeyHintUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CameraSystemManager cameraSystemManager;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text flashLabel;

    [Header("Fade")]
    [SerializeField] private float fadeSpeed = 6f;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        bool show = cameraSystemManager.CurrentState == CameraModeState.Active
            && cameraSystemManager.CurrentViewMode == CameraViewMode.Handheld
            && !cameraSystemManager.IsViewTransitioning;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);

        flashLabel.text = cameraSystemManager.FlashEnabled ? "Flash On" : "Flash Off";
    }
}
