public class ScrapBookElementMeta : UnityEngine.MonoBehaviour
{
    public string type; // "photo" / "sticker" / "textbox"
    public string id;   // photo: fileName, sticker/textbox: DecoPreset.presetName
    public int page;    // scrapbook page the element sits on, 0-based
}
