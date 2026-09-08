using System;
using System.Collections;
using UnityEngine;

public sealed class CameraViewTransitionHandler : MonoBehaviour
{
    [Header("Object Refs")]
    [SerializeField] private CameraRaiseAnimationHandler cameraRaiseAnimationHandler;
    [SerializeField] private CanvasGroup fullscreenCanvasGroup;

    [Header("Fullscreen Transition Pose Offset")]
    [SerializeField] private Vector3 fullscreenLocalPositionOffset = new Vector3(0f, 0f, -0.08f);
    [SerializeField] private Vector3 fullscreenLocalRotationOffset = Vector3.zero;
    [SerializeField] private float fullscreenScaleMultiplier = 1.15f;

    [Header("Timing")]
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public bool IsConfigured => cameraRaiseAnimationHandler != null && fullscreenCanvasGroup != null;
    public bool IsTransitioning => current != null;

    private Coroutine current;
    private Vector3 handheldLocalPosition;
    private Vector3 handheldLocalRotation;
    private Vector3 handheldLocalScale;
    private bool handheldPoseSaved;

    public bool PlayTransition(CameraViewMode targetMode, Action onComplete)
    {
        if (!IsConfigured || current != null) return false;

        current = StartCoroutine(TransitionRoutine(targetMode, onComplete));
        return true;
    }

    public void SetImmediate(CameraViewMode mode)
    {
        if (!IsConfigured) return;
        if (current != null) StopCoroutine(current);
        current = null;

        Transform cameraModel = cameraRaiseAnimationHandler.CameraModelTransform;
        if (cameraModel == null) return;

        if (mode == CameraViewMode.Handheld)
        {
            if (cameraModel.gameObject.activeSelf)
                SaveHandheldPose(cameraModel);
            else if (handheldPoseSaved)
                ApplyHandheldPose(cameraModel);
            cameraRaiseAnimationHandler.SetCameraModelVisible(true);
            SetFullscreenUI(false, 0f);
        }
        else
        {
            if (!handheldPoseSaved || cameraModel.gameObject.activeSelf)
                SaveHandheldPose(cameraModel);
            cameraRaiseAnimationHandler.SetCameraModelVisible(false);
            SetFullscreenUI(true, 1f);
        }
    }

    public void ShowFullscreenUIImmediate()
    {
        if (fullscreenCanvasGroup != null) SetFullscreenUI(true, 1f);
    }

    public void HideFullscreenUIImmediate()
    {
        if (fullscreenCanvasGroup != null) SetFullscreenUI(false, 0f);
    }

    public void SetCameraModelVisibleImmediate(bool visible)
    {
        if (cameraRaiseAnimationHandler != null)
            cameraRaiseAnimationHandler.SetCameraModelVisible(visible);
    }

    private IEnumerator TransitionRoutine(CameraViewMode targetMode, Action onComplete)
    {
        Transform cameraModel = cameraRaiseAnimationHandler.CameraModelTransform;
        if (cameraModel == null)
        {
            current = null;
            yield break;
        }

        if (targetMode == CameraViewMode.Fullscreen)
        {
            SaveHandheldPose(cameraModel);
            cameraRaiseAnimationHandler.SetCameraModelVisible(true);
            SetFullscreenUI(true, 0f);
            yield return Animate(
                cameraModel,
                handheldLocalPosition,
                handheldLocalRotation,
                handheldLocalScale,
                FullscreenLocalPosition,
                FullscreenLocalRotation,
                FullscreenLocalScale,
                0f,
                1f);
            ApplyFullscreenPose(cameraModel);
            cameraRaiseAnimationHandler.SetCameraModelVisible(false);
            SetFullscreenUI(true, 1f);
        }
        else
        {
            cameraRaiseAnimationHandler.SetCameraModelVisible(true);
            ApplyFullscreenPose(cameraModel);
            SetFullscreenUI(true, 1f);
            yield return Animate(
                cameraModel,
                FullscreenLocalPosition,
                FullscreenLocalRotation,
                FullscreenLocalScale,
                handheldLocalPosition,
                handheldLocalRotation,
                handheldLocalScale,
                1f,
                0f);
            ApplyHandheldPose(cameraModel);
            SetFullscreenUI(false, 0f);
        }

        current = null;
        onComplete?.Invoke();
    }

    private IEnumerator Animate(
        Transform cameraModel,
        Vector3 fromPosition,
        Vector3 fromRotation,
        Vector3 fromScale,
        Vector3 toPosition,
        Vector3 toRotation,
        Vector3 toScale,
        float fromAlpha,
        float toAlpha)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));

            cameraModel.localPosition = Vector3.Lerp(fromPosition, toPosition, t);
            cameraModel.localEulerAngles = Vector3.Lerp(fromRotation, toRotation, t);
            cameraModel.localScale = Vector3.Lerp(fromScale, toScale, t);
            fullscreenCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            yield return null;
        }
    }

    private void SaveHandheldPose(Transform cameraModel)
    {
        handheldLocalPosition = cameraModel.localPosition;
        handheldLocalRotation = cameraModel.localEulerAngles;
        handheldLocalScale = cameraModel.localScale;
        handheldPoseSaved = true;
    }

    private void ApplyHandheldPose(Transform cameraModel)
    {
        cameraModel.localPosition = handheldLocalPosition;
        cameraModel.localEulerAngles = handheldLocalRotation;
        cameraModel.localScale = handheldLocalScale;
    }

    private void ApplyFullscreenPose(Transform cameraModel)
    {
        cameraModel.localPosition = FullscreenLocalPosition;
        cameraModel.localEulerAngles = FullscreenLocalRotation;
        cameraModel.localScale = FullscreenLocalScale;
    }

    private Vector3 FullscreenLocalPosition => handheldLocalPosition + fullscreenLocalPositionOffset;
    private Vector3 FullscreenLocalRotation => handheldLocalRotation + fullscreenLocalRotationOffset;
    private Vector3 FullscreenLocalScale => handheldLocalScale * fullscreenScaleMultiplier;

    private void SetFullscreenUI(bool active, float alpha)
    {
        fullscreenCanvasGroup.gameObject.SetActive(active);
        fullscreenCanvasGroup.alpha = alpha;
        fullscreenCanvasGroup.interactable = active && alpha >= 1f;
        fullscreenCanvasGroup.blocksRaycasts = active && alpha >= 1f;
    }
}
