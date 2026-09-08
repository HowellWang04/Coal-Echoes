using UnityEngine;

[ExecuteAlways]
public class GrandpaDiaryPageReferenceLayout : MonoBehaviour
{
    private static readonly Vector2 ReferenceSize = new Vector2(1224f, 756f);

    private void OnEnable()
    {
        ApplyReferenceLayout();
    }

    private void OnValidate()
    {
        ApplyReferenceLayout();
    }

    private void ApplyReferenceLayout()
    {
        var pageRect = transform as RectTransform;
        if (pageRect == null)
            return;

        pageRect.anchorMin = new Vector2(0.5f, 0.5f);
        pageRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageRect.pivot = new Vector2(0.5f, 0.5f);
        pageRect.anchoredPosition = Vector2.zero;
        pageRect.sizeDelta = ReferenceSize;
    }
}
