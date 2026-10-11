using UnityEngine;

public class EchoSystemManager : MonoBehaviour
{
    [SerializeField] private CameraSystemManager cameraSystemManager;
    [SerializeField] private EchoPopupUI echoPopupUI;

    private void OnEnable()
    {
        if (cameraSystemManager != null)
        {
            cameraSystemManager.OnNewEchoDiscovered += ShowNewEcho;
            cameraSystemManager.OnCaptureStarted += HidePopup;
        }
    }

    private void OnDisable()
    {
        if (cameraSystemManager != null)
        {
            cameraSystemManager.OnNewEchoDiscovered -= ShowNewEcho;
            cameraSystemManager.OnCaptureStarted -= HidePopup;
        }
    }

    private void ShowNewEcho()
    {
        if (echoPopupUI != null) echoPopupUI.Show();
    }

    private void HidePopup()
    {
        if (echoPopupUI != null) echoPopupUI.Hide();
    }
}
