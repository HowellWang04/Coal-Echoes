using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.HighDefinition;

public sealed class OpeningSequenceUI : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    private class OpeningContent
    {
        public string[] paragraphs;
    }

    private enum Phase { Idle, Transition, Typing, WaitingForClick }

    [SerializeField] private TextAsset narration;
    [SerializeField] private Camera openingCamera;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private RectTransform textArea;
    [SerializeField] private CanvasGroup textGroup;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private RectTransform continueArrow;
    [SerializeField] private UnityEngine.UI.Image backdrop;

    private const float NormalSpeed = 32f;
    private const float AcceleratedSpeed = 128f;
    private const float CameraDuration = 1.5f;
    private const float TextFadeDuration = 0.35f;

    private Phase phase;
    private float visibleCharacters;
    private bool accelerated;
    private bool advanceRequested;
    private Action onComplete;
    private Vector2 arrowPosition;
    private Tween cameraTween;
    private Tween textTween;
    private Tween backdropTween;
    private Tween arrowTween;

    public void Play(Action completed)
    {
        if (phase != Phase.Idle) return;

        onComplete = completed;
        phase = Phase.Transition;
        gameObject.SetActive(true);
        arrowPosition = continueArrow.anchoredPosition;
        continueArrow.gameObject.SetActive(false);
        textGroup.alpha = 0f;
        bodyText.maxVisibleCharacters = 0;
        var color = backdrop.color;
        color.a = 0f;
        backdrop.color = color;
        StartCoroutine(PlayOpening());
    }

    private IEnumerator PlayOpening()
    {
        var skyRotation = new Vector3(-85f, openingCamera.transform.eulerAngles.y, 0f);
        cameraTween = openingCamera.transform.DORotate(skyRotation, CameraDuration)
            .SetEase(Ease.InOutSine).SetUpdate(true);
        yield return cameraTween.WaitForCompletion(true);

        // Move between the separate menu and player locations while only sky is visible.
        openingCamera.transform.position = playerCamera.transform.position;
        backdropTween = backdrop.DOFade(0.45f, TextFadeDuration).SetUpdate(true);
        var content = JsonUtility.FromJson<OpeningContent>(narration.text);
        for (int paragraphIndex = 0; paragraphIndex < content.paragraphs.Length; paragraphIndex++)
        {
            PrepareParagraph(content.paragraphs[paragraphIndex]);
            textTween = textGroup.DOFade(1f, TextFadeDuration).SetUpdate(true);
            yield return textTween.WaitForCompletion(true);
            phase = Phase.Typing;
            yield return new WaitUntil(() => advanceRequested);

            phase = Phase.Transition;
            arrowTween?.Kill();
            continueArrow.gameObject.SetActive(false);
            if (paragraphIndex == content.paragraphs.Length - 1)
                backdropTween = backdrop.DOFade(0f, TextFadeDuration).SetUpdate(true);
            textTween = textGroup.DOFade(0f, TextFadeDuration).SetUpdate(true);
            yield return textTween.WaitForCompletion(true);
        }

        openingCamera.GetComponent<HDAdditionalCameraData>().volumeLayerMask =
            playerCamera.GetComponent<HDAdditionalCameraData>().volumeLayerMask;
        cameraTween = DOTween.Sequence()
            .Join(openingCamera.transform.DORotate(playerCamera.transform.eulerAngles, CameraDuration)
                .SetEase(Ease.InOutSine))
            .Join(openingCamera.DOFieldOfView(playerCamera.fieldOfView, CameraDuration)
                .SetEase(Ease.InOutSine))
            .SetUpdate(true);
        yield return cameraTween.WaitForCompletion(true);

        phase = Phase.Idle;
        var completed = onComplete;
        onComplete = null;
        completed?.Invoke();
    }

    private void PrepareParagraph(string paragraph)
    {
        phase = Phase.Transition;
        accelerated = false;
        advanceRequested = false;
        visibleCharacters = 0f;
        bodyText.text = paragraph;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        continueArrow.anchoredPosition = arrowPosition;
    }

    private void Update()
    {
        RevealText(Time.unscaledDeltaTime);
    }

    private void RevealText(float deltaTime)
    {
        if (phase != Phase.Typing) return;

        visibleCharacters += deltaTime * (accelerated ? AcceleratedSpeed : NormalSpeed);
        int total = bodyText.textInfo.characterCount;
        bodyText.maxVisibleCharacters = Mathf.Min(Mathf.FloorToInt(visibleCharacters), total);
        if (bodyText.maxVisibleCharacters < total) return;

        phase = Phase.WaitingForClick;
        continueArrow.gameObject.SetActive(true);
        arrowTween = continueArrow.DOAnchorPosY(arrowPosition.y - 7f, 0.6f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(
                textArea, eventData.position, eventData.pressEventCamera)) return;

        if (phase == Phase.Typing)
            accelerated = true;
        else if (phase == Phase.WaitingForClick)
            advanceRequested = true;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        cameraTween?.Kill();
        textTween?.Kill();
        backdropTween?.Kill();
        arrowTween?.Kill();
        phase = Phase.Idle;
        onComplete = null;
    }
}
