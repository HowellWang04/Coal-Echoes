using TMPro;
using UnityEngine;
using DG.Tweening;

[DefaultExecutionOrder(1100)]
public class EchoTargetingUI : MonoBehaviour
{
    [SerializeField] private CameraSystemManager cameraSystemManager;
    [SerializeField] private EchoTargetDetector detector;
    [SerializeField] private EchoViewfinderGraphic fullscreenFrame;
    [SerializeField] private EchoViewfinderGraphic handheldFrame;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform handheldScreen;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color readyColor = new Color(1f, .8f, .2f, 1f);
    [SerializeField] private float discoveryExpansion = .06f;
    [SerializeField] private float expansionDuration = .18f;
    [SerializeField] private float statusFontHeightRatio = .05f;
    [SerializeField] private float statusTopMarginRatio = .025f;

    private float discoveryProgress;
    private Sequence statusEntrance;
    private EchoTrigger shownTarget;
    private bool shownDiscovered;
    private float statusDrop;
    private float statusOpacity;
    private float statusRoll;

    private void Awake() => HideImmediate();
    private void OnDisable() => HideImmediate();

    private void LateUpdate()
    {
        if (!detector.IsTakingAim) { HideImmediate(); return; }
        bool handheld = cameraSystemManager.CurrentViewMode == CameraViewMode.Handheld;
        EchoTrigger target = detector.CurrentTarget;
        discoveryProgress = Mathf.MoveTowards(discoveryProgress, target != null ? 1f : 0f,
            Time.unscaledDeltaTime / expansionDuration);
        float expansion = 1f + discoveryExpansion * (1f - Mathf.Pow(1f - discoveryProgress, 3f));
        fullscreenFrame.SetExpansion(expansion);
        handheldFrame.SetExpansion(expansion);
        if (handheld) handheldFrame.ProjectOntoScreen(playerCamera, handheldScreen);
        fullscreenFrame.gameObject.SetActive(!handheld);
        handheldFrame.gameObject.SetActive(handheld);
        Color frameColor = target != null ? readyColor : Color.white;
        fullscreenFrame.color = frameColor;
        handheldFrame.color = frameColor;
        if (target != null)
        {
            bool discovered = EchoDiscoveryStorage.IsDiscovered(target);
            if (shownTarget != target || shownDiscovered != discovered || !statusText.gameObject.activeSelf)
                ShowStatus(target, discovered);
            UpdateStatusLayout(handheld);
            Color textColor = readyColor;
            textColor.a *= statusOpacity;
            statusText.color = textColor;
        }
        else HideStatus();
    }

    private void ShowStatus(EchoTrigger target, bool discovered)
    {
        statusEntrance?.Kill();
        shownTarget = target;
        shownDiscovered = discovered;
        statusText.text = discovered ? "Discovered Echo" : "Undiscovered Echo";
        statusText.gameObject.SetActive(true);
        statusDrop = .05f;
        statusOpacity = 0f;
        statusRoll = -1.5f;
        // Position settles with a small overshoot; the softer rotation finishes a moment later.
        statusEntrance = DOTween.Sequence()
            .Join(DOTween.To(() => statusDrop, value => statusDrop = value, 0f, .42f).SetEase(Ease.OutBack, 1.25f))
            .Join(DOTween.To(() => statusOpacity, value => statusOpacity = value, 1f, .16f).SetEase(Ease.OutQuad))
            .Join(DOTween.To(() => statusRoll, value => statusRoll = value, 0f, .48f).SetEase(Ease.OutBack, 1.4f))
            .SetUpdate(true)
            .OnComplete(() => statusEntrance = null);
    }

    private void UpdateStatusLayout(bool handheld)
    {
        RectTransform area = (RectTransform)transform;
        Vector2 top = new Vector2(area.rect.center.x, area.rect.yMax);
        Vector2 down = Vector2.down;
        float width = area.rect.width;
        float height = area.rect.height;
        float angle = 0f;
        if (handheld)
        {
            top = ProjectScreenPoint(new Vector3(0, .5f, 0), area);
            Vector2 bottom = ProjectScreenPoint(new Vector3(0, -.5f, 0), area);
            Vector2 left = ProjectScreenPoint(new Vector3(-.5f, .5f, 0), area);
            Vector2 right = ProjectScreenPoint(new Vector3(.5f, .5f, 0), area);
            down = (bottom - top).normalized;
            height = Vector2.Distance(top, bottom);
            width = Vector2.Distance(left, right);
            angle = Mathf.Atan2(right.y - left.y, right.x - left.x) * Mathf.Rad2Deg;
        }
        RectTransform label = statusText.rectTransform;
        label.anchoredPosition = top + down * height * (statusTopMarginRatio + .05f - statusDrop);
        label.sizeDelta = new Vector2(width * .9f, height * .1f);
        label.localRotation = Quaternion.Euler(0, 0, angle + statusRoll);
        statusText.fontSize = height * statusFontHeightRatio;
    }

    private Vector2 ProjectScreenPoint(Vector3 point, RectTransform area)
    {
        Vector3 pixel = playerCamera.WorldToScreenPoint(handheldScreen.TransformPoint(point));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, pixel, null, out Vector2 local);
        return local;
    }

    private void HideStatus()
    {
        statusEntrance?.Kill();
        statusEntrance = null;
        shownTarget = null;
        if (statusText != null) statusText.gameObject.SetActive(false);
    }

    public void HideImmediate()
    {
        discoveryProgress = 0f;
        if (fullscreenFrame != null) fullscreenFrame.gameObject.SetActive(false);
        if (handheldFrame != null) handheldFrame.gameObject.SetActive(false);
        HideStatus();
    }
}
