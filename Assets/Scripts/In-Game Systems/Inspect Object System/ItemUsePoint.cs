using UnityEngine;
using UnityEngine.Events;

// A spot where a backpack tool is used with F, such as the guardhouse lock (Lock Pick)
// or the gate chain (Bolt Cutter). InspectSystemManager shows the prompt while the player
// looks at one of this object's colliders and carries the required item.
// With a save id the use is saved; on load onRestored sets the result without replaying it.
public sealed class ItemUsePoint : MonoBehaviour
{
    [SerializeField] private BackpackItemDefinition requiredItem;
    [SerializeField] private string promptText = "Use";
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private string saveId;
    [SerializeField] private UnityEvent onUsed;
    [SerializeField] private UnityEvent onRestored;

    public bool Used { get; private set; }
    // Level1HintFlowManager keeps a level 1 tool locked until its hint shows.
    public bool Allowed { get; set; } = true;
    public bool IsAvailable => Allowed && !Used && ItemBackpackManager.Instance.HasItem(requiredItem);
    public string PromptText => promptText;
    public Vector3 PromptPosition => promptAnchor != null ? promptAnchor.position : transform.position;

    private void Start()
    {
        GameSaveManager.SavesCleared += HandleSavesCleared;
        if (string.IsNullOrEmpty(saveId) || !GameProgress.IsUsed(saveId)) return;
        Used = true;
        onRestored.Invoke();
    }

    private void OnDestroy()
    {
        GameSaveManager.SavesCleared -= HandleSavesCleared;
    }

    // What onRestored set up resets itself, such as SwingDoor closing again.
    private void HandleSavesCleared() => Used = false;

    public void Use()
    {
        if (!IsAvailable) return;
        Used = true;
        if (!string.IsNullOrEmpty(saveId)) GameProgress.MarkUsed(saveId);
        onUsed.Invoke();
    }
}
