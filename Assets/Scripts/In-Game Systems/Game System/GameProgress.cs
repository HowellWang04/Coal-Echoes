using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Level progress the other saves do not cover: the hint showing and its ticked tasks,
// the inner monologue lines already played, and the tool use points already used.
public static class GameProgress
{
    [Serializable]
    private class SaveData
    {
        public int hintIndex = -1;
        public List<string> doneTasks = new();
        public List<string> playedMonologues = new();
        public List<string> usedPoints = new();
    }

    private static string FilePath => Path.Combine(Application.persistentDataPath, "Progress", "Progress.json");
    private static SaveData data;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetCache() => data = null;

    public static bool Exists => File.Exists(FilePath);

    // -1 until the first hint has shown.
    public static int HintIndex => Data.hintIndex;
    public static IReadOnlyList<string> DoneTasks => Data.doneTasks;
    public static IReadOnlyList<string> PlayedMonologues => Data.playedMonologues;
    public static bool IsUsed(string pointId) => Data.usedPoints.Contains(pointId);

    public static void SetHint(int index)
    {
        if (Data.hintIndex == index) return;
        data.hintIndex = index;
        data.doneTasks.Clear();
        Save();
    }

    public static void CompleteTask(string taskId) => AddAndSave(Data.doneTasks, taskId);
    public static void MarkPlayed(string lineId) => AddAndSave(Data.playedMonologues, lineId);
    public static void MarkUsed(string pointId) => AddAndSave(Data.usedPoints, pointId);

    private static SaveData Data
    {
        get
        {
            data ??= File.Exists(FilePath)
                ? JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) : new SaveData();
            return data;
        }
    }

    private static void AddAndSave(List<string> list, string id)
    {
        if (list.Contains(id)) return;
        list.Add(id);
        Save();
    }

    private static void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
    }
}
