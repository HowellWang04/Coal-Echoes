// GPU samples and material restoration checks without starting Play Mode.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Use Edit Mode.");
const string folder = "ArtSource/WorkersDormitory/CleanAging";
var installed = UnityEngine.Object.FindFirstObjectByType<DormitoryMaterialAging>(UnityEngine.FindObjectsInactive.Include);
var settings = new UnityEditor.SerializedObject(installed);
var dorm = (Transform)settings.FindProperty("dormitory").objectReferenceValue;
var originals = dorm.GetComponentsInChildren<Renderer>(true).ToDictionary(r => r, r => r.sharedMaterials);
var root = new GameObject("Dormitory aging verification") { hideFlags = HideFlags.HideAndDontSave };
var aging = root.AddComponent<DormitoryMaterialAging>();
UnityEditor.EditorUtility.CopySerialized(installed, aging);
var cameraObject = UnityEngine.Object.Instantiate(GameObject.Find("Gameplay Root/MainMenuCamera"));
cameraObject.name = "Dormitory aging verification camera";
cameraObject.hideFlags = HideFlags.HideAndDontSave;
var camera = cameraObject.GetComponent<Camera>();
camera.enabled = false;
camera.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().antialiasing =
    UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None;
var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
var previous = RenderTexture.active;
var pictures = new System.Collections.Generic.Dictionary<string, Color32[]>();
void Capture(string name)
{
    camera.Render();
    camera.Render();
    var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
    RenderTexture.active = target;
    image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
    image.Apply();
    pictures[name] = image.GetPixels32();
    System.IO.File.WriteAllBytes(folder + "/" + name + ".png", image.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(image);
}
double Difference(string a, string b)
{
    long sum = 0;
    for (int i = 0; i < pictures[a].Length; i++)
    {
        var x = pictures[a][i]; var y = pictures[b][i];
        sum += System.Math.Abs(x.r-y.r) + System.Math.Abs(x.g-y.g) + System.Math.Abs(x.b-y.b);
    }
    return (double)sum / (pictures[a].Length * 3);
}
bool Restored() => originals.All(p => p.Key.sharedMaterials.SequenceEqual(p.Value));
try
{
    camera.targetTexture = target;
    camera.aspect = 1600f / 900f;
    camera.fieldOfView = 42f;
    camera.transform.position = dorm.TransformPoint(new Vector3(13, 12, 40));
    camera.transform.LookAt(dorm.position + Vector3.up * 5f);
    Capture("aged");
    aging.Prepare();
    var temporary = dorm.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials)
        .Where(m => (m.hideFlags & HideFlags.DontSave) != 0).Distinct().ToArray();
    int changedRenderers = originals.Count(p => !p.Key.sharedMaterials.SequenceEqual(p.Value));
    Capture("clean");
    aging.SetProgress(.5f);
    Capture("half");
    aging.SetProgress(.9999f);
    Capture("nearEnd");
    aging.SetProgress(1f);
    Capture("restored");
    bool finished = Restored() && temporary.All(m => m == null);
    aging.Prepare();
    aging.SetProgress(.4f);
    // Non-ExecuteAlways behaviours do not receive the Play lifecycle here.
    typeof(DormitoryMaterialAging).GetMethod("OnDisable",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(aging, null);
    bool interrupted = Restored();
    aging.Prepare();
    aging.Prepare();
    aging.Restore();
    bool repeated = Restored();
    camera.CopyFrom(GameObject.Find("Gameplay Root/MainMenuCamera").GetComponent<Camera>());
    camera.transform.SetPositionAndRotation(GameObject.Find("Gameplay Root/MainMenuCamera").transform.position,
        GameObject.Find("Gameplay Root/MainMenuCamera").transform.rotation);
    camera.enabled = false;
    camera.targetTexture = target;
    camera.aspect = 1600f / 900f;
    Capture("menuAged");
    aging.Prepare();
    Capture("menuClean");
    aging.Restore();
    var report = new { changedRenderers, temporaryMaterials = temporary.Length, finished, interrupted, repeated,
        cleanDifference = Difference("aged", "clean"), middleDifference = Difference("clean", "half"),
        endpointDifference = Difference("aged", "restored"), nearEndDifference = Difference("nearEnd", "restored"),
        playing = UnityEditor.EditorApplication.isPlaying,
        interruptionScope = "OnDisable invoked explicitly in Edit Mode. Actual Play lifecycle remains user verification." };
    System.IO.File.WriteAllText(folder + "/checks.json",
        Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
    if (!finished || !interrupted || !repeated || report.cleanDifference < .1 || report.middleDifference < .1)
        throw new System.Exception("Dormitory aging verification failed.");
    return report;
}
finally
{
    aging.Restore();
    camera.targetTexture = null;
    RenderTexture.active = previous;
    UnityEngine.Object.DestroyImmediate(target);
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEngine.Object.DestroyImmediate(root);
}
