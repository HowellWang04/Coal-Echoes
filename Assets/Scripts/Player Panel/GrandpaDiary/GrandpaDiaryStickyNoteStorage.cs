using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class GrandpaDiaryUnlockedStickyNoteManifest
{
    public List<string> unlockedSlotIds = new();
    public List<GrandpaDiaryStickyNoteStyleAssignment> styleAssignments = new();
}

[Serializable]
public class GrandpaDiaryStickyNoteStyleAssignment
{
    public string slotId;
    public int styleIndex;
}

public static class GrandpaDiaryStickyNoteStorage
{
    private static string Dir => Path.Combine(Application.persistentDataPath, "GrandpaDiary");
    private static string ManifestPath => Path.Combine(Dir, "UnlockedStickyNotes.json");

    private static GrandpaDiaryUnlockedStickyNoteManifest LoadManifest()
    {
        if (!File.Exists(ManifestPath))
            return new GrandpaDiaryUnlockedStickyNoteManifest();

        var manifest = JsonUtility.FromJson<GrandpaDiaryUnlockedStickyNoteManifest>(File.ReadAllText(ManifestPath));
        if (manifest == null)
            return new GrandpaDiaryUnlockedStickyNoteManifest();

        manifest.unlockedSlotIds ??= new List<string>();
        manifest.styleAssignments ??= new List<GrandpaDiaryStickyNoteStyleAssignment>();
        return manifest;
    }

    private static void SaveManifest(GrandpaDiaryUnlockedStickyNoteManifest manifest)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true));
    }

    public static List<string> LoadUnlockedSlotIds()
    {
        return new List<string>(LoadManifest().unlockedSlotIds);
    }

    public static bool IsUnlocked(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
            return false;

        return LoadManifest().unlockedSlotIds.Contains(slotId);
    }

    public static void Unlock(string slotId)
    {
        if (string.IsNullOrEmpty(slotId))
            return;

        var manifest = LoadManifest();
        if (manifest.unlockedSlotIds.Contains(slotId))
            return;

        manifest.unlockedSlotIds.Add(slotId);
        SaveManifest(manifest);
    }

    public static int GetOrCreateStyleIndex(string slotId, int styleCount)
    {
        if (string.IsNullOrEmpty(slotId) || styleCount <= 0)
            return 0;

        var manifest = LoadManifest();
        var assignment = manifest.styleAssignments.Find(item => item.slotId == slotId);
        if (assignment != null && assignment.styleIndex >= 0 && assignment.styleIndex < styleCount)
            return assignment.styleIndex;

        int styleIndex = UnityEngine.Random.Range(0, styleCount);
        if (assignment == null)
        {
            assignment = new GrandpaDiaryStickyNoteStyleAssignment { slotId = slotId };
            manifest.styleAssignments.Add(assignment);
        }

        assignment.styleIndex = styleIndex;
        SaveManifest(manifest);
        return styleIndex;
    }
}
