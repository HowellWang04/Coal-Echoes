using System;
using System.IO;
using UnityEngine;

public static class GrandpaDiaryProgress
{
    [Serializable]
    private class SaveData
    {
        public bool acquired;
    }

    private static bool? acquired;

#if UNITY_EDITOR
    public const string PickupPreviewKey = "ProjectCoal.PreviewDiaryPickupNextPlay";
    private static bool previewAcquisition;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCache()
    {
        acquired = null;
#if UNITY_EDITOR
        previewAcquisition = UnityEditor.SessionState.GetBool(PickupPreviewKey, false);
        UnityEditor.SessionState.EraseBool(PickupPreviewKey);
        if (previewAcquisition) acquired = false;
#endif
    }

    public static bool HasDiary
    {
        get
        {
            if (!acquired.HasValue) acquired = Load(Application.persistentDataPath);
            return acquired.Value;
        }
    }

    public static bool Acquire()
    {
        if (HasDiary) return false;
        Save(Application.persistentDataPath, true);
        acquired = true;
        return true;
    }

    private static bool Load(string root)
    {
        string path = Path.Combine(root, "GrandpaDiary", "Acquisition.json");
        if (File.Exists(path))
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path)).acquired;

        // Existing saves predate diary pickup. Persist fresh false before ordinary photos can be taken.
        bool legacySave = File.Exists(Path.Combine(root, "Echo", "DiscoveredEchoes.json"))
            || File.Exists(Path.Combine(root, "CapturedPhotos", "CapturedPhotosManifest.json"))
            || File.Exists(Path.Combine(root, "ScrapBook", "RegisteredPhotos.json"))
            || File.Exists(Path.Combine(root, "ScrapBook", "CanvasLayout.json"));
        Save(root, legacySave);
        return legacySave;
    }

    private static void Save(string root, bool value)
    {
#if UNITY_EDITOR
        if (previewAcquisition) return;
#endif
        string path = Path.Combine(root, "GrandpaDiary", "Acquisition.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(new SaveData { acquired = value }, true));
    }
}
