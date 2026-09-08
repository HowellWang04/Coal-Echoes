using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class CameraPhotoPreviewUIManager : MonoBehaviour
{
    private enum PreviewMode { None, Capture, Thumbnail }

    [Header("Preview UI")]
    [SerializeField] private Image photoDisplayArea;
    [SerializeField] private GameObject photoFrame;
    [SerializeField] private Button photoClickArea; 

    [Header("Flash UI")]
    [SerializeField] private GameObject cameraFlash;
    [SerializeField] private float flashDuration = 0.8f;

    [Header("Animation")]
    [SerializeField] private Animator photoFadingAnimation;
    [SerializeField] private string fadeAnimStateName = "PhotoFadeAnimation";

    private PreviewMode currentMode = PreviewMode.None;

    private void Awake()
    {
        HidePreviewImmediate();
        if (photoClickArea != null)
            photoClickArea.onClick.AddListener(OnPhotoClicked);
    }

    public void ShowCapturedPreview(Texture2D texture)
    {
        currentMode = PreviewMode.Capture;
        ApplyTexture(texture);
        SetPreviewVisible(true);

        if (cameraFlash != null) StartCoroutine(Flash());
        if (photoFadingAnimation != null && !string.IsNullOrEmpty(fadeAnimStateName))
            photoFadingAnimation.Play(fadeAnimStateName);
    }

    public void ShowPreviewFromThumbnail(Texture2D texture)
    {
        currentMode = PreviewMode.Thumbnail;
        ApplyTexture(texture);
        SetPreviewVisible(true);

    }

    public void ClosePreview()
    {
        if (currentMode == PreviewMode.None) return;
        HidePreviewImmediate();
    }
    private void OnPhotoClicked()
    {
        if (currentMode == PreviewMode.Thumbnail)
            ClosePreview();
    }
    public void HidePreviewImmediate()
    {
        currentMode = PreviewMode.None;
        SetPreviewVisible(false);
    }

    private void ApplyTexture(Texture2D texture)
    {
        if (photoDisplayArea == null || texture == null) return;

        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        photoDisplayArea.sprite = sprite;
    }

    private void SetPreviewVisible(bool on)
    {
        gameObject.SetActive(on);
        if (photoFrame != null) photoFrame.SetActive(on);
    }

    private IEnumerator Flash()
    {
        cameraFlash.SetActive(true);
        yield return new WaitForSeconds(flashDuration);
        cameraFlash.SetActive(false);
    }
}
