using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public sealed class CameraRaiseAnimationHandler : MonoBehaviour
{
    public Transform CameraModelTransform => cameraModel != null ? cameraModel.transform : null;

    [Header("Camera Model Ref")]
    [SerializeField] private GameObject cameraModel;
    [SerializeField] private Transform focusTarget;

    [Header("Preview Camera Ref")]
    [SerializeField] private Camera previewCamera;

    [Header("Handheld Focus Volume Ref")]
    [SerializeField] private Volume handheldFocusVolume;

    [Header("Start State (below screen)")]
    [SerializeField] private Vector3 startLocalPosition = new Vector3(0f, -0.6f, 1.2f);
    [SerializeField] private Vector3 startLocalRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 startLocalScale    = new Vector3(0.3f, 0.3f, 0.3f);

    [Header("End State (close to eye)")]
    [SerializeField] private Vector3 endLocalPosition = new Vector3(0f, 0f, 0.4f);
    [SerializeField] private Vector3 endLocalRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 endLocalScale    = new Vector3(2f, 2f, 2f);

    [Header("Timing")]
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine current;
    private DepthOfField dof;

    private void Awake()
    {
        if (handheldFocusVolume != null)
            handheldFocusVolume.profile.TryGet(out dof);
        SetCameraModelVisible(false);
    }

    public void PlayRaise(Action onComplete)
    {
        if (current != null) StopCoroutine(current);
        SetCameraModelVisible(true);
        current = StartCoroutine(AnimateRoutine(startLocalPosition, startLocalRotation, startLocalScale,
                                                endLocalPosition,   endLocalRotation,   endLocalScale,
                                                hideOnComplete: false, onComplete));
    }

    public void PlayLower(Action onComplete)
    {
        if (current != null) StopCoroutine(current);
        SetCameraModelVisible(true);
        hideCameraModelShadows();
        cameraModel.transform.localPosition    = endLocalPosition;
        cameraModel.transform.localEulerAngles = endLocalRotation;
        cameraModel.transform.localScale       = endLocalScale;
        current = StartCoroutine(AnimateRoutine(endLocalPosition, endLocalRotation, endLocalScale,
                                                startLocalPosition, startLocalRotation, startLocalScale,
                                                hideOnComplete: true, onComplete));
    }

    public void SetCameraModelVisible(bool visible)
    {
        if (cameraModel != null) cameraModel.SetActive(visible);
        if (previewCamera != null) previewCamera.enabled = visible;
        if (handheldFocusVolume != null) handheldFocusVolume.weight = visible ? 1f : 0f;
        if (visible) UpdateHandheldFocus();
    }

    private IEnumerator AnimateRoutine(
        Vector3 fromPos, Vector3 fromRot, Vector3 fromScale,
        Vector3 toPos,   Vector3 toRot,   Vector3 toScale,
        bool hideOnComplete, Action onComplete)
    {
        cameraModel.transform.localPosition    = fromPos;
        cameraModel.transform.localEulerAngles = fromRot;
        cameraModel.transform.localScale       = fromScale;
        UpdateHandheldFocus();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));

            cameraModel.transform.localPosition    = Vector3.Lerp(fromPos,   toPos,   t);
            cameraModel.transform.localEulerAngles = Vector3.Lerp(fromRot,   toRot,   t);
            cameraModel.transform.localScale       = Vector3.Lerp(fromScale, toScale, t);
            UpdateHandheldFocus();

            yield return null;
        }

        if (hideOnComplete) SetCameraModelVisible(false);
        current = null;
        onComplete?.Invoke();
    }

    private void UpdateHandheldFocus()
    {
        Camera mainCamera = Camera.main;
        Transform target = focusTarget != null ? focusTarget : cameraModel?.transform;
        if (dof == null || mainCamera == null || target == null) return;

        float focusDistance = Vector3.Dot(
            target.position - mainCamera.transform.position,
            mainCamera.transform.forward);
        dof.focusDistance.Override(Mathf.Max(0.1f, focusDistance));
    }

    private void hideCameraModelShadows()
    {
        var renderers = cameraModel.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }
}
