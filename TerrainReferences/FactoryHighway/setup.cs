var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var r=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("TerrainReferences/FactoryHighway/route.json"));
string folder="Assets/Art Resources/Environment/FactoryRoads/Highway/";
foreach(var name in new[]{"clean_asphalt_diff_2k.jpg","clean_asphalt_nor_gl_2k.jpg","CleanAsphalt_Mask.png"}){
 string path=folder+"Textures/"+name;var ti=(TextureImporter)AssetImporter.GetAtPath(path);
 ti.wrapMode=TextureWrapMode.Repeat;ti.maxTextureSize=2048;ti.anisoLevel=8;ti.mipmapEnabled=true;
 ti.sRGBTexture=name.Contains("diff");if(name.Contains("nor_gl"))ti.textureType=TextureImporterType.NormalMap;
 ti.SaveAndReimport();
}
var dirt=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art Resources/Environment/FactoryRoads/DirtSample48m/DirtRoadSpline.mat");
var asphalt=new Material(dirt){name="Clean Asphalt"};
asphalt.SetTexture("_BaseColorMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Textures/clean_asphalt_diff_2k.jpg"));
asphalt.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Textures/clean_asphalt_nor_gl_2k.jpg"));
asphalt.SetTexture("_MaskMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Textures/CleanAsphalt_Mask.png"));
asphalt.SetColor("_BaseColor",Color.white);asphalt.SetFloat("_RoadEdgeWidth",0.35f);asphalt.SetFloat("_RoadEdgeBreakup",0.12f);asphalt.SetFloat("_RoadUvWidth",15.5f/2.1f);asphalt.SetFloat("_DrawOrder",2);asphalt.renderQueue=2002;
AssetDatabase.CreateAsset(asphalt,folder+"CleanAsphalt.mat");
var markings=new System.Collections.Generic.List<Material>();
foreach(var item in new[]{(name:"White Road Marking",color:new Color(.8f,.78f,.7f,1)),(name:"Yellow Road Marking",color:new Color(.9f,.59f,.09f,1))}){
 var mat=new Material(dirt){name=item.name};mat.SetTexture("_BaseColorMap",null);mat.SetTexture("_NormalMap",null);mat.SetTexture("_MaskMap",null);mat.shaderKeywords=new[]{"_MATERIAL_AFFECTS_ALBEDO"};
 mat.SetColor("_BaseColor",item.color);mat.SetFloat("_RoadEdgeWidth",0.025f);mat.SetFloat("_RoadEdgeBreakup",0.01f);mat.SetFloat("_RoadUvWidth",1);mat.SetFloat("_AffectNormal",0);mat.SetFloat("_AffectMetal",0);mat.SetFloat("_AffectSmoothness",0);mat.SetFloat("_DecalColorMask1",0);mat.SetFloat("_DecalColorMask2",0);mat.SetFloat("_DecalColorMask3",0);mat.SetFloat("_DrawOrder",3);mat.renderQueue=2003;
 AssetDatabase.CreateAsset(mat,folder+item.name.Replace(" ","")+".mat");markings.Add(mat);
}
var parent=new GameObject("厂外公路与连接路");Undo.RegisterCreatedObjectUndo(parent,"Create highway");parent.transform.SetParent(GameObject.Find("In-Scene Objects").transform,false);
var go=new GameObject("双向四车道柏油公路");go.transform.SetParent(parent.transform,false);
var gate=new Vector3((float)r["gate"][0],0,(float)r["gate"][1]);var forward=new Vector3((float)r["forward"][0],0,(float)r["forward"][1]);var side=new Vector3((float)r["side"][0],0,(float)r["side"][1]);
go.transform.position=gate+forward*(float)r["offset"];go.transform.rotation=Quaternion.LookRotation(side);
var highway=go.AddComponent<FactoryHighway>();highway.heights=r["heights"].Select(v=>(float)v).ToArray();highway.start=(float)r["start"];highway.spacing=(float)r["spacing"];
go.GetComponent<MeshRenderer>().sharedMaterials=new[]{asphalt,markings[0],markings[1]};highway.Rebuild();
var centers=r["approach"].Select(p=>new Vector3((float)p[0],(float)p[1],(float)p[2])).ToArray();
foreach(float sign in new[]{-1f,1f}){
 var road=new GameObject(sign<0?"厂门连接路向内":"厂门连接路向外");road.transform.SetParent(parent.transform,false);
 road.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
 var container=road.AddComponent<UnityEngine.Splines.SplineContainer>();var spline=container.Spline;
 for(int i=0;i<centers.Length;i++){
  var tangent=i==0?centers[1]-centers[0]:i==centers.Length-1?centers[i]-centers[i-1]:centers[i+1]-centers[i-1];tangent.y=0;
  var across=Vector3.Cross(Vector3.up,tangent).normalized;var point=centers[i]+across*(sign*1.95f);
  spline.Add(new UnityEngine.Splines.BezierKnot(new Unity.Mathematics.float3(point.x,point.y,point.z)),UnityEngine.Splines.TangentMode.AutoSmooth);
 }
 var component=road.AddComponent<FactoryDirtRoadSpline>();component.width=8.5f;component.textureLength=16f;
 component.widthMultiplier=new AnimationCurve(new Keyframe(0,3.5f/8.5f),new Keyframe(.045f,3.5f/8.5f),new Keyframe(.22f,1),new Keyframe(.89f,1),new Keyframe(1,1.6f));
 road.GetComponent<MeshRenderer>().sharedMaterial=dirt;component.Rebuild();
}
foreach(var road in UnityEngine.Object.FindObjectsByType<FactoryDirtRoadSpline>(FindObjectsSortMode.None))road.Rebuild();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return new{root=parent.name,highwayVertices=go.GetComponent<MeshFilter>().sharedMesh.vertexCount,approachKnots=centers.Length};

