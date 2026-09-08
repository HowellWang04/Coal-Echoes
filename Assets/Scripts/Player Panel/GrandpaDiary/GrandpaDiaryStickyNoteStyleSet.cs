using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GrandpaDiaryStickyNoteStyleSet",
    menuName = "Project Coal/Grandpa Diary/Sticky Note Style Set")]
public class GrandpaDiaryStickyNoteStyleSet : ScriptableObject
{
    [Serializable]
    public class Style
    {
        public Sprite sprite;
        public Vector2 textAnchoredPosition;
        public Vector2 textSize;
        public float textRotation;
        public Vector4 textSafeMargins;
    }

    [SerializeField] private Style[] styles = new Style[4];

    public int StyleCount => styles != null ? styles.Length : 0;

    public Style GetStyle(int index)
    {
        if (styles == null || index < 0 || index >= styles.Length)
            return null;

        return styles[index];
    }
}
