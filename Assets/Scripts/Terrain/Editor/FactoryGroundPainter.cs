using System.Collections.Generic;
using System.Linq;
using MapMagic.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

// Paints grey-brown dust, compacted aprons and coal dust inside the factory walls as three layers after
// MapMagic's natural stack. Masks come from the walls, buildings, road splines and coal piles, so the menu can be run
// again after moving things or after MapMagic regenerates the tiles.
public static class FactoryGroundPainter
{
    private const string LayerFolder = "Assets/Art Resources/Environment/Terrain/FactoryGround/";
    private static readonly string[] LayerNames = { "Factory_Dust", "Factory_Compacted", "Factory_CoalDust" };
    private static readonly string[] WallRoots = { "Rear and East Wall", "Front Wall" };
    private static readonly string[] BuildingGroups = { "大型厂房", "混凝土厂房", "砖砌厂房" };
    private const float EdgeFade = 25f;
    private const float RoadReach = 24f;
    private const float RoadCell = 16f;

    private struct Building
    {
        public Matrix4x4 worldToLocal;
        public Rect rect;
    }

    private struct Pile
    {
        public Vector2 centre;
        public float radius;
    }

    [MenuItem("Tools/Coal Echoes/Paint Factory Ground")]
    private static void Paint()
    {
        var factoryLayers = LayerNames
            .Select(n => AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerFolder + n + ".terrainlayer")).ToArray();
        if (factoryLayers.Any(l => l == null))
        {
            Debug.LogError("Factory ground layers are missing in " + LayerFolder);
            return;
        }

        var buildings = FindBuildings();
        var hullPoints = WallRoots.SelectMany(r => GameObject.Find(r).transform.Cast<Transform>())
            .Select(t => new Vector2(t.position.x, t.position.z)).ToList();
        foreach (var b in buildings)
        {
            var toWorld = b.worldToLocal.inverse;
            foreach (var c in new[] { b.rect.min, b.rect.max, new Vector2(b.rect.xMin, b.rect.yMax), new Vector2(b.rect.xMax, b.rect.yMin) })
            {
                Vector3 w = toWorld.MultiplyPoint3x4(new Vector3(c.x, 0, c.y));
                hullPoints.Add(new Vector2(w.x, w.z));
            }
        }
        var hull = ConvexHull(hullPoints);
        var roads = BuildRoadGrid();
        var piles = FindPiles();

        float reach = EdgeFade + 10f;
        var bounds = Rect.MinMaxRect(hull.Min(v => v.x) - reach, hull.Min(v => v.y) - reach,
            hull.Max(v => v.x) + reach, hull.Max(v => v.y) + reach);
        foreach (var pile in piles)
        {
            float r = pile.radius + 6f;
            bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, pile.centre.x - r), Mathf.Min(bounds.yMin, pile.centre.y - r),
                Mathf.Max(bounds.xMax, pile.centre.x + r), Mathf.Max(bounds.yMax, pile.centre.y + r));
        }

        var mapMagic = Object.FindAnyObjectByType<MapMagicObject>();
        foreach (var terrain in mapMagic.GetComponentsInChildren<Terrain>(true))
        {
            var td = terrain.terrainData;
            var p = terrain.transform.position;
            if (!new Rect(p.x, p.z, td.size.x, td.size.z).Overlaps(bounds))
                continue;
            bool fresh = td.terrainLayers.Length == 3;
            if (!fresh && !(td.terrainLayers.Length == 6 && td.terrainLayers.Skip(3).SequenceEqual(factoryLayers)))
            {
                Debug.LogError("Unexpected terrain layers on " + td.name, terrain);
                continue;
            }

            Undo.RegisterCompleteObjectUndo(td, "Paint factory ground");
            if (fresh)
                td.terrainLayers = td.terrainLayers.Concat(factoryLayers).ToArray();

            // Terrain shader UVs place the shared tile edge on the outer pixel centers.
            int ar = td.alphamapResolution;
            float step = td.size.x / (ar - 1);
            int x0 = Mathf.Clamp(Mathf.FloorToInt((bounds.xMin - p.x) / step), 0, ar);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((bounds.yMin - p.z) / step), 0, ar);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((bounds.xMax - p.x) / step) + 1, 0, ar);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((bounds.yMax - p.z) / step) + 1, 0, ar);
            var alpha = td.GetAlphamaps(x0, z0, x1 - x0, z1 - z0);
            for (int z = z0; z < z1; z++)
                for (int x = x0; x < x1; x++)
                {
                    int az = z - z0, ax = x - x0;
                    float natural = alpha[az, ax, 0] + alpha[az, ax, 1] + alpha[az, ax, 2];
                    // Keep slope rock bare; the factory ground only covers flat sand.
                    float flat = natural > 0 ? 1 - alpha[az, ax, 1] / natural : 1;
                    Weights(new Vector2(p.x + x * step, p.z + z * step), hull, buildings, roads, piles,
                        out float dust, out float compact, out float coal);
                    dust *= flat;
                    compact *= flat;
                    coal *= flat;

                    // Coal dust lies over the aprons, aprons over the general dust.
                    alpha[az, ax, 3] = dust * (1 - compact) * (1 - coal);
                    alpha[az, ax, 4] = compact * (1 - coal);
                    alpha[az, ax, 5] = coal;
                    float keep = (1 - dust) * (1 - compact) * (1 - coal);
                    for (int l = 0; l < 3; l++)
                        alpha[az, ax, l] = natural > 0 ? alpha[az, ax, l] / natural * keep : l == 0 ? keep : 0;
                }
            td.SetAlphamaps(x0, z0, alpha);
            // Recompute the natural stack from heights so repeated runs do not drift.
            MapMagicTerrainTextureSync.RefreshTerrain(terrain,
                new Rect(p.x + x0 * step, p.z + z0 * step, (x1 - x0) * step, (z1 - z0) * step), false);
            terrain.Flush();
            EditorUtility.SetDirty(td);
            Debug.Log("Painted factory ground on " + td.name, terrain);
        }
    }

    private static void Weights(Vector2 w, List<Vector2> hull, List<Building> buildings,
        Dictionary<Vector2Int, List<Vector2>> roads, List<Pile> piles, out float dust, out float compact, out float coal)
    {
        float broad = Fbm(w, 70f, 0f);
        float mid = Fbm(w, 18f, 37f);
        // Coal piles stain the ground under them and a few metres around, inside the walls or not.
        float pileCoal = 0;
        foreach (var pile in piles)
            pileCoal = Mathf.Max(pileCoal, 1 - Step(pile.radius * 0.75f, pile.radius + 2.5f + 2f * mid, (w - pile.centre).magnitude));
        float inside = 1 - Step(-5f, EdgeFade, HullDistance(hull, w) + (mid - 0.5f) * 16f);
        if (inside <= 0)
        {
            dust = compact = 0;
            coal = pileCoal * 0.95f;
            return;
        }

        dust = Mathf.Lerp(0.35f, 0.9f, Step(0.3f, 0.7f, broad));

        float building = float.MaxValue;
        foreach (var b in buildings)
            building = Mathf.Min(building, RectDistance(b, w));
        float apron = 5f + 3f * mid;
        compact = (1 - Step(apron - 2f, apron + 2f, building)) * Mathf.Lerp(0.6f, 1f, Fbm(w, 5f, 91f));

        // Coal dust gathers in patches along road shoulders and wall bases rather than a uniform tint.
        float roadCoal = (1 - Step(4f, 9f + 6f * mid, RoadDistance(roads, w))) * Step(0.35f, 0.65f, Fbm(w, 12f, 71f));
        float wallCoal = (1 - Step(0f, 3f + 4f * mid, building)) * Step(0.5f, 0.7f, Fbm(w, 9f, 53f));
        coal = Mathf.Max(roadCoal, wallCoal) * 0.95f;

        dust *= inside;
        compact *= inside;
        coal = Mathf.Max(coal * inside, pileCoal * 0.95f);
    }

    private static List<Pile> FindPiles()
    {
        var piles = new List<Pile>();
        foreach (var lod in Object.FindObjectsByType<LODGroup>())
        {
            if (!lod.name.StartsWith("CoalPile_"))
                continue;
            var renderers = lod.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers)
                b.Encapsulate(r.bounds);
            var p = lod.transform.position;
            piles.Add(new Pile { centre = new Vector2(p.x, p.z), radius = Mathf.Max(b.extents.x, b.extents.z) });
        }
        return piles;
    }

    private static List<Building> FindBuildings()
    {
        var buildings = new List<Building>();
        foreach (var group in BuildingGroups)
            foreach (Transform root in GameObject.Find(group).transform)
            {
                if (!root.gameObject.activeInHierarchy)
                    continue;
                // Yaw-only frame keeps the apron distance in metres.
                var worldToLocal = Matrix4x4.TRS(root.position, Quaternion.Euler(0, root.eulerAngles.y, 0), Vector3.one).inverse;
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    var b = r.localBounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                        var local = worldToLocal.MultiplyPoint3x4(r.localToWorldMatrix.MultiplyPoint3x4(corner));
                        min = Vector2.Min(min, new Vector2(local.x, local.z));
                        max = Vector2.Max(max, new Vector2(local.x, local.z));
                    }
                }
                if (min.x < max.x)
                    buildings.Add(new Building { worldToLocal = worldToLocal, rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y) });
            }
        return buildings;
    }

    private static Dictionary<Vector2Int, List<Vector2>> BuildRoadGrid()
    {
        var grid = new Dictionary<Vector2Int, List<Vector2>>();
        foreach (var road in Object.FindObjectsByType<FactoryDirtRoadSpline>())
        {
            var container = road.GetComponent<SplineContainer>();
            float length = container.CalculateLength();
            for (float d = 0; d <= length; d += 1f)
            {
                Vector3 point = container.EvaluatePosition(d / length);
                var v = new Vector2(point.x, point.z);
                var cell = Vector2Int.FloorToInt(v / RoadCell);
                if (!grid.TryGetValue(cell, out var list))
                    grid[cell] = list = new List<Vector2>();
                list.Add(v);
            }
        }
        return grid;
    }

    private static float RoadDistance(Dictionary<Vector2Int, List<Vector2>> roads, Vector2 w)
    {
        var cell = Vector2Int.FloorToInt(w / RoadCell);
        int range = Mathf.CeilToInt(RoadReach / RoadCell);
        float best = RoadReach * RoadReach;
        for (int dz = -range; dz <= range; dz++)
            for (int dx = -range; dx <= range; dx++)
                if (roads.TryGetValue(cell + new Vector2Int(dx, dz), out var list))
                    foreach (var v in list)
                        best = Mathf.Min(best, (v - w).sqrMagnitude);
        return Mathf.Sqrt(best);
    }

    private static float RectDistance(Building b, Vector2 w)
    {
        var local = b.worldToLocal.MultiplyPoint3x4(new Vector3(w.x, 0, w.y));
        float dx = Mathf.Max(b.rect.xMin - local.x, local.x - b.rect.xMax);
        float dz = Mathf.Max(b.rect.yMin - local.z, local.z - b.rect.yMax);
        return new Vector2(Mathf.Max(dx, 0), Mathf.Max(dz, 0)).magnitude + Mathf.Min(Mathf.Max(dx, dz), 0);
    }

    // Negative inside the counter-clockwise hull.
    private static float HullDistance(List<Vector2> hull, Vector2 w)
    {
        bool inside = true;
        float best = float.MaxValue;
        for (int i = 0; i < hull.Count; i++)
        {
            var a = hull[i];
            var edge = hull[(i + 1) % hull.Count] - a;
            inside &= edge.x * (w.y - a.y) - edge.y * (w.x - a.x) >= 0;
            float t = Mathf.Clamp01(Vector2.Dot(w - a, edge) / edge.sqrMagnitude);
            best = Mathf.Min(best, (a + edge * t - w).magnitude);
        }
        return inside ? -best : best;
    }

    private static List<Vector2> ConvexHull(List<Vector2> points)
    {
        var sorted = points.OrderBy(v => v.x).ThenBy(v => v.y).ToList();
        var hull = new List<Vector2>();
        for (int pass = 0; pass < 2; pass++)
        {
            int start = hull.Count;
            foreach (var v in sorted)
            {
                while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], v) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(v);
            }
            hull.RemoveAt(hull.Count - 1);
            sorted.Reverse();
        }
        return hull;
    }

    private static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    private static float Fbm(Vector2 w, float scale, float seed)
    {
        float sum = 0, amplitude = 0.5f, frequency = 1 / scale;
        for (int i = 0; i < 3; i++)
        {
            sum += amplitude * Mathf.PerlinNoise(w.x * frequency + 1000 + seed, w.y * frequency + 1000 - seed);
            amplitude *= 0.5f;
            frequency *= 2;
        }
        return sum / 0.875f;
    }

    private static float Step(float from, float to, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, x));
}
