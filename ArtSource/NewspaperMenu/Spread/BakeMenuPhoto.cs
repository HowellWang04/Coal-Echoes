// Re-bake the photo the newspaper shows outside Play Mode. Run through Unity CLI eval_file
// or the MCP execute_code tool. It matches the menu at the start of Play Mode: MainMenuCamera,
// sunk dunes, rewound sky, clean dormitory, open gate without the motorcycle, and the runtime
// print material. The scene is restored.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Stop Play Mode before baking the menu photo.");

const string path = "Assets/Art Resources/UI/NewspaperMenu/SpreadPreview/CameraPhoto.png";
const int width = 1920, height = 1080;
var newspaper = UnityEngine.Object.FindAnyObjectByType<NewspaperMenuUI>(UnityEngine.FindObjectsInactive.Include);
var fields = new UnityEditor.SerializedObject(newspaper);
T Field<T>(string name) where T : UnityEngine.Object => (T)fields.FindProperty(name).objectReferenceValue;
var photo = Field<UnityEngine.UI.RawImage>("photo").rectTransform;
var paper = Field<UnityEngine.RectTransform>("paper").rect.size;
var sand = Field<FactorySandAccumulation>("sandAccumulation");
var sky = Field<OpeningSkyMotion>("skyMotion");
var dormitory = Field<DormitoryMaterialAging>("dormitoryAging");
var sceneState = UnityEngine.Object.FindAnyObjectByType<OpeningSceneState>(UnityEngine.FindObjectsInactive.Include);

var sourceCamera = UnityEngine.GameObject.Find("Gameplay Root/MainMenuCamera").GetComponent<UnityEngine.Camera>();
var cameraObject = UnityEngine.Object.Instantiate(sourceCamera.gameObject);
cameraObject.name = "Menu Photo Bake Camera";
cameraObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
var camera = cameraObject.GetComponent<UnityEngine.Camera>();
camera.enabled = false;
// Without temporal history a single frame would ghost, so the bake skips TAA.
cameraObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().antialiasing =
    UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None;

var material = new UnityEngine.Material(Field<UnityEngine.Material>("printMaterial"));
var sourceRT = new UnityEngine.RenderTexture(width, height, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var printRT = new UnityEngine.RenderTexture(width, height, 0, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var capture = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
var previousActive = UnityEngine.RenderTexture.active;
bool previousSRGB = UnityEngine.GL.sRGBWrite;
try
{
    sand.Prepare();
    sky.Prepare();
    dormitory.Prepare();
    sceneState.Prepare();
    camera.targetTexture = sourceRT;
    camera.aspect = (float)width / height;
    // A few frames let exposure and volumetric fog settle.
    for (int i = 0; i < 4; i++) camera.Render();

    // Same values NewspaperMenuUI.UpdateLayout sets at 16:9 before the zoom. The photo slot is
    // already 16:9, so it keeps its edit-time rect, and the paper fills the 1920 x 1080 canvas.
    var position = photo.anchoredPosition;
    var size = photo.sizeDelta;
    material.SetFloat("_EffectStrength", 1);
    material.SetFloat("_DisplayScale", size.x / paper.x);
    material.SetVector("_PrintRect", new UnityEngine.Vector4(position.x / paper.x,
        (paper.y + position.y - size.y) / paper.y, size.x / paper.x, size.y / paper.y));
    UnityEngine.GL.sRGBWrite = true;
    UnityEngine.Graphics.Blit(sourceRT, printRT, material);

    UnityEngine.RenderTexture.active = printRT;
    capture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
    capture.Apply();
    System.IO.File.WriteAllBytes(path, capture.EncodeToPNG());
    UnityEditor.AssetDatabase.ImportAsset(path);
}
finally
{
    UnityEngine.GL.sRGBWrite = previousSRGB;
    UnityEngine.RenderTexture.active = previousActive;
    sceneState.Restore();
    dormitory.Restore();
    sky.Restore();
    sand.Restore();
    camera.targetTexture = null;
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEngine.Object.DestroyImmediate(material);
    UnityEngine.Object.DestroyImmediate(capture);
    sourceRT.Release();
    printRT.Release();
    UnityEngine.Object.DestroyImmediate(sourceRT);
    UnityEngine.Object.DestroyImmediate(printRT);
}
return path;
