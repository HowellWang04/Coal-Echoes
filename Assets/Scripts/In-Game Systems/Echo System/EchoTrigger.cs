using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class EchoTrigger : MonoBehaviour
{
    public static readonly HashSet<EchoTrigger> Active = new();

    [SerializeField] private string echoId;
    [SerializeField, Min(0f)] private float maxDistanceOverride;
    [SerializeField] private bool frontOnly;

    [TextArea(3, 6)]
    [FormerlySerializedAs("diaryContent")]
    public string scrapbookContent;

    public string stickyNoteSlotId;

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
