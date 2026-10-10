using System;
using System.Collections;
using UnityEngine;

public sealed class CameraPhotoCapture : MonoBehaviour
{
    [Header("Capture Options")]
    [SerializeField] private bool reuseBuffer = true;
    [SerializeField] private int captureWidth = 1920;
    [SerializeField] private int captureHeight = 1080;

    private Texture2D buffer;

    private void Start()
    {
        // 在Start阶段就创建CaptureBuffer，后续拍照流程中如果尺寸不变则复用这个Buffer，避免首次拍照时此流程造成的卡顿
        buffer = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        var rt = RenderTexture.GetTemporary(captureWidth, captureHeight, 0, RenderTextureFormat.Default);
        Graphics.Blit(buffer, rt);
        RenderTexture.ReleaseTemporary(rt);
    }

    public IEnumerator Capture(Action<Texture2D> onCaptured)
    {
        yield return new WaitForEndOfFrame();

        int w = Screen.width;
        int h = Screen.height;

        if (!reuseBuffer || buffer == null || buffer.width != w || buffer.height != h)
            buffer = new Texture2D(w, h, TextureFormat.RGB24, false);

        var region = new Rect(0, 0, w, h);
        buffer.ReadPixels(region, 0, 0, false);
        buffer.Apply(false);

        var rt = RenderTexture.GetTemporary(captureWidth, captureHeight, 0, RenderTextureFormat.Default);
        Graphics.Blit(buffer, rt);

        var copy = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        copy.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0, false);
        copy.Apply(false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        onCaptured?.Invoke(copy);
    }
}
