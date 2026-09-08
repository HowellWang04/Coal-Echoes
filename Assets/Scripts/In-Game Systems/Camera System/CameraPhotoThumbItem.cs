using System;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class PhotoThumbItem : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private RawImage thumbImage;
    [SerializeField] private Button button;
    [SerializeField] private Toggle selectToggle;

    private Texture2D sourceTexture;
    private string fileName;
    private Action onClick;
    private AlbumPhotoContextMenu contextMenu;
    private Sequence deleteTween;
    private Tween reflowTween;
    private UIEffectBase[] deleteEffects;

    public string FileName => fileName;

    private void Awake()
    {
        deleteEffects = GetComponentsInChildren<UIEffectBase>(true);
        SetDeleteEffectRate(0f);
    }

    private void OnDestroy()
    {
        deleteTween?.Kill();
        reflowTween?.Kill();
        if (button != null) button.onClick.RemoveAllListeners();
        if (selectToggle != null) selectToggle.onValueChanged.RemoveAllListeners();
    }

    public void Bind(Texture2D tex, Action onClick = null, string fileName = null, AlbumPhotoContextMenu contextMenu = null)
    {
        sourceTexture = tex;
        this.onClick = onClick;
        this.fileName = fileName;
        this.contextMenu = contextMenu;
        if (thumbImage != null) thumbImage.texture = tex;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick());
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (contextMenu != null && !string.IsNullOrEmpty(fileName))
                contextMenu.Show(fileName, sourceTexture, transform as RectTransform, eventData.position);
        }
    }

    public void SetDeleteMode(bool on, Action<bool> onToggleChanged = null)
    {
        if (selectToggle != null)
        {
            selectToggle.gameObject.SetActive(on);
            selectToggle.isOn = false;
            selectToggle.onValueChanged.RemoveAllListeners();
            if (on && onToggleChanged != null)
                selectToggle.onValueChanged.AddListener(isOn => onToggleChanged(isOn));
        }
        if (button != null) button.interactable = !on;
    }

    public void SetSelected(bool selected)
    {
        if (selectToggle != null) selectToggle.isOn = selected;
    }

    public void PlayDeleteAnimation(UIAnimationProfile profile, float delay, Action onComplete)
    {
        var buttonFeedback = GetComponent<UIButtonTweenFeedback>();
        if (buttonFeedback != null)
            buttonFeedback.enabled = false;

        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        SetDeleteEffectRate(0f);

        deleteTween?.Kill();
        deleteTween = DOTween.Sequence()
            .SetDelay(delay)
            .Append(DOTween.To(
                    () => 0f,
                    SetDeleteEffectRate,
                    1f,
                    profile.photoDeleteDuration)
                .SetEase(profile.photoDeleteEase))
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void SetDeleteEffectRate(float rate)
    {
        if (deleteEffects == null) return;

        foreach (var effect in deleteEffects)
            if (effect != null)
                effect.SetRate(rate, UIEffectTweener.CullingMask.Transition);
    }

    public void PlayReflowAnimation(
        UIAnimationProfile profile,
        Vector3 oldWorldPosition,
        Action onComplete)
    {
        var rectTransform = transform as RectTransform;
        Vector3 targetWorldPosition = rectTransform.position;
        rectTransform.position = oldWorldPosition;

        reflowTween?.Kill();
        reflowTween = rectTransform
            .DOMove(targetWorldPosition, profile.photoReflowDuration)
            .SetEase(profile.photoReflowEase)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void PlayReflowEnterAnimation(UIAnimationProfile profile, Action onComplete)
    {
        var rectTransform = transform as RectTransform;
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        float targetY = rectTransform.anchoredPosition.y;
        rectTransform.anchoredPosition += Vector2.down * profile.photoDeleteDrop * 0.75f;
        canvasGroup.alpha = 0f;

        reflowTween?.Kill();
        reflowTween = DOTween.Sequence()
            .Join(rectTransform.DOAnchorPosY(targetY, profile.photoReflowDuration)
                .SetEase(profile.photoReflowEase))
            .Join(canvasGroup.DOFade(1f, profile.photoReflowDuration)
                .SetEase(profile.photoReflowEase))
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

}
