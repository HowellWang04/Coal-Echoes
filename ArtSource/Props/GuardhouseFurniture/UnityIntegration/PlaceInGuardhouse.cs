// Method body executed through Unity MCP execute_code. Places the desk, chair, desk lamp and existing
// third-party clutter under Guard Post/室内陈设, and moves the photo frame and logbook there. Positions are in Guard Post local space:
// floor at y 0.18, inner walls at x ±2.55 and z ±2.1, front wall (door and service window) at +z,
// observation window toward the gate at +x. Run ImportFurniture.cs first. Does not save the scene.
var guard = GameObject.Find("Guard Post").transform;
var group = guard.Find("室内陈设");
if (group == null) { group = new GameObject("室内陈设").transform; group.SetParent(guard, false); Undo.RegisterCreatedObjectUndo(group.gameObject, "Furnish guardhouse"); }
const string F = "Assets/Art Resources/Props/GuardhouseFurniture/";
const string M = "Assets/Art Resources/ThirdPartyPacks/Military_VOL4_Furniture/Meshes/";
const string H = "Assets/Art Resources/ThirdPartyPacks/Industrial_Harbour/HDRP/Art/Prefabs/";
var items = new (string path, string name, Vector3 lp, float yaw, float scale, string material)[] {
    (F + "GuardDesk.prefab", "GuardDesk", new Vector3(1.9f, .18f, 1.79f), 180, 1, null),   // under the service window, in the gate-side corner
    (F + "GuardChair.prefab", "GuardChair", new Vector3(1.72f, .18f, 1.2f), -18, 1, null), // pulled out and turned, left in a hurry
    (M + "SM_Bed_01a.FBX", "值班床", new Vector3(-2f, .18f, -1f), 0, 1, "GuardBed_Worn"),
    (M + "SM_Locker_01b.FBX", "铁皮柜_A", new Vector3(1.2f, .18f, -1.85f), 0, 1, "GuardLocker_GreyGreen"),
    (M + "SM_Locker_01b.FBX", "铁皮柜_B", new Vector3(1.6f, .18f, -1.85f), 0, 1, "GuardLocker_GreyGreen"),
    (H + "SM_WoodBox.prefab", "木箱", new Vector3(-1.1f, .18f, -1.72f), 188, .6f, null),
    // Base in the back corner from the chair; fitted so the cord and fallen shade stay on the top and off the logbook.
    ("Assets/Art Resources/Props/DeskLamp/DeskLamp.prefab", "DeskLamp", new Vector3(1.42f, .96f, 1.9f), 210, 1, null),
};
foreach (var it in items)
{
    var old = group.Find(it.name); if (old) Undo.DestroyObjectImmediate(old.gameObject);
    var inst = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(it.path), group);
    Undo.RegisterCreatedObjectUndo(inst, "Furnish guardhouse");
    inst.name = it.name;
    inst.transform.localPosition = it.lp; inst.transform.localRotation = Quaternion.Euler(0, it.yaw, 0); inst.transform.localScale = Vector3.one * it.scale;
    if (it.material != null)
    {
        // Guardhouse-only darker copies; the pack materials stay untouched.
        var r = inst.GetComponentInChildren<Renderer>(); var ms = r.sharedMaterials;
        ms[0] = AssetDatabase.LoadAssetAtPath<Material>(F + "Materials/" + it.material + ".mat"); r.sharedMaterials = ms;
    }
    // The Military pack ships Unreal collision hulls (UCX_) as plain meshes; they render as pale boards.
    foreach (var hull in inst.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("UCX_")))
        hull.gameObject.SetActive(false);
    if (inst.GetComponentInChildren<Collider>() == null && it.name != "DeskLamp")
    {
        var r = inst.GetComponentInChildren<MeshRenderer>(); var mb = r.GetComponent<MeshFilter>().sharedMesh.bounds;
        var b = new Bounds(inst.transform.InverseTransformPoint(r.transform.TransformPoint(mb.center)), Vector3.zero);
        for (int i = 0; i < 8; i++) b.Encapsulate(inst.transform.InverseTransformPoint(r.transform.TransformPoint(new Vector3((i & 1) == 0 ? mb.min.x : mb.max.x, (i & 2) == 0 ? mb.min.y : mb.max.y, (i & 4) == 0 ? mb.min.z : mb.max.z))));
        var col = inst.AddComponent<BoxCollider>(); col.center = b.center; col.size = b.size;
    }
}
// The two optional echoes are existing scene objects with EchoTrigger, so they are moved, not re-created.
// Frame: root +Z is the photo face (EchoTrigger front check), back against the rear wall, 7 degrees off level.
var frame = guard.parent.Find("OldPhotoFrame_StreetMarket") ?? group.Find("OldPhotoFrame_StreetMarket");
Undo.SetTransformParent(frame, group, "Furnish guardhouse");
frame.localPosition = new Vector3(-.15f, 1.62f, -2.083f); frame.localRotation = Quaternion.Euler(0, 0, -7);
// Logbook: its pivot is at the middle of the book (moved there so it turns in place when inspected),
// so these values put the cover flat on the desk in front of the chair.
var log = guard.parent.Find("Logbook") ?? group.Find("Logbook");
Undo.SetTransformParent(log, group, "Furnish guardhouse");
log.localPosition = new Vector3(1.9528f, .9926f, 1.7196f); log.localRotation = Quaternion.Euler(0, 96, 0);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(guard.gameObject.scene);
return group.childCount;
