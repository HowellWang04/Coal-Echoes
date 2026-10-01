var guids = new [] { "abc00000000003131122131253365392", "abc00000000006068067014534057388", "e732f41246f40fb41a93356b5094542a", "4909fab81b83d354bb1b10a1a7c6a397", "016d55e65b496bd4fa8b0bb494b5e99c", "a1d09dd92e2f80b45bbc9b895ca14d15", "dca1f35398e508747b9808668e609956", "3ab13016242ff3d45ad4f9432b51c521" };
var paths = guids.Select(g => UnityEditor.AssetDatabase.GUIDToAssetPath(g)).ToArray();
if (paths.Any(p => string.IsNullOrEmpty(p) || !p.StartsWith("Assets/Art Resources/ThirdPartyPacks/Industrial_Harbour/"))) throw new System.Exception("Unexpected deletion path");
var targets = new System.Collections.Generic.HashSet<string>(paths);
var users = new System.Collections.Generic.List<object>();
int scanned = 0;
foreach (var path in UnityEditor.AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !UnityEditor.AssetDatabase.IsValidFolder(p) && !targets.Contains(p))) {
    scanned++;
    var hits = UnityEditor.AssetDatabase.GetDependencies(path, false).Where(targets.Contains).ToArray();
    if (hits.Length > 0) users.Add(new { path, dependencies = hits });
}
if (users.Count > 0) { System.IO.File.WriteAllText("ArtSource/Cleanup/RailRemoval/remaining_dependencies.json", Newtonsoft.Json.JsonConvert.SerializeObject(users, Newtonsoft.Json.Formatting.Indented)); return new { deleted = false, scanned, users }; }
var deleted = new System.Collections.Generic.List<object>();
for (int i = 0; i < paths.Length; i++) {
    var success = UnityEditor.AssetDatabase.DeleteAsset(paths[i]);
    deleted.Add(new { path = paths[i], guid = guids[i], success });
    if (!success) throw new System.Exception("Deletion failed " + paths[i]);
}
var report = new { deleted = true, scanned, assets = deleted, preserved = new [] { "M_DirtRails_3.mat", "T_DirtRails_B.PNG" } };
System.IO.File.WriteAllText("ArtSource/Cleanup/RailRemoval/asset_removal.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return report;
