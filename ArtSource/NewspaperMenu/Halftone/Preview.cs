// Run with Unity CLI eval_file in Edit Mode. No saved scene objects are changed.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Stop Play Mode before capturing this preview.");

var sourceCamera = UnityEngine.GameObject.Find("Gameplay Root/MainMenuCamera").GetComponent<UnityEngine.Camera>();
var shaderPath = "Assets/Scripts/MainMenu/NewspaperHalftone.shader";
UnityEditor.AssetDatabase.ImportAsset(shaderPath);
var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Shader>(shaderPath);
if (UnityEditor.ShaderUtil.ShaderHasError(shader))
    throw new System.InvalidOperationException("Halftone shader import failed.");

var materialPath = "Assets/Art Resources/UI/NewspaperMenu/Halftone/NewspaperHalftone.mat";
var savedMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
if (savedMaterial == null)
{
    savedMaterial = new UnityEngine.Material(shader);
    savedMaterial.SetTexture("_PaperTex", UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Art Resources/UI/NewspaperMenu/Newsprint.png"));
    UnityEditor.AssetDatabase.CreateAsset(savedMaterial, materialPath);
    UnityEditor.AssetDatabase.SaveAssetIfDirty(savedMaterial);
}
var material = new UnityEngine.Material(savedMaterial);
var cameraObject = UnityEngine.Object.Instantiate(sourceCamera.gameObject);
cameraObject.name = "HalftonePreviewCamera";
cameraObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var camera = cameraObject.GetComponent<UnityEngine.Camera>();
camera.enabled = false;
cameraObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().antialiasing = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None;
var previousActive = UnityEngine.RenderTexture.active;
var previousSRGB = UnityEngine.GL.sRGBWrite;
var source = new UnityEngine.RenderTexture(1920, 1080, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var output = new UnityEngine.RenderTexture(1920, 1080, 0, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var capture = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
var folder = "ArtSource/NewspaperMenu/Halftone/";
var reports = new System.Collections.Generic.List<object>();
System.Action<UnityEngine.RenderTexture, string> save = (target, name) =>
{
    UnityEngine.RenderTexture.active = target;
    capture.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0);
    capture.Apply();
    System.IO.File.WriteAllBytes(folder + name + ".png", capture.EncodeToPNG());
};
try
{
    camera.targetTexture = source;
    camera.aspect = 1920f / 1080f;
    camera.Render();
    save(source, "neutral-camera-original");
    var original = capture.GetPixels32();
    UnityEngine.GL.sRGBWrite = true;

    material.SetFloat("_EffectStrength", 0);
    UnityEngine.Graphics.Blit(source, output, material);
    save(output, "neutral-effect-zero");
    var bypass = capture.GetPixels32();
    int maxBypassDifference = 0;
    for (int i = 0; i < original.Length; i++)
    {
        maxBypassDifference = System.Math.Max(maxBypassDifference, System.Math.Abs(original[i].r - bypass[i].r));
        maxBypassDifference = System.Math.Max(maxBypassDifference, System.Math.Abs(original[i].g - bypass[i].g));
        maxBypassDifference = System.Math.Max(maxBypassDifference, System.Math.Abs(original[i].b - bypass[i].b));
    }
    material.SetFloat("_EffectStrength", 1);
    UnityEngine.Graphics.Blit(source, output, material);
    save(output, "neutral-newsprint");

    reports.Add(new { sourceCamera = sourceCamera.name, position = sourceCamera.transform.position.ToString(), rotation = sourceCamera.transform.eulerAngles.ToString(), resolution = "1920x1080", circleDensity = savedMaterial.GetFloat("_CircleDensity"), dotStrength = savedMaterial.GetFloat("_DotStrength"), inkWear = savedMaterial.GetFloat("_InkWear"), maxBypassDifference, shaderErrors = UnityEditor.ShaderUtil.GetShaderMessages(shader).Select(m => m.message).ToArray(), playing = UnityEditor.EditorApplication.isPlaying });
    var report = Newtonsoft.Json.JsonConvert.SerializeObject(reports, Newtonsoft.Json.Formatting.Indented);
    System.IO.File.WriteAllText(folder + "neutral-checks.json", report);
}
finally
{
    camera.targetTexture = null;
    UnityEngine.RenderTexture.active = previousActive;
    UnityEngine.GL.sRGBWrite = previousSRGB;
    UnityEngine.Object.DestroyImmediate(capture);
    UnityEngine.Object.DestroyImmediate(source);
    UnityEngine.Object.DestroyImmediate(output);
    UnityEngine.Object.DestroyImmediate(material);
    UnityEngine.Object.DestroyImmediate(cameraObject);
}
return reports;
