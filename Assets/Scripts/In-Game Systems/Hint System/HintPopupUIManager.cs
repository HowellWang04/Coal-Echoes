using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class HintSubtaskData
{
    public string taskId;
    public string text;
}

[Serializable]
public class HintTaskData
{
    public string taskId;
    public string text;
    public HintSubtaskData[] subtasks;
}

[Serializable]
public class HintData
{
    public string hintId;
    public string heading;
    public HintTaskData[] tasks;
}

[Serializable]
public class HintContent
{
    public HintData[] hints;
}

// Top-right hint popup. Shows the hints from the JSON in order, starting when Begin is called;
// when every task of the current hint is done it waits a moment and moves on to the next one.
// A hint never appears while an inner monologue line is showing: the line comes first.
// The current hint and its ticked tasks are saved in GameProgress and come back on Continue.
public sealed class HintPopupUIManager : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private TextAsset hintJson;
    [SerializeField] private InnerMonologueUIManager monologue;

    [Header("UI Refs")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text headingText;
    [SerializeField] private RectTransform rowsRoot;
    [SerializeField] private HintTaskRow rowPrefab;

    [Header("Box Sprites")]
    [SerializeField] private Sprite unfinishedBox;
    [SerializeField] private Sprite finishedBox;

    [Header("Layout")]
    [SerializeField] private int subtaskIndent = 28;
    [SerializeField] private float nextHintDelay = 1.2f;

    private class TaskState
    {
        public HintTaskRow row;
        public bool done;
        public TaskState parent;
        public readonly List<TaskState> children = new();
    }

    private HintData[] hints;
    private int currentIndex = -1;
    private readonly List<TaskState> topTasks = new();
    private readonly Dictionary<string, TaskState> tasksById = new();
    private Coroutine advanceRoutine;

    public string CurrentHintId => currentIndex >= 0 && currentIndex < hints.Length ? hints[currentIndex].hintId : null;
    public bool Finished => currentIndex >= hints.Length;

    // True once the hint with this id has shown, and for every hint after it.
    public bool HasReached(string hintId)
    {
        int index = Array.FindIndex(hints, hint => hint.hintId == hintId);
        return index >= 0 && currentIndex >= index;
    }

    // Only knows the tasks of the hint currently showing.
    public bool IsTaskDone(string taskId) => tasksById.TryGetValue(taskId, out var task) && task.done;

    private void Awake()
    {
        hints = JsonUtility.FromJson<HintContent>(hintJson.text).hints;
        popupRoot.SetActive(false);
    }

    // Shows the first hint, or the saved one. Called once the player has control after the opening.
    public void Begin()
    {
        if (currentIndex >= 0 || advanceRoutine != null) return;
        advanceRoutine = StartCoroutine(ShowWhenReady(Mathf.Max(GameProgress.HintIndex, 0), 0f));
    }

    // Ticks a task of the current hint. Ids from other hints are ignored, so callers can
    // report a condition every frame without caring which hint is showing.
    public void CompleteTask(string taskId)
    {
        if (advanceRoutine != null) return;
        if (!tasksById.TryGetValue(taskId, out var task) || task.done) return;
        if (task.children.Count > 0) return; // A parent task only finishes through its subtasks.

        GameProgress.CompleteTask(taskId);
        MarkDone(task);
        if (task.parent != null && task.parent.children.TrueForAll(child => child.done))
            MarkDone(task.parent);

        if (topTasks.TrueForAll(top => top.done))
            advanceRoutine = StartCoroutine(ShowWhenReady(currentIndex + 1, nextHintDelay));
    }

    private void MarkDone(TaskState task)
    {
        task.done = true;
        task.row.SetDone(finishedBox);
    }

    private IEnumerator ShowWhenReady(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (monologue != null) yield return new WaitUntil(() => !monologue.IsBusy);
        advanceRoutine = null;
        ShowHint(index);
    }

    private void ShowHint(int index)
    {
        ClearRows();
        currentIndex = index;
        var savedTasks = GameProgress.HintIndex == index ? new List<string>(GameProgress.DoneTasks) : null;
        GameProgress.SetHint(index);

        if (index >= hints.Length)
        {
            popupRoot.SetActive(false);
            return;
        }

        var hint = hints[index];
        bool hasHeading = !string.IsNullOrEmpty(hint.heading);
        headingText.gameObject.SetActive(hasHeading);
        headingText.text = hint.heading;

        foreach (var taskData in hint.tasks)
        {
            var task = AddRow(taskData.taskId, taskData.text, 0, null);
            topTasks.Add(task);
            if (taskData.subtasks == null) continue;

            foreach (var subtaskData in taskData.subtasks)
                task.children.Add(AddRow(subtaskData.taskId, subtaskData.text, subtaskIndent, task));
        }

        popupRoot.SetActive(true);

        // Tasks ticked before the game was saved come back ticked; if that finishes the hint, it moves on.
        if (savedTasks == null) return;
        foreach (string taskId in savedTasks) CompleteTask(taskId);
    }

    private TaskState AddRow(string taskId, string text, int indent, TaskState parent)
    {
        var row = Instantiate(rowPrefab, rowsRoot, false);
        row.Setup(text, indent, unfinishedBox);

        var task = new TaskState { row = row, parent = parent };
        tasksById[taskId] = task;
        return task;
    }

    private void ClearRows()
    {
        foreach (var task in tasksById.Values)
            Destroy(task.row.gameObject);
        tasksById.Clear();
        topTasks.Clear();
    }
}
