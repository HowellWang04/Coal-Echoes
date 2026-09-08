using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class UIButtonTweenFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private UIAnimationProfile animationProfile;
    [SerializeField] private RectTransform visualRoot;

    private Button button;
    private Canvas buttonCanvas;
    private Tween scaleTween;
    private Vector3 baseScale;
    private ColorBlock baseColors;
    private int baseSortingOrder;
    private bool hasBaseScale;
    private bool hasBaseColors;
    private bool isHovered;
    private bool isPressed;
    private bool isFocused;
    private bool isTabActive;
    private bool useSidebarSettings;

    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(UIAnimationProfile profile)
    {
        animationProfile = profile;
        CacheReferences();
    }

    public void UseSidebarSettings()
    {
        useSidebarSettings = true;
    }

    public void SetTabActive(bool active)
    {
        isTabActive = active;
        buttonCanvas.sortingOrder = baseSortingOrder + (active ? 1 : 0);
        ApplySidebarColors();
        AnimateToCurrentState();
    }

    private void OnEnable()
    {
        if (hasBaseScale)
            AnimateToCurrentState();
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        scaleTween = null;
        isHovered = false;
        isPressed = false;
        isFocused = false;

        if (visualRoot != null && hasBaseScale)
            visualRoot.localScale = baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AnimateToCurrentState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        AnimateToCurrentState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.IsInteractable()) return;

        isPressed = true;
        AnimateToCurrentState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        AnimateToCurrentState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isFocused = true;
        AnimateToCurrentState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isFocused = false;
        isPressed = false;
        AnimateToCurrentState();
    }

    private void AnimateToCurrentState()
    {
        if (animationProfile == null || visualRoot == null || button == null) return;

        bool canInteract = button.IsInteractable();
        bool usePressedState = canInteract && isPressed;
        bool useHighlightedState = canInteract && (isHovered || isFocused);
        bool keepActiveSidebarSize = canInteract && useSidebarSettings && isTabActive;
        float scaleMultiplier = keepActiveSidebarSize
            ? 1f
            : usePressedState
                ? GetPressScale()
                : useHighlightedState
                    ? GetHoverScale()
                    : 1f;
        float duration = usePressedState
            ? GetPressDuration()
            : GetHoverDuration();
        Ease ease = usePressedState
            ? GetPressEase()
            : GetHoverEase();

        scaleTween?.Kill();
        scaleTween = visualRoot
            .DOScale(baseScale * scaleMultiplier, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void ApplySidebarColors()
    {
        if (!useSidebarSettings || !hasBaseColors || animationProfile == null) return;

        var colors = baseColors;
        if (isTabActive)
        {
            Color activeColor = Darken(baseColors.normalColor, animationProfile.sidebarActiveBrightness);
            colors.normalColor = activeColor;
            colors.highlightedColor = activeColor;
            colors.pressedColor = activeColor;
            colors.selectedColor = activeColor;
        }

        button.colors = colors;
    }

    private static Color Darken(Color color, float brightness)
    {
        return new Color(
            color.r * brightness,
            color.g * brightness,
            color.b * brightness,
            color.a);
    }

    private float GetHoverDuration() => useSidebarSettings
        ? animationProfile.sidebarHoverDuration
        : animationProfile.hoverDuration;

    private float GetHoverScale() => useSidebarSettings
        ? animationProfile.sidebarHoverScale
        : animationProfile.hoverScale;

    private Ease GetHoverEase() => useSidebarSettings
        ? animationProfile.sidebarHoverEase
        : animationProfile.hoverEase;

    private float GetPressDuration() => useSidebarSettings
        ? animationProfile.sidebarPressDuration
        : animationProfile.pressDuration;

    private float GetPressScale() => useSidebarSettings
        ? animationProfile.sidebarPressScale
        : animationProfile.pressScale;

    private Ease GetPressEase() => useSidebarSettings
        ? animationProfile.sidebarPressEase
        : animationProfile.pressEase;

    private void CacheReferences()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (!hasBaseColors && button != null)
        {
            baseColors = button.colors;
            hasBaseColors = true;
        }

        if (buttonCanvas == null)
        {
            buttonCanvas = GetComponent<Canvas>();
            if (buttonCanvas != null)
                baseSortingOrder = buttonCanvas.sortingOrder;
        }

        if (visualRoot == null)
            visualRoot = transform as RectTransform;

        if (!hasBaseScale && visualRoot != null)
        {
            baseScale = visualRoot.localScale;
            hasBaseScale = true;
        }
    }
}
