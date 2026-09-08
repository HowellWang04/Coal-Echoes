using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GrandpaDiaryPageData
{
    public string pageId;
    public int pageIndex;
    public string pageTitle;
    public List<GrandpaDiaryTextBlockData> textBlocks = new();
    public List<GrandpaDiaryStickyNoteSlotData> stickyNoteSlots = new();
}

[Serializable]
public class GrandpaDiaryTextBlockData
{
    public string markerId;

    [TextArea(3, 8)]
    public string content;
}

[Serializable]
public class GrandpaDiaryStickyNoteSlotData
{
    public string slotId;
    public string markerId;
    public string triggerId;
    public string pageSide;

    [TextArea(2, 6)]
    public string noteContent;

    public bool unlockedByDefault;
}

[Serializable]
public class GrandpaDiaryStickyNotePositionOverride
{
    public string pageId;
    public GrandpaDiaryPageSide pageSide;
    public Vector2 anchoredPosition;
}
