using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Reports clicks on a sidebar tab button while it is locked, so the panel can explain why
// nothing opens. A non-interactable Button swallows the click without telling anyone.
[RequireComponent(typeof(Button))]
public sealed class SidebarLockedClick : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!button.interactable) Clicked?.Invoke();
    }
}
