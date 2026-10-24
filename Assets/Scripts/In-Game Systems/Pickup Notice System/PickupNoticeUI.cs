using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Bottom-right pickup notices: one row per newly obtained item, a hand icon and the item's name.
// The newest row slides in from the right into the bottom slot and pushes the others up; rows
// leave oldest first from the top, after holdDuration or as soon as more than maxRows are showing.
// Backpack items announce themselves through ItemBackpackManager.OnItemAdded; anything else calls Show.
public sealed class PickupNoticeUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform rowsRoot;
    [SerializeField] private RectTransform rowTemplate;   // inactive child of rowsRoot with a CanvasGroup and a TMP_Text

    [Header("Layout")]
    [SerializeField] private int maxRows = 3;
    [SerializeField] private float rowSpacing = 8f;
    [SerializeField] private float slideDistance = 320f;

    [Header("Timing")]
    [SerializeField] private float arrivalInterval = 0.25f;
    [SerializeField] private float slideInDuration = 0.35f;
    [SerializeField] private float moveUpDuration = 0.2f;
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    private class Row
    {
        public RectTransform rect;
        public CanvasGroup group;
        public int slot;              // 0 is the bottom slot
        public float age;
        public float leaveAge = -1f;  // age when it started fading out; -1 while still showing
        public float yVelocity;
        public bool Leaving => leaveAge >= 0f;
    }

    private readonly List<Row> rows = new();   // oldest first
    private readonly Queue<string> waiting = new();
    private float nextArrivalTime;

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        ItemBackpackManager.Instance.OnItemAdded += ShowBackpackItem;
    }

    private void OnDestroy()
    {
        if (ItemBackpackManager.Instance != null)
            ItemBackpackManager.Instance.OnItemAdded -= ShowBackpackItem;
    }

    // Rows asked for in the same frame still slide in one after another.
    public void Show(string itemName)
    {
        waiting.Enqueue(itemName);
    }

    private void ShowBackpackItem(BackpackItemDefinition item) => Show(item.DisplayName);

    private void Update()
    {
        if (waiting.Count > 0 && Time.unscaledTime >= nextArrivalTime)
        {
            AddRow(waiting.Dequeue());
            nextArrivalTime = Time.unscaledTime + arrivalInterval;
        }

        float slotHeight = rowTemplate.rect.height + rowSpacing;
        float dt = Time.unscaledDeltaTime;
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            var row = rows[i];
            row.age += dt;
            if (!row.Leaving && row.age >= slideInDuration + holdDuration) row.leaveAge = row.age;

            float slideIn = Mathf.Clamp01(row.age / slideInDuration);
            float x = slideDistance * Mathf.Pow(1f - slideIn, 3f);   // ease out
            float y = Mathf.SmoothDamp(row.rect.anchoredPosition.y, row.slot * slotHeight,
                ref row.yVelocity, moveUpDuration, Mathf.Infinity, dt);
            row.rect.anchoredPosition = new Vector2(x, y);

            if (!row.Leaving)
            {
                row.group.alpha = slideIn;
                continue;
            }

            float fade = 1f - (row.age - row.leaveAge) / fadeOutDuration;
            row.group.alpha = fade;
            if (fade > 0f) continue;
            Destroy(row.rect.gameObject);
            rows.RemoveAt(i);
        }
    }

    private void AddRow(string itemName)
    {
        foreach (var row in rows) row.slot++;

        var rect = Instantiate(rowTemplate, rowsRoot, false);
        rect.GetComponentInChildren<TMP_Text>().text = itemName;
        rect.anchoredPosition = new Vector2(slideDistance, 0f);
        var group = rect.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        rect.gameObject.SetActive(true);
        rows.Add(new Row { rect = rect, group = group });

        // More than maxRows showing: the oldest ones leave now instead of waiting out their time.
        int showing = 0;
        foreach (var row in rows)
            if (!row.Leaving) showing++;
        for (int i = 0; showing > maxRows; i++)
        {
            if (rows[i].Leaving) continue;
            rows[i].leaveAge = rows[i].age;
            showing--;
        }
    }
}
