// Run after ImportMotorcycle.cs. Positions the preview without moving the player.
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var player = scene.GetRootGameObjects().First(x => x.name == "Player").transform;
var parent = scene.GetRootGameObjects().First(x => x.name == "场景内Object").transform;
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art Resources/Environment/Gate Area/DustyMotorcycle/Dusty_Red_Motorcycle.prefab");
var bike = parent.Cast<UnityEngine.Transform>().FirstOrDefault(t => t.name == "Dusty Motorcycle Preview");
if (bike == null)
{
    var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    bike = instance.transform;
    bike.SetParent(parent, true);
    bike.name = "Dusty Motorcycle Preview";
    UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Place original motorcycle preview");
}
UnityEditor.Undo.RecordObject(bike, "Position motorcycle beside player");
var position = player.position + player.forward * 2.4f - player.right * 1.2f;
var yaw = UnityEngine.Quaternion.Euler(0, player.eulerAngles.y + 30, 0);
System.Func<UnityEngine.Vector3, UnityEngine.RaycastHit> ground = point => UnityEngine.Physics.RaycastAll(point + UnityEngine.Vector3.up * 10, UnityEngine.Vector3.down, 30).Where(h => h.collider is UnityEngine.TerrainCollider).OrderBy(h => h.distance).First();
var front = ground(position + yaw * new UnityEngine.Vector3(.75f,0,0)).point;
var rear = ground(position + yaw * new UnityEngine.Vector3(-.68f,0,0)).point;
// Match the wheel contact heights, preserving an upright bike across the slope.
var longitudinal = (front - rear).normalized;
var sideways = UnityEngine.Vector3.Cross(longitudinal, UnityEngine.Vector3.up).normalized;
var up = UnityEngine.Vector3.Cross(sideways, longitudinal).normalized;
bike.rotation = UnityEngine.Quaternion.LookRotation(sideways, up);
bike.position = position;
var localContacts = new[] { new UnityEngine.Vector3(.75f,.002f,0), new UnityEngine.Vector3(-.68f,.002f,0) };
var correction = localContacts.Select(v => { var pt = bike.TransformPoint(v); return ground(pt).point.y - pt.y; }).Average();
bike.position += UnityEngine.Vector3.up * correction;
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(bike);
UnityEditor.EditorUtility.SetDirty(bike);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.Selection.activeGameObject = bike.gameObject;
var bounds = bike.GetComponentInChildren<UnityEngine.Renderer>().bounds;
var view = UnityEditor.SceneView.lastActiveSceneView;
if (view != null)
{
    var eye = bike.position + bike.rotation * new UnityEngine.Vector3(3.0f,2.1f,-3.7f);
    var target = bounds.center;
    view.orthographic = false;
    view.LookAtDirect(target, UnityEngine.Quaternion.LookRotation(target-eye), 2.0f);
    view.Repaint();
}
return "Placed " + bike.name + " position " + bike.position.ToString("F4") + " rotation " + bike.eulerAngles.ToString("F3") + " bounds " + bounds.size.ToString("F3") + " Player unchanged " + player.position.ToString("F4");
