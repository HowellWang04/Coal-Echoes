using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-2)]
public class ItemBackpackManager : MonoBehaviour
{
    public static ItemBackpackManager Instance { get; private set; }

    // 所有可能进背包的物品，读档时按 itemId 找回对应资源
    [SerializeField] private List<BackpackItemDefinition> catalog = new();

    private readonly List<BackpackItemDefinition> items = new();

    public IReadOnlyList<BackpackItemDefinition> Items => items;

    // 背包内容变化时触发，ItemBackpackUIManager 监听此事件刷新 UI
    public event Action OnBackpackChanged;

    // 新获得物品时触发，读档恢复时不触发；获得提示和任务提示可以监听
    public event Action<BackpackItemDefinition> OnItemAdded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (var itemId in ItemBackpackStorage.Load())
        {
            var item = catalog.Find(entry => entry != null && entry.ItemId == itemId);
            if (item != null) items.Add(item);
            else Debug.LogWarning($"Backpack save has unknown item '{itemId}'.", this);
        }
        GameSaveManager.SavesCleared += HandleSavesCleared;
    }

    private void OnDestroy()
    {
        GameSaveManager.SavesCleared -= HandleSavesCleared;
    }

    private void HandleSavesCleared()
    {
        items.Clear();
        OnBackpackChanged?.Invoke();
    }

    public bool HasItem(BackpackItemDefinition item) => item != null && items.Contains(item);

    public bool HasItem(string itemId) => items.Exists(item => item.ItemId == itemId);

    /// <summary>
    /// 把物品放进背包并存档。已经有了就不重复添加，返回 false。
    /// </summary>
    public bool AddItem(BackpackItemDefinition item)
    {
        if (item == null || items.Contains(item)) return false;
        if (!catalog.Contains(item))
            Debug.LogWarning($"{item.name} is not in the backpack catalog and will not load from the save.", this);

        items.Add(item);
        ItemBackpackStorage.Save(items.ConvertAll(entry => entry.ItemId));

        OnItemAdded?.Invoke(item);
        OnBackpackChanged?.Invoke();
        return true;
    }

    public int Count => items.Count;
}
