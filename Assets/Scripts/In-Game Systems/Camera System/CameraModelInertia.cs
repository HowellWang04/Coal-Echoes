using UnityEngine;

public sealed class CameraModelInertia : MonoBehaviour
{
    [Header("Object Refs")]
    [SerializeField] private Transform sourceCamera;
    [SerializeField] private Transform inertiaTarget;

    [Header("Rotation Inertia")]
    [SerializeField] private float rotationInfluence = 0.35f;
    [SerializeField] private float maxPitch = 3f;
    [SerializeField] private float maxYaw = 4f;
    [SerializeField] private float rollInfluence = 0.25f;
    [SerializeField] private float returnSmoothTime = 0.12f;

    [Header("Idle Sway")]
    [SerializeField] private float idlePositionAmount = 0.002f;
    [SerializeField] private float idleRotationAmount = 0.15f;
    [SerializeField] private float idleFrequency = 0.8f;

    [Header("Movement Sway")]
    [SerializeField] private float movementPositionAmount = 0.012f;
    [SerializeField] private float movementRotationAmount = 0.8f;
    [SerializeField] private float movementFrequency = 6f;
    [SerializeField] private float fullMovementSpeed = 2f;
    [SerializeField] private float movementBlendSmoothTime = 0.15f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 previousSourcePosition;
    private Quaternion previousSourceRotation;
    private Vector3 currentOffset;
    private Vector3 smoothVelocity;
    private float movementBlend;
    private float movementBlendVelocity;
    private float movementPhase;

    private void OnEnable()
    {
        if (sourceCamera == null && Camera.main != null)
            sourceCamera = Camera.main.transform;
        if (inertiaTarget == null)
            inertiaTarget = transform;

        if (inertiaTarget != null)
        {
            baseLocalPosition = inertiaTarget.localPosition;
            baseLocalRotation = inertiaTarget.localRotation;
        }
        if (sourceCamera != null)
        {
            previousSourcePosition = sourceCamera.position;
            previousSourceRotation = sourceCamera.rotation;
        }
        currentOffset = Vector3.zero;
        smoothVelocity = Vector3.zero;
        movementBlend = 0f;
        movementBlendVelocity = 0f;
        movementPhase = 0f;
    }

    private void LateUpdate()
    {
        if (sourceCamera == null || inertiaTarget == null) return;

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        Quaternion rotationDelta = Quaternion.Inverse(previousSourceRotation) * sourceCamera.rotation;
        Vector3 deltaEuler = NormalizeEuler(rotationDelta.eulerAngles);

        currentOffset.x -= deltaEuler.x * rotationInfluence;
        currentOffset.y -= deltaEuler.y * rotationInfluence;
        currentOffset.z += deltaEuler.y * rollInfluence;
        currentOffset.x = Mathf.Clamp(currentOffset.x, -maxPitch, maxPitch);
        currentOffset.y = Mathf.Clamp(currentOffset.y, -maxYaw, maxYaw);
        currentOffset.z = Mathf.Clamp(currentOffset.z, -maxYaw, maxYaw);

        currentOffset = Vector3.SmoothDamp(
            currentOffset,
            Vector3.zero,
            ref smoothVelocity,
            returnSmoothTime);

        Vector3 movementDelta = sourceCamera.position - previousSourcePosition;
        movementDelta.y = 0f;
        float movementSpeed = movementDelta.magnitude / deltaTime;
        float targetMovementBlend = Mathf.Clamp01(movementSpeed / Mathf.Max(0.01f, fullMovementSpeed));
        movementBlend = Mathf.SmoothDamp(
            movementBlend,
            targetMovementBlend,
            ref movementBlendVelocity,
            movementBlendSmoothTime);
        movementPhase += deltaTime * movementFrequency * Mathf.Lerp(0.5f, 1f, movementBlend);

        float idlePhase = Time.time * idleFrequency;
        Vector3 idlePosition = new Vector3(
            Mathf.Sin(idlePhase) * 0.6f,
            Mathf.Sin(idlePhase * 1.37f + 1.2f),
            Mathf.Sin(idlePhase * 0.73f + 2.1f) * 0.5f) * idlePositionAmount;
        Vector3 idleRotation = new Vector3(
            Mathf.Sin(idlePhase * 1.11f + 0.8f),
            Mathf.Sin(idlePhase * 0.83f),
            Mathf.Sin(idlePhase * 0.91f + 2f) * 0.8f) * idleRotationAmount;

        Vector3 movementPosition = new Vector3(
            Mathf.Sin(movementPhase),
            -Mathf.Cos(movementPhase * 2f) * 0.65f,
            Mathf.Cos(movementPhase) * 0.35f) * movementPositionAmount;
        Vector3 movementRotation = new Vector3(
            Mathf.Cos(movementPhase * 2f) * 0.5f,
            Mathf.Sin(movementPhase) * 0.35f,
            -Mathf.Sin(movementPhase)) * movementRotationAmount;

        float idleBlend = 1f - movementBlend;
        inertiaTarget.localPosition = baseLocalPosition
            + idlePosition * idleBlend
            + movementPosition * movementBlend;
        inertiaTarget.localRotation = baseLocalRotation * Quaternion.Euler(
            currentOffset
            + idleRotation * idleBlend
            + movementRotation * movementBlend);
        previousSourcePosition = sourceCamera.position;
        previousSourceRotation = sourceCamera.rotation;
    }

    private void OnDisable()
    {
        if (inertiaTarget == null) return;
        inertiaTarget.localPosition = baseLocalPosition;
        inertiaTarget.localRotation = baseLocalRotation;
    }

    private static Vector3 NormalizeEuler(Vector3 euler)
    {
        euler.x = Mathf.DeltaAngle(0f, euler.x);
        euler.y = Mathf.DeltaAngle(0f, euler.y);
        euler.z = Mathf.DeltaAngle(0f, euler.z);
        return euler;
    }
}
