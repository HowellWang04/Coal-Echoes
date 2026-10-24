using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[Serializable]
public sealed class NewspaperPhotoPass : CustomPass
{
    [NonSerialized] public UnityEngine.UI.RawImage photo;
    [NonSerialized] public Material material;

    private RTHandle source;
    private RTHandle printed;
    private Texture previewTexture;

    protected override bool executeInSceneView => false;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        previewTexture = photo.texture;
    }

    protected override void Execute(CustomPassContext ctx)
    {
        int width = ctx.hdCamera.camera.pixelWidth;
        int height = ctx.hdCamera.camera.pixelHeight;
        if (printed == null || printed.rt.width != width || printed.rt.height != height)
        {
            source?.Release();
            printed?.Release();
            source = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Bilinear, name: "Newspaper camera source");
            printed = RTHandles.Alloc(width, height, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                filterMode: FilterMode.Bilinear, name: "Newspaper printed photo");
            photo.texture = printed.rt;
        }

        // Read the post-processed frame before overlay UI. MainMenuCamera keeps
        // rendering to the screen with its normal projection and exposure.
        CustomPassUtils.Copy(ctx, ctx.cameraColorBuffer, source);
        ctx.cmd.Blit(source.nameID, printed.nameID, material);
    }

    protected override void Cleanup()
    {
        if (photo != null) photo.texture = previewTexture;
        source?.Release();
        printed?.Release();
        source = null;
        printed = null;
    }
}
