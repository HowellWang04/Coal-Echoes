using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public sealed class NewspaperMenuUI : MonoBehaviour
{
    [Serializable]
    public sealed class Article
    {
        public string heading;
        public string subheading;
        public string[] columns;
    }

    [Serializable]
    public sealed class Notice
    {
        public string heading;
        public string body;
    }

    [Serializable]
    public sealed class Content
    {
        public string masthead;
        public string englishTitle;
        public string issueDate;
        public Article[] sections;
        public Notice[] notices;
        public string photoHeading;
        public string photoCaption;
        public string footer;
    }

    [Header("Layout")]
    [SerializeField] private RectTransform paper;
    [SerializeField] private UnityEngine.UI.RawImage photo;
    [SerializeField] private Material printMaterial;
    [SerializeField] private CustomPassVolume captureVolume;
    [SerializeField] private FactorySandAccumulation sandAccumulation;
    [SerializeField] private OpeningSkyMotion skyMotion;
    [SerializeField] private DormitoryMaterialAging dormitoryAging;
    [SerializeField] private float zoomDelay = 0.8f;
    [SerializeField] private float zoomDuration = 1.8f;
    [SerializeField] private float revealDuration = 1.4f;

    [Header("Year Title")]
    [Tooltip("Placed where the year comes to rest; it lifts off the issue date during the transition.")]
    [SerializeField] private TMP_Text yearTitle;
    [SerializeField] private int issueYear = 1980;
    [SerializeField] private int presentYear = 2026;
    [SerializeField] private float yearFadeDuration = 0.6f;

    [Header("Copy")]
    [SerializeField] private TextAsset copy;
    [SerializeField] private TMP_Text masthead;
    [SerializeField] private TMP_Text englishTitle;
    [SerializeField] private TMP_Text issueDate;
    [SerializeField] private TMP_Text[] headings;
    [SerializeField] private TMP_Text[] subheadings;
    [SerializeField] private TMP_Text[] bodies;
    [SerializeField] private TMP_Text[] noticeHeadings;
    [SerializeField] private TMP_Text[] noticeBodies;
    [SerializeField] private TMP_Text photoHeading;
    [SerializeField] private TMP_Text photoCaption;
    [SerializeField] private TMP_Text footer;
    [SerializeField] private TMP_FontAsset datelineFont;

    private const string DatelineText = "本报讯";

    private RectTransform viewport;
    private Vector2 photoSlotPosition;
    private Vector2 photoSlotSize;
    private Vector2 viewportSize;
    private Material material;
    private NewspaperPhotoPass photoPass;
    private float zoom;
    private Tween exitTween;
    private Vector2 yearRestPosition;
    private Vector2 yearStartPosition;
    private float yearStartScale;
    private string issueDateText;

    private void Awake()
    {
        viewport = (RectTransform)transform;
        photoSlotPosition = photo.rectTransform.anchoredPosition;
        photoSlotSize = photo.rectTransform.sizeDelta;
        // Anchored positions hold before the canvas scaler has sized the viewport in Awake.
        yearRestPosition = yearTitle.rectTransform.anchoredPosition;
        ApplyCopy();
    }

    public void ApplyCopy()
    {
        var content = JsonUtility.FromJson<Content>(copy.text);
        // Period papers set the "本报讯" lead-in in a different face from the body; the font must sit under Resources.
        string datelineMarkup = $"<font=\"{datelineFont.name}\">{DatelineText}</font>";
        masthead.text = content.masthead;
        englishTitle.text = content.englishTitle;
        issueDate.text = content.issueDate;
        int column = 0;
        for (int i = 0; i < content.sections.Length; i++)
        {
            headings[i].text = content.sections[i].heading;
            if (i < subheadings.Length) subheadings[i].text = content.sections[i].subheading;
            foreach (string body in content.sections[i].columns)
                bodies[column++].text = body.Replace(DatelineText, datelineMarkup);
        }
        for (int i = 0; i < content.notices.Length; i++)
        {
            noticeHeadings[i].text = content.notices[i].heading;
            noticeBodies[i].text = content.notices[i].body;
        }
        photoHeading.text = content.photoHeading;
        photoCaption.text = content.photoCaption;
        footer.text = content.footer;
    }

    public void Show(Camera menuCamera)
    {
        sandAccumulation.Prepare();
        skyMotion.Prepare();
        dormitoryAging.Prepare();
        material = new Material(printMaterial);
        material.SetFloat("_EffectStrength", 1f);
        zoom = 0f;
        Canvas.ForceUpdateCanvases();
        UpdateLayout();
        photoPass = new NewspaperPhotoPass { name = "Newspaper camera photo", photo = photo, material = material };
        captureVolume.targetCamera = menuCamera;
        captureVolume.customPasses.Add(photoPass);
        captureVolume.enabled = true;
    }

    private void LateUpdate()
    {
        if (material != null && viewport.rect.size != viewportSize) UpdateLayout();
    }

    private void UpdateLayout()
    {
        viewportSize = viewport.rect.size;
        float aspect = viewportSize.x / viewportSize.y;
        var photoSize = new Vector2(Mathf.Min(photoSlotSize.x, photoSlotSize.y * aspect),
            Mathf.Min(photoSlotSize.y, photoSlotSize.x / aspect));
        var photoPosition = photoSlotPosition + new Vector2(
            (photoSlotSize.x - photoSize.x) * .5f, -(photoSlotSize.y - photoSize.y) * .5f);
        photo.rectTransform.sizeDelta = photoSize;
        photo.rectTransform.anchoredPosition = photoPosition;

        var paperSize = paper.rect.size;
        var center = new Vector2(photoPosition.x + photoSize.x * .5f - paperSize.x * .5f,
            paperSize.y * .5f + photoPosition.y - photoSize.y * .5f);
        float fitScale = Mathf.Min(viewportSize.x / paperSize.x, viewportSize.y / paperSize.y);
        float fullScale = viewportSize.x / photoSize.x;
        float scale = Mathf.Lerp(fitScale, fullScale, zoom);
        paper.localScale = Vector3.one * scale;
        paper.anchoredPosition = Vector2.Lerp(Vector2.zero, -center * fullScale, zoom);
        // Halftone dots fade by their size on screen, so they surface as the photo fills the view.
        material.SetFloat("_DisplayScale", photoSize.x * scale / viewportSize.x);
        material.SetVector("_PrintRect", new Vector4(photoPosition.x / paperSize.x,
            (paperSize.y + photoPosition.y - photoSize.y) / paperSize.y,
            photoSize.x / paperSize.x, photoSize.y / paperSize.y));
    }

    public void EnterPhoto(Action completed)
    {
        // The paper holds still for a moment before zooming, while the time-lapse in the photo starts at once.
        // The year lifts off at once and lands on the present as the ink finishes fading, then fades itself.
        float timeLapse = zoomDelay + zoomDuration + revealDuration;
        LiftYear();
        exitTween = DOTween.Sequence()
            .AppendInterval(zoomDelay)
            .Append(DOTween.To(() => zoom, value => { zoom = value; UpdateLayout(); }, 1f, zoomDuration)
                .SetEase(Ease.InOutCubic))
            .Append(DOTween.To(() => material.GetFloat("_EffectStrength"),
                value => material.SetFloat("_EffectStrength", value), 0f, revealDuration)
                .SetEase(Ease.InOutSine))
            .Insert(0f, DOTween.To(() => sandAccumulation.Progress, sandAccumulation.SetProgress,
                1f, timeLapse).SetEase(Ease.InOutSine))
            .Insert(0f, DOTween.To(() => skyMotion.Progress, skyMotion.SetProgress,
                1f, timeLapse).SetEase(Ease.InOutSine))
            .Insert(0f, DOTween.To(() => dormitoryAging.Progress, dormitoryAging.SetProgress,
                1f, timeLapse).SetEase(Ease.InOutSine))
            .Insert(0f, DOTween.To(() => 0f, SetYearProgress, 1f, timeLapse).SetEase(Ease.InOutSine))
            .Append(DOTween.To(() => yearTitle.alpha, value => yearTitle.alpha = value,
                0f, yearFadeDuration))
            .SetUpdate(true)
            .OnComplete(() => completed());
    }

    private void LiftYear()
    {
        // Start the title exactly over the year printed in the issue date, then hide that year on the paper.
        string year = ChineseYear(issueYear) + "年";
        issueDateText = issueDate.text;
        issueDate.ForceMeshUpdate();
        var first = issueDate.textInfo.characterInfo[0];
        Vector2 start = viewport.InverseTransformPoint(
            issueDate.transform.TransformPoint(new Vector3(first.origin, first.ascender)));
        var rect = viewport.rect;
        yearStartPosition = start - (rect.min + rect.size * yearTitle.rectTransform.anchorMin);
        yearStartScale = issueDate.fontSize * paper.localScale.x / yearTitle.fontSize;
        issueDate.text = $"<alpha=#00>{year}<alpha=#FF>{issueDateText.Substring(year.Length)}";
        yearTitle.alpha = 1f;
        yearTitle.gameObject.SetActive(true);
        SetYearProgress(0f);
    }

    private void SetYearProgress(float progress)
    {
        yearTitle.text = ChineseYear(Mathf.RoundToInt(Mathf.Lerp(issueYear, presentYear, progress))) + "年";
        yearTitle.rectTransform.anchoredPosition = Vector2.Lerp(yearStartPosition, yearRestPosition, progress);
        yearTitle.transform.localScale = Vector3.one * Mathf.Lerp(yearStartScale, 1f, progress);
    }

    private static string ChineseYear(int year)
    {
        const string digits = "〇一二三四五六七八九";
        var text = year.ToString().ToCharArray();
        for (int i = 0; i < text.Length; i++) text[i] = digits[text[i] - '0'];
        return new string(text);
    }

    private void OnDisable()
    {
        exitTween?.Kill();
        exitTween = null;
        if (issueDateText != null)
        {
            issueDate.text = issueDateText;
            issueDateText = null;
            yearTitle.gameObject.SetActive(false);
            yearTitle.rectTransform.anchoredPosition = yearRestPosition;
            yearTitle.transform.localScale = Vector3.one;
        }
        if (photoPass == null) return;

        // Disabling the volume cleans up the pass before releasing its material.
        captureVolume.enabled = false;
        captureVolume.customPasses.Clear();
        zoom = 0f;
        paper.localScale = Vector3.one;
        paper.anchoredPosition = Vector2.zero;
        photo.rectTransform.anchoredPosition = photoSlotPosition;
        photo.rectTransform.sizeDelta = photoSlotSize;
        CoreUtils.Destroy(material);
        material = null;
        photoPass = null;
    }
}
