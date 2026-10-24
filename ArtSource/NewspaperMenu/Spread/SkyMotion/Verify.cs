// Render the real camera composition with a disposable sky override in Edit Mode.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Use Edit Mode.");
const string folder = "ArtSource/NewspaperMenu/Spread/SkyMotion";
System.IO.Directory.CreateDirectory(folder);
var installed = UnityEngine.Object.FindFirstObjectByType<OpeningSkyMotion>(UnityEngine.FindObjectsInactive.Include);
var installedSettings = new UnityEditor.SerializedObject(installed);
var source = (UnityEngine.Rendering.Volume)installedSettings.FindProperty("sourceVolume").objectReferenceValue;
string sourcePath = UnityEditor.AssetDatabase.GetAssetPath(source.sharedProfile);
byte[] sourceBytes = System.IO.File.ReadAllBytes(sourcePath);
var root = new UnityEngine.GameObject("Opening sky verification");
root.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var volume = root.AddComponent<UnityEngine.Rendering.Volume>();
volume.isGlobal = true;
volume.priority = source.priority + 1;
volume.enabled = false;
var motion = root.AddComponent<OpeningSkyMotion>();
UnityEditor.EditorUtility.CopySerialized(installed, motion);
var settings = new UnityEditor.SerializedObject(motion);
settings.FindProperty("skyVolume").objectReferenceValue = volume;
settings.ApplyModifiedPropertiesWithoutUndo();
var cameraObject = UnityEngine.Object.Instantiate(UnityEngine.GameObject.Find("Gameplay Root/MainMenuCamera"));
cameraObject.name = "Opening sky verification camera";
cameraObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var camera = cameraObject.GetComponent<UnityEngine.Camera>();
camera.enabled = false;
camera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().antialiasing =
    UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None;
var target = new UnityEngine.RenderTexture(1280, 720, 24, UnityEngine.RenderTextureFormat.ARGB32,
    UnityEngine.RenderTextureReadWrite.sRGB);
var previous = UnityEngine.RenderTexture.active;
var pictures = new System.Collections.Generic.List<UnityEngine.Color32[]>();
var angles = new System.Collections.Generic.List<float>();
UnityEngine.Color32[] Capture(string name)
{
    camera.Render();
    var image = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
    UnityEngine.RenderTexture.active = target;
    image.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0);
    image.Apply();
    var pixels = image.GetPixels32();
    System.IO.File.WriteAllBytes(folder + "/" + name + ".png", image.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(image);
    return pixels;
}
try
{
    camera.targetTexture = target;
    camera.aspect = 1280f / 720f;
    camera.Render();
    var original = Capture("original");
    motion.Prepare();
    var runtimeProfile = volume.sharedProfile;
    runtimeProfile.TryGet(out UnityEngine.Rendering.HighDefinition.HDRISky sky);
    for (int i = 0; i < 5; i++)
    {
        float time = i / 4f;
        float progress = .5f - .5f * UnityEngine.Mathf.Cos(UnityEngine.Mathf.PI * time);
        if (i > 0) motion.SetProgress(progress);
        angles.Add(i == 4 ? 0f : sky.rotation.value);
        pictures.Add(Capture("frame" + i));
    }
    int changedSkyPixels = 0;
    int maximumEndDifference = 0;
    long endDifference = 0;
    for (int i = 0; i < original.Length; i++)
    {
        var a = pictures[0][i];
        var b = pictures[2][i];
        if (i / 1280 > 500 && (a.r != b.r || a.g != b.g || a.b != b.b)) changedSkyPixels++;
        var end = pictures[4][i];
        int difference = System.Math.Max(System.Math.Abs(original[i].r - end.r),
            System.Math.Max(System.Math.Abs(original[i].g - end.g), System.Math.Abs(original[i].b - end.b)));
        maximumEndDifference = System.Math.Max(maximumEndDifference, difference);
        endDifference += difference;
    }
    bool finishedCleanly = !volume.enabled && volume.sharedProfile == null && runtimeProfile == null && sky == null;
    motion.Prepare();
    motion.SetProgress(.45f);
    runtimeProfile = volume.sharedProfile;
    motion.Restore();
    bool interruptedCleanly = !volume.enabled && volume.sharedProfile == null && runtimeProfile == null;
    var report = new
    {
        playing = UnityEditor.EditorApplication.isPlaying,
        sweepAngle = settings.FindProperty("sweepAngle").floatValue,
        angles,
        changedSkyPixels,
        maximumEndDifference,
        meanEndDifference = (double)endDifference / original.Length,
        finishedCleanly,
        interruptedCleanly,
        sourceFileUnchanged = sourceBytes.SequenceEqual(System.IO.File.ReadAllBytes(sourcePath)),
        temporalScope = "Manual GPU samples in Edit Mode. Continuous Play Mode motion and interruption remain unverified."
    };
    System.IO.File.WriteAllText(folder + "/checks.json",
        Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
    if (changedSkyPixels == 0 || !finishedCleanly || !interruptedCleanly || !report.sourceFileUnchanged)
        throw new System.Exception("Sky motion verification failed.");
    return report;
}
finally
{
    motion.Restore();
    camera.targetTexture = null;
    UnityEngine.RenderTexture.active = previous;
    UnityEngine.Object.DestroyImmediate(target);
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEngine.Object.DestroyImmediate(root);
}
