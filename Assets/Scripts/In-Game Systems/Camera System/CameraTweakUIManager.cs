using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class CameraTweakUIManager : MonoBehaviour
{
    [Header("Camera Ref")]
    [SerializeField] private Camera photoCamera;

    [Header("Photo Volume Refs")]
    [SerializeField] private Volume previewVolume;
    [SerializeField] private Volume fullscreenVolume;

    [Header("UI Components Refs")]
    [SerializeField] private GameObject cameraTab;
    [SerializeField] private GameObject postTab;
    [SerializeField] private Button cameraTabButton;
    [SerializeField] private Button postTabButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Toggle gridToggle;
    [SerializeField] private GameObject gridOverlay;

    [Header("Focal Length")]
    [SerializeField] private Slider focalLengthSlider;
    [SerializeField] private TMP_Text focalLengthText;

    [Header("Range (mm)")]
    [SerializeField] private float minFocalLength = 7f;
    [SerializeField] private float maxFocalLength = 275f;

    [Header("Depth of Field")]
    [SerializeField] private Slider apertureSlider;
    [SerializeField] private TMP_Text apertureText;
    [SerializeField] private float minAperture = 1.4f;
    [SerializeField] private float maxAperture = 16f;

    [Header("Focus Distance")]
    [SerializeField] private Slider focusDistanceSlider;
    [SerializeField] private TMP_Text focusDistanceText;
    [SerializeField] private float minFocusDistance = 0.1f;
    [SerializeField] private float maxFocusDistance = 1000f;

    [Header("Vignette")]
    [SerializeField] private Slider vignetteSlider;
    [SerializeField] private TMP_Text vignetteText;
    [SerializeField] private float minVignette = 0f;
    [SerializeField] private float maxVignette = 1f;

    [Header("Exposure")]
    [SerializeField] private Slider exposureSlider;
    [SerializeField] private TMP_Text exposureText;
    [SerializeField] private float minExposure = -3f;
    [SerializeField] private float maxExposure = 3f;

    [Header("Bloom")]
    [SerializeField] private Slider bloomSlider;
    [SerializeField] private TMP_Text bloomText;
    [SerializeField] private float minBloom = 0f;
    [SerializeField] private float maxBloom = 10f;

    [Header("Saturation")]
    [SerializeField] private Slider saturationSlider;
    [SerializeField] private TMP_Text saturationText;
    [SerializeField] private float minSaturation = -100f;
    [SerializeField] private float maxSaturation = 100f;

    private DepthOfField previewDepthOfField;
    private DepthOfField fullscreenDepthOfField;
    private float defaultFocusDistance;

    private Vignette previewVignette;
    private Vignette fullscreenVignette;
    private ColorAdjustments previewColorAdjustments;
    private ColorAdjustments fullscreenColorAdjustments;
    private Bloom previewBloom;
    private Bloom fullscreenBloom;

    private float defaultFocalLength;
    private float defaultAperture;
    private float defaultVignetteIntensity;
    private float defaultExposure;
    private float defaultBloomIntensity;
    private float defaultSaturation;

    private bool defaultsSaved = false;
    private UITabTransition cameraTabTransition;
    private UITabTransition postTabTransition;
    private UITabTransition activeTabTransition;

    // 35mm full-frame sensor height (24mm)
    private const float SensorHalfHeight = 12f;

    private void Awake()
    {
        Initialize();
        ConfigureTabTransitions();
    }

    private void Initialize()
    {
        if (defaultsSaved) return;

        if (previewVolume != null)
        {
            previewVolume.profile.TryGet(out previewDepthOfField);
            previewVolume.profile.TryGet(out previewVignette);
            previewVolume.profile.TryGet(out previewColorAdjustments);
            previewVolume.profile.TryGet(out previewBloom);
        }
        if (fullscreenVolume != null)
        {
            fullscreenVolume.profile.TryGet(out fullscreenDepthOfField);
            fullscreenVolume.profile.TryGet(out fullscreenVignette);
            fullscreenVolume.profile.TryGet(out fullscreenColorAdjustments);
            fullscreenVolume.profile.TryGet(out fullscreenBloom);
        }
        if (photoCamera != null)
        {
            float fieldOfView = photoCamera.fieldOfView;
            photoCamera.focalLength = FocalLengthFromFov(fieldOfView);
            photoCamera.fieldOfView = fieldOfView;
        }
        SaveDefaults();
    }

    private void OnEnable()
    {
        if (focalLengthSlider != null)
        {
            focalLengthSlider.minValue = 0f;
            focalLengthSlider.maxValue = 1f;
            focalLengthSlider.wholeNumbers = false;
            focalLengthSlider.onValueChanged.AddListener(OnFocalLengthChanged);
            SyncFocalLengthSlider();
        }

        if (apertureSlider != null)
        {
            apertureSlider.minValue = 0f;
            apertureSlider.maxValue = 1f;
            apertureSlider.wholeNumbers = false;
            apertureSlider.onValueChanged.AddListener(OnApertureChanged);
            SyncApertureSlider();
        }

        if (focusDistanceSlider != null)
        {
            focusDistanceSlider.minValue = 0f;
            focusDistanceSlider.maxValue = 1f;
            focusDistanceSlider.wholeNumbers = false;
            focusDistanceSlider.onValueChanged.AddListener(OnFocusDistanceChanged);
            SyncFocusDistanceSlider();
        }

        if (vignetteSlider != null)
        {
            vignetteSlider.minValue = minVignette;
            vignetteSlider.maxValue = maxVignette;
            vignetteSlider.onValueChanged.AddListener(OnVignetteChanged);
            SyncVignetteSlider();
        }

        if (exposureSlider != null)
        {
            exposureSlider.minValue = minExposure;
            exposureSlider.maxValue = maxExposure;
            exposureSlider.onValueChanged.AddListener(OnExposureChanged);
            SyncExposureSlider();
        }

        if (bloomSlider != null)
        {
            bloomSlider.minValue = minBloom;
            bloomSlider.maxValue = maxBloom;
            bloomSlider.onValueChanged.AddListener(OnBloomChanged);
            SyncBloomSlider();
        }

        if (saturationSlider != null)
        {
            saturationSlider.minValue = minSaturation;
            saturationSlider.maxValue = maxSaturation;
            saturationSlider.onValueChanged.AddListener(OnSaturationChanged);
            SyncSaturationSlider();
        }

        if (cameraTabButton != null) cameraTabButton.onClick.AddListener(ShowCameraTab);
        if (postTabButton != null)   postTabButton.onClick.AddListener(ShowPostTab);
        if (resetButton != null)     resetButton.onClick.AddListener(ResetCameraTweaks);
        if (gridToggle != null)      gridToggle.onValueChanged.AddListener(OnGridToggleChanged);

        ShowCameraTab();
    }

    private void OnDisable()
    {
        if (activeTabTransition != null)
            CompleteTabTransitionState();

        if (focalLengthSlider != null) focalLengthSlider.onValueChanged.RemoveListener(OnFocalLengthChanged);
        if (apertureSlider != null)    apertureSlider.onValueChanged.RemoveListener(OnApertureChanged);
        if (focusDistanceSlider != null) focusDistanceSlider.onValueChanged.RemoveListener(OnFocusDistanceChanged);
        if (vignetteSlider != null)    vignetteSlider.onValueChanged.RemoveListener(OnVignetteChanged);
        if (exposureSlider != null)    exposureSlider.onValueChanged.RemoveListener(OnExposureChanged);
        if (bloomSlider != null)       bloomSlider.onValueChanged.RemoveListener(OnBloomChanged);
        if (saturationSlider != null)  saturationSlider.onValueChanged.RemoveListener(OnSaturationChanged);
        if (cameraTabButton != null)   cameraTabButton.onClick.RemoveListener(ShowCameraTab);
        if (postTabButton != null)     postTabButton.onClick.RemoveListener(ShowPostTab);
        if (resetButton != null)       resetButton.onClick.RemoveListener(ResetCameraTweaks);
        if (gridToggle != null)        gridToggle.onValueChanged.RemoveListener(OnGridToggleChanged);
    }

    // ── Focal Length ────────────────────────────────────────────

    private float FovFromFocalLength(float mm) =>
        2f * Mathf.Atan(SensorHalfHeight / mm) * Mathf.Rad2Deg;

    private float FocalLengthFromFov(float fov) =>
        SensorHalfHeight / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

    private void OnFocalLengthChanged(float value)
    {
        if (photoCamera == null) return;
        float mm = minFocalLength * Mathf.Pow(maxFocalLength / minFocalLength, value);
        photoCamera.focalLength = mm;
        photoCamera.fieldOfView = FovFromFocalLength(mm);
        if (focalLengthText != null) focalLengthText.text = $"{mm:F0}mm";
    }

    private void SyncFocalLengthSlider()
    {
        if (photoCamera == null || focalLengthSlider == null) return;
        float mm = Mathf.Clamp(FocalLengthFromFov(photoCamera.fieldOfView), minFocalLength, maxFocalLength);
        float value = Mathf.Log(mm / minFocalLength) / Mathf.Log(maxFocalLength / minFocalLength);
        focalLengthSlider.SetValueWithoutNotify(value);
        if (focalLengthText != null) focalLengthText.text = $"{mm:F0}mm";
    }

    // ── Aperture ─────────────────────────────────────────────────

    private void OnApertureChanged(float value)
    {
        float aperture = minAperture * Mathf.Pow(maxAperture / minAperture, value);
        if (photoCamera != null) photoCamera.aperture = aperture;
        if (apertureText != null) apertureText.text = $"{aperture:F1}";
    }

    private void SyncApertureSlider()
    {
        if (photoCamera == null || apertureSlider == null) return;
        float aperture = Mathf.Clamp(photoCamera.aperture, minAperture, maxAperture);
        float value = Mathf.Log(aperture / minAperture) / Mathf.Log(maxAperture / minAperture);
        apertureSlider.SetValueWithoutNotify(value);
        if (apertureText != null) apertureText.text = $"{photoCamera.aperture:F1}";
    }

    // ── Vignette ──────────────────────────────────────────────────

    private void OnFocusDistanceChanged(float value)
    {
        float distance = minFocusDistance * Mathf.Pow(maxFocusDistance / minFocusDistance, value);
        SetFocusDistance(distance);
        if (focusDistanceText != null) focusDistanceText.text = distance >= 100f ? $"{distance:F0} m" : $"{distance:F1} m";
    }

    private void SetFocusDistance(float distance)
    {
        if (previewDepthOfField != null) previewDepthOfField.focusDistance.Override(distance);
        if (fullscreenDepthOfField != null) fullscreenDepthOfField.focusDistance.Override(distance);
    }

    private void SyncFocusDistanceSlider()
    {
        DepthOfField dof = previewDepthOfField != null ? previewDepthOfField : fullscreenDepthOfField;
        if (dof == null || focusDistanceSlider == null) return;
        float distance = Mathf.Clamp(dof.focusDistance.value, minFocusDistance, maxFocusDistance);
        float value = Mathf.Log(distance / minFocusDistance) / Mathf.Log(maxFocusDistance / minFocusDistance);
        focusDistanceSlider.SetValueWithoutNotify(value);
        if (focusDistanceText != null) focusDistanceText.text = distance >= 100f ? $"{distance:F0} m" : $"{distance:F1} m";
    }

    private void OnVignetteChanged(float value)
    {
        if (previewVignette != null) previewVignette.intensity.Override(value);
        if (fullscreenVignette != null) fullscreenVignette.intensity.Override(value);
        if (vignetteText != null) vignetteText.text = value <= 0f ? "Off" : $"{value * 100f:F0}%";
    }

    private void SyncVignetteSlider()
    {
        Vignette vignette = previewVignette != null ? previewVignette : fullscreenVignette;
        if (vignette == null || vignetteSlider == null) return;
        vignetteSlider.SetValueWithoutNotify(vignette.intensity.value);
        if (vignetteText != null) vignetteText.text = vignette.intensity.value <= 0f ? "Off" : $"{vignette.intensity.value * 100f:F0}%";
    }

    // ── Exposure ──────────────────────────────────────────────────

    private void OnExposureChanged(float value)
    {
        if (previewColorAdjustments != null) previewColorAdjustments.postExposure.Override(value);
        if (fullscreenColorAdjustments != null) fullscreenColorAdjustments.postExposure.Override(value);
        if (exposureText != null) exposureText.text = $"{value:+0.0;−0.0;0.0} EV";
    }

    private void SyncExposureSlider()
    {
        ColorAdjustments colorAdjustments = previewColorAdjustments != null
            ? previewColorAdjustments
            : fullscreenColorAdjustments;
        if (colorAdjustments == null || exposureSlider == null) return;
        exposureSlider.SetValueWithoutNotify(colorAdjustments.postExposure.value);
        if (exposureText != null) exposureText.text = $"{colorAdjustments.postExposure.value:+0.0;−0.0;0.0} EV";
    }

    // ── Bloom ─────────────────────────────────────────────────────

    private void OnBloomChanged(float value)
    {
        if (previewBloom != null) previewBloom.intensity.Override(Mathf.InverseLerp(minBloom, maxBloom, value));
        if (fullscreenBloom != null) fullscreenBloom.intensity.Override(Mathf.InverseLerp(minBloom, maxBloom, value));
        if (bloomText != null) bloomText.text = value <= minBloom ? "Off" : $"{Mathf.InverseLerp(minBloom, maxBloom, value) * 100f:F0}%";
    }

    private void SyncBloomSlider()
    {
        Bloom bloom = previewBloom != null ? previewBloom : fullscreenBloom;
        if (bloom == null || bloomSlider == null) return;
        float value = Mathf.Lerp(minBloom, maxBloom, bloom.intensity.value);
        bloomSlider.SetValueWithoutNotify(value);
        if (bloomText != null) bloomText.text = value <= minBloom ? "Off" : $"{Mathf.InverseLerp(minBloom, maxBloom, value) * 100f:F0}%";
    }

    // ── Saturation ────────────────────────────────────────────────

    private void OnSaturationChanged(float value)
    {
        if (previewColorAdjustments != null) previewColorAdjustments.saturation.Override(value);
        if (fullscreenColorAdjustments != null) fullscreenColorAdjustments.saturation.Override(value);
        if (saturationText != null) saturationText.text = Mathf.RoundToInt(value) == 0 ? "Normal" : $"{value:+0;−0;0}";
    }

    private void SyncSaturationSlider()
    {
        ColorAdjustments colorAdjustments = previewColorAdjustments != null
            ? previewColorAdjustments
            : fullscreenColorAdjustments;
        if (colorAdjustments == null || saturationSlider == null) return;
        saturationSlider.SetValueWithoutNotify(colorAdjustments.saturation.value);
        if (saturationText != null) saturationText.text = Mathf.RoundToInt(colorAdjustments.saturation.value) == 0 ? "Normal" : $"{colorAdjustments.saturation.value:+0;−0;0}";
    }

    // ── Tabs & Grid ───────────────────────────────────────────────

    private void ShowCameraTab()
    {
        SwitchTab(cameraTabTransition);
    }

    private void ShowPostTab()
    {
        SwitchTab(postTabTransition);
    }

    private void ConfigureTabTransitions()
    {
        var animationScope = GetComponentInParent<UIAnimationScope>();
        cameraTabTransition = animationScope.RegisterTab(cameraTab);
        postTabTransition = animationScope.RegisterTab(postTab);
        cameraTabTransition.HideImmediate();
        postTabTransition.HideImmediate();
    }

    private void SwitchTab(UITabTransition nextTab)
    {
        if (activeTabTransition == nextTab) return;

        activeTabTransition?.Hide();
        nextTab.Show();
        activeTabTransition = nextTab;
    }

    private void CompleteTabTransitionState()
    {
        cameraTabTransition.HideImmediate();
        postTabTransition.HideImmediate();
        activeTabTransition.ShowImmediate();
    }

    private void OnGridToggleChanged(bool isOn)
    {
        if (gridOverlay != null) gridOverlay.SetActive(isOn);
    }

    // ── Defaults ──────────────────────────────────────────────────

    private void SaveDefaults()
    {
        if (defaultsSaved) return;
        defaultsSaved = true;

        DepthOfField dof = previewDepthOfField != null ? previewDepthOfField : fullscreenDepthOfField;
        if (dof != null) defaultFocusDistance = dof.focusDistance.value;

        if (photoCamera != null)
            defaultFocalLength = FocalLengthFromFov(photoCamera.fieldOfView);
        Vignette vignette = previewVignette != null ? previewVignette : fullscreenVignette;
        ColorAdjustments colorAdjustments = previewColorAdjustments != null
            ? previewColorAdjustments
            : fullscreenColorAdjustments;
        Bloom bloom = previewBloom != null ? previewBloom : fullscreenBloom;
        if (photoCamera != null)
            defaultAperture = photoCamera.aperture;
        if (vignette != null)
            defaultVignetteIntensity = vignette.intensity.value;
        if (colorAdjustments != null)
        {
            defaultExposure    = colorAdjustments.postExposure.value;
            defaultSaturation  = colorAdjustments.saturation.value;
        }
        if (bloom != null)
            defaultBloomIntensity = bloom.intensity.value;
    }

    public void ResetCameraTweaks()
    {
        Initialize();

        if (photoCamera != null)
        {
            photoCamera.focalLength = defaultFocalLength;
            photoCamera.fieldOfView = FovFromFocalLength(defaultFocalLength);
        }
        if (photoCamera != null) photoCamera.aperture = defaultAperture;
        SetFocusDistance(defaultFocusDistance);
        if (previewVignette != null) previewVignette.intensity.Override(defaultVignetteIntensity);
        if (fullscreenVignette != null) fullscreenVignette.intensity.Override(defaultVignetteIntensity);
        if (previewColorAdjustments != null)
        {
            previewColorAdjustments.postExposure.Override(defaultExposure);
            previewColorAdjustments.saturation.Override(defaultSaturation);
        }
        if (fullscreenColorAdjustments != null)
        {
            fullscreenColorAdjustments.postExposure.Override(defaultExposure);
            fullscreenColorAdjustments.saturation.Override(defaultSaturation);
        }
        if (previewBloom != null) previewBloom.intensity.Override(defaultBloomIntensity);
        if (fullscreenBloom != null) fullscreenBloom.intensity.Override(defaultBloomIntensity);

        SyncFocalLengthSlider();
        SyncApertureSlider();
        SyncFocusDistanceSlider();
        SyncVignetteSlider();
        SyncExposureSlider();
        SyncBloomSlider();
        SyncSaturationSlider();
    }
}
