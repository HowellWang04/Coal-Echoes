using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ScrapbookFlyFeedback : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Color frameColor = new Color32(216, 195, 151, 255);

    private UIAnimationProfile animationProfile;
    private RectTransform layerRect;
    private RectTransform flyingPhotoRect;
    private RawImage flyingPhoto;
    private CanvasGroup flyingPhotoCanvasGroup;
    private Tween flyTween;
    private Tween targetPunchTween;
    private Vector3 targetBaseScale;

    private void Awake()
    {
        animationProfile = GetComponentInParent<UIAnimationScope>().AnimationProfile;
        layerRect = transform as RectTransform;
        targetBaseScale = target.localScale;
        CreateFlyingPhoto();
    }

    public void Play(Texture texture, RectTransform source)
    {
        flyTween?.Kill();
        targetPunchTween?.Kill();
        target.localScale = targetBaseScale;

        flyingPhoto.texture = texture;
        flyingPhoto.color = Color.white;
        flyingPhotoCanvasGroup.alpha = 1f;
        MatchSourceRect(source);
        flyingPhotoRect.gameObject.SetActive(true);

        Vector2 start = flyingPhotoRect.anchoredPosition;
        Vector2 end = layerRect.InverseTransformPoint(target.TransformPoint(target.rect.center));
        float duration = animationProfile.scrapbookFlyDuration;
        float launchDuration = duration * 0.18f;
        float flightDuration = duration * 0.62f;
        float snapDuration = duration - launchDuration - flightDuration;
        Vector2 lift = start + Vector2.up * animationProfile.scrapbookArcHeight * 0.2f;
        Vector2 approachDirection = (lift - end).normalized;
        Vector2 approach = end + approachDirection * animationProfile.scrapbookArcHeight * 0.65f;
        Vector2 control = (lift + approach) * 0.5f + Vector2.up * animationProfile.scrapbookArcHeight;
        float tilt = end.x < start.x ? 2.5f : -2.5f;
        float approachScale = Mathf.Max(0.4f, animationProfile.scrapbookEndScale * 2.4f);
        Vector3 finalScale = new Vector3(
            animationProfile.scrapbookEndScale * 0.55f,
            animationProfile.scrapbookEndScale * 1.15f,
            1f);
        float progress = 0f;

        flyTween = DOTween.Sequence()
            .Append(flyingPhotoRect.DOAnchorPos(lift, launchDuration).SetEase(Ease.OutQuad))
            .Join(flyingPhotoRect.DOScale(1.04f, launchDuration).SetEase(Ease.OutQuad))
            .Join(flyingPhotoRect.DORotate(new Vector3(0f, 0f, tilt * -0.4f), launchDuration)
                .SetEase(Ease.OutQuad))
            .Append(DOTween.To(
                    () => progress,
                    value =>
                    {
                        progress = value;
                        flyingPhotoRect.anchoredPosition = QuadraticBezier(lift, control, approach, value);
                    },
                    1f,
                    flightDuration)
                .SetEase(animationProfile.scrapbookFlyEase))
            .Join(flyingPhotoRect.DOScale(approachScale, flightDuration).SetEase(Ease.InQuad))
            .Join(flyingPhotoRect.DORotate(new Vector3(0f, 0f, tilt), flightDuration)
                .SetEase(Ease.InOutSine))
            .Append(flyingPhotoRect.DOAnchorPos(end, snapDuration).SetEase(Ease.InCubic))
            .Join(flyingPhotoRect.DOScale(finalScale, snapDuration).SetEase(Ease.InCubic))
            .Join(flyingPhotoRect.DORotate(Vector3.zero, snapDuration).SetEase(Ease.InCubic))
            .Join(flyingPhotoCanvasGroup.DOFade(0f, snapDuration * 0.8f)
                .SetDelay(snapDuration * 0.2f))
            .SetUpdate(true)
            .OnComplete(() =>
            {
                flyingPhotoRect.gameObject.SetActive(false);
                targetPunchTween = target
                    .DOPunchScale(
                        Vector3.one * animationProfile.scrapbookIconPunchScale,
                        animationProfile.scrapbookIconPunchDuration,
                        3,
                        0.5f)
                    .SetUpdate(true);
            });
    }

    private void CreateFlyingPhoto()
    {
        var photoObject = new GameObject(
            "FlyingPhoto",
            typeof(RectTransform),
            typeof(CanvasGroup));
        flyingPhotoRect = photoObject.GetComponent<RectTransform>();
        flyingPhotoRect.SetParent(layerRect, false);
        flyingPhotoRect.anchorMin = new Vector2(0.5f, 0.5f);
        flyingPhotoRect.anchorMax = new Vector2(0.5f, 0.5f);
        flyingPhotoRect.pivot = new Vector2(0.5f, 0.5f);
        flyingPhotoCanvasGroup = photoObject.GetComponent<CanvasGroup>();
        CreateFlyingPhotoImage();
        CreateFlyingFrame();
        photoObject.SetActive(false);
    }

    private void CreateFlyingPhotoImage()
    {
        var imageObject = new GameObject(
            "FlyingPhotoImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        var imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.SetParent(flyingPhotoRect, false);
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(-24f, -14f);

        flyingPhoto = imageObject.GetComponent<RawImage>();
        flyingPhoto.raycastTarget = false;
    }

    private void CreateFlyingFrame()
    {
        var frameObject = new GameObject(
            "FlyingFrame",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        var frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.SetParent(flyingPhotoRect, false);
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        var frameImage = frameObject.GetComponent<Image>();
        frameImage.sprite = frameSprite;
        frameImage.color = frameColor;
        frameImage.raycastTarget = false;
    }

    private void MatchSourceRect(RectTransform source)
    {
        var corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector2 bottomLeft = layerRect.InverseTransformPoint(corners[0]);
        Vector2 topRight = layerRect.InverseTransformPoint(corners[2]);
        flyingPhotoRect.anchoredPosition = (bottomLeft + topRight) * 0.5f;
        flyingPhotoRect.sizeDelta = new Vector2(
            Mathf.Abs(topRight.x - bottomLeft.x),
            Mathf.Abs(topRight.y - bottomLeft.y));
        flyingPhotoRect.localScale = Vector3.one;
        flyingPhotoRect.localRotation = Quaternion.identity;
    }

    private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float time)
    {
        float inverse = 1f - time;
        return inverse * inverse * start
            + 2f * inverse * time * control
            + time * time * end;
    }

    private void OnDisable()
    {
        flyTween?.Kill();
        targetPunchTween?.Kill();
        if (flyingPhoto != null)
            flyingPhotoRect.gameObject.SetActive(false);
        if (target != null)
            target.localScale = targetBaseScale;
    }
}
