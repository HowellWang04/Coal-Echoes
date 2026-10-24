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
}

[Serializable]
public class GrandpaDiaryTextBlockData
{
    public string markerId;

    [TextArea(3, 8)]
    public string content;
}
