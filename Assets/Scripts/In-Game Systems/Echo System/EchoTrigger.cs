using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class EchoTrigger : MonoBehaviour
{
    public static readonly HashSet<EchoTrigger> Active = new();

    [SerializeField] private string echoId;
    [SerializeField, Min(0f)] private float maxDistanceOverride;
    [SerializeField] private bool frontOnly;
    // A scene recreated from an old photo, such as the Group Photo at the gate, only counts
    // while the camera stands within this radius of the marked spot and looks within this angle
    // of its forward. That aim replaces keeping the target in the middle of the frame.
    [SerializeField] private Transform standPoint;
    [SerializeField, Min(0f)] private float standRadius = 1.2f;
    [SerializeField, Range(0f, 90f)] private float standAngle = 10f;
    // Raised for every saved photo of this echo, not only the first; main-story echoes advance the level here.
    [SerializeField] private UnityEvent onCaptured;

    [TextArea(3, 6)]
    [FormerlySerializedAs("diaryContent")]
    public string scrapbookContent;

    public EchoNoteLayout noteLayout = new();

    public string EchoId => echoId;
    public float MaxDistanceOverride => maxDistanceOverride;
    public bool FrontOnly => frontOnly;

    private Renderer[] targetRenderers;
    private readonly HashSet<Renderer> lowerLodRenderers = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(echoId)) echoId = System.Guid.NewGuid().ToString("N");
    }

    private void OnEnable()
    {
        targetRenderers = GetComponentsInChildren<Renderer>(true);
        lowerLodRenderers.Clear();
        foreach (var group in GetComponentsInChildren<LODGroup>(true))
        {
            var lods = group.GetLODs();
            for (int i = 1; i < lods.Length; i++)
                foreach (var renderer in lods[i].renderers)
                    lowerLodRenderers.Add(renderer);
        }
        Active.Add(this);
    }

    private void OnDisable() => Active.Remove(this);

    public bool HasStandPoint => standPoint != null;

    public bool IsStandingIn(Transform view)
    {
        if (standPoint == null) return true;
        Vector3 offset = view.position - standPoint.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= standRadius * standRadius
            && Vector3.Angle(view.forward, standPoint.forward) <= standAngle;
    }

    public void NotifyCaptured() => onCaptured.Invoke();

    // Combine the object's own geometry in its local space, not a world-axis box.
    public bool TryGetBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var renderer in targetRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy
                || lowerLodRenderers.Contains(renderer)) continue;
            if (renderer.GetComponentInParent<EchoTrigger>() != this) continue;
            Bounds local = renderer.localBounds;
            Matrix4x4 matrix = transform.worldToLocalMatrix * renderer.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                else bounds.Encapsulate(point);
            }
        }
        return found;
    }
}
