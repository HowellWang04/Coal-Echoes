using System;
using UnityEngine;

public enum EchoNoteSide { Below, Above, Left, Right }

// Where an Echo's handwritten note sits on its photo in the scrapbook. Set per EchoTrigger,
// saved with each photo in EchoPhotoStorage, so the note keeps its spot wherever the photo goes.
[Serializable]
public class EchoNoteLayout
{
    public EchoNoteSide side = EchoNoteSide.Below;
    [Range(-0.5f, 0.5f)] public float shift = 0.25f; // along that edge, as a fraction of the photo's width or height
    [Range(-15f, 15f)] public float angle = -4f;
}
