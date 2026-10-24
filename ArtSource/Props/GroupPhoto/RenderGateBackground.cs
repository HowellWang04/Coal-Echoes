// Render the factory gate as it looked on 3 October 1980, as the background of the Group Photo.
// Run through the MCP execute_code tool outside Play Mode. Edit the settings block first.
// 1980 state: sunk dunes, clean dormitory, gate open without the chain and repainted (the
// textures from Scripts/make_clean_gate_textures.py), fresh guardhouse plaster (the clean wall
// colour from ArtSource/Guardhouse/PolyHaven/Scripts/wall_eras.py), no motorcycle, no dead
// grass (the three grass groups and terrain detail), no telecom antennas, the player hidden,
// and a temporary red banner strung between the two piers. Everything is restored afterwards.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Stop Play Mode before rendering the Group Photo background.");

// ---- settings ----
string projectDir = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
string groupPhotoDir = System.IO.Path.Combine(projectDir, "ArtSource/Props/GroupPhoto");
string bannerTexture = System.IO.Path.Combine(groupPhotoDir, "Banner/Banner_brush.png");
string cleanGateDir = System.IO.Path.Combine(groupPhotoDir, "CleanGate");
string outputDir = System.IO.Path.Combine(groupPhotoDir, "Background");
int width = 3840, height = 2160;      // downscaled to 1920 x 1080 later; no TAA, so this is the antialiasing
// name, metres out from the gate along its outward normal, yaw to the right (toward the dormitory),
// pitch up, vertical field of view. Eye height matches the player's camera. The in-game retake
// spot is calibrated by hand for the game camera's wider view, so re-rendering leaves it alone.
var shots = new (string name, float distance, float yaw, float pitch, float fov)[]
{
    ("gate_d16_y5_f42", 16f, 5f, 2f, 42f),
};
const float eyeHeight = 1.68f;
const float bannerWidth = 9.6f, bannerHeight = .8f, bannerTop = 3.95f, bannerOut = .55f, bannerSag = .09f;
// -------------------

var leftPier = UnityEngine.GameObject.Find("In-Scene Objects/工厂大门/FactoryGate/Left_Pier");
var rightPier = UnityEngine.GameObject.Find("In-Scene Objects/工厂大门/FactoryGate/Right_Pier");
UnityEngine.Bounds Bounds(UnityEngine.GameObject go)
{
    var rs = go.GetComponentsInChildren<UnityEngine.Renderer>();
    var b = rs[0].bounds;
    foreach (var r in rs) b.Encapsulate(r.bounds);
    return b;
}
var leftBounds = Bounds(leftPier);
var rightBounds = Bounds(rightPier);
float groundY = leftPier.transform.position.y;
// Left_Pier stands on the guardhouse side, which is screen right seen from outside.
var across = leftBounds.center - rightBounds.center; across.y = 0;
float pierGap = across.magnitude;
var screenRight = across / pierGap;
var outward = UnityEngine.Vector3.Cross(screenRight, UnityEngine.Vector3.up).normalized;
var gateCenter = (leftBounds.center + rightBounds.center) * .5f; gateCenter.y = groundY;
var player = UnityEngine.GameObject.Find("Player");
if (UnityEngine.Vector3.Dot(player.transform.position - gateCenter, outward) < 0) outward = -outward;

var newspaper = UnityEngine.Object.FindAnyObjectByType<NewspaperMenuUI>(UnityEngine.FindObjectsInactive.Include);
var fields = new UnityEditor.SerializedObject(newspaper);
var sand = (FactorySandAccumulation)fields.FindProperty("sandAccumulation").objectReferenceValue;
var dormitory = (DormitoryMaterialAging)fields.FindProperty("dormitoryAging").objectReferenceValue;
var sceneState = UnityEngine.Object.FindAnyObjectByType<OpeningSceneState>(UnityEngine.FindObjectsInactive.Include);
var hidden = new System.Collections.Generic.List<UnityEngine.GameObject>();
foreach (var path in new[] { "In-Scene Objects/工厂大门/门口干草", "In-Scene Objects/工厂大门/门口补草",
             "In-Scene Objects/工厂大门/厂内干草", "In-Scene Objects/工厂建筑群/03 工业设备/天线" })
{
    var go = UnityEngine.GameObject.Find(path);
    if (go != null && go.activeSelf) hidden.Add(go);
}
var playerRenderers = new System.Collections.Generic.List<UnityEngine.Renderer>();
foreach (var r in player.GetComponentsInChildren<UnityEngine.Renderer>()) if (r.enabled) playerRenderers.Add(r);
var terrains = UnityEngine.Terrain.activeTerrains;
var terrainDetails = new bool[terrains.Length];

// Repainted gate and fresh guardhouse plaster: material copies whose textures are swapped by name
// for the clean ones that exist.
var temporary = new System.Collections.Generic.List<UnityEngine.Object>();
var gateRenderers = UnityEngine.GameObject.Find("In-Scene Objects/工厂大门/FactoryGate").GetComponentsInChildren<UnityEngine.Renderer>(true)
    .Concat(UnityEngine.GameObject.Find("In-Scene Objects/工厂大门/Guard Post/Guardhouse_Rebuilt").GetComponentsInChildren<UnityEngine.Renderer>(true));
var originalMaterials = new System.Collections.Generic.Dictionary<UnityEngine.Renderer, UnityEngine.Material[]>();
var cleanMaterials = new System.Collections.Generic.Dictionary<UnityEngine.Material, UnityEngine.Material>();
var cleanTextures = new System.Collections.Generic.Dictionary<string, UnityEngine.Texture2D>();
UnityEngine.Texture2D CleanTexture(string name)
{
    if (cleanTextures.TryGetValue(name, out var cached)) return cached;
    string file = System.IO.Path.Combine(cleanGateDir, name + ".png");
    UnityEngine.Texture2D tex = null;
    if (System.IO.File.Exists(file))
    {
        // Colour maps are sRGB; ORM and normal maps are data.
        tex = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, true, !name.EndsWith("_BaseColor"));
        UnityEngine.ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(file));
        tex.anisoLevel = 8;
        temporary.Add(tex);
    }
    cleanTextures[name] = tex;
    return tex;
}
void RepaintGate()
{
    foreach (var r in gateRenderers)
    {
        var materials = r.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            var m = materials[i];
            if (m == null) continue;
            if (!cleanMaterials.TryGetValue(m, out var copy))
            {
                copy = new UnityEngine.Material(m);
                foreach (var property in m.GetTexturePropertyNames())
                {
                    var tex = m.GetTexture(property);
                    var clean = tex != null ? CleanTexture(tex.name) : null;
                    if (clean != null) copy.SetTexture(property, clean);
                }
                cleanMaterials[m] = copy;
                temporary.Add(copy);
            }
            materials[i] = copy;
        }
        originalMaterials[r] = r.sharedMaterials;
        r.sharedMaterials = materials;
    }
}

// Banner cloth: hung from its two top corners, so it sags in the middle and folds a little.
var bannerMesh = new UnityEngine.Mesh();
const int cols = 64, rows = 8;
var vertices = new UnityEngine.Vector3[(cols + 1) * (rows + 1)];
var uvs = new UnityEngine.Vector2[vertices.Length];
var bannerCenter = gateCenter + outward * bannerOut + UnityEngine.Vector3.up * (bannerTop - bannerHeight * .5f);
for (int y = 0; y <= rows; y++)
for (int x = 0; x <= cols; x++)
{
    float u = (float)x / cols, v = (float)y / rows;
    float sag = bannerSag * 4f * u * (1f - u) * (1.1f - .1f * v);
    float fold = .025f * UnityEngine.Mathf.Sin(u * 41f + 1.3f) * (1f - .6f * v)
               + .04f * UnityEngine.Mathf.Exp(-u * 18f) * (1f - v) + .04f * UnityEngine.Mathf.Exp(-(1f - u) * 18f) * (1f - v);
    vertices[y * (cols + 1) + x] = bannerCenter + screenRight * ((u - .5f) * bannerWidth)
        + UnityEngine.Vector3.up * ((v - .5f) * bannerHeight - sag) + outward * fold;
    uvs[y * (cols + 1) + x] = new UnityEngine.Vector2(u, v);
}
var triangles = new int[cols * rows * 6];
for (int y = 0, t = 0; y < rows; y++)
for (int x = 0; x < cols; x++, t += 6)
{
    int i = y * (cols + 1) + x;
    // Clockwise seen from outside.
    triangles[t] = i; triangles[t + 1] = i + cols + 1; triangles[t + 2] = i + cols + 2;
    triangles[t + 3] = i; triangles[t + 4] = i + cols + 2; triangles[t + 5] = i + 1;
}
bannerMesh.vertices = vertices; bannerMesh.uv = uvs; bannerMesh.triangles = triangles;
bannerMesh.RecalculateNormals(); bannerMesh.RecalculateTangents(); bannerMesh.RecalculateBounds();

var bannerTex = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, true);
UnityEngine.ImageConversion.LoadImage(bannerTex, System.IO.File.ReadAllBytes(bannerTexture));
bannerTex.wrapMode = UnityEngine.TextureWrapMode.Clamp; bannerTex.anisoLevel = 8;
var bannerMaterial = new UnityEngine.Material(UnityEngine.Shader.Find("HDRP/Lit"));
bannerMaterial.SetTexture("_BaseColorMap", bannerTex);
bannerMaterial.SetFloat("_Smoothness", .12f);
bannerMaterial.SetFloat("_DoubleSidedEnable", 1f);
UnityEditor.Rendering.HighDefinition.HDShaderUtils.ResetMaterialKeywords(bannerMaterial);
var ropeMaterial = new UnityEngine.Material(UnityEngine.Shader.Find("HDRP/Lit"));
ropeMaterial.SetColor("_BaseColor", new UnityEngine.Color(.32f, .29f, .24f));
ropeMaterial.SetFloat("_Smoothness", .1f);

temporary.AddRange(new UnityEngine.Object[] { bannerMesh, bannerTex, bannerMaterial, ropeMaterial });
var banner = new UnityEngine.GameObject("Group Photo Banner (temporary)");
banner.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
banner.AddComponent<UnityEngine.MeshFilter>().sharedMesh = bannerMesh;
banner.AddComponent<UnityEngine.MeshRenderer>().sharedMaterial = bannerMaterial;
temporary.Add(banner);
// Ropes from the top corners to the inner faces of the piers, tied just under the caps.
foreach (int side in new[] { -1, 1 })
{
    var corner = vertices[rows * (cols + 1) + (side < 0 ? 0 : cols)];
    var pierBounds = side < 0 ? rightBounds : leftBounds;
    var tie = pierBounds.center + screenRight * (-side * .7f) + outward * .55f; tie.y = pierBounds.max.y - .3f;
    var rope = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cylinder);
    UnityEngine.Object.DestroyImmediate(rope.GetComponent<UnityEngine.Collider>());
    rope.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
    rope.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial = ropeMaterial;
    rope.transform.position = (corner + tie) * .5f;
    rope.transform.up = (tie - corner).normalized;
    rope.transform.localScale = new UnityEngine.Vector3(.014f, (tie - corner).magnitude * .5f, .014f);
    temporary.Add(rope);
}

var sourceCamera = UnityEngine.GameObject.Find("Gameplay Root/MainMenuCamera");
var cameraObject = UnityEngine.Object.Instantiate(sourceCamera);
cameraObject.name = "Group Photo Render Camera";
cameraObject.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
temporary.Add(cameraObject);
var camera = cameraObject.GetComponent<UnityEngine.Camera>();
camera.enabled = false;
cameraObject.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>().antialiasing =
    UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.None;
var target = new UnityEngine.RenderTexture(width, height, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
var capture = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
temporary.Add(capture);
var previousActive = UnityEngine.RenderTexture.active;
var log = new System.Text.StringBuilder();
log.AppendLine($"gate centre {gateCenter:F2}, outward {outward:F3}, pier gap {pierGap:F2}");
System.IO.Directory.CreateDirectory(outputDir);
try
{
    sand.Prepare();
    dormitory.Prepare();
    sceneState.Prepare();
    RepaintGate();
    foreach (var go in hidden) go.SetActive(false);
    foreach (var r in playerRenderers) r.enabled = false;
    for (int i = 0; i < terrains.Length; i++) { terrainDetails[i] = terrains[i].drawTreesAndFoliage; terrains[i].drawTreesAndFoliage = false; }
    camera.targetTexture = target;
    camera.aspect = (float)width / height;
    float baseYaw = UnityEngine.Quaternion.LookRotation(-outward).eulerAngles.y;
    foreach (var shot in shots)
    {
        var position = gateCenter + outward * shot.distance;
        if (UnityEngine.Physics.Raycast(position + UnityEngine.Vector3.up * 30f, UnityEngine.Vector3.down, out var hit, 60f))
            position.y = hit.point.y;
        position.y += eyeHeight;
        cameraObject.transform.SetPositionAndRotation(position, UnityEngine.Quaternion.Euler(-shot.pitch, baseYaw + shot.yaw, 0f));
        camera.fieldOfView = shot.fov;
        // A few frames let exposure and volumetric fog settle.
        for (int i = 0; i < 8; i++) camera.Render();
        UnityEngine.RenderTexture.active = target;
        capture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
        capture.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(outputDir, shot.name + ".png"), capture.EncodeToPNG());
        log.AppendLine($"{shot.name}: camera {position:F2} euler {cameraObject.transform.eulerAngles:F1} fov {shot.fov}");
    }
}
finally
{
    UnityEngine.RenderTexture.active = previousActive;
    for (int i = 0; i < terrains.Length; i++) terrains[i].drawTreesAndFoliage = terrainDetails[i];
    foreach (var r in playerRenderers) r.enabled = true;
    foreach (var go in hidden) go.SetActive(true);
    foreach (var pair in originalMaterials) pair.Key.sharedMaterials = pair.Value;
    sceneState.Restore();
    dormitory.Restore();
    sand.Restore();
    camera.targetTexture = null;
    target.Release();
    UnityEngine.Object.DestroyImmediate(target);
    foreach (var o in temporary) if (o != null) UnityEngine.Object.DestroyImmediate(o);
}
return log.ToString();
