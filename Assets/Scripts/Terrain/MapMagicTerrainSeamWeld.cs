using System.Collections.Generic;
using System.Linq;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MapMagicObject))]
public sealed class MapMagicTerrainSeamWeld : MonoBehaviour
{
    private MapMagicObject mapMagic;
    private bool pending;
    private int idleUpdates;

    private void OnEnable()
    {
        mapMagic = GetComponent<MapMagicObject>();
        TerrainTile.OnAllComplete += OnGenerationComplete;
    }

    private void OnDisable()
    {
        TerrainTile.OnAllComplete -= OnGenerationComplete;
    }

    private void OnGenerationComplete(MapMagicObject completed)
    {
        if (completed == mapMagic)
        {
            pending = true;
            idleUpdates = 0;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }
    }

    private void Update()
    {
        if (!pending)
            return;
        if (mapMagic.IsGenerating() || Den.Tools.Tasks.CoroutineManager.IsWorking)
        {
            idleUpdates = 0;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
            return;
        }

        // Terrain LOD corner writes can finish on the following editor update.
        if (++idleUpdates < 3)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
            return;
        }

        pending = false;
        WeldNow();
    }

    public void WeldNow()
    {
        var tiles = mapMagic.tiles.All().Where(t => t.main?.terrain != null && t.main.applyReady).ToArray();
        var samples = new Dictionary<Vector2Int, (double sum, int count)>();
        var maps = new Dictionary<TerrainTile, float[,]>();
        int resolution = (int)mapMagic.tileResolution;
        int last = resolution - 1;

        foreach (var tile in tiles)
        {
            var data = tile.main.terrain.terrainData;
            if (data.heightmapResolution != resolution)
                continue;

            data.SyncHeightmap();
            var heights = data.GetHeights(0, 0, resolution, resolution);
            maps.Add(tile, heights);
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x += z == 0 || z == last ? 1 : last)
                {
                    var key = new Vector2Int(tile.coord.x * last + x, tile.coord.z * last + z);
                    samples.TryGetValue(key, out var total);
                    samples[key] = (total.sum + heights[z, x], total.count + 1);
                }
        }

        foreach (var pair in maps)
        {
            var tile = pair.Key;
            var heights = pair.Value;
            bool changed = false;
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x += z == 0 || z == last ? 1 : last)
                {
                    var key = new Vector2Int(tile.coord.x * last + x, tile.coord.z * last + z);
                    var total = samples[key];
                    if (total.count < 2)
                        continue;

                    float value = (float)(total.sum / total.count);
                    changed |= heights[z, x] != value;
                    heights[z, x] = value;
                }

            if (!changed)
                continue;

            // Update the plugin's cached edges too, so subsequent LOD switches use the same border.
            tile.main.terrain.terrainData.SetHeights(0, 0, heights);
            tile.main.edges.heightEdges.ReadFloats2D(heights);
            tile.main.terrain.Flush();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(tile.main.terrain.terrainData);
#endif
        }

        foreach (var tile in tiles)
            Weld.WeldSurroundingDraftsToThisMain(mapMagic.tiles, tile.coord);
    }
}
