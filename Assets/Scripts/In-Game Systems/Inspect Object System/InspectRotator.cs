using UnityEngine;

public class InspectRotator : MonoBehaviour
{
    private const float LegacyMouseAxisSensitivity = 0.1f;
    private const float ScrollDeltaPerStep = 120f;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 5f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 0.1f;
    [SerializeField] private float minDistance = 0.2f;
    [SerializeField] private float maxDistance = 2f;

    private Camera cam;
    private PlayerInputProvider inputProvider;
    private float currentDistance;
    private Vector3 originPosition;
    private bool active = false;

    public void Activate(Camera camera, float startDistance, PlayerInputProvider provider)
    {
        cam = camera;
        inputProvider = provider;
        currentDistance = startDistance;
        originPosition = transform.position;
        active = true;
    }

    public void Deactivate()
    {
        active = false;
    }

    private void Update()
    {
        if (!active || cam == null || inputProvider == null) return;

        HandleRotation();
        HandleZoom();
    }

    private void HandleRotation()
    {
        var gameplay = inputProvider.Controls.Gameplay;
        if (!gameplay.InspectRotate.IsPressed()) return;

        Vector2 mouseDelta = gameplay.Look.ReadValue<Vector2>() * LegacyMouseAxisSensitivity;
        float mouseX = mouseDelta.x * rotateSpeed;
        float mouseY = mouseDelta.y * rotateSpeed;

        transform.Rotate(cam.transform.up, -mouseX, Space.World);
        transform.Rotate(cam.transform.right, mouseY, Space.World);
    }

    private void HandleZoom()
    {
        float scroll = inputProvider.Controls.Gameplay.InspectZoom.ReadValue<float>();
        if (Mathf.Abs(scroll) < 0.001f) return;

        scroll = scroll / ScrollDeltaPerStep * LegacyMouseAxisSensitivity;

        currentDistance -= scroll * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        Vector3 newPos = cam.transform.position + cam.transform.forward * currentDistance;
        transform.position = newPos;
    }
}
