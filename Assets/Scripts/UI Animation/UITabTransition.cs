using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UITabTransition : MonoBehaviour
{
    private UIAnimationProfile animationProfile;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Sequence transition;
    private Vector2 basePosition;

    public void Initialize(UIAnimationProfile profile)
    {
        animationProfile = profile;
        rectTransform = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        basePosition = rectTransform.anchoredPosition;
    }

    public void Show()
    {
        bool wasActive = gameObject.activeSelf;
        gameObject.SetActive(true);
        transition?.Kill();

        if (!wasActive)
        {
            rectTransform.anchoredPosition = basePosition + Vector2.right * animationProfile.tabOffset;
            canvasGroup.alpha = 0f;
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = false;

        transition = DOTween.Sequence()
            .Join(canvasGroup.DOFade(1f, animationProfile.tabDuration).SetEase(animationProfile.tabEase))
            .Join(rectTransform.DOAnchorPos(basePosition, animationProfile.tabDuration).SetEase(animationProfile.tabEase))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;

        transition?.Kill();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = false;

        transition = DOTween.Sequence()
            .Join(canvasGroup.DOFade(0f, animationProfile.tabDuration).SetEase(animationProfile.tabEase))
            .Join(rectTransform.DOAnchorPos(basePosition + Vector2.left * animationProfile.tabOffset, animationProfile.tabDuration).SetEase(animationProfile.tabEase))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                rectTransform.anchoredPosition = basePosition;
                gameObject.SetActive(false);
            });
    }

    public void HideImmediate()
    {
        transition?.Kill();
        transition = null;
        rectTransform.anchoredPosition = basePosition;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void ShowImmediate()
    {
        transition?.Kill();
        transition = null;
        gameObject.SetActive(true);
        rectTransform.anchoredPosition = basePosition;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void OnDisable()
    {
        transition?.Kill();
        transition = null;
    }
}
