using System.Collections.Generic;
using System.Linq;
using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Core;
using MapMagic.Nodes.MatrixGenerators;
using MapMagic.Terrains;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MapMagicTerrainTextureSync
{
    private const string AutoMenu = "Tools/Coal Echoes/Auto Align Terrain Textures";
    private const string RefreshMenu = "Tools/Coal Echoes/Align Selected Terrain Textures";
    private const string Preference = "ProjectCoal.AutoAlignTerrainTextures";
    private const double Delay = 0.5;
    private const float Tolerance = 1.01f / 255;
    private static readonly Dictionary<Terrain, Rect> Pending = new Dictionary<Terrain, Rect>();
    private static readonly Dictionary<int, Dictionary<Terrain, Rect>> HeightEdits = new Dictionary<int, Dictionary<Terrain, Rect>>();
    private static double lastChange;
    private static bool refreshing;

    static MapMagicTerrainTextureSync()
    {
        TerrainCallbacks.heightmapChanged += OnHeightmapChanged;
        EditorApplication.update += Update;
        Undo.undoRedoEvent += OnUndoRedo;
        EditorApplication.playModeStateChanged += _ =>
        {
            Pending.Clear();
            HeightEdits.Clear();
        };
    }

    [MenuItem(AutoMenu)]
    private static void ToggleAutomatic()
    {
        EditorPrefs.SetBool(Preference, !EditorPrefs.GetBool(Preference, true));
        Pending.Clear();
        HeightEdits.Clear();
    }

    [MenuItem(AutoMenu, true)]
    private static bool ValidateAutomatic()
    {
        Menu.SetChecked(AutoMenu, EditorPrefs.GetBool(Preference, true));
        return true;
    }

    [MenuItem(RefreshMenu)]
    private static void RefreshSelected()
    {
        var terrain = Selection.activeGameObject.GetComponent<Terrain>();
        var data = terrain.terrainData;
        var p = terrain.transform.position;
        var region = new Rect(p.x, p.z, data.size.x, data.size.z);
        var mapMagic = terrain.GetComponentInParent<MapMagicObject>();
        foreach (var target in mapMagic.GetComponentsInChildren<Terrain>(true))
        {
            var position = target.transform.position;
            if (new Rect(position.x, position.z, target.terrainData.size.x, target.terrainData.size.z).Overlaps(region))
                RefreshTerrain(target, region, true);
        }
    }

    [MenuItem(RefreshMenu, true)]
    private static bool ValidateSelected()
    {
        var terrain = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<Terrain>();
        var mapMagic = terrain == null ? null : terrain.GetComponentInParent<MapMagicObject>();
        return !EditorApplication.isPlayingOrWillChangePlaymode && mapMagic != null && !mapMagic.IsGenerating();
    }

    private static void OnHeightmapChanged(Terrain terrain, RectInt region, bool synched)
    {
        if (refreshing || EditorApplication.isPlayingOrWillChangePlaymode || !EditorPrefs.GetBool(Preference, true))
            return;
        QueueRegion(terrain, region);
    }

    // Also available to editor tools that change height data without a Terrain callback.
    public static void QueueRegion(Terrain terrain, RectInt heightRegion)
    {
        var mapMagic = terrain.GetComponentInParent<MapMagicObject>();
        if (mapMagic == null || mapMagic.graph == null)
            return;

        var data = terrain.terrainData;
        var p = terrain.transform.position;
        float dx = data.size.x / (data.heightmapResolution - 1);
        float dz = data.size.z / (data.heightmapResolution - 1);
        var bounds = Rect.MinMaxRect(p.x + heightRegion.xMin * dx, p.z + heightRegion.yMin * dz,
            p.x + (heightRegion.xMax - 1) * dx, p.z + (heightRegion.yMax - 1) * dz);
        var cavity = mapMagic.graph.generators.OfType<Cavity200>().SingleOrDefault();
        if (cavity == null)
            return;
        int undoGroup = Undo.GetCurrentGroup();
        if (!HeightEdits.TryGetValue(undoGroup, out var edited))
        {
            edited = new Dictionary<Terrain, Rect>();
            HeightEdits.Add(undoGroup, edited);
        }

        // Include the other LOD and neighbours whose slope/cavity samples touch the edit.
        foreach (var target in mapMagic.GetComponentsInChildren<Terrain>(true))
        {
            var td = target.terrainData;
            float padding = Mathf.Max(3 * td.size.x / (td.heightmapResolution - 1), cavity.spread * 4);
            var expanded = Rect.MinMaxRect(bounds.xMin - padding, bounds.yMin - padding,
                bounds.xMax + padding, bounds.yMax + padding);
            var tp = target.transform.position;
            if (!expanded.Overlaps(new Rect(tp.x, tp.z, td.size.x, td.size.z)))
                continue;
            MergeRegion(Pending, target, expanded);
            MergeRegion(edited, target, expanded);
        }
        lastChange = EditorApplication.timeSinceStartup;
    }

    private static void MergeRegion(Dictionary<Terrain, Rect> regions, Terrain terrain, Rect region)
    {
        if (regions.TryGetValue(terrain, out var previous))
            region = Rect.MinMaxRect(Mathf.Min(previous.xMin, region.xMin), Mathf.Min(previous.yMin, region.yMin),
                Mathf.Max(previous.xMax, region.xMax), Mathf.Max(previous.yMax, region.yMax));
        regions[terrain] = region;
    }

    private static void OnUndoRedo(in UndoRedoInfo info)
    {
        if (!EditorPrefs.GetBool(Preference, true) || !HeightEdits.TryGetValue(info.undoGroup, out var edited))
            return;
        foreach (var pair in edited)
            if (pair.Key != null)
                MergeRegion(Pending, pair.Key, pair.Value);
        lastChange = EditorApplication.timeSinceStartup;
    }

    private static void Update()
    {
        if (Pending.Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            GUIUtility.hotControl != 0 || EditorApplication.timeSinceStartup - lastChange < Delay ||
            Den.Tools.Tasks.CoroutineManager.IsWorking)
            return;

        // Process one terrain per editor update to avoid repainting many tiles in one frame.
        var entry = Pending.First();
        if (entry.Key == null)
        {
            Pending.Remove(entry.Key);
            return;
        }
        var mapMagic = entry.Key.GetComponentInParent<MapMagicObject>();
        if (mapMagic != null && mapMagic.IsGenerating())
            return;
        Pending.Remove(entry.Key);
        if (mapMagic != null)
            RefreshTerrain(entry.Key, entry.Value, false);
    }

    public static int RefreshTerrain(Terrain terrain, Rect worldRegion, bool recordUndo = true)
    {
        var mapMagic = terrain.GetComponentInParent<MapMagicObject>();
        if (EditorApplication.isPlayingOrWillChangePlaymode || mapMagic == null || mapMagic.IsGenerating())
            return 0;
        var slopeNode = mapMagic.graph.generators.OfType<Slope200>().SingleOrDefault();
        var cavityNode = mapMagic.graph.generators.OfType<Cavity200>().SingleOrDefault();
        var textures = mapMagic.graph.generators.OfType<TexturesOutput200>().SingleOrDefault();
        var td = terrain.terrainData;
        // This tool owns the project's existing sand / slope rock / cavity rock layer stack.
        // Hand-painted layers after those three (factory ground) keep their weight.
        if (slopeNode == null || cavityNode == null || textures == null || textures.layers.Length != 3 ||
            td.terrainLayers.Length < 3 ||
            !td.terrainLayers.Take(3).SequenceEqual(textures.layers.Select(l => l.prototype)))
            return 0;

        refreshing = true;
        try
        {
            var terrains = mapMagic.GetComponentsInChildren<Terrain>(true)
                .OrderBy(t => t.terrainData.heightmapResolution == td.heightmapResolution ? 0 : 1)
                .ThenByDescending(t => t.terrainData.heightmapResolution).ToArray();
            foreach (var t in terrains)
                t.terrainData.SyncHeightmap();

            int n = td.heightmapResolution;
            int ar = td.alphamapResolution;
            var p = terrain.transform.position;
            float spacing = td.size.x / (n - 1);
            int margin = Mathf.Max(3, Mathf.CeilToInt(cavityNode.spread * 4 / spacing));
            int side = n + margin * 2;
            var origin = p - new Vector3(margin * spacing, 0, margin * spacing);
            var size = new Vector3(td.size.x + margin * spacing * 2, td.size.y, td.size.z + margin * spacing * 2);
            var heights = td.GetHeights(0, 0, n, n);
            var matrix = new MatrixWorld(new CoordRect(0, 0, side, side), origin, size);
            for (int z = 0; z < side; z++)
                for (int x = 0; x < side; x++)
                    matrix.arr[z * side + x] = x >= margin && x < n + margin && z >= margin && z < n + margin
                        ? heights[z - margin, x - margin]
                        : SampleHeight(terrains, terrain, origin.x + x * spacing, origin.z + z * spacing);

            var slope = Slope200.Slope(matrix, td.size.y, slopeNode.from, slopeNode.to, slopeNode.range);
            var cavity = new MatrixWorld(matrix.rect, origin, size);
            Cavity200.Cavity(matrix, cavity, cavityNode.type, cavityNode.intensity, cavityNode.spread,
                spacing, new Vector2D(td.size.x, td.size.z), null);
            var sand = new MatrixWorld(matrix.rect, origin, size);
            sand.Fill(1);
            Matrix[] layers = { sand, slope, cavity };
            Matrix.BlendLayers(layers, textures.layers.Select(l => l.Opacity).ToArray());

            int x0 = Mathf.Clamp(Mathf.FloorToInt((worldRegion.xMin - p.x) / td.size.x * ar), 0, ar);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((worldRegion.yMin - p.z) / td.size.z * ar), 0, ar);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((worldRegion.xMax - p.x) / td.size.x * ar), 0, ar);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((worldRegion.yMax - p.z) / td.size.z * ar), 0, ar);
            if (x0 == x1 || z0 == z1)
                return 0;
            var alpha = td.GetAlphamaps(x0, z0, x1 - x0, z1 - z0);
            int changed = 0;
            for (int z = z0; z < z1; z++)
                for (int x = x0; x < x1; x++)
                {
                    // Terrain shader UVs place the shared tile edge on the outer pixel centers.
                    float px = margin + x * (n - 1f) / (ar - 1);
                    float pz = margin + z * (n - 1f) / (ar - 1);
                    float extra = 0;
                    for (int l = 3; l < td.alphamapLayers; l++)
                        extra += alpha[z - z0, x - x0, l];
                    float natural = Mathf.Clamp01(1 - extra);
                    bool different = false;
                    for (int l = 0; l < 3; l++)
                        different |= Mathf.Abs(alpha[z - z0, x - x0, l] - layers[l].GetInterpolated(px, pz) * natural) > Tolerance;
                    if (!different)
                        continue;
                    changed++;
                    for (int l = 0; l < 3; l++)
                        alpha[z - z0, x - x0, l] = layers[l].GetInterpolated(px, pz) * natural;
                }
            if (changed == 0)
                return 0;

            // Automatic paint is derived from the height edit; it must not add a second undo step.
            if (recordUndo)
                Undo.RegisterCompleteObjectUndo(td, "Align terrain textures to height");
            td.SetAlphamaps(x0, z0, alpha);
            terrain.Flush();
            EditorUtility.SetDirty(td);
            var tile = terrain.GetComponentInParent<TerrainTile>();
            if (tile != null)
            {
                var detail = tile.main != null && tile.main.terrain == terrain ? tile.main : tile.draft;
                var allAlpha = td.GetAlphamaps(0, 0, ar, ar);
                if (detail.edges.splatEdges != null)
                    for (int l = 0; l < detail.edges.splatEdges.Length; l++)
                        detail.edges.splatEdges[l].ReadFloats3D(allAlpha, l);
                EditorUtility.SetDirty(tile);
                EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
            }
            return changed;
        }
        finally
        {
            refreshing = false;
        }
    }

    private static float SampleHeight(Terrain[] terrains, Terrain fallback, float x, float z)
    {
        foreach (var t in terrains)
        {
            var p = t.transform.position;
            var size = t.terrainData.size;
            if (x < p.x || x > p.x + size.x || z < p.z || z > p.z + size.z)
                continue;
            return (t.terrainData.GetInterpolatedHeight((x - p.x) / size.x, (z - p.z) / size.z) + p.y - fallback.transform.position.y)
                / fallback.terrainData.size.y;
        }
        var position = fallback.transform.position;
        var data = fallback.terrainData;
        return data.GetInterpolatedHeight(Mathf.Clamp01((x - position.x) / data.size.x),
            Mathf.Clamp01((z - position.z) / data.size.z)) / data.size.y;
    }
}
