using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Re-bake after changing the factory terrain or moving the sandstorm area.
public static class FactoryHeatHazeSetup
{
    [MenuItem("Tools/Project Coal/Refresh Factory Heat Haze")]
    public static void Refresh()
    {
        GameObject sandstorm = GameObject.Find("Factory Sandstorm");
        if (sandstorm == null || EditorApplication.isPlaying)
        {
            Debug.LogError("Open the main scene outside Play Mode to refresh Factory Heat Haze.");
            return;
        }

        const string folder = "Assets/Settings/HeatHaze";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Settings", "HeatHaze");

        Vector3 center = sandstorm.transform.position;
        Vector3 size = sandstorm.GetComponent<LocalVolumetricFog>().parameters.size;
        const int resolution = 256;
        Color[] pixels = new Color[resolution * resolution];
        Terrain[] terrains = Terrain.activeTerrains;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector3 point = new Vector3(center.x + ((x + 0.5f) / resolution - 0.5f) * size.x,
                    0, center.z + ((z + 0.5f) / resolution - 0.5f) * size.z);
                bool found = false;
                foreach (Terrain terrain in terrains)
                {
                    Vector3 origin = terrain.transform.position;
                    Vector3 terrainSize = terrain.terrainData.size;
                    if (terrain.gameObject.scene != sandstorm.scene ||
                        point.x < origin.x || point.x >= origin.x + terrainSize.x ||
                        point.z < origin.z || point.z >= origin.z + terrainSize.z)
                        continue;

                    float height = terrain.SampleHeight(point) + origin.y;
                    pixels[z * resolution + x] = new Color(height, 0, 0, 1);
                    found = true;
                    break;
                }
                if (!found)
                {
                    Debug.LogError("Factory Heat Haze area extends beyond the loaded terrain. Load the factory tiles before refreshing.");
                    return;
                }
            }
        }

        string heightPath = folder + "/FactoryGroundHeight.asset";
        Texture2D heightMap = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);
        if (heightMap == null)
        {
            heightMap = new Texture2D(resolution, resolution, TextureFormat.RFloat, false, true);
            heightMap.name = "FactoryGroundHeight";
            AssetDatabase.CreateAsset(heightMap, heightPath);
        }
        heightMap.wrapMode = TextureWrapMode.Clamp;
        heightMap.filterMode = FilterMode.Bilinear;
        heightMap.SetPixels(pixels);
        heightMap.Apply(false, false);
        EditorUtility.SetDirty(heightMap);

        string materialPath = folder + "/M_FactoryHeatHaze.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Project Coal/Heat Haze"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetVector("_Area", new Vector4(center.x, center.z, size.x, size.z));
        material.SetTexture("_GroundHeight", heightMap);
        EditorUtility.SetDirty(material);

        // The copied scene color must retain HDR highlights before post processing.
        HDRenderPipelineAsset pipeline = (HDRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        RenderPipelineSettings settings = pipeline.currentPlatformRenderPipelineSettings;
        if (settings.customBufferFormat == RenderPipelineSettings.CustomBufferFormat.R8G8B8A8 ||
            settings.customBufferFormat == RenderPipelineSettings.CustomBufferFormat.SignedR8G8B8A8)
        {
            Undo.RecordObject(pipeline, "Enable HDR Heat Haze Buffer");
            settings.customBufferFormat = RenderPipelineSettings.CustomBufferFormat.R11G11B10;
            pipeline.currentPlatformRenderPipelineSettings = settings;
            EditorUtility.SetDirty(pipeline);
        }

        Transform existing = sandstorm.transform.parent.Find("Factory Heat Haze");
        GameObject effect = existing == null ? new GameObject("Factory Heat Haze") : existing.gameObject;
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(effect, "Create Factory Heat Haze");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(effect, sandstorm.scene);
            effect.transform.SetParent(sandstorm.transform.parent, false);
            effect.transform.position = center;
        }
        CustomPassVolume volume = effect.GetComponent<CustomPassVolume>();
        if (volume == null)
            volume = Undo.AddComponent<CustomPassVolume>(effect);
        Undo.RecordObject(volume, "Configure Factory Heat Haze");
        volume.isGlobal = true;
        volume.injectionPoint = CustomPassInjectionPoint.BeforePreRefraction;
        volume.customPasses.Clear();
        volume.customPasses.Add(new FullScreenCustomPass
        {
            name = "Heat Haze Copy",
            fullscreenPassMaterial = material,
            materialPassName = "Copy Color",
            fetchColorBuffer = true,
            targetColorBuffer = CustomPass.TargetBuffer.Custom,
            targetDepthBuffer = CustomPass.TargetBuffer.None
        });
        volume.customPasses.Add(new FullScreenCustomPass
        {
            name = "Heat Haze Distortion",
            fullscreenPassMaterial = material,
            materialPassName = "Heat Distortion",
            targetColorBuffer = CustomPass.TargetBuffer.Camera,
            targetDepthBuffer = CustomPass.TargetBuffer.None
        });
        EditorUtility.SetDirty(volume);
        EditorSceneManager.MarkSceneDirty(sandstorm.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = effect;
        Debug.Log("Factory Heat Haze refreshed. Save the main scene. Preview in Scene view with Always Refresh enabled, or in Play Mode.");
    }
}
