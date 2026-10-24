using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Controller.FinalCharacterController;

public class InspectUIManager : MonoBehaviour
{
    [Header("Object Ref")]
    [SerializeField] private PlayerInputProvider inputProvider;
    [SerializeField] private GameObject inspectPanel;
    [SerializeField] private PlayerMovementManager playerMovementManager;

    [Header("UI Components Refs")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private GameObject readTextPanel;   // what is written on the item, such as the duty log
    [SerializeField] private TMP_Text readText;

    [Header("Inspect Layer")]
    [SerializeField] private int inspectLayer = 6;

    [Header("Pick Up")]
    [SerializeField] private float flyDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float clearanceStep = 0.05f;   // how far in each try moves the copy

    public bool IsInspecting { get; private set; }
    // The E that puts an item down must not pick the next one up in the same frame.
    public bool StoppedThisFrame => stoppedFrame == Time.frameCount;
    public event Action OnInspectStopped;

    private GameObject inspectClone;
    private Camera currentCamera;
    private GameObject putBackTarget;   // scene item hidden while its copy is inspected
    private InspectableItem inspectedItem;
    private Tween pickUpTween;
    private readonly Collider[] overlaps = new Collider[16];
    private readonly RaycastHit[] sightHits = new RaycastHit[16];
    private int startedFrame = -1;
    private int stoppedFrame = -1;

    private CursorLockMode prevLockState;
    private bool prevCursorVisible;

    // Scene items are picked up: the copy flies from where the item lies to the camera.
    public void StartInspect(InspectableItem item, Camera cam)
    {
        if (IsInspecting) return;
        StartInspect(item.itemName, item.itemDescription, item.gameObject,
            item.inspectDistance, item.inspectScale, item.inspectRotationOffset, cam, true);
        // Tools go into the backpack, so only items that stay in the scene are put back.
        if (item.backpackItem == null) putBackTarget = item.gameObject;
        inspectedItem = item;
        bool hasText = !string.IsNullOrEmpty(item.readText);
        if (readTextPanel != null) readTextPanel.SetActive(hasText);
        if (hasText && readText != null) readText.text = item.readText;
    }

    public void StartInspect(BackpackItemDefinition item, Camera cam)
    {
        StartInspect(item.DisplayName, item.Usage, item.InspectPrefab,
            item.InspectDistance, item.InspectScale, item.InspectRotationOffset, cam, false);
    }

    private void StartInspect(string itemName, string itemDescription, GameObject source,
        float distance, float scale, Vector3 rotationOffset, Camera cam, bool pickUp)
    {
        if (IsInspecting) return;

        IsInspecting = true;
        startedFrame = Time.frameCount;
        currentCamera = cam;

        prevLockState = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        if (playerMovementManager != null) playerMovementManager.SetPlayerMovementControl(false);
        SetOtherActions(false);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        if (inspectPanel != null) inspectPanel.SetActive(true);
        if (itemNameText != null) itemNameText.text = itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = itemDescription;

        // Spawn clone
        SpawnClone(source, distance, scale, rotationOffset, pickUp);
    }

    public void StopInspect()
    {
        if (!IsInspecting) return;
        IsInspecting = false;
        stoppedFrame = Time.frameCount;

        if (inspectClone != null)
        {
            var rotator = inspectClone.GetComponent<InspectRotator>();
            if (rotator != null) rotator.Deactivate();
            pickUpTween?.Kill();
            if (putBackTarget != null) PutBack(inspectClone, putBackTarget);
            else Destroy(inspectClone);
            inspectClone = null;
        }
        putBackTarget = null;

        if (readTextPanel != null) readTextPanel.SetActive(false);
        if (inspectPanel != null) inspectPanel.SetActive(false);
        if (playerMovementManager != null) playerMovementManager.SetPlayerMovementControl(true);
        SetOtherActions(true);
        Cursor.lockState = prevLockState;
        Cursor.visible = prevCursorVisible;

        currentCamera = null;

        OnInspectStopped?.Invoke();
        var finished = inspectedItem;
        inspectedItem = null;
        if (finished != null) finished.onInspectEnd.Invoke();
    }

    private void Update()
    {
        if (!IsInspecting || inputProvider == null) return;

        // E puts the item down as well, except in the frame whose E picked it up.
        var gameplay = inputProvider.Controls.Gameplay;
        if (gameplay.Cancel.WasPressedThisFrame()
            || (Time.frameCount != startedFrame && gameplay.Inspect.WasPressedThisFrame()))
            StopInspect();
    }

    // Raising the camera and the Player's Panel wait until the item is put down.
    private void SetOtherActions(bool enabled)
    {
        if (inputProvider == null) return;
        var gameplay = inputProvider.Controls.Gameplay;
        if (enabled)
        {
            gameplay.CameraModeToggle.Enable();
            gameplay.PlayerPanelToggle.Enable();
        }
        else
        {
            gameplay.CameraModeToggle.Disable();
            gameplay.PlayerPanelToggle.Disable();
        }
    }

    private void SpawnClone(GameObject source, float distance, float scale, Vector3 rotationOffset, bool pickUp)
    {
        // The offset is relative to the camera, so an item faces the player the same way wherever they look from.
        Quaternion inspectRot = currentCamera.transform.rotation * Quaternion.Euler(rotationOffset);
        Vector3 inspectScale = source.transform.lossyScale * scale;

        // A picked up copy starts where the item lies, and the item hides until it is put back.
        inspectClone = pickUp
            ? Instantiate(source, source.transform.position, source.transform.rotation)
            : Instantiate(source, currentCamera.transform.position, inspectRot);
        inspectClone.SetActive(true);
        inspectClone.transform.localScale = pickUp ? source.transform.lossyScale : inspectScale;
        if (pickUp) source.SetActive(false);

        SetLayerRecursive(inspectClone, inspectLayer);

        // Disable shadows and colliders
        var renderers = inspectClone.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
}

        var colliders = inspectClone.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) Destroy(col);

        var inspectable = inspectClone.GetComponent<InspectableItem>();
        if (inspectable != null) Destroy(inspectable);

        // Only the item left in the scene counts as an echo, not the copy in front of the camera.
        foreach (var echo in inspectClone.GetComponentsInChildren<EchoTrigger>()) Destroy(echo);

        var rigidbodies = inspectClone.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies) Destroy(rb);

        // Add rotation control, once the copy is in front of the camera
        var rotator = inspectClone.AddComponent<InspectRotator>();

        // Nobody walks while inspecting, so walls and furniture are checked once, here: the copy comes
        // closer when something would cut into or hide it, and zooming out stops at the same point.
        float farthest = FarthestClearDistance(inspectClone.transform, inspectRot, inspectScale, rotator.MaxDistance);
        distance = Mathf.Min(distance, farthest);
        Vector3 inspectPos = currentCamera.transform.position
            + currentCamera.transform.forward * distance;

        if (!pickUp)
        {
            inspectClone.transform.position = inspectPos;
            rotator.Activate(currentCamera, distance, farthest, inputProvider);
            return;
        }

        pickUpTween = FlyTo(inspectClone.transform, inspectPos, inspectRot, inspectScale)
            .OnComplete(() => rotator.Activate(currentCamera, distance, farthest, inputProvider));
    }

    // Steps in from the farthest zoom until the copy neither overlaps the scene nor sits behind something.
    // Closer than the camera's near plane it would be cut off, so that is as close as it gets.
    private float FarthestClearDistance(Transform clone, Quaternion rotation, Vector3 scale, float maxDistance)
    {
        Bounds local = LocalBounds(clone);
        Vector3 halfExtents = Vector3.Scale(local.extents, scale);
        Vector3 offset = rotation * Vector3.Scale(local.center, scale);
        Transform cam = currentCamera.transform;
        float near = currentCamera.nearClipPlane;
        for (float d = maxDistance; d > near; d -= clearanceStep)
            if (IsClearAt(clone, cam.position + cam.forward * d + offset, halfExtents, rotation)) return d;
        return near;
    }

    private bool IsClearAt(Transform clone, Vector3 center, Vector3 halfExtents, Quaternion rotation)
    {
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, overlaps, rotation,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (!IsOwn(overlaps[i].transform, clone)) return false;

        Vector3 eye = currentCamera.transform.position;
        Vector3 toCenter = center - eye;
        count = Physics.RaycastNonAlloc(eye, toCenter.normalized, sightHits, toCenter.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
            if (!IsOwn(sightHits[i].transform, clone)) return false;
        return true;
    }

    // The copy's own colliders are only gone at the end of the frame, and the camera sits in the player.
    private bool IsOwn(Transform hit, Transform clone) =>
        hit.IsChildOf(clone) || (playerMovementManager != null && hit.IsChildOf(playerMovementManager.transform));

    // The copy's meshes in its own space, before scaling.
    private static Bounds LocalBounds(Transform root)
    {
        var bounds = new Bounds();
        bool found = false;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            Bounds b = renderer.localBounds;
            Matrix4x4 matrix = root.worldToLocalMatrix * renderer.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = matrix.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (!found) { bounds = new Bounds(corner, Vector3.zero); found = true; }
                else bounds.Encapsulate(corner);
            }
        }
        return bounds;
    }

    // The copy flies back to where the item lay, then the item shows again.
    private void PutBack(GameObject clone, GameObject original)
    {
        Transform target = original.transform;
        FlyTo(clone.transform, target.position, target.rotation, target.lossyScale)
            .OnComplete(() =>
            {
                Destroy(clone);
                original.SetActive(true);
            });
    }

    private Sequence FlyTo(Transform t, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return DOTween.Sequence()
            .Join(t.DOMove(position, flyDuration).SetEase(Ease.InOutSine))
            .Join(t.DORotateQuaternion(rotation, flyDuration).SetEase(Ease.InOutSine))
            .Join(t.DOScale(scale, flyDuration).SetEase(Ease.InOutSine));
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    
}
