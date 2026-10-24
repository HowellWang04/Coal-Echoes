using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScrapBookUIManager : MonoBehaviour
{
    [Header("Category Buttons")]
    [SerializeField] private Button photoCategoryBtn;
    [SerializeField] private Button decoCategoryBtn;

    [Header("Element Scroll Content， which is also the topbar in the scrap book UI")]
    [SerializeField] private Transform elementContent;

    [Header("Photo Element Size")]
    [SerializeField] private Vector2 photoElementSize = new Vector2(142, 80);
    [SerializeField] private GameObject photoElementPrefab;

    [Header("ScrapBook Canvas")]
    [SerializeField] private ScrapBookCanvas scrapBookCanvas;

    [Header("Page Turning")]
    [SerializeField] private Button prevPageBtn;
    [SerializeField] private Button nextPageBtn;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private int pageCount = 10;

    [Header("Decoration Presets")]
    [SerializeField] private ScrapBookDecoPreset[] decoPresets;

    [Header("Thumbnail Settings")]
    [SerializeField] private int thumbMaxSize = 512; // covers the 142-unit top bar photo at 4K (canvas scale 2)

    private enum Category { Photo, Decoration }
    private Category currentCategory = Category.Photo;

    private readonly List<GameObject> spawnedElements = new();
    private readonly Dictionary<string, Texture2D> thumbCache = new();

    private bool canvasRestored = false;

    private void Awake()
    {
        GameSaveManager.SavesCleared += HandleSavesCleared;
    }

    private void OnDestroy()
    {
        GameSaveManager.SavesCleared -= HandleSavesCleared;
    }

    // Drops a restored layout so the next open reads the now empty save.
    private void HandleSavesCleared()
    {
        if (canvasRestored && scrapBookCanvas != null) scrapBookCanvas.ClearElements();
        canvasRestored = false;
        foreach (var texture in thumbCache.Values) Destroy(texture);
        thumbCache.Clear();
    }

    private void OnEnable()
    {
        if (scrapBookCanvas != null)
            scrapBookCanvas.SetPhotoElementPrefab(photoElementPrefab);

        if (photoCategoryBtn != null) photoCategoryBtn.onClick.AddListener(() => SwitchCategory(Category.Photo));
        if (decoCategoryBtn != null) decoCategoryBtn.onClick.AddListener(() => SwitchCategory(Category.Decoration));
        if (prevPageBtn != null) prevPageBtn.onClick.AddListener(() => TurnPage(-1));
        if (nextPageBtn != null) nextPageBtn.onClick.AddListener(() => TurnPage(1));

        if (ScrapBookDataManager.Instance != null)
        {
            ScrapBookDataManager.Instance.OnPhotosChanged += RefreshCurrentCategory;
            ScrapBookDataManager.Instance.OnDecorationsChanged += RefreshCurrentCategory;
        }

        RefreshCurrentCategory();

        // Restore saved canvas layout once per session (elements persist while the panel is just deactivated).
        if (!canvasRestored)
        {
            RestoreCanvas();
            canvasRestored = true;
            if (scrapBookCanvas != null) scrapBookCanvas.ShowPage(0);
        }
        RefreshPageUI();
    }

    private void OnDisable()
    {
        if (photoCategoryBtn != null) photoCategoryBtn.onClick.RemoveAllListeners();
        if (decoCategoryBtn != null) decoCategoryBtn.onClick.RemoveAllListeners();
        if (prevPageBtn != null) prevPageBtn.onClick.RemoveAllListeners();
        if (nextPageBtn != null) nextPageBtn.onClick.RemoveAllListeners();

        if (ScrapBookDataManager.Instance != null)
        {
            ScrapBookDataManager.Instance.OnPhotosChanged -= RefreshCurrentCategory;
            ScrapBookDataManager.Instance.OnDecorationsChanged -= RefreshCurrentCategory;

            if (scrapBookCanvas != null)
                ScrapBookDataManager.Instance.SaveCanvas(scrapBookCanvas.GetAllElementData());
        }
    }

    private void RestoreCanvas()
    {
        if (scrapBookCanvas == null || ScrapBookDataManager.Instance == null) return;

        foreach (var data in ScrapBookDataManager.Instance.LoadCanvas())
        {
            ScrapBookDecoPreset preset = data.type == "photo" ? null : FindPreset(data.id);
            scrapBookCanvas.RebuildElement(data, preset);
        }
    }

    private void TurnPage(int delta)
    {
        if (scrapBookCanvas == null) return;

        scrapBookCanvas.ShowPage(Mathf.Clamp(scrapBookCanvas.CurrentPage + delta, 0, pageCount - 1));
        RefreshPageUI();
    }

    private void RefreshPageUI()
    {
        if (scrapBookCanvas == null) return;

        int page = scrapBookCanvas.CurrentPage;
        if (pageText != null) pageText.text = $"{page + 1} / {pageCount}";
        if (prevPageBtn != null) prevPageBtn.interactable = page > 0;
        if (nextPageBtn != null) nextPageBtn.interactable = page < pageCount - 1;
    }

    private ScrapBookDecoPreset FindPreset(string presetName)
    {
        if (decoPresets == null) return null;
        foreach (var preset in decoPresets)
            if (preset != null && preset.presetName == presetName) return preset;
        return null;
    }

    private void SwitchCategory(Category cat)
    {
        if (currentCategory == cat) return;
        currentCategory = cat;
        RefreshCurrentCategory();
    }

    private void RefreshCurrentCategory()
    {
        ClearSpawned();

        if (currentCategory == Category.Photo)
            PopulatePhotos();
        else
            PopulateDecorations();
    }

    private void PopulatePhotos()
    {
        if (ScrapBookDataManager.Instance == null) return;

        var photos = ScrapBookDataManager.Instance.Photos;
        for (int i = 0; i < photos.Count; i++)
        {
            string fileName = photos[i];
            Texture2D thumb = GetOrCreateThumbnail(fileName);
            if (thumb == null) continue;

            if (photoElementPrefab == null) return;

            GameObject go = Instantiate(photoElementPrefab, elementContent);
            go.name = fileName;

            Vector2 elementSize = GetAlbumPhotoElementSize();
            ScrapBookPhotoVisual.Configure(go, thumb, elementSize);

            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = elementSize.x;
            le.preferredHeight = elementSize.y;

            if (EchoPhotoStorage.Load(fileName).Count > 0)
                go.GetComponent<EchoPhotoMark>().Show();

            var draggable = go.AddComponent<ScrapBookDraggable>();
            draggable.SetupPhoto(fileName, thumb, scrapBookCanvas);

            spawnedElements.Add(go);
        }
    }

    private Vector2 GetAlbumPhotoElementSize()
    {
        const float aspect = 16f / 9f;
        float width = Mathf.Max(photoElementSize.x, photoElementSize.y * aspect);
        return new Vector2(width, width / aspect);
    }

    private void PopulateDecorations()
    {
        if (decoPresets == null) return;

        foreach (var preset in decoPresets)
        {
            Debug.Log($"[ScrapBook] preset: {preset?.presetName}, icon: {preset?.icon}, type: {preset?.decoType}");
            if (preset == null || preset.icon == null) continue;

            GameObject go = new GameObject(preset.presetName);
            go.transform.SetParent(elementContent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 80);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 80;

            var image = go.AddComponent<Image>();
            image.sprite = preset.icon;
            image.preserveAspect = true;

            var draggable = go.AddComponent<ScrapBookDraggable>();
            draggable.Setup(preset, scrapBookCanvas);

            spawnedElements.Add(go);
        }
    }

    private void ClearSpawned()
    {
        foreach (var go in spawnedElements) Destroy(go);
        spawnedElements.Clear();
    }

    private Texture2D GetOrCreateThumbnail(string fileName)
    {
        if (thumbCache.TryGetValue(fileName, out var cached))
            return cached;

        Texture2D tex = CameraPhotoStorage.LoadTexture(fileName);
        if (tex == null) return null;

        Texture2D thumb = MakeThumbnail(tex, thumbMaxSize);
        Object.Destroy(tex);
        thumbCache[fileName] = thumb;
        return thumb;
    }

    private static Texture2D MakeThumbnail(Texture2D src, int maxSize)
    {
        int w = src.width, h = src.height;
        float scale = Mathf.Min(maxSize / (float)w, maxSize / (float)h, 1f);
        int tw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
        int th = Mathf.Max(1, Mathf.RoundToInt(h * scale));

        RenderTexture prev = RenderTexture.active;

        // Halve in steps: each bilinear pass then averages 2x2 pixels. One big jump skips most pixels and looks jagged.
        Texture current = src;
        RenderTexture step = null;
        int cw = w, ch = h;
        while (cw / 2 >= tw && ch / 2 >= th)
        {
            cw /= 2;
            ch /= 2;
            RenderTexture next = RenderTexture.GetTemporary(cw, ch, 0);
            next.filterMode = FilterMode.Bilinear;
            Graphics.Blit(current, next);
            if (step != null) RenderTexture.ReleaseTemporary(step);
            step = next;
            current = next;
        }

        RenderTexture rt = RenderTexture.GetTemporary(tw, th, 0);
        Graphics.Blit(current, rt);
        if (step != null) RenderTexture.ReleaseTemporary(step);

        RenderTexture.active = rt;

        Texture2D dst = new Texture2D(tw, th, TextureFormat.RGB24, false);
        dst.ReadPixels(new Rect(0, 0, tw, th), 0, 0, false);
        dst.Apply(false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return dst;
    }
}
