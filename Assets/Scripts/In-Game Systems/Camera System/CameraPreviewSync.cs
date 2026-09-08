using UnityEngine;

public sealed class CameraPreviewSync : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;

    private Camera previewCamera;

    private void Awake()
    {
        previewCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (sourceCamera == null || previewCamera == null) return;

        transform.SetPositionAndRotation(
            sourceCamera.transform.position,
            sourceCamera.transform.rotation);
        previewCamera.fieldOfView = sourceCamera.fieldOfView;
        previewCamera.nearClipPlane = sourceCamera.nearClipPlane;
        previewCamera.farClipPlane = sourceCamera.farClipPlane;
    }
}
