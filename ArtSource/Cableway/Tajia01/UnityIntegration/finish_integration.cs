var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
var guid = UnityEditor.AssetDatabase.AssetPathToGUID(scene.path);
var records = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("ArtSource/Cableway/Tajia01/UnityIntegration/preview_distances_before.json"));
foreach (var record in records)
{
    UnityEditor.GlobalObjectId id;
    UnityEditor.GlobalObjectId.TryParse("GlobalObjectId_V1-2-" + guid + "-" + (string)record["id"] + "-0", out id);
    var tile = (MapMagic.Terrains.TerrainTile)UnityEditor.GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
    tile.distance = (float)record["distance"];
    UnityEditor.EditorUtility.SetDirty(tile);
}
var saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
foreach (var name in new[] { "Scene_Before.png", "Scene_After.png" })
    UnityEditor.AssetDatabase.DeleteAsset("Assets/ArtSource/Cableway/Tajia01/UnityIntegration/" + name);
foreach (var dir in new[] { "Assets/ArtSource/Cableway/Tajia01/UnityIntegration", "Assets/ArtSource/Cableway/Tajia01", "Assets/ArtSource/Cableway", "Assets/ArtSource" })
    if (System.IO.Directory.Exists(dir) && System.IO.Directory.GetFileSystemEntries(dir).Length == 0)
        UnityEditor.AssetDatabase.DeleteAsset(dir);
return new { saved, restoredPreviewDistances = records.Count };
