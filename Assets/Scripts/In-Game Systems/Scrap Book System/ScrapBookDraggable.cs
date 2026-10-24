using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrapBookDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private string photoFileName;
    private Texture2D photoThumb;
    private ScrapBookDecoPreset decoPreset;

    private GameObject dragGhost;
    private RectTransform ghostContent; // the element as it would be placed, checked against the page
    private CanvasGroup ghostGroup;
    private bool ghostFits;
    private RectTransform canvasRect;
    private Canvas rootCanvas;
    private ScrapBookCanvas scrapBookCanvas;

    // A photo was dropped onto the ScrapBook page by the player.
    public static event Action OnPhotoPlaced;

    // Setup for photo elements: thumb for ghost preview, fileName for loading full res on drop
    public void SetupPhoto(string fileName, Texture2D thumb, ScrapBookCanvas canvas)
    {
        photoFileName = fileName;
        photoThumb = thumb;
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
        if (rootCanvas == null || canvasRect == null) return;

        dragGhost = new GameObject("DragGhost", typeof(RectTransform));
        dragGhost.transform.SetParent(rootCanvas.transform, false);

        var rt = (RectTransform)dragGhost.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Build the ghost exactly as the element will be placed (an Echo photo with its note), at its placed on-screen size.
        if (photoThumb != null)
        {
            ghostContent = (RectTransform)scrapBookCanvas.CreatePhotoVisual(photoThumb, photoFileName, dragGhost.transform).transform;

            // Placed photos start at the top bar's on-screen width (see OnEndDrag)
            float topBarWidth = ((RectTransform)transform).rect.width * transform.lossyScale.x;
            ghostContent.localScale = Vector3.one * (topBarWidth / (ghostContent.rect.width * rt.lossyScale.x));
        }
        else if (decoPreset != null && decoPreset.icon != null)
        {
            ghostContent = (RectTransform)new GameObject("Deco", typeof(RectTransform)).transform;
            ghostContent.SetParent(dragGhost.transform, false);
            ghostContent.sizeDelta = decoPreset.defaultSize;

            var img = ghostContent.gameObject.AddComponent<Image>();
            img.sprite = decoPreset.icon;
            if (decoPreset.decoType == DecoType.TextBox) img.type = Image.Type.Sliced;
            else img.preserveAspect = true;
            img.raycastTarget = false;

            // Decorations are placed at scale 1 on the page
            ghostContent.localScale = Vector3.one * (canvasRect.lossyScale.x / rt.lossyScale.x);
        }

        ghostGroup = dragGhost.AddComponent<CanvasGroup>();
        ghostGroup.blocksRaycasts = false;

        UpdateGhost(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateGhost(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhost == null) return;

        UpdateGhost(eventData);
        bool fits = ghostFits;
        Destroy(dragGhost);
        dragGhost = null;
        ghostContent = null;

        // Any part outside the page (an Echo photo's note included): nothing is placed, the element stays in the top bar.
        if (!fits) return;

        // Convert screen position to local position inside ScrapBookCanvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

        // Place element on canvas: load full resolution photo
        if (!string.IsNullOrEmpty(photoFileName))
        {
            Texture2D fullTex = CameraPhotoStorage.LoadTexture(photoFileName);
            if (fullTex != null)
            {
                GameObject placed = scrapBookCanvas.AddPhotoElement(fullTex, localPos, photoFileName);

                // Start at the same on-screen size it had in the top bar (saved as the element's scale)
                float topBarWidth = ((RectTransform)transform).rect.width * transform.lossyScale.x;
                float placedWidth = ((RectTransform)placed.transform).rect.width * placed.transform.lossyScale.x;
                placed.GetComponent<ScrapBookElement>().SetScale(topBarWidth / placedWidth);

                OnPhotoPlaced?.Invoke();
            }
            return;
        }
        if (decoPreset != null)
            scrapBookCanvas.AddDecoElement(decoPreset, localPos);
    }

    // Follow the pointer, and fade the ghost further while it would not fit on the page.
    private void UpdateGhost(PointerEventData eventData)
    {
        if (dragGhost == null || rootCanvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 pos);

        dragGhost.GetComponent<RectTransform>().anchoredPosition = pos;

        ghostFits = ghostContent != null && ScrapBookElement.FitsInside(canvasRect, ghostContent);
        ghostGroup.alpha = ghostFits ? 0.7f : 0.35f;
    }
}
