using UnityEngine;

public sealed class GrandpaDiaryPickup : MonoBehaviour
{
    [SerializeField] private Transform promptAnchor;

    public bool IsAvailable => !GrandpaDiaryProgress.HasDiary;
    public Vector3 PromptPosition => promptAnchor.position;
    public bool Collect() => GrandpaDiaryProgress.Acquire();
}
