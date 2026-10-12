using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// One canvas element's saved state.
[Serializable]
public class ScrapBookElementData
{
    public string type;      // "photo" / "sticker" / "textbox"
    public string id;        // photo: fileName, sticker/textbox: DecoPreset.presetName
    public Vector2 position; // anchoredPosition
    public float rotation;   // Z axis angle
    public float scale;      // uniform scale
    public string text;      // textbox only, empty otherwise
}

// Wrapper for JsonUtility (it can't serialize a bare List).
[Serializable]
public class ScrapBookPhotoManifest
{
    public List<string> fileNames = new();
}

[Serializable]
public class ScrapBookCanvasData
{
    public List<ScrapBookElementData> elements = new();
}

[DefaultExecutionOrder(-2)]
public class ScrapBookDataManager : MonoBehaviour
{
    public static ScrapBookDataManager Instance { get; private set; }

    // Photo entries: stored by file name (shared with CameraPhotoStorage)
    private readonly List<string> photos = new();
    public IReadOnlyList<string> Photos => photos;

    // Decoration entries: stored by decoration identifier
    private readonly List<string> decorations = new();
    public IReadOnlyList<string> Decorations => decorations;

    // UI refresh events
    public event Action OnPhotosChanged;
    public event Action OnDecorationsChanged;

    private static string SaveDir => Path.Combine(Application.persistentDataPath, "ScrapBook");
    private static string PhotosPath => Path.Combine(SaveDir, "RegisteredPhotos.json");
    private static string CanvasPath => Path.Combine(SaveDir, "CanvasLayout.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadPhotos();
    }

    // Add a photo to ScrapBook by file name (consistent with CameraPhotoStorage).
    public bool RegisterPhoto(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        if (photos.Contains(fileName)) return false; // Prevent duplicate entries

        photos.Add(fileName);
        SavePhotos();
        OnPhotosChanged?.Invoke();
        return true;
    }

    public void RemovePhoto(string fileName)
    {
        if (photos.Remove(fileName))
        {
            SavePhotos();
            OnPhotosChanged?.Invoke();
        }
    }

    public void AddDecoration(string decorationId)
    {
        if (string.IsNullOrEmpty(decorationId)) return;

        decorations.Add(decorationId);
        OnDecorationsChanged?.Invoke();
    }

    public void RemoveDecoration(string decorationId)
    {
        if (decorations.Remove(decorationId))
            OnDecorationsChanged?.Invoke();
    }

    // --- Persistence ---

    private void SavePhotos()
    {
        Directory.CreateDirectory(SaveDir);

        var manifest = new ScrapBookPhotoManifest { fileNames = new List<string>(photos) };
        File.WriteAllText(PhotosPath, JsonUtility.ToJson(manifest, true));
    }

    private void LoadPhotos()
    {
        if (!File.Exists(PhotosPath)) return;

        var manifest = JsonUtility.FromJson<ScrapBookPhotoManifest>(File.ReadAllText(PhotosPath));
        if (manifest == null) return;

        photos.Clear();
        photos.AddRange(manifest.fileNames);
        OnPhotosChanged?.Invoke();
    }

    public void SaveCanvas(List<ScrapBookElementData> elements)
    {
        Directory.CreateDirectory(SaveDir);

        var data = new ScrapBookCanvasData { elements = elements };
        File.WriteAllText(CanvasPath, JsonUtility.ToJson(data, true));
    }

    public List<ScrapBookElementData> LoadCanvas()
    {
        if (!File.Exists(CanvasPath)) return new List<ScrapBookElementData>();

        var data = JsonUtility.FromJson<ScrapBookCanvasData>(File.ReadAllText(CanvasPath));
        return data?.elements ?? new List<ScrapBookElementData>();
    }
}
