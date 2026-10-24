using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

[Serializable]
public class MonologueLineData
{
    public string id;
    public string text;
}

[Serializable]
public class MonologueContent
{
    public MonologueLineData[] lines;
}

// Inner monologue subtitle at the bottom of the screen. Play(id) queues a line from the JSON;
// each id plays once per save (GameProgress). The text types out without taking clicks, holds for a time
// based on its length, then fades. Lines that arrive while one is showing wait their turn.
public sealed class InnerMonologueUIManager : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private TextAsset monologueJson;

    [Header("UI Refs")]
    [SerializeField] private CanvasGroup subtitleGroup;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private RectTransform thoughtIcon;
    [SerializeField] private RectTransform band;

    [Header("Timing")]
    [SerializeField] private float charactersPerSecond = 55f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private float baseHoldDuration = 1.6f;
    [SerializeField] private float holdPerCharacter = 0.04f;

    [Header("Layout")]
    [SerializeField] private float iconGap = 14f;
    [SerializeField] private Vector2 bandPadding = new(220f, 34f);

    private readonly Dictionary<string, string> textsById = new();
    private readonly HashSet<string> playedIds = new();
    private readonly Queue<string> queue = new();
    private Coroutine playRoutine;
    private Tween fadeTween;

    // True while a line is showing or waiting; the hint popup holds its next hint until this clears.
    public bool IsBusy => playRoutine != null;

    private void Awake()
    {
        foreach (var line in JsonUtility.FromJson<MonologueContent>(monologueJson.text).lines)
            textsById[line.id] = line.text;
        playedIds.UnionWith(GameProgress.PlayedMonologues);
        GameSaveManager.SavesCleared += playedIds.Clear;

        subtitleGroup.alpha = 0f;
        subtitleGroup.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GameSaveManager.SavesCleared -= playedIds.Clear;
        fadeTween?.Kill();
    }

    // Safe to call every frame or from a UnityEvent: lines already played are ignored.
    public void Play(string id)
    {
        if (!textsById.ContainsKey(id))
        {
            Debug.LogWarning($"[InnerMonologue] No line with id {id} in {monologueJson.name}.");
            return;
        }
        if (!playedIds.Add(id)) return;
        GameProgress.MarkPlayed(id);

        queue.Enqueue(id);
        playRoutine ??= StartCoroutine(PlayQueue());
    }

    private IEnumerator PlayQueue()
    {
        while (queue.Count > 0)
            yield return ShowLine(textsById[queue.Dequeue()]);

        subtitleGroup.gameObject.SetActive(false);
        playRoutine = null;
    }

    private IEnumerator ShowLine(string text)
    {
        subtitleGroup.gameObject.SetActive(true);
        bodyText.text = text;
        bodyText.maxVisibleCharacters = int.MaxValue;
        bodyText.ForceMeshUpdate();
        PlaceIconAndBand();

        int total = bodyText.textInfo.characterCount;
        bodyText.maxVisibleCharacters = 0;
        fadeTween?.Kill();
        fadeTween = subtitleGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);

        float visible = 0f;
        while (visible < total)
        {
            visible += charactersPerSecond * Time.deltaTime;
            bodyText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(visible));
            yield return null;
        }

        yield return new WaitForSeconds(baseHoldDuration + holdPerCharacter * total);

        fadeTween?.Kill();
        fadeTween = subtitleGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad);
        yield return fadeTween.WaitForCompletion();
    }

    // The icon sits left of the first line and the band covers the whole text block; both are
    // measured on the fully laid out text so they do not move while it types out.
    private void PlaceIconAndBand()
    {
        var textInfo = bodyText.textInfo;
        var firstLine = textInfo.lineInfo[0];
        float left = textInfo.characterInfo[firstLine.firstCharacterIndex].bottomLeft.x;
        float middle = (firstLine.ascender + firstLine.descender) * 0.5f;
        float iconX = left - iconGap - thoughtIcon.rect.width * 0.5f;
        thoughtIcon.position = bodyText.rectTransform.TransformPoint(new Vector3(iconX, middle, 0f));

        var bounds = bodyText.textBounds;
        band.position = bodyText.rectTransform.TransformPoint(bounds.center);
        band.sizeDelta = new Vector2(bounds.size.x + bandPadding.x * 2f, bounds.size.y + bandPadding.y * 2f);
    }
}
