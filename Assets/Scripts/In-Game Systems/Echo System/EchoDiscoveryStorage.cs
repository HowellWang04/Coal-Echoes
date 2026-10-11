using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EchoDiscoveryStorage
{
    [Serializable]
    private class Manifest
    {
        public List<string> echoIds = new();
    }

    private static string FilePath => Path.Combine(Application.persistentDataPath, "Echo", "DiscoveredEchoes.json");
    private static HashSet<string> discovered;
    private static HashSet<string> legacySlots;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { discovered = null; legacySlots = null; }

    private static void EnsureLoaded()
    {
        if (discovered != null) return;
        var manifest = File.Exists(FilePath)
            ? JsonUtility.FromJson<Manifest>(File.ReadAllText(FilePath)) : new Manifest();
        discovered = new HashSet<string>(manifest.echoIds);
        legacySlots = new HashSet<string>(GrandpaDiaryStickyNoteStorage.LoadUnlockedSlotIds());
    }

    public static bool IsDiscovered(EchoTrigger echo)
    {
        EnsureLoaded();
        return discovered.Contains(echo.EchoId)
            || (!string.IsNullOrEmpty(echo.stickyNoteSlotId) && legacySlots.Contains(echo.stickyNoteSlotId));
    }

    public static bool Record(EchoTrigger echo)
    {
        bool firstDiscovery = !IsDiscovered(echo);
        if (discovered.Contains(echo.EchoId)) return firstDiscovery;
        var ids = new List<string>(discovered) { echo.EchoId };
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, JsonUtility.ToJson(new Manifest { echoIds = ids }, true));
        discovered.Add(echo.EchoId);
        return firstDiscovery;
    }
}
