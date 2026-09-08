using System;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GrandpaDiaryPageTurnTransition : MonoBehaviour
{
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private Color shadowColor = new Color(0.55f, 0.5f, 0.42f, 1f);
    [SerializeField, Range(0f, 1f)] private float shadowIntensity = 0.65f;

    private Sequence transition;
    private GrandpaDiaryPageView currentPage;
    private GrandpaDiaryPageView targetPage;
    private UIEffect[] outgoingEffects;
    private UIEffect[] incomingEffects;

    public bool Play(
        GrandpaDiaryPageView current,
        GrandpaDiaryPageView target,
        bool forward,
        Action onMidpoint,
        Action onComplete)
    {
        if (!current.TryGetPageHalves(out RectTransform currentLeft, out RectTransform currentRight)
            || !target.TryGetPageHalves(out RectTransform targetLeft, out RectTransform targetRight))
            return false;

        Stop();

        currentPage = current;
        targetPage = target;
        currentPage.ResetPageHalfScales();
        targetPage.ResetPageHalfScales();

        RectTransform outgoingHalf = forward ? currentRight : currentLeft;
        RectTransform incomingHalf = forward ? targetLeft : targetRight;
        outgoingEffects = ConfigureShading(outgoingHalf);
        incomingEffects = ConfigureShading(incomingHalf);
        SetShading(outgoingEffects, 0f);
        SetShading(incomingEffects, 1f);
        incomingHalf.localScale = new Vector3(0f, 1f, 1f);
        currentPage.BringToFront();

        float halfDuration = duration * 0.5f;
        transition = DOTween.Sequence()
            .Append(outgoingHalf.DOScaleX(0f, halfDuration).SetEase(Ease.InQuad))
            .Join(DOTween.To(
                    () => 0f,
                    rate => SetShading(outgoingEffects, rate),
                    1f,
                    halfDuration)
                .SetEase(Ease.InQuad))
            .AppendCallback(() =>
            {
                onMidpoint?.Invoke();
                targetPage.BringToFront();
            })
            .Append(incomingHalf.DOScaleX(1f, halfDuration).SetEase(Ease.OutQuad))
            .Join(DOTween.To(
                    () => 1f,
                    rate => SetShading(incomingEffects, rate),
                    0f,
                    halfDuration)
                .SetEase(Ease.OutQuad))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                ResetPages();
                transition = null;
                onComplete?.Invoke();
            });

        return true;
    }

    private void Stop()
    {
        transition?.Kill();
        transition = null;
        ResetPages();
    }

    private void ResetPages()
    {
        currentPage?.ResetPageHalfScales();
        targetPage?.ResetPageHalfScales();
        currentPage = null;
        targetPage = null;
        SetShading(outgoingEffects, 0f);
        SetShading(incomingEffects, 0f);
        outgoingEffects = null;
        incomingEffects = null;
    }

    private UIEffect[] ConfigureShading(RectTransform pageHalf)
    {
        Graphic[] graphics = pageHalf.GetComponentsInChildren<Graphic>(true);
        var effects = new UIEffect[graphics.Length];

        for (int index = 0; index < graphics.Length; index++)
        {
            UIEffect effect = graphics[index].GetComponent<UIEffect>();
            if (effect == null)
                effect = graphics[index].gameObject.AddComponent<UIEffect>();

            effect.colorFilter = ColorFilter.Multiply;
            effect.color = shadowColor;
            effects[index] = effect;
        }

        return effects;
    }

    private void SetShading(UIEffect[] effects, float rate)
    {
        if (effects == null) return;

        float intensity = rate * shadowIntensity;
        foreach (UIEffect effect in effects)
            if (effect != null)
                effect.colorIntensity = intensity;
    }

    private void OnDisable()
    {
        Stop();
    }
}
