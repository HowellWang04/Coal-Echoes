using UnityEngine;
using UnityEngine.UI;

public static class ScrapBookPhotoVisual
{
    private static readonly Vector2 albumPhotoPrefabSize = new Vector2(240f, 135f);
    private static readonly Vector2 albumPhotoImageSizeDelta = new Vector2(-24f, -14f);

    public static void Configure(GameObject photoRoot, Texture texture, Vector2 size)
    {
        photoRoot.GetComponent<RectTransform>().sizeDelta = size;

        var photoThumbItem = photoRoot.GetComponent<PhotoThumbItem>();
        if (photoThumbItem != null) photoThumbItem.enabled = false;

        var button = photoRoot.GetComponent<Button>();
        if (button != null) button.enabled = false;

        var selectToggle = photoRoot.GetComponentInChildren<Toggle>(true);
        if (selectToggle != null) selectToggle.gameObject.SetActive(false);

        var rawImage = photoRoot.GetComponentInChildren<RawImage>(true);
        rawImage.texture = texture;
        rawImage.rectTransform.sizeDelta = new Vector2(
            albumPhotoImageSizeDelta.x * size.x / albumPhotoPrefabSize.x,
            albumPhotoImageSizeDelta.y * size.y / albumPhotoPrefabSize.y);
    }
}
