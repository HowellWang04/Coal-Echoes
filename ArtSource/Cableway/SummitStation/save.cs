var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
var station=GameObject.Find("CablewaySummitStation");
if(station==null || GameObject.Find("CablewayTower_11")!=null)throw new Exception("Station replacement incomplete.");
foreach(var name in new[]{"before.png","after.png","return-wheel.png"}) {
    var source="Assets/ArtSource/Cableway/SummitStation/"+name;
    if(System.IO.File.Exists(source)) {
        System.IO.File.Copy(source,"ArtSource/Cableway/SummitStation/"+name,true);
        AssetDatabase.DeleteAsset(source);
    }
}
foreach(var folder in new[]{"Assets/ArtSource/Cableway/SummitStation","Assets/ArtSource/Cableway","Assets/ArtSource"})
    if(System.IO.Directory.Exists(folder) && System.IO.Directory.GetFileSystemEntries(folder).Length==0) AssetDatabase.DeleteAsset(folder);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
Selection.activeGameObject=station;
var view=SceneView.lastActiveSceneView;
view.camera.ResetAspect();view.camera.ResetProjectionMatrix();view.Repaint();
return new {scene=scene.path,saved=!scene.isDirty,selected=station.name,playing=EditorApplication.isPlaying};
