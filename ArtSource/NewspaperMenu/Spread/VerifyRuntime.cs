// Exercise the actual menu controller and HDRP pass on temporary objects in Edit Mode.
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Use Edit Mode for this isolated check.");
if (DG.Tweening.DOTween.TotalActiveTweens() != 0) throw new System.InvalidOperationException("Finish other tween previews before this isolated check.");
const string folder = "ArtSource/NewspaperMenu/Spread/Runtime";
System.IO.Directory.CreateDirectory(folder);
var sceneRoot = UnityEngine.GameObject.Find("MainMenuUIRoot");
var sceneCamera = UnityEngine.GameObject.Find("Gameplay Root/MainMenuCamera").GetComponent<UnityEngine.Camera>();
var preview = UnityEngine.Object.Instantiate(sceneRoot);
preview.name = "RuntimeSpreadCheck";
preview.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var controller = preview.GetComponentInChildren<NewspaperMenuUI>();
// Image checks must not animate the live MapMagic TerrainData from Edit Mode.
// Sand height/cleanup checks use their own disposable preview scenes.
var sandSettings = new UnityEditor.SerializedObject(preview.GetComponentInChildren<FactorySandAccumulation>());
sandSettings.FindProperty("depth").floatValue = 0f;
sandSettings.ApplyModifiedPropertiesWithoutUndo();
var skyMotion = preview.GetComponentInChildren<OpeningSkyMotion>();
var dormitoryAging = preview.GetComponentInChildren<DormitoryMaterialAging>();
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var type = typeof(NewspaperMenuUI);
var photo = (UnityEngine.UI.RawImage)type.GetField("photo", flags).GetValue(controller);
var canvas = preview.GetComponent<UnityEngine.Canvas>();
canvas.enabled = true;
var group = preview.transform.Find("MenuPresentation").GetComponent<UnityEngine.CanvasGroup>();
group.alpha = 1;
var worldObject = UnityEngine.Object.Instantiate(sceneCamera.gameObject);
worldObject.name = "RuntimePhotoCheckCamera";
worldObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var world = worldObject.GetComponent<UnityEngine.Camera>();
world.enabled = false;
var worldData = worldObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
worldData.antialiasing = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None;
var uiObject = UnityEngine.Object.Instantiate(worldObject);
uiObject.name = "RuntimeUICheckCamera";
uiObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var ui = uiObject.GetComponent<UnityEngine.Camera>();
ui.enabled = false;
ui.cullingMask = 1 << 5;
uiObject.transform.position = UnityEngine.Vector3.zero;
uiObject.transform.rotation = UnityEngine.Quaternion.identity;
var uiData = uiObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
uiData.customRenderingSettings = true;
uiData.renderingPathCustomFrameSettings.SetEnabled(UnityEngine.Rendering.HighDefinition.FrameSettingsField.Postprocess, false);
uiData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)UnityEngine.Rendering.HighDefinition.FrameSettingsField.Postprocess] = true;
uiData.clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
uiData.backgroundColorHDR = UnityEngine.Color.black;
canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceCamera;
canvas.worldCamera = ui;
canvas.planeDistance = 1;
var previousActive = UnityEngine.RenderTexture.active;
bool previousSRGB = UnityEngine.GL.sRGBWrite;
var reports = new System.Collections.Generic.List<object>();
UnityEngine.Color32[] Read(UnityEngine.RenderTexture target, string path)
{
    var t = new UnityEngine.Texture2D(target.width, target.height, UnityEngine.TextureFormat.RGB24, false);
    UnityEngine.RenderTexture.active = target;
    t.ReadPixels(new UnityEngine.Rect(0, 0, target.width, target.height), 0, 0);
    t.Apply();
    var pixels = t.GetPixels32();
    if (path != null) System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(t);
    return pixels;
}
try
{
    type.GetMethod("Awake", flags).Invoke(controller, null);
    foreach (var size in new[] { new UnityEngine.Vector2Int(1920, 1080), new UnityEngine.Vector2Int(1280, 960) })
    {
        var worldRT = new UnityEngine.RenderTexture(size.x, size.y, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
        var uiRT = new UnityEngine.RenderTexture(size.x, size.y, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
        var compareRT = new UnityEngine.RenderTexture(size.x, size.y, 0, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
        try
        {
            world.targetTexture = worldRT;
            ui.targetTexture = uiRT;
            world.aspect = ui.aspect = (float)size.x / size.y;
            UnityEngine.Canvas.ForceUpdateCanvases();
            controller.Show(world);
            world.Render();
            UnityEngine.Canvas.ForceUpdateCanvases();
            foreach (var text in preview.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.ForceMeshUpdate();
            ui.Render();
            Read(uiRT, folder + "/menu-" + size.x + ".png");
            bool liveTexture = photo.texture is UnityEngine.RenderTexture;
            if (!liveTexture) throw new System.Exception("Custom Pass did not produce a render texture.");
            UnityEngine.GL.sRGBWrite = true;
            UnityEngine.Graphics.Blit(photo.texture, compareRT);
            var first = Read(compareRT, folder + "/photo-" + size.x + ".png");
            var rotation = world.transform.rotation;
            world.transform.Rotate(0, 2, 0);
            world.Render();
            UnityEngine.Graphics.Blit(photo.texture, compareRT);
            var changed = Read(compareRT, null);
            int changedPixels = first.Where((p, i) => p.r != changed[i].r || p.g != changed[i].g || p.b != changed[i].b).Count();
            world.transform.rotation = rotation;

            int completed = 0;
            controller.EnterPhoto(() => completed++);
            var tween = (DG.Tweening.Tween)type.GetField("exitTween", flags).GetValue(controller);
            DG.Tweening.TweenExtensions.Pause(tween);
            var material = (UnityEngine.Material)type.GetField("material", flags).GetValue(controller);
            DG.Tweening.TweenExtensions.Goto(tween, .9f, false);
            float halfwayZoom = (float)type.GetField("zoom", flags).GetValue(controller);
            float printDuringZoom = material.GetFloat("_EffectStrength");
            DG.Tweening.TweenExtensions.Goto(tween, 1.8f, false);
            world.Render();
            UnityEngine.Canvas.ForceUpdateCanvases();
            ui.Render();
            Read(uiRT, folder + "/zoomed-" + size.x + ".png");
            var corners = new UnityEngine.Vector3[4];
            photo.rectTransform.GetWorldCorners(corners);
            var lower = ui.WorldToScreenPoint(corners[0]);
            var upper = ui.WorldToScreenPoint(corners[2]);
            float edgeError = new[] { System.Math.Abs(lower.x), System.Math.Abs(lower.y), System.Math.Abs(upper.x - size.x), System.Math.Abs(upper.y - size.y) }.Max();
            float fullPrint = material.GetFloat("_EffectStrength");
            DG.Tweening.TweenExtensions.Goto(tween, 2.5f, false);
            float halfwayReveal = material.GetFloat("_EffectStrength");
            DG.Tweening.TweenExtensions.Goto(tween, 3.2f, false);
            world.Render();
            UnityEngine.Canvas.ForceUpdateCanvases();
            ui.Render();
            var screen = Read(uiRT, folder + "/revealed-" + size.x + ".png");
            var original = Read(worldRT, folder + "/original-" + size.x + ".png");
            UnityEngine.GL.sRGBWrite = true;
            UnityEngine.Graphics.Blit(photo.texture, compareRT);
            var revealed = Read(compareRT, folder + "/bypass-" + size.x + ".png");
            int maximumDifference = 0;
            int screenInteriorDifference = 0;
            long totalDifference = 0;
            for (int i = 0; i < original.Length; i++)
            {
                int r = System.Math.Abs(original[i].r - revealed[i].r);
                int g = System.Math.Abs(original[i].g - revealed[i].g);
                int b = System.Math.Abs(original[i].b - revealed[i].b);
                maximumDifference = System.Math.Max(maximumDifference, System.Math.Max(r, System.Math.Max(g, b)));
                totalDifference += r + g + b;
                int x = i % size.x, y = i / size.x;
                if (x > 2 && x < size.x - 3 && y > 2 && y < size.y - 3)
                    screenInteriorDifference = System.Math.Max(screenInteriorDifference, new[] { System.Math.Abs(original[i].r - screen[i].r), System.Math.Abs(original[i].g - screen[i].g), System.Math.Abs(original[i].b - screen[i].b) }.Max());
            }
            controller.gameObject.SetActive(false);
            type.GetMethod("OnDisable", flags).Invoke(controller, null);
            skyMotion.Restore();
            dormitoryAging.Restore();
            var volume = controller.GetComponent<UnityEngine.Rendering.HighDefinition.CustomPassVolume>();
            bool cleaned = !volume.enabled && volume.customPasses.Count == 0 && type.GetField("material", flags).GetValue(controller) == null;
            controller.gameObject.SetActive(true);
            controller.Show(world);
            world.Render();
            int interruptedCallbacks = 0;
            controller.EnterPhoto(() => interruptedCallbacks++);
            var interrupted = (DG.Tweening.Tween)type.GetField("exitTween", flags).GetValue(controller);
            DG.Tweening.TweenExtensions.Pause(interrupted);
            DG.Tweening.TweenExtensions.Goto(interrupted, .9f, false);
            controller.gameObject.SetActive(false);
            type.GetMethod("OnDisable", flags).Invoke(controller, null);
            skyMotion.Restore();
            dormitoryAging.Restore();
            bool interruptedCleanly = interruptedCallbacks == 0
                && volume.customPasses.Count == 0 && !(photo.texture is UnityEngine.RenderTexture);
            controller.gameObject.SetActive(true);
            reports.Add(new { resolution = size.ToString(), liveTexture, changedPixels, halfwayZoom, printDuringZoom, fullPrint, halfwayReveal, completed,
                fullPhotoLower = lower.ToString(), fullPhotoUpper = upper.ToString(), edgeError, maximumDifference, screenInteriorDifference, meanDifference = (double)totalDifference / (original.Length * 3), interruptedCleanly,
                cleaned, interruptedCallbacks, cancellationLifecycle = "Not verified in Edit Mode because DOTween player lifecycle is not initialized", remainingPasses = volume.customPasses.Count,
                photoRestoredToPreview = !(photo.texture is UnityEngine.RenderTexture), playing = UnityEditor.EditorApplication.isPlaying });
            if (changedPixels == 0 || edgeError > 1f || completed != 1 || maximumDifference > 1 || screenInteriorDifference > 2 || !cleaned || !interruptedCleanly)
                throw new System.Exception("Runtime image or transition check failed " + Newtonsoft.Json.JsonConvert.SerializeObject(reports));
        }
        finally
        {
            world.targetTexture = null;
            ui.targetTexture = null;
            UnityEngine.RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(worldRT);
            UnityEngine.Object.DestroyImmediate(uiRT);
            UnityEngine.Object.DestroyImmediate(compareRT);
        }
    }
    System.IO.File.WriteAllText(folder + "/checks.json", Newtonsoft.Json.JsonConvert.SerializeObject(reports, Newtonsoft.Json.Formatting.Indented));
}
finally
{
    type.GetMethod("OnDisable", flags).Invoke(controller, null);
    skyMotion.Restore();
    dormitoryAging.Restore();
    UnityEngine.RenderTexture.active = previousActive;
    UnityEngine.GL.sRGBWrite = previousSRGB;
    UnityEngine.Object.DestroyImmediate(preview);
    UnityEngine.Object.DestroyImmediate(worldObject);
    UnityEngine.Object.DestroyImmediate(uiObject);
    // Edit Mode creates sequences without initializing DOTween's player lifecycle.
    // Clear only this check's sequences after confirming no earlier tweens existed.
    DG.Tweening.DOTween.Clear(true);
}
return reports;
