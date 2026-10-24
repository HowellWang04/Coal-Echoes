using UnityEngine;
using UnityEngine.Events;

public class InspectableItem : MonoBehaviour
{
    [Header("Info")]
    public string itemName = "Unknown Item";
    [TextArea(2, 4)]
    public string itemDescription = "";
    [TextArea(6, 16)]
    public string readText = "";           // 物品上写的字，例如值班记录本的内容；查看时显示在旁边，留空不显示

    [Header("Inspect Settings")]
    public float inspectDistance = 0.5f;   // 物品在相机前多远
    public float inspectScale = 1f;        // 展示时的缩放倍数
    public Vector3 inspectRotationOffset;  // 初始旋转偏移，相对相机

    [Header("Backpack")]
    public BackpackItemDefinition backpackItem;  // 留空则只查看，不进背包

    [Header("Events")]
    public UnityEvent onInspectEnd;        // 查看结束时触发，例如看完值班记录本播 M5

    private void Start()
    {
        GameSaveManager.SavesCleared += HandleSavesCleared;
        // 读档后已经在背包里的物品不再留在场景中
        if (ItemBackpackManager.Instance != null && ItemBackpackManager.Instance.HasItem(backpackItem))
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GameSaveManager.SavesCleared -= HandleSavesCleared;
    }

    // 新游戏时把读档藏起来的道具放回场景
    private void HandleSavesCleared()
    {
        if (backpackItem != null) gameObject.SetActive(true);
    }
}