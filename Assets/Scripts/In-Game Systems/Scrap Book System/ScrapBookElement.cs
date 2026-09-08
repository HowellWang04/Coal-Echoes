using UnityEngine;
using UnityEngine.EventSystems;

public class ScrapBookElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerClickHandler, IScrollHandler
{
    [Header("Constraints")]
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float scaleStep = 0.1f;
    [SerializeField] private float rotateSensitivity = 0.5f;

    private RectTransform rect;
    private RectTransform canvasRect;
    private Canvas rootCanvas;
    private bool isRightDragging;
    private float currentScale = 1f;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
    }

    public void Init(RectTransform parentCanvasRect)
    {
        canvasRect = parentCanvasRect;
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
            // Rotate
            float angle = -eventData.delta.x * rotateSensitivity;
            rect.Rotate(0, 0, angle);
        }
    }

    // --- Resize (scroll wheel) ---

    public void OnScroll(PointerEventData eventData)
    {
        float scroll = eventData.scrollDelta.y;
        currentScale += scroll > 0 ? scaleStep : -scaleStep;
        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
        rect.localScale = Vector3.one * currentScale;
        ClampToCanvas();
    }

    // --- Bring to front on click ---

    public void OnPointerClick(PointerEventData eventData)
    {
        rect.SetAsLastSibling();
    }

    // --- Helpers ---

    private void ClampToCanvas()
    {
        if (canvasRect == null) return;

        Vector2 pos = rect.anchoredPosition;
        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 halfElement = rect.rect.size * currentScale * 0.5f;

        float maxX = halfCanvas.x - halfElement.x;
        float maxY = halfCanvas.y - halfElement.y;

        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
        pos.y = Mathf.Clamp(pos.y, -maxY, maxY);

        rect.anchoredPosition = pos;
    }

    private float GetCanvasScale()
    {
        return rootCanvas != null ? rootCanvas.scaleFactor : 1f;
    }
}
