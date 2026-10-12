using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-2)]
public class ItemBackpackManager : MonoBehaviour
{
    public static ItemBackpackManager Instance { get; private set; }

    // 每条背包记录：保存物品的数据快照，以及原始 Prefab 引用（用于预览渲染克隆）
    public class BackpackEntry
    {
        public string itemName;
        public string itemDescription;
        public GameObject prefab;          // 原始物体的 GameObject（用于生成预览克隆）
        public float inspectDistance;
        public float inspectScale;
        public Vector3 inspectRotationOffset;
    }

    private readonly List<BackpackEntry> items = new();

    public IReadOnlyList<BackpackEntry> Items => items;

    // 背包内容变化时触发，BackpackTabUI 监听此事件刷新 UI
    public event Action OnBackpackChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 拾取物品时调用。传入场景中的 InspectableItem，记录数据后禁用原物体。
    /// </summary>
    public void AddItem(InspectableItem item)
    {
        var entry = new BackpackEntry
        {
            itemName             = item.itemName,
            itemDescription      = item.itemDescription,
            prefab               = item.gameObject,
            inspectDistance      = item.inspectDistance,
            inspectScale         = item.inspectScale,
            inspectRotationOffset = item.inspectRotationOffset
        };

        items.Add(entry);

        // 禁用场景中的原始物体（保留对象以便存档，不彻底销毁）
        item.gameObject.SetActive(false);

        OnBackpackChanged?.Invoke();
    }

    public int Count => items.Count;
}
