using System;
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

    [Header("Inspect Layer")]
    [SerializeField] private int inspectLayer = 6;

    public bool IsInspecting { get; private set; }
    public event Action OnInspectStopped;

    private GameObject inspectClone;
    private Camera currentCamera;
    private InspectableItem currentItem;

    private CursorLockMode prevLockState;
    private bool prevCursorVisible;

    public void StartInspect(InspectableItem item, Camera cam)
    {
        if (IsInspecting) return;

        IsInspecting = true;
        currentItem = item;
        currentCamera = cam;

        prevLockState = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        if (playerMovementManager != null) playerMovementManager.SetPlayerMovementControl(false);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        if (inspectPanel != null) inspectPanel.SetActive(true);
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = item.itemDescription;

        // Spawn clone
        SpawnClone(item);
    }

    public void StopInspect()
    {
        if (!IsInspecting) return;
        IsInspecting = false;

        if (inspectClone != null)
        {
            var rotator = inspectClone.GetComponent<InspectRotator>();
            if (rotator != null) rotator.Deactivate();
            Destroy(inspectClone);
            inspectClone = null;
        }

        if (inspectPanel != null) inspectPanel.SetActive(false);
        if (playerMovementManager != null) playerMovementManager.SetPlayerMovementControl(true);
        Cursor.lockState = prevLockState;
        Cursor.visible = prevCursorVisible;

        currentItem = null;
        currentCamera = null;

        OnInspectStopped?.Invoke();
    }

    private void Update()
    {
        if (!IsInspecting) return;

        if (inputProvider != null
            && inputProvider.Controls.Gameplay.Cancel.WasPressedThisFrame())
            StopInspect();
    }

    private void SpawnClone(InspectableItem item)
    {
        Vector3 spawnPos = currentCamera.transform.position
            + currentCamera.transform.forward * item.inspectDistance;

        inspectClone = Instantiate(item.gameObject, spawnPos, Quaternion.identity);
        inspectClone.SetActive(true);
        inspectClone.transform.localScale = item.transform.lossyScale * item.inspectScale;
        inspectClone.transform.rotation = Quaternion.Euler(item.inspectRotationOffset);

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

        var rigidbodies = inspectClone.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies) Destroy(rb);

        // Add rotation control
        var rotator = inspectClone.AddComponent<InspectRotator>();
        rotator.Activate(currentCamera, item.inspectDistance, inputProvider);
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    
}
