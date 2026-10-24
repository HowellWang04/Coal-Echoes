using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private Sprite interactKeyIcon;   // F
    [SerializeField] private Sprite inspectKeyIcon;    // E
    [SerializeField] private Image dotIcon;
    [SerializeField] private TMP_Text promptText;
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
        HidePrompt();
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

        if (TryShowLookInteraction()) return;

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
            if (IsBlocked(camPos, checkPos, entries[i].item)) continue;

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
            // Tools are picked up with F; items that stay where they are are looked at with E.
            bool pickUp = best.backpackItem != null;
            ShowPrompt(inInspectRange, false, !pickUp);
            var gameplay = inputProvider.Controls.Gameplay;

            if (inInspectRange
                && (pickUp ? gameplay.Interact : gameplay.Inspect).WasPressedThisFrame())
            {
                HidePrompt();
                inspectUI.StartInspect(best, playerCamera);
                // Tools go into the backpack; other inspectables stay where they are.
                if (pickUp)
                {
                    ItemBackpackManager.Instance.AddItem(best.backpackItem);
                    best.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            currentTarget = null;
            HidePrompt();
        }
    }

    // F interactions found by looking straight at them: the diary pickup and tool use points.
    private bool TryShowLookInteraction()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        var pickup = hit.collider.GetComponentInParent<GrandpaDiaryPickup>();
        if (pickup != null && pickup.IsAvailable)
        {
            promptWorldPos = pickup.PromptPosition;
            ShowPrompt(true, true);
            promptText.text = "Get Grandpa's things";
            if (inputProvider.Controls.Gameplay.Interact.WasPressedThisFrame() && pickup.Collect())
            {
                HidePrompt();
                playerPanel.RefreshGrandpaDiaryAvailability();
            }
            return true;
        }

        var usePoint = hit.collider.GetComponentInParent<ItemUsePoint>();
        if (usePoint != null && usePoint.IsAvailable)
        {
            promptWorldPos = usePoint.PromptPosition;
            ShowPrompt(true, true);
            promptText.text = usePoint.PromptText;
            if (inputProvider.Controls.Gameplay.Interact.WasPressedThisFrame())
            {
                HidePrompt();
                usePoint.Use();
            }
            return true;
        }

        return false;
    }

    // Walls and closed doors hide what is behind them, such as the Bolt Cutter in the locked guardhouse.
    private bool IsBlocked(Vector3 from, Vector3 to, InspectableItem item)
    {
        return Physics.Linecast(from, to, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.transform.IsChildOf(item.transform);
    }

    private bool IsInViewport(Vector3 worldPos)
    {
        Vector3 vp = playerCamera.WorldToViewportPoint(worldPos);
        return vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    private bool CanDetect()
    {
        if (!inputProvider.Controls.Gameplay.enabled) return false;
        if (cameraSystemManager != null && cameraSystemManager.CurrentState != CameraModeState.Disabled) return false;
        if (playerPanel != null && playerPanel.IsOpen) return false;
        if (inspectUI != null && (inspectUI.IsInspecting || inspectUI.StoppedThisFrame)) return false;
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

    private void ShowPrompt(bool inInspectRange, bool showText = false, bool inspectKey = false)
    {
        if (promptText != null) promptText.gameObject.SetActive(showText);
        if (promptUI != null) promptUI.SetActive(true);
        if (inspectIcon != null)
        {
            inspectIcon.gameObject.SetActive(inInspectRange);
            inspectIcon.sprite = inspectKey ? inspectKeyIcon : interactKeyIcon;
        }
        if (dotIcon != null)    dotIcon.gameObject.SetActive(!inInspectRange);
        showingDot = !inInspectRange;
    }

    private void HidePrompt()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (promptUI != null) promptUI.SetActive(false);
        if (dotRect != null) dotRect.localScale = Vector3.one;
        showingDot  = false;
        currentTarget = null;
    }
}
