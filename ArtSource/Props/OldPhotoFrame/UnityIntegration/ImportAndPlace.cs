using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class OldPhotoFramePlacement
{
    const string Folder = "Assets/Art Resources/Props/OldPhotoFrame";
    static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/" + name + ".png");

    static Material Make(string name, string colorMap, string normalMap, string maskMap, Color color, float smoothness, float metallic = 0)
    {
        string path = Folder + "/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", metallic);
        if (colorMap != null) m.SetTexture("_BaseColorMap", Tex(colorMap));
        if (normalMap != null) { m.SetTexture("_NormalMap", Tex(normalMap)); m.SetFloat("_NormalScale", .4f); }
        if (maskMap != null) m.SetTexture("_MaskMap", Tex(maskMap));
        HDMaterial.ValidateMaterial(m);
        EditorUtility.SetDirty(m);
        return m;
    }

    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before asset placement.");
        Scene main = SceneManager.GetSceneByPath("Assets/Scenes/Main Game Scene.unity");
        if (!main.isLoaded) throw new InvalidOperationException("Main scene is not loaded.");
        if (GameObject.Find("OldPhotoFrame_StreetMarket") != null) throw new InvalidOperationException("Frame already placed.");
        foreach (string path in Directory.GetFiles(Folder + "/Textures", "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            string name = Path.GetFileNameWithoutExtension(path);
            bool normal = name.EndsWith("_Normal");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = name.Contains("BaseColor") || name.Contains("EdgeColor") || name == "StreetMarket_Original";
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = name.StartsWith("Wood_") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        var materials = new Dictionary<string, Material>();
        materials["Frame_AgedWalnut"] = Make("Frame_AgedWalnut", "Wood_BaseColor", "Wood_Normal", "Wood_HDRP_Mask", Color.white, .4f);
        materials["WornWood_Edges"] = Make("WornWood_Edges", "Wood_EdgeColor", null, null, Color.white, .24f);
        materials["Backboard_Fiberboard"] = Make("Backboard_Fiberboard", "Backboard_BaseColor", null, null, Color.white, .06f);
        materials["Photo_OriginalBlackAndWhite"] = Make("Photo_OriginalBlackAndWhite", "StreetMarket_Original", null, null, Color.white, .17f);
        materials["Hardware_OxidizedIron"] = Make("Hardware_OxidizedIron", null, null, null, new Color(.043f,.036f,.027f,1), .26f, .58f);
        materials["Glass_FractureEdges"] = Make("Glass_FractureEdges", null, null, null, new Color(.24f,.27f,.26f,1), .68f);
        var glass = Make("Glass_LocalCracks", "Glass_BaseColorOpacity", "Glass_Normal", "Glass_HDRP_Mask", Color.white, .95f);
        glass.SetFloat("_SurfaceType", 1);
        glass.SetFloat("_BlendMode", 0);
        glass.SetFloat("_TransparentZWrite", 0);
        glass.SetFloat("_RefractionModel", 0);
        glass.SetFloat("_EnableBlendModePreserveSpecularLighting", 1);
        glass.SetFloat("_NormalScale", .34f);
        HDMaterial.ValidateMaterial(glass);
        EditorUtility.SetDirty(glass);
        materials["Glass_LocalCracks"] = glass;

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Models/OldPhotoFrame.fbx");
        var root = new GameObject("OldPhotoFrame_StreetMarket");
        SceneManager.MoveGameObjectToScene(root, main);
        Undo.RegisterCreatedObjectUndo(root, "Place old photograph frame");
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source, main);
        model.name = "Model";
        model.transform.SetParent(root.transform, false);
        var renderers = model.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials[m.name]).ToArray();
            if (renderer.name == "GlassPane" || renderer.name == "FineFractureEdges") renderer.shadowCastingMode = ShadowCastingMode.Off;
            if (renderer.name == "FineFractureEdges") renderer.enabled = false;
        }
        var pane = renderers.Single(r => r.name == "GlassPane");
        var photo = renderers.Single(r => r.name == "Photograph");
        var top = renderers.Single(r => r.name == "Frame_Top");
        var bottom = renderers.Single(r => r.name == "Frame_Bottom");
        Vector3 front = (pane.bounds.center - photo.bounds.center).normalized;
        Vector3 up = (top.bounds.center - bottom.bounds.center).normalized;
        model.transform.rotation = Quaternion.Inverse(Quaternion.LookRotation(front, up));
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        model.transform.position -= bounds.center;
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        // Prefab uses a centred pivot, local +Z front and +Y up.
        bool prefabSaved;
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, Folder + "/OldPhotoFrame.prefab", InteractionMode.AutomatedAction, out prefabSaved);
        if (!prefabSaved) throw new InvalidOperationException("Prefab save failed.");

        var table = GameObject.Find("In-scene Objects/Factory Entrance/table/SM_Table_Chair_Set_02a");
        if (table == null) throw new InvalidOperationException("Gatehouse table not found.");
        var tableRenderer = table.GetComponent<MeshRenderer>();
        var tableMesh = table.GetComponent<MeshFilter>();
        var rayMethod = typeof(HandleUtility).GetMethod("IntersectRayMesh", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
            new Type[]{typeof(Ray),typeof(Mesh),typeof(Matrix4x4),typeof(RaycastHit).MakeByRefType()},null);
        Vector3 center = tableRenderer.bounds.center;
        var ray = new Ray(center + Vector3.up*3f, Vector3.down);
        object[] arguments = {ray, tableMesh.sharedMesh, table.transform.localToWorldMatrix, new RaycastHit()};
        if (!(bool)rayMethod.Invoke(null, arguments)) throw new InvalidOperationException("No tabletop hit at table centre.");
        var hit = (RaycastHit)arguments[3];
        if (hit.normal.normalized.y < .95f) throw new InvalidOperationException("Table surface is not horizontal.");
        root.transform.rotation = Quaternion.Euler(-90f, table.transform.eulerAngles.y + 188f, 0);
        root.transform.position = hit.point;
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        root.transform.position += Vector3.up*(hit.point.y - bounds.min.y + .001f);
        // The tabletop has a shallow centre seam. Support all corners above the raised surface.
        float supportHeight = hit.point.y;
        foreach (var corner in new[]{new Vector3(-.16f,-.1259f,0),new Vector3(.16f,-.1259f,0),new Vector3(.16f,.1259f,0),new Vector3(-.16f,.1259f,0)})
        {
            var pos = root.transform.TransformPoint(corner);
            object[] cornerArgs = {new Ray(pos+Vector3.up,Vector3.down),tableMesh.sharedMesh,table.transform.localToWorldMatrix,new RaycastHit()};
            if (!(bool)rayMethod.Invoke(null,cornerArgs)) throw new InvalidOperationException("Frame corner is outside tabletop.");
            supportHeight = Mathf.Max(supportHeight,((RaycastHit)cornerArgs[3]).point.y);
        }
        root.transform.position += Vector3.up*(supportHeight-hit.point.y);
        root.transform.SetParent(GameObject.Find("In-scene Objects/Factory Entrance").transform, true);
        PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
        Selection.activeGameObject = root;
        var view = SceneView.lastActiveSceneView;
        view.LookAt(root.transform.position, Quaternion.Euler(55f,table.transform.eulerAngles.y,0), .205f, false, true);
        view.sceneLighting = true;
        view.Repaint();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(main);
        // Save only after visual inspection in a separate step.
        var result = new { scene=main.path,objectName=root.name,position=root.transform.position.ToString("F5"),rotation=root.transform.eulerAngles.ToString("F4"),
            tabletop=hit.point.ToString("F5"),normal=hit.normal.ToString("F5"),sourceFront=front.ToString("F5"),sourceUp=up.ToString("F5"),
            materials=materials.Values.Select(m=>new {m.name,shader=m.shader.name,queue=m.renderQueue,transparent=m.GetFloat("_SurfaceType")}).ToArray() };
        File.WriteAllText("ArtSource/Props/OldPhotoFrame/UnityIntegration/placement.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
