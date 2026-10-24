using UnityEngine;

// The motorcycle's storage box. One F press hands over Grandpa's Diary, the Group Photo
// (into the Photo Album) and the Lock Pick (into the backpack), and each new one gets a
// pickup notice in that order. It stays available until all three are collected, so saves
// that already had the diary can still pick up the rest.
public sealed class GrandpaDiaryPickup : MonoBehaviour
{
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private BackpackItemDefinition lockPick;
    [SerializeField] private Texture2D groupPhoto;   // needs Read/Write for the album to save it
    [SerializeField] private CameraPhotoAlbumUIManager photoAlbum;
    [SerializeField] private PickupNoticeUI pickupNotices;
    [SerializeField] private string diaryName = "Grandpa's Diary";
    [SerializeField] private string groupPhotoName = "Group Photo";

    public bool IsAvailable => !GrandpaDiaryProgress.HasDiary
        || !ItemBackpackManager.Instance.HasItem(lockPick)
        || !photoAlbum.HasPhoto(Level1HintFlowManager.GroupPhotoFileName);
    public Vector3 PromptPosition => promptAnchor.position;

    // Returns true when the diary itself was new.
    public bool Collect()
    {
        bool diaryIsNew = GrandpaDiaryProgress.Acquire();
        if (diaryIsNew) pickupNotices.Show(diaryName);
        if (photoAlbum.AddPhotoFromTexture(groupPhoto, Level1HintFlowManager.GroupPhotoFileName) != null)
            pickupNotices.Show(groupPhotoName);
        ItemBackpackManager.Instance.AddItem(lockPick);   // the notice comes through OnItemAdded
        return diaryIsNew;
    }
}
