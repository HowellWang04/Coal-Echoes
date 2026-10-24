using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One checkbox line in the hint popup. Lives on the row prefab.
public sealed class HintTaskRow : MonoBehaviour
{
    [SerializeField] private Image box;
    [SerializeField] private TMP_Text label;
    [SerializeField] private HorizontalLayoutGroup layout;

    public void Setup(string text, int indent, Sprite unfinishedBox)
    {
        label.text = text;
        layout.padding.left = indent;
        box.sprite = unfinishedBox;
    }

    public void SetDone(Sprite finishedBox)
    {
        box.sprite = finishedBox;
    }
}
