using UnityEngine;

[DefaultExecutionOrder(1000)]
public class EchoTargetDetector : MonoBehaviour
{
    [SerializeField] private Camera photoCamera;
    [SerializeField] private Camera handheldCamera;
    [SerializeField] private CameraSystemManager cameraSystemManager;
    [SerializeField] private PlayerPanelUIManager playerPanel;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private LayerMask occlusionMask = ~0;

    [SerializeField, Min(0f)] private float maxDistance = 3f;
    [SerializeField, Range(0f, .5f)] private float centerTolerance = .1f;
    [SerializeField, Range(0f, 1f)] private float minimumScreenSize = .1f;
    [SerializeField, Range(0f, .5f)] private float exitMargin = .1f;

    public EchoTrigger CurrentTarget { get; private set; }
    public bool IsTakingAim => cameraSystemManager.CurrentState == CameraModeState.Active
        && !cameraSystemManager.IsViewTransitioning && !playerPanel.IsOpen;

    private readonly RaycastHit[] hits = new RaycastHit[32];
    private Camera AimCamera => cameraSystemManager.CurrentViewMode == CameraViewMode.Handheld
        ? handheldCamera : photoCamera;

    private void LateUpdate()
    {
        if (!IsTakingAim || !GrandpaDiaryProgress.HasDiary) { ClearTarget(); return; }

        EchoTrigger best = null;
        float bestScore = float.PositiveInfinity;
        foreach (var target in EchoTrigger.Active)
        {
            if (target.gameObject.scene != gameObject.scene) continue;
            if (!Qualifies(target, target == CurrentTarget, out float score)) continue;
            if (score < bestScore)
            {
                best = target;
                bestScore = score;
            }
        }
        CurrentTarget = best;
    }

    public void ClearTarget() => CurrentTarget = null;

    // The shutter uses the same target that produced the last displayed yellow frame.
    public EchoTrigger CaptureTarget() => IsTakingAim && GrandpaDiaryProgress.HasDiary ? CurrentTarget : null;

    public bool Qualifies(EchoTrigger target, bool retaining, out float score)
    {
        score = float.PositiveInfinity;
        if (!target.isActiveAndEnabled || !target.TryGetBounds(out Bounds bounds)) return false;

        Camera camera = AimCamera;
        Vector3 center = target.transform.TransformPoint(bounds.center);
        Vector3 toCamera = camera.transform.position - center;
        float slack = retaining ? 1f + exitMargin : 1f;
        float distanceLimit = target.MaxDistanceOverride > 0f ? target.MaxDistanceOverride : maxDistance;
        if (toCamera.sqrMagnitude > distanceLimit * distanceLimit * slack * slack) return false;
        if (target.FrontOnly && Vector3.Dot(target.transform.forward, toCamera) <= 0f) return false;
        if (!target.IsStandingIn(camera.transform)) return false;

        Vector3 viewportCenter = camera.WorldToViewportPoint(center);
        if (viewportCenter.z <= camera.nearClipPlane) return false;
        Vector2 offset = new Vector2(viewportCenter.x - .5f, viewportCenter.y - .5f);
        // A stand point already fixes the aim, so its target need not sit in the middle.
        if (!target.HasStandPoint
            && (Mathf.Abs(offset.x) > centerTolerance * slack || Mathf.Abs(offset.y) > centerTolerance * slack))
            return false;

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            Vector3 local = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 projected = camera.WorldToViewportPoint(target.transform.TransformPoint(local));
            if (projected.z <= camera.nearClipPlane) return false;
            min = Vector2.Min(min, projected);
            max = Vector2.Max(max, projected);
        }
        float aspect = camera.aspect;
        float shortSide = Mathf.Min((max.x - min.x) * aspect, max.y - min.y) / Mathf.Min(aspect, 1f);
        if (shortSide < minimumScreenSize / slack) return false;

        // Always require a clear center. Four automatic interior samples allow a little edge occlusion.
        if (!IsClear(target, center)) return false;
        int visible = 0;
        Vector3 sampleExtents = bounds.extents * .6f;
        for (int i = 0; i < 4; i++)
        {
            Vector3 sample = bounds.center + new Vector3(
                (i & 1) == 0 ? -sampleExtents.x : sampleExtents.x,
                (i & 2) == 0 ? -sampleExtents.y : sampleExtents.y, 0f);
            if (IsClear(target, target.transform.TransformPoint(sample))) visible++;
        }
        if (visible < 3) return false;
        score = offset.sqrMagnitude;
        return true;
    }

    private bool IsClear(EchoTrigger target, Vector3 point)
    {
        Vector3 delta = point - AimCamera.transform.position;
        int count = Physics.RaycastNonAlloc(AimCamera.transform.position, delta.normalized, hits,
            delta.magnitude, occlusionMask, QueryTriggerInteraction.Ignore);
        // A saturated buffer may have omitted an occluder, so don't claim visibility.
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].transform;
            if (hit.IsChildOf(target.transform) || (playerRoot != null && hit.IsChildOf(playerRoot))) continue;
            return false;
        }
        return true;
    }
}
