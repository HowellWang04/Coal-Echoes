var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if (!scene.isLoaded || EditorApplication.isPlaying) throw new Exception("Main scene must be in edit mode.");
var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
if (all.Any(t => t.name == "CablewaySummitStation")) throw new Exception("Summit station already exists.");
if (!System.IO.File.Exists("ArtSource/Cableway/SummitStation/Before/Main Game Scene.live.unity")) throw new Exception("Backup is required.");
var plan = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("ArtSource/Cableway/SummitStation/site_plan.json"));
Vector3 V(Newtonsoft.Json.Linq.JToken p) => new Vector3((float)p[0], (float)p[1], (float)p[2]);
var source = all.Single(t => t.name == "CablewayFoothillStation");
var sourceTerminal = source.Find("CablewayReturnTerminal");
var terrain = Terrain.activeTerrains.Single(t => AssetDatabase.GetAssetPath(t.terrainData) == "Assets/TerrainData/SheepRangeGenerated/Tile_0_0_Main.asset");
float Ground(Vector3 p) => terrain.SampleHeight(p) + terrain.transform.position.y;
Undo.IncrementCurrentGroup();
Undo.SetCurrentGroupName("Build summit cableway station");
var root = new GameObject("CablewaySummitStation");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
root.transform.SetParent(source.parent, false);
root.transform.position = V(plan["origin"]);
root.transform.rotation = Quaternion.Euler(0, (float)plan["yaw"], 0);
Undo.RegisterCreatedObjectUndo(root, "Build summit cableway station");
foreach (Transform part in source) {
    var clone = UnityEngine.Object.Instantiate(part.gameObject, root.transform);
    clone.name = part.name;
    clone.transform.localPosition = sourceTerminal.InverseTransformPoint(part.position);
    clone.transform.localRotation = Quaternion.Inverse(sourceTerminal.rotation) * part.rotation;
    clone.transform.localScale = part.lossyScale;
    PrefabUtility.RecordPrefabInstancePropertyModifications(clone.transform);
}
// A level station foundation with a skirt sunk into the existing hillside.
// The terrain heights and the MapMagic graph are left untouched.
var pad = new GameObject("StationFoundation");
pad.transform.SetParent(root.transform, false);
GameObjectUtility.SetStaticEditorFlags(pad, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
float x0=-8f, x1=7f, z0=-3.5f, z1=12f, top=-.03f;
var perimeter = new List<Vector3>();
for(int i=0;i<15;i++) perimeter.Add(new Vector3(x0+i,top,z0));
for(int i=0;i<16;i++) perimeter.Add(new Vector3(x1,top,Mathf.Lerp(z0,z1,i/16f)));
for(int i=0;i<15;i++) perimeter.Add(new Vector3(x1-i,top,z1));
for(int i=0;i<16;i++) perimeter.Add(new Vector3(x0,top,Mathf.Lerp(z1,z0,i/16f)));
var vertices = new List<Vector3>();
var uv = new List<Vector2>();
var triangles = new List<int>();
void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud) {
    int k=vertices.Count; vertices.AddRange(new[]{a,b,c,d}); uv.AddRange(new[]{ua,ub,uc,ud});
    triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
}
Quad(new Vector3(x0,top,z0),new Vector3(x0,top,z1),new Vector3(x1,top,z1),new Vector3(x1,top,z0),new Vector2(0,0),new Vector2(0,(z1-z0)/3),new Vector2((x1-x0)/3,(z1-z0)/3),new Vector2((x1-x0)/3,0));
float distance=0;
for(int i=0;i<perimeter.Count;i++) {
    var a=perimeter[i]; var b=perimeter[(i+1)%perimeter.Count];
    var lowA=a; var lowB=b;
    lowA.y=Ground(root.transform.TransformPoint(a))-root.transform.position.y-.4f;
    lowB.y=Ground(root.transform.TransformPoint(b))-root.transform.position.y-.4f;
    float next=distance+Vector3.Distance(a,b);
    Quad(a,b,lowB,lowA,new Vector2(distance/3,0),new Vector2(next/3,0),new Vector2(next/3,(top-lowB.y)/3),new Vector2(distance/3,(top-lowA.y)/3));
    distance=next;
}
var mesh=new Mesh(); mesh.name="SummitStationFoundation";
mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
var folder="Assets/Art Resources/Environment/Cableway/CablewaySummitStation";
if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Art Resources/Environment/Cableway","CablewaySummitStation");
AssetDatabase.CreateAsset(mesh,folder+"/StationFoundation.asset");
pad.AddComponent<MeshFilter>().sharedMesh=mesh;
pad.AddComponent<MeshRenderer>().sharedMaterial=source.Find("CablewayStationShell_LineA/ConcretePlinth").GetComponent<Renderer>().sharedMaterial;
pad.AddComponent<MeshCollider>().sharedMesh=mesh;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
AssetDatabase.SaveAssetIfDirty(mesh);
Selection.activeGameObject=root;
var view=SceneView.lastActiveSceneView;
var forward=root.transform.forward; var right=root.transform.right;
view.LookAtDirect(root.transform.TransformPoint(new Vector3(0,1,4)),Quaternion.LookRotation((forward-right*.8f+Vector3.down*.6f).normalized),24);
view.camera.ResetAspect(); view.camera.ResetProjectionMatrix(); view.Repaint();
return new {root=AnimationUtility.CalculateTransformPath(root.transform,null),parts=root.transform.childCount,foundation=AssetDatabase.GetAssetPath(mesh)};
