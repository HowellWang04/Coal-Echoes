using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrapBookDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private string photoFileName;
    private Texture2D photoThumb;
    private GameObject photoElementPrefab;
    private ScrapBookDecoPreset decoPreset;

    private GameObject dragGhost;
    private RectTransform canvasRect;
    private Canvas rootCanvas;
    private ScrapBookCanvas scrapBookCanvas;

    // Setup for photo elements: thumb for ghost preview, fileName for loading full res on drop
    public void SetupPhoto(string fileName, Texture2D thumb, GameObject prefab, ScrapBookCanvas canvas)
    {
        photoFileName = fileName;
        photoThumb = thumb;
        photoElementPrefab = prefab;
        decoPreset = null;
        scrapBookCanvas = canvas;
        CacheRefs();
    }

    // Setup for decoration elements (sticker or textbox)
    public void Setup(ScrapBookDecoPreset preset, ScrapBookCanvas canvas)
    {
        decoPreset = preset;
        photoFileName = null;
        photoThumb = null;
        photoElementPrefab = null;
        scrapBookCanvas = canvas;
        CacheRefs();
    }

    private void Start()
    {
        CacheRefs();
    }

    private void CacheRefs()
    {
        if (rootCanvas == null)
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null) rootCanvas = parentCanvas.rootCanvas;
        }
        if (canvasRect == null && scrapBookCanvas != null)
            canvasRect = scrapBookCanvas.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        CacheRefs();
        if (rootCanvas == null) return;

        dragGhost = new GameObject("DragGhost");
        dragGhost.transform.SetParent(rootCanvas.transform, false);

        var rt = dragGhost.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Build ghost visual
        if (photoThumb != null)
        {
            rt.sizeDelta = (transform as RectTransform).rect.size;

            GameObject photoVisual = Instantiate(photoElementPrefab, dragGhost.transform);
            var photoVisualRect = photoVisual.GetComponent<RectTransform>();
            photoVisualRect.anchorMin = new Vector2(0.5f, 0.5f);
            photoVisualRect.anchorMax = new Vector2(0.5f, 0.5f);
            photoVisualRect.pivot = new Vector2(0.5f, 0.5f);
            photoVisualRect.anchoredPosition = Vector2.zero;
            ScrapBookPhotoVisual.Configure(photoVisual, photoThumb, rt.sizeDelta);
        }
        else if (decoPreset != null && decoPreset.icon != null)
        {
            rt.sizeDelta = new Vector2(80, 80);
            var img = dragGhost.AddComponent<Image>();
            img.sprite = decoPreset.icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        var cg = dragGhost.AddComponent<CanvasGroup>();
        cg.alpha = 0.7f;
        cg.blocksRaycasts = false;

        UpdateGhostPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost);
            dragGhost = null;
        }

        if (scrapBookCanvas == null || canvasRect == null) return;

        // Check if drop position is inside ScrapBookCanvas
        if (!RectTransformUtility.RectangleContainsScreenPoint(canvasRect, eventData.position, eventData.pressEventCamera))
            return;

        // Convert screen position to local position inside ScrapBookCanvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

        // Place element on canvas: load full resolution photo
        if (!string.IsNullOrEmpty(photoFileName))
        {
            Texture2D fullTex = CameraPhotoStorage.LoadTexture(photoFileName);
            if (fullTex != null)
                scrapBookCanvas.AddPhotoElement(fullTex, localPos, photoFileName);
            return;
        }
        if (decoPreset != null)
            scrapBookCanvas.AddDecoElement(decoPreset, localPos);
    }

    private void UpdateGhostPosition(PointerEventData eventData)
    {
        if (dragGhost == null || rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 pos);

        dragGhost.GetComponent<RectTransform>().anchoredPosition = pos;
    }
}
