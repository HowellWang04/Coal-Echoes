using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class OldPhotoFrameVerification
{
    public static object Main()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
        var root = GameObject.Find("In-scene Objects/Factory Entrance/OldPhotoFrame_StreetMarket");
        if (root == null || root.scene != scene) throw new Exception("Frame is missing from main scene.");
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        if (renderers.Any(r=>r.sharedMaterials.Any(m=>m==null || m.shader.name!="HDRP/Lit" || !m.shader.isSupported)))
            throw new Exception("Missing or unsupported HDRP material.");
        var glass = renderers.Single(r=>r.name=="GlassPane");
        if (glass.sharedMaterial.GetFloat("_SurfaceType")!=1 || glass.sharedMaterial.GetTexture("_BaseColorMap")==null)
            throw new Exception("Transparent glass not configured.");
        if (renderers.Single(r=>r.name=="FineFractureEdges").enabled)
            throw new Exception("Submillimetre fracture geometry must be disabled in this scene.");
        var table = GameObject.Find("In-scene Objects/Factory Entrance/table/SM_Table_Chair_Set_02a");
        var intersect=typeof(HandleUtility).GetMethod("IntersectRayMesh",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic,null,
            new Type[]{typeof(Ray),typeof(Mesh),typeof(Matrix4x4),typeof(RaycastHit).MakeByRefType()},null);
        var corners = new[]{new Vector3(-.16f,-.1259f,0),new Vector3(.16f,-.1259f,0),new Vector3(.16f,.1259f,0),new Vector3(-.16f,.1259f,0)};
        var cornerResults=corners.Select(p=>{
            var pos=root.transform.TransformPoint(p);
            object[] args={new Ray(pos+Vector3.up,Vector3.down),table.GetComponent<MeshFilter>().sharedMesh,table.transform.localToWorldMatrix,new RaycastHit()};
            bool hit=(bool)intersect.Invoke(null,args);var info=(RaycastHit)args[3];
            if(!hit || info.normal.normalized.y<.95f)throw new Exception("A frame corner is outside the tabletop.");
            return new{hit,tableHeight=info.point.y,lowestFrameGap=bounds.min.y-info.point.y};
        }).ToArray();
        if(cornerResults.Any(c=>c.lowestFrameGap<0 || c.lowestFrameGap>.02f))
            throw new Exception("Frame is embedded in or floating above the tabletop.");
        var view=SceneView.lastActiveSceneView;
        view.LookAt(root.transform.position,Quaternion.Euler(55f,22.8912f,0),.205f,false,true);
        view.sceneLighting=true;view.Repaint();Selection.activeGameObject=root;
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved=EditorSceneManager.SaveScene(scene);
        if(!saved)throw new Exception("Scene save failed.");
        AssetDatabase.SaveAssets();
        var report=new{scene=scene.path,saved,sceneDirty=scene.isDirty,objectPath="In-scene Objects/Factory Entrance/OldPhotoFrame_StreetMarket",
            position=root.transform.position.ToString("F5"),rotation=root.transform.eulerAngles.ToString("F5"),scale=root.transform.lossyScale.ToString("F5"),
            rendererCount=renderers.Length,enabledRenderers=renderers.Count(r=>r.enabled),
            visibleTriangles=renderers.Where(r=>r.enabled).Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),
            boundsSize=bounds.size.ToString("F5"),corners=cornerResults,
            glassMaterial=AssetDatabase.GetAssetPath(glass.sharedMaterial),glassQueue=glass.sharedMaterial.renderQueue,
            photoTexture=AssetDatabase.GetAssetPath(renderers.Single(r=>r.name=="Photograph").sharedMaterial.GetTexture("_BaseColorMap")),
            compileFailed=EditorUtility.scriptCompilationFailed,playModeTested=false,sceneLightingUsed=true};
        File.WriteAllText("ArtSource/Props/OldPhotoFrame/UnityIntegration/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        return report;
    }
}
