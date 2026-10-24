using UnityEngine;

// One tool the player can carry, such as the Lock Pick or the Bolt Cutter.
// itemId is the save key; do not change it once the item ships.
[CreateAssetMenu(fileName = "BackpackItem", menuName = "Coal Echoes/Backpack Item")]
public sealed class BackpackItemDefinition : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string usage;
    [SerializeField] private Sprite icon;

    [Header("Inspect")]
    [SerializeField] private GameObject inspectPrefab;
    [SerializeField] private float inspectDistance = 0.5f;
    [SerializeField] private float inspectScale = 1f;
    [SerializeField] private Vector3 inspectRotationOffset;

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public string Usage => usage;
    public Sprite Icon => icon;
    public GameObject InspectPrefab => inspectPrefab;
    public float InspectDistance => inspectDistance;
    public float InspectScale => inspectScale;
    public Vector3 InspectRotationOffset => inspectRotationOffset;
}
