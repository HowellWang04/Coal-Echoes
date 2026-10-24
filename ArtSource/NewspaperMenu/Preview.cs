var source = UnityEngine.GameObject.Find("MainMenuUIRoot");
var sourceCamera = UnityEngine.GameObject.Find("Gameplay Root/MainMenuCamera").GetComponent<UnityEngine.Camera>();
var preview = UnityEngine.Object.Instantiate(source);
preview.name = "NewspaperMenuPreview";
var cameraObject = UnityEngine.Object.Instantiate(sourceCamera.gameObject);
cameraObject.name = "NewspaperPreviewCamera";
var camera = cameraObject.GetComponent<UnityEngine.Camera>();
cameraObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().antialiasing = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None;
var canvas = preview.GetComponent<UnityEngine.Canvas>();
canvas.enabled = true;
canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
canvas.worldCamera = camera;
canvas.planeDistance = 1f;
var reports = new System.Collections.Generic.List<object>();
try
{
    foreach (var size in new[] { new UnityEngine.Vector2Int(1920, 1080), new UnityEngine.Vector2Int(1280, 960) })
    {
        var target = new UnityEngine.RenderTexture(size.x, size.y, 24);
        var previous = UnityEngine.RenderTexture.active;
        var texture = new UnityEngine.Texture2D(size.x, size.y, UnityEngine.TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.aspect = (float)size.x / size.y;
            UnityEngine.Canvas.ForceUpdateCanvases();
            foreach (var text in preview.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.ForceMeshUpdate();
            camera.Render();
            UnityEngine.RenderTexture.active = target;
            texture.ReadPixels(new UnityEngine.Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            var path = "ArtSource/NewspaperMenu/menu-" + size.x + ".png";
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            reports.Add(new { path, overflow = preview.transform.Find("MenuPresentation/Newspaper").GetComponentsInChildren<TMPro.TMP_Text>().Where(t => t.isTextOverflowing).Select(t => t.name).ToArray() });
        }
        finally
        {
            camera.targetTexture = null;
            UnityEngine.RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
finally
{
    UnityEngine.Object.DestroyImmediate(preview);
    UnityEngine.Object.DestroyImmediate(cameraObject);
}
return reports;
