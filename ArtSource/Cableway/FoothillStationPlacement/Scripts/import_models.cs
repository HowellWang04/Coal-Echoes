var basePath="Assets/Art Resources/Environment/Cableway/";
var names=new[]{"CablewayStationShell","CablewayStationRollerFrame"};
if(UnityEngine.Shader.Find("HDRP/Lit")==null)throw new System.Exception("HDRP Lit shader not found");
foreach(var name in names){
 var dest=basePath+name;System.IO.Directory.CreateDirectory(dest+"/Textures");System.IO.Directory.CreateDirectory(dest+"/Materials");
 System.IO.File.Copy("ArtSource/Cableway/"+name+"/Exports/"+name+".fbx",dest+"/"+name+".fbx",true);
 foreach(var path in System.IO.Directory.GetFiles("ArtSource/Cableway/"+name+"/Textures","*.png"))if(path.EndsWith("_BaseColor.png")||path.EndsWith("_Normal.png")||path.EndsWith("_HDRP_Mask.png"))System.IO.File.Copy(path,dest+"/Textures/"+System.IO.Path.GetFileName(path),true);
}
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var reports=new System.Collections.Generic.List<object>();
foreach(var name in names){
 var dest=basePath+name;
 foreach(var path in System.IO.Directory.GetFiles(dest+"/Textures","*.png")){
  var imp=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);imp.maxTextureSize=2048;imp.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;
  imp.sRGBTexture=path.EndsWith("_BaseColor.png");if(path.EndsWith("_Normal.png"))imp.textureType=UnityEditor.TextureImporterType.NormalMap;imp.SaveAndReimport();
 }
 var mats=name=="CablewayStationShell"?new[]{"Station_Steel","Station_Cladding","Station_Concrete"}:new[]{"RollerFrame_Steel","RollerFrame_DarkSteel","RollerFrame_Concrete","RollerFrame_OxidizedEdges"};
 foreach(var matName in mats){
  var path=dest+"/Materials/"+matName+".mat";var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
  if(mat==null){mat=new UnityEngine.Material(UnityEngine.Shader.Find("HDRP/Lit"));UnityEditor.AssetDatabase.CreateAsset(mat,path);}
  mat.SetColor("_BaseColor",UnityEngine.Color.white);mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",1);
  if(matName=="RollerFrame_OxidizedEdges"){mat.SetColor("_BaseColor",new UnityEngine.Color(.13f,.065f,.025f));mat.SetFloat("_Metallic",.25f);mat.SetFloat("_Smoothness",.14f);}
  else{
   mat.SetTexture("_BaseColorMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(dest+"/Textures/"+matName+"_BaseColor.png"));
   mat.SetTexture("_NormalMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(dest+"/Textures/"+matName+"_Normal.png"));
   mat.SetTexture("_MaskMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(dest+"/Textures/"+matName+"_HDRP_Mask.png"));
   mat.SetFloat("_NormalMapSpace",0);mat.SetFloat("_NormalScale",1);
  }
  UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(mat);UnityEditor.EditorUtility.SetDirty(mat);UnityEditor.AssetDatabase.SaveAssetIfDirty(mat);
 }
 var fbx=dest+"/"+name+".fbx";var model=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);
 model.globalScale=1;model.useFileScale=true;model.importAnimation=false;model.importCameras=false;model.importLights=false;
 foreach(var matName in mats)model.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),matName),UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(dest+"/Materials/"+matName+".mat"));
 model.SaveAndReimport();
 var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(fbx);
 reports.Add(new{name,scale=prefab.transform.localScale.ToString(),renderers=prefab.GetComponentsInChildren<UnityEngine.Renderer>(true).Select(r=>new{name=r.name,materials=r.sharedMaterials.Select(m=>m==null?null:UnityEditor.AssetDatabase.GetAssetPath(m)).ToArray()}).ToArray()});
}
System.IO.File.WriteAllText("ArtSource/Cableway/FoothillStationPlacement/import_report.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
return reports;
