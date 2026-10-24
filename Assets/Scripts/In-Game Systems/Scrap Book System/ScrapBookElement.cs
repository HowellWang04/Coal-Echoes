using UnityEngine;
using UnityEngine.EventSystems;

public class ScrapBookElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerClickHandler, IScrollHandler
{
    [Header("Constraints")]
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float scaleStep = 0.1f;

    private RectTransform rect;
    private RectTransform canvasRect;
    private ScrapBookElementContextMenu contextMenu;
    private Canvas rootCanvas;
    private bool isRightDragging;
    private float currentScale = 1f;
    private float lastPointerAngle;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
    }

    public void Init(RectTransform parentCanvasRect, ScrapBookElementContextMenu elementContextMenu)
    {
        canvasRect = parentCanvasRect;
        contextMenu = elementContextMenu;
    }

    // Set scale directly (used when restoring a saved element), keeping currentScale in sync.
    public void SetScale(float scale)
    {
        currentScale = Mathf.Clamp(scale, minScale, maxScale);
        rect.localScale = Vector3.one * currentScale;
    }

    // --- Move (left drag) & Rotate (right drag) ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        isRightDragging = eventData.button == PointerEventData.InputButton.Right;
        if (isRightDragging) lastPointerAngle = GetPointerAngle(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Move
            rect.anchoredPosition += eventData.delta / GetCanvasScale();
            ClampToCanvas();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Rotate by how far the pointer turned around the element's center; skip a step that no longer fits the page
            float pointerAngle = GetPointerAngle(eventData);
            float step = Mathf.DeltaAngle(lastPointerAngle, pointerAngle);
            rect.Rotate(0, 0, step);
            if (!FitsCanvasSize()) rect.Rotate(0, 0, -step);
            lastPointerAngle = pointerAngle;
            ClampToCanvas();
        }
    }

    // --- Resize (scroll wheel) ---

    public void OnScroll(PointerEventData eventData)
    {
        float previousScale = currentScale;
        float scroll = eventData.scrollDelta.y;
        currentScale += scroll > 0 ? scaleStep : -scaleStep;
        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
        rect.localScale = Vector3.one * currentScale;
        if (!FitsCanvasSize()) SetScale(previousScale);
        ClampToCanvas();
    }

    // --- Bring to front on click, right-click (not right-drag) opens the delete menu ---

    public void OnPointerClick(PointerEventData eventData)
    {
        rect.SetAsLastSibling();

        if (eventData.button == PointerEventData.InputButton.Right && !eventData.dragging && contextMenu != null)
            contextMenu.Show(gameObject, eventData.position);
    }

    // --- Helpers ---

    // True when the whole element, children included (an Echo photo's note and stamp), lies inside the page.
    // Also used by the top bar's drag ghost to decide whether a drop is accepted.
    public static bool FitsInside(RectTransform page, RectTransform element)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(page, element);
        Rect area = page.rect;
        return bounds.min.x >= area.xMin && bounds.max.x <= area.xMax
            && bounds.min.y >= area.yMin && bounds.max.y <= area.yMax;
    }

    // Push the element (children included, rotated and scaled) back inside the page.
    private void ClampToCanvas()
    {
        if (canvasRect == null) return;

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, rect);
        Rect area = canvasRect.rect;
        Vector2 shift = Vector2.zero;

        if (bounds.min.x < area.xMin) shift.x = area.xMin - bounds.min.x;
        else if (bounds.max.x > area.xMax) shift.x = area.xMax - bounds.max.x;
        if (bounds.min.y < area.yMin) shift.y = area.yMin - bounds.min.y;
        else if (bounds.max.y > area.yMax) shift.y = area.yMax - bounds.max.y;

        rect.anchoredPosition += shift;
    }

    // Whether the element is still small enough to fit the page somewhere, whatever its position.
    private bool FitsCanvasSize()
    {
        if (canvasRect == null) return true;

        Vector3 size = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, rect).size;
        return size.x <= canvasRect.rect.width && size.y <= canvasRect.rect.height;
    }

    // Screen-space angle (degrees, counter-clockwise) from the element's center to the pointer.
    private float GetPointerAngle(PointerEventData eventData)
    {
        Vector2 center = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, rect.position);
        Vector2 dir = eventData.position - center;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private float GetCanvasScale()
    {
        return rootCanvas != null ? rootCanvas.scaleFactor : 1f;
    }
}
