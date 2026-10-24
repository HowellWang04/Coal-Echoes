using UnityEngine;
using UnityEngine.UI;

public class ItemBackpackUIManager : MonoBehaviour
{
    [Header("Slot Refs")]
    [SerializeField] private ItemBackpackSlot slotPrefab;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject emptyHint;

    [Header("Inspect Refs")]
    [SerializeField] private InspectUIManager inspectUI;
    [SerializeField] private Camera playerCamera;

    [Header("Scroll Refs")]
    [SerializeField] private ScrollRect scrollRect;

    private UIAnimationScope uiAnimationScope;

    private void Awake()
    {
        uiAnimationScope = GetComponentInParent<UIAnimationScope>();
    }

    private void OnEnable()
    {
        if (ItemBackpackManager.Instance != null)
            ItemBackpackManager.Instance.OnBackpackChanged += RefreshUI;

        if (inspectUI != null)
            inspectUI.OnInspectStopped += EnableScroll;

        RefreshUI();
    }

    private void OnDisable()
    {
        if (ItemBackpackManager.Instance != null)
            ItemBackpackManager.Instance.OnBackpackChanged -= RefreshUI;

        if (inspectUI != null)
            inspectUI.OnInspectStopped -= EnableScroll;
    }

    public void DisableScroll()
    {
        if (scrollRect != null) scrollRect.enabled = false;
    }

    private void EnableScroll()
    {
        if (scrollRect != null) scrollRect.enabled = true;
    }

    private void RefreshUI()
    {
        if (slotContainer == null || slotPrefab == null) return;

        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);

        int count = ItemBackpackManager.Instance != null ? ItemBackpackManager.Instance.Count : 0;
        if (emptyHint != null) emptyHint.SetActive(count == 0);
        if (count == 0) return;

        foreach (var item in ItemBackpackManager.Instance.Items)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.Setup(item, inspectUI, playerCamera, DisableScroll);
            if (uiAnimationScope != null && slot.SlotButton != null)
                uiAnimationScope.RegisterButton(slot.SlotButton);
        }
    }
}
