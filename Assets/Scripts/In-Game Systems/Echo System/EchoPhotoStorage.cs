using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

// Summary:
// Maps a saved photo's fileName to the Echo scrapbook texts it captured.
// One JSON file under persistentDataPath, mirroring CameraPhotoStorage.

[Serializable]
public class EchoPhotoEntry
{
    public string fileName;
    [FormerlySerializedAs("diaryContents")]
    public List<string> scrapbookContents = new();
    public EchoNoteLayout noteLayout = new(); // entries saved before this field load with the default layout
}

[Serializable]
public class EchoPhotoManifest
{
    public List<EchoPhotoEntry> entries = new();
}

public static class EchoPhotoStorage
{
    private static string Dir => Path.Combine(Application.persistentDataPath, "Echo");
    private static string ManifestPath => Path.Combine(Dir, "EchoPhotos.json");

    private static EchoPhotoManifest LoadManifest()
    {
        if (!File.Exists(ManifestPath)) return new EchoPhotoManifest();

        var m = JsonUtility.FromJson<EchoPhotoManifest>(File.ReadAllText(ManifestPath));
        return m ?? new EchoPhotoManifest();
    }

    private static void SaveManifest(EchoPhotoManifest manifest)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true));
    }

    // Record the Echo scrapbook texts a photo captured and where its note sits.
    public static void Save(string fileName, List<string> scrapbookContents, EchoNoteLayout noteLayout)
    {
        if (string.IsNullOrEmpty(fileName) || scrapbookContents == null || scrapbookContents.Count == 0) return;

        var manifest = LoadManifest();

        var entry = manifest.entries.Find(e => e.fileName == fileName);
        if (entry == null)
        {
            entry = new EchoPhotoEntry { fileName = fileName };
            manifest.entries.Add(entry);
        }
        entry.scrapbookContents = new List<string>(scrapbookContents);
        entry.noteLayout = noteLayout;

        SaveManifest(manifest);
    }

    // Get the Echo scrapbook texts for a photo (empty list if none).
    public static List<string> Load(string fileName)
    {
        var entry = Find(fileName);
        return entry != null ? entry.scrapbookContents : new List<string>();
    }

    // Get a photo's whole Echo entry, or null if it carries no Echo.
    public static EchoPhotoEntry Find(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;

        var entry = LoadManifest().entries.Find(e => e.fileName == fileName);
        return entry != null && entry.scrapbookContents.Count > 0 ? entry : null;
    }
}
