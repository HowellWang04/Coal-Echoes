using UnityEngine;
using UnityEngine.UI;

public class InspectSystemManager : MonoBehaviour
{
    [Header("Object Refs")]
    [SerializeField] private PlayerInputProvider inputProvider;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CameraSystemManager cameraSystemManager;
    [SerializeField] private PlayerPanelUIManager playerPanel;
    [SerializeField] private InspectUIManager inspectUI;

    [Header("UI Refs")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private RectTransform promptRect;
    [SerializeField] private Image inspectIcon;
    [SerializeField] private Image dotIcon;
    [SerializeField] private float promptHeightOffset = 0.1f;
    [SerializeField] private Vector2 promptScreenOffset = Vector2.zero;


    [Header("Detection Distance")]
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private float dotDistance = 8f;


    [Header("Dot Pulse")]
    [SerializeField] private float dotPulseSpeed = 2f;
    [SerializeField] private float dotMinScale = 0.8f;
    [SerializeField] private float dotMaxScale = 1.2f;



    private struct ItemEntry
    {
        public InspectableItem item;
        public Collider col;
    }

    private ItemEntry[] entries;
    private InspectableItem currentTarget;
    private Vector3 promptWorldPos;
    private RectTransform dotRect;
    private bool showingDot;

    private void Awake()
    {
        if (dotIcon != null) dotRect = dotIcon.GetComponent<RectTransform>();
    }

    private void Start()
    {
        var all = FindObjectsByType<InspectableItem>();
        entries = new ItemEntry[all.Length];
        for (int i = 0; i < all.Length; i++)
            entries[i] = new ItemEntry
            {
                item = all[i],
                col  = all[i].GetComponentInChildren<Collider>()
            };
    }

    private void Update()
    {
        if (!CanDetect())
        {
            HidePrompt();
            return;
        }

        InspectableItem best    = null;
        Collider        bestCol = null;
        float           bestDist = float.MaxValue;

        Vector3 camPos = playerCamera.transform.position;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].item == null) continue;
             if (!entries[i].item.gameObject.activeInHierarchy) continue;
             
            float dist = Vector3.Distance(camPos, entries[i].item.transform.position);
            if (dist > dotDistance) continue;

            Vector3 checkPos = entries[i].col != null ? entries[i].col.bounds.center : entries[i].item.transform.position;
            if (!IsInViewport(checkPos)) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best     = entries[i].item;
                bestCol  = entries[i].col;
            }
        }

        if (best != null)
        {
            currentTarget  = best;
            promptWorldPos = bestCol != null
                ? bestCol.bounds.center + Vector3.up * (bestCol.bounds.extents.y + promptHeightOffset)
                : best.transform.position + Vector3.up * promptHeightOffset;

            bool inInspectRange = bestDist <= maxDistance;
            ShowPrompt(inInspectRange);

            if (inInspectRange
                && inputProvider != null
                && inputProvider.Controls.Gameplay.Interact.WasPressedThisFrame())
            {
                HidePrompt();
                inspectUI.StartInspect(best, playerCamera);
                // Add to backpack using ItemBackpackManager
                ItemBackpackManager.Instance.AddItem(best);
            }
        }
        else
        {
            currentTarget = null;
            HidePrompt();
        }
    }

    private bool IsInViewport(Vector3 worldPos)
    {
        Vector3 vp = playerCamera.WorldToViewportPoint(worldPos);
        return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    private bool CanDetect()
    {
        if (cameraSystemManager != null && cameraSystemManager.CurrentState != CameraModeState.Disabled) return false;
        if (playerPanel != null && playerPanel.IsOpen) return false;
        if (inspectUI != null && inspectUI.IsInspecting) return false;
        return true;
    }

    private void LateUpdate()
    {
        if (promptUI == null || !promptUI.activeSelf || playerCamera == null) return;

        Vector3 screenPos = playerCamera.WorldToScreenPoint(promptWorldPos);
        if (screenPos.z > 0)
            promptRect.position = (Vector2)screenPos + promptScreenOffset;

        if (showingDot && dotRect != null)
        {
            float s = Mathf.Lerp(dotMinScale, dotMaxScale,
                        (Mathf.Sin(Time.time * dotPulseSpeed) + 1f) * 0.5f);
            dotRect.localScale = new Vector3(s, s, 1f);
        }
    }

    private void ShowPrompt(bool inInspectRange)
    {
        if (promptUI != null) promptUI.SetActive(true);
        if (inspectIcon != null) inspectIcon.gameObject.SetActive(inInspectRange);
        if (dotIcon != null)    dotIcon.gameObject.SetActive(!inInspectRange);
        showingDot = !inInspectRange;
    }

    private void HidePrompt()
    {
        if (promptUI != null) promptUI.SetActive(false);
        if (dotRect != null) dotRect.localScale = Vector3.one;
        showingDot  = false;
        currentTarget = null;
    }
}
