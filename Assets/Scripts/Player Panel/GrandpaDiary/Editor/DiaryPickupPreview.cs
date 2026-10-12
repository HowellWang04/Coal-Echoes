using UnityEditor;

public static class DiaryPickupPreview
{
    [MenuItem("Tools/Coal Echoes/Diary/Preview Pickup Next Play")]
    public static void Prepare()
    {
        SessionState.SetBool(GrandpaDiaryProgress.PickupPreviewKey, true);
        UnityEngine.Debug.Log("The next Play session will start without the diary. This preview does not write diary acquisition to your save.");
    }

    [MenuItem("Tools/Coal Echoes/Diary/Preview Pickup Next Play", true)]
    private static bool CanPrepare() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/Coal Echoes/Diary/Cancel Pickup Preview")]
    public static void Cancel() => SessionState.EraseBool(GrandpaDiaryProgress.PickupPreviewKey);
}
