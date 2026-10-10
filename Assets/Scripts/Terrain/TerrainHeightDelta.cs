using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Nodes;
using MapMagic.Products;
using UnityEngine;

[System.Serializable]
[GeneratorMenu(menu = "Map/Project Coal", name = "Terrain Height Delta", disengageable = true)]
public class TerrainHeightDelta : Generator, IInlet<MatrixWorld>, IOutlet<MatrixWorld>
{
    public MatrixAsset delta;
    public Vector2 offset;
    public Vector2 size;

    public override (string, int) GetCodeFileLine() => GetCodeFileLineBase();

    public float SampleDelta(float x, float z)
    {
        // Keep sub-pixel coordinates precise when the delta covers several kilometres.
        double u = ((double)x - offset.x) / size.x;
        double v = ((double)z - offset.y) / size.y;
        if (u < 0 || u > 1 || v < 0 || v > 1)
            return 0;

        var matrix = delta.matrix;
        double sx = u * (matrix.rect.size.x - 1);
        double sz = v * (matrix.rect.size.z - 1);
        int ix = Mathf.Min((int)sx, matrix.rect.size.x - 2);
        int iz = Mathf.Min((int)sz, matrix.rect.size.z - 2);
        int index = iz * matrix.rect.size.x + ix;
        double tx = sx - ix;
        double tz = sz - iz;
        double a = matrix.arr[index] * (1 - tx) + matrix.arr[index + 1] * tx;
        double b = matrix.arr[index + matrix.rect.size.x] * (1 - tx) +
            matrix.arr[index + matrix.rect.size.x + 1] * tx;
        return (float)(a * (1 - tz) + b * tz);
    }

    public override void Generate(TileData data, StopToken stop)
    {
        if (stop != null && stop.stop) return;
        var source = data.ReadInletProduct(this);
        if (source == null) return;
        if (!enabled || delta == null)
        {
            data.StoreProduct(this, source);
            return;
        }

        var result = new MatrixWorld(source);
        var active = data.area.active;
        var spacing = active.PixelSize;
        for (int z = result.rect.Min.z; z < result.rect.Max.z; z++)
        {
            if (stop != null && stop.stop) return;
            float worldZ = active.worldPos.z + (z - active.rect.offset.z) * spacing.z;
            for (int x = result.rect.Min.x; x < result.rect.Max.x; x++)
            {
                float worldX = active.worldPos.x + (x - active.rect.offset.x) * spacing.x;
                // Signed bilinear samples preserve lowering and use the same coordinates in both LODs.
                result[x, z] += SampleDelta(worldX, worldZ);
            }
        }
        data.StoreProduct(this, result);
    }
}
