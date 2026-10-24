using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One row in the Backpack tab: icon, name and a short line on what the item is for.
// Clicking the row opens the item model in the inspect view.
public class ItemBackpackSlot : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text usageText;
    [SerializeField] private Button slotButton;

    private BackpackItemDefinition item;
    private InspectUIManager inspectUI;
    private Camera playerCamera;
    private Action onInspectStarted;

    public Button SlotButton => slotButton;

    public void Setup(BackpackItemDefinition data, InspectUIManager inspect, Camera cam, Action onInspectStartedCallback = null)
    {
        item = data;
        inspectUI = inspect;
        playerCamera = cam;
        onInspectStarted = onInspectStartedCallback;

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }
        if (itemNameText != null) itemNameText.text = data.DisplayName;
        if (usageText != null) usageText.text = data.Usage;
        if (slotButton != null) slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        if (inspectUI == null || playerCamera == null) return;
        if (inspectUI.IsInspecting || item.InspectPrefab == null) return;

        onInspectStarted?.Invoke();
        inspectUI.StartInspect(item, playerCamera);
    }
}
