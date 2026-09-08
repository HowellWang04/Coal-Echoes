using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "UIAnimationProfile", menuName = "UI Animation/Profile")]
public sealed class UIAnimationProfile : ScriptableObject
{
    [Header("Button")]
    [Min(0f)] public float hoverDuration = 0.12f;
    [Min(0f)] public float hoverScale = 1.04f;
    public Ease hoverEase = Ease.OutQuad;
    [Min(0f)] public float pressDuration = 0.07f;
    [Min(0f)] public float pressScale = 0.96f;
    public Ease pressEase = Ease.OutQuad;

    [Header("Sidebar Button")]
    [Min(0f)] public float sidebarHoverDuration = 0.12f;
    [Min(0f)] public float sidebarHoverScale = 1.04f;
    public Ease sidebarHoverEase = Ease.OutQuad;
    [Min(0f)] public float sidebarPressDuration = 0.07f;
    [Min(0f)] public float sidebarPressScale = 0.96f;
    public Ease sidebarPressEase = Ease.OutQuad;
    [Range(0f, 1f)] public float sidebarActiveBrightness = 0.85f;

    [Header("Panel")]
    [Min(0f)] public float panelDuration = 0.36f;
    [Range(0f, 1f)] public float panelStartAlpha = 0.88f;
    public Ease panelEase = Ease.OutQuart;

    [Header("Tab")]
    [Min(0f)] public float tabDuration = 0.18f;
    [Min(0f)] public float tabOffset = 24f;
    public Ease tabEase = Ease.OutCubic;

    [Header("Scrapbook Fly")]
    [Min(0f)] public float scrapbookFlyDuration = 0.56f;
    [Min(0f)] public float scrapbookArcHeight = 110f;
    [Min(0f)] public float scrapbookEndScale = 0.16f;
    public Ease scrapbookFlyEase = Ease.InOutCubic;
    [Min(0f)] public float scrapbookIconPunchDuration = 0.2f;
    [Min(0f)] public float scrapbookIconPunchScale = 0.08f;

    [Header("Photo Delete")]
    [Min(0f)] public float photoDeleteDuration = 0.22f;
    [Min(0f)] public float photoDeleteStagger = 0.025f;
    [Min(0f)] public float photoDeleteDrop = 18f;
    [Min(0f)] public float photoDeleteEndScale = 0.82f;
    [Min(0f)] public float photoDeleteRotation = 3f;
    public Ease photoDeleteEase = Ease.InCubic;
    [Min(0f)] public float photoReflowDuration = 0.24f;
    public Ease photoReflowEase = Ease.OutCubic;
}
