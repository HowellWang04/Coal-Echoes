var folder="Assets/Art Resources/Environment/FactoryRoads/Sample48m";
var shoulder=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(folder+"/Shoulder.mat");shoulder.SetColor("_BaseColor",new UnityEngine.Color(.86f,.85f,.8f,1));shoulder.SetFloat("_NormalScale",.25f);UnityEditor.EditorUtility.SetDirty(shoulder);UnityEditor.AssetDatabase.SaveAssets();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/FactoryRoad48m.prefab"),scene);
var cg=new UnityEngine.GameObject("Sample Camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,scene);
var cam=cg.AddComponent<UnityEngine.Camera>();cam.scene=scene;cam.nearClipPlane=.05f;cam.farClipPlane=200;
var hd=cg.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();hd.volumeLayerMask=1<<31;hd.clearColorMode=UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;hd.backgroundColorHDR=new UnityEngine.Color(.025f,.03f,.035f);
var lg=new UnityEngine.GameObject("Sun");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lg,scene);lg.transform.rotation=UnityEngine.Quaternion.Euler(55,-25,0);
var light=lg.AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Directional;light.lightUnit=UnityEngine.Rendering.LightUnit.Lux;light.intensity=18000;light.shadows=UnityEngine.LightShadows.None;lg.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
var vg=new UnityEngine.GameObject("Exposure");vg.layer=31;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(vg,scene);var v=vg.AddComponent<UnityEngine.Rendering.Volume>();v.isGlobal=true;v.priority=100000;
var profile=UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();v.sharedProfile=profile;
var ex=profile.Add<UnityEngine.Rendering.HighDefinition.Exposure>(true);ex.mode.Override(UnityEngine.Rendering.HighDefinition.ExposureMode.Fixed);ex.fixedExposure.Override(11.5f);
profile.Add<UnityEngine.Rendering.HighDefinition.Bloom>(true).intensity.Override(0);profile.Add<UnityEngine.Rendering.HighDefinition.VisualEnvironment>(true).skyType.Override(0);
var rt=new UnityEngine.RenderTexture(1800,1000,24,UnityEngine.RenderTextureFormat.ARGB32);cam.targetTexture=rt;
var files=new System.Collections.Generic.List<string>();
var renderer=root.transform.Find("Road 6m x 48m").GetComponent<UnityEngine.Renderer>();var dressed=renderer.sharedMaterial;
try{
foreach(var mode in new[]{"overview","plain","eye-level","detail"}){
root.transform.rotation=UnityEngine.Quaternion.identity;
root.transform.Find("Cracks and Repairs").gameObject.SetActive(mode!="plain");renderer.sharedMaterial=mode=="plain"?UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(folder+"/Asphalt48m_Plain.mat"):dressed;
cam.orthographic=false;
if(mode=="overview"||mode=="plain"){root.transform.rotation=UnityEngine.Quaternion.Euler(0,90,0);cam.transform.position=new UnityEngine.Vector3(0,33,-24);cam.transform.LookAt(UnityEngine.Vector3.zero);cam.orthographic=true;cam.orthographicSize=16;}
else if(mode=="eye-level"){cam.transform.position=new UnityEngine.Vector3(.5f,1.68f,-22);cam.transform.LookAt(new UnityEngine.Vector3(0,.1f,-7));cam.fieldOfView=65;}
else{cam.transform.position=new UnityEngine.Vector3(5,5,-22);cam.transform.LookAt(new UnityEngine.Vector3(0,0,-16));cam.fieldOfView=55;}
cam.Render();var prev=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture.active=rt;
var img=new UnityEngine.Texture2D(1800,1000,UnityEngine.TextureFormat.RGB24,false);img.ReadPixels(new UnityEngine.Rect(0,0,1800,1000),0,0);img.Apply();
var file="ArtSource/FactoryRoads/road48m-"+mode+".png";System.IO.File.WriteAllBytes(file,UnityEngine.ImageConversion.EncodeToPNG(img));files.Add(file);UnityEngine.Object.DestroyImmediate(img);UnityEngine.RenderTexture.active=prev;
}return files;
}finally{cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);foreach(var component in profile.components)UnityEngine.Object.DestroyImmediate(component);UnityEngine.Object.DestroyImmediate(profile);}
