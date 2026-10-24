using System;
using System.IO;
using UnityEngine;

// Starts a new game in place: deletes every save, drops the cached copies and tells every
// system that loaded a save to go back to its new-game state. Only the main menu calls it,
// before anything has been played this session, so loaded saves are all there is to undo.
public static class GameSaveManager
{
    private static readonly string[] SaveFolders =
        { "CapturedPhotos", "ScrapBook", "Echo", "GrandpaDiary", "Backpack", "Progress" };

    // Systems that keep a loaded save in memory reset themselves here.
    public static event Action SavesCleared;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvent() => SavesCleared = null;

    // Saves from before progress was recorded still count once they hold the diary.
    public static bool HasSave => GameProgress.Exists || GrandpaDiaryProgress.HasDiary;

    public static void ClearSaves()
    {
        foreach (string folder in SaveFolders)
        {
            string path = Path.Combine(Application.persistentDataPath, folder);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        GrandpaDiaryProgress.ResetCache();
        EchoDiscoveryStorage.ResetCache();
        GameProgress.ResetCache();
        SavesCleared?.Invoke();
    }
}
