using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class CameraPhotoManifest
{
    public List<string> fileNames = new();
}

public static class CameraPhotoStorage
{
    public static string PhotosDir => Path.Combine(Application.persistentDataPath, "CapturedPhotos");
    public static string ManifestPath => Path.Combine(PhotosDir, "CapturedPhotosManifest.json");

    // Creates the necessary directories if they do not exist
    public static void EnsureDirs()
    {
        Directory.CreateDirectory(PhotosDir);
    }

    // Loads the manifest (list of file names) from disk
    public static List<string> LoadManifest()
    {
        EnsureDirs();

        if (!File.Exists(ManifestPath))
            return new List<string>();

        string json = File.ReadAllText(ManifestPath);
        var m = JsonUtility.FromJson<CameraPhotoManifest>(json);
        return m?.fileNames ?? new List<string>();
    }

    // Saves the manifest (list of file names) to disk
    public static void SaveManifest(List<string> fileNames)
    {
        EnsureDirs();
        var m = new CameraPhotoManifest { fileNames = fileNames };
        string json = JsonUtility.ToJson(m, true);
        File.WriteAllText(ManifestPath, json);
    }

    // Saves the Texture2D as a PNG file into the CapturedPhotos directory and returns the file name.
    // Captured photos get a timestamped name; story photos such as the Group Photo pass a fixed one.
    public static string SavePhotoPng(Texture2D tex, string fileName = null)
    {
        EnsureDirs();

        fileName ??= $"CapturedPhoto_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
        string path = Path.Combine(PhotosDir, fileName);
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        return fileName;
    }

    public static Texture2D LoadTexture(string fileName)
    {
        string path = Path.Combine(PhotosDir, fileName);
        if (!File.Exists(path)) return null;

        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        tex.Apply();
        return tex;
    }
}
