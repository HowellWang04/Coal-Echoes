using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIPanelSlideTransition : MonoBehaviour
{
    private UIAnimationProfile animationProfile;
    private RectTransform panelRectTransform;
    private RectTransform sidebarRectTransform;
    private RectTransform tabsRectTransform;
    private RectTransform layoutRoot;
    private LayoutGroup panelLayoutGroup;
    private CanvasGroup canvasGroup;
    private Sequence transition;
    private Vector2 sidebarOpenPosition;
    private Vector2 tabsOpenPosition;

    public void Initialize(UIAnimationProfile profile, RectTransform sidebarRoot, RectTransform tabsRoot)
    {
        animationProfile = profile;
        panelRectTransform = transform as RectTransform;
        sidebarRectTransform = sidebarRoot;
        tabsRectTransform = tabsRoot;
        layoutRoot = sidebarRoot.parent as RectTransform;
        panelLayoutGroup = layoutRoot.GetComponent<LayoutGroup>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        sidebarOpenPosition = sidebarRectTransform.anchoredPosition;
        tabsOpenPosition = tabsRectTransform.anchoredPosition;
    }

    public void Show()
    {
        transition?.Kill();
        panelLayoutGroup.enabled = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        sidebarOpenPosition = sidebarRectTransform.anchoredPosition;
        tabsOpenPosition = tabsRectTransform.anchoredPosition;
        panelLayoutGroup.enabled = false;

        float offset = panelRectTransform.rect.width;
        sidebarRectTransform.anchoredPosition = sidebarOpenPosition + Vector2.left * offset;
        tabsRectTransform.anchoredPosition = tabsOpenPosition + Vector2.right * offset;
        canvasGroup.alpha = animationProfile.panelStartAlpha;
        canvasGroup.blocksRaycasts = false;

        transition = DOTween.Sequence()
            .Join(sidebarRectTransform.DOAnchorPos(sidebarOpenPosition, animationProfile.panelDuration)
                .SetEase(animationProfile.panelEase))
            .Join(tabsRectTransform.DOAnchorPos(tabsOpenPosition, animationProfile.panelDuration)
                .SetEase(animationProfile.panelEase))
            .Join(canvasGroup.DOFade(1f, animationProfile.panelDuration * 0.65f)
                .SetEase(Ease.OutQuad))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                panelLayoutGroup.enabled = true;
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
                canvasGroup.blocksRaycasts = true;
            });
    }

    public void HideImmediate()
    {
        transition?.Kill();
        transition = null;
        sidebarRectTransform.anchoredPosition = sidebarOpenPosition;
        tabsRectTransform.anchoredPosition = tabsOpenPosition;
        panelLayoutGroup.enabled = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    private void OnDisable()
    {
        transition?.Kill();
        transition = null;
        panelLayoutGroup.enabled = true;
    }
}
