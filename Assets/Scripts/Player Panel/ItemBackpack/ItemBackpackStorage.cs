using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Saves the ids of the items in the backpack, in the order they were collected.
public static class ItemBackpackStorage
{
    [Serializable]
    private class Manifest
    {
        public List<string> itemIds = new();
    }

    private static string FilePath => Path.Combine(Application.persistentDataPath, "Backpack", "BackpackItems.json");

    public static List<string> Load()
    {
        if (!File.Exists(FilePath)) return new List<string>();
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(FilePath));
        return manifest?.itemIds ?? new List<string>();
    }

    public static void Save(List<string> itemIds)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, JsonUtility.ToJson(new Manifest { itemIds = itemIds }, true));
    }
}
