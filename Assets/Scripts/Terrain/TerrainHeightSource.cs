using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Nodes;
using MapMagic.Products;
using UnityEngine;

[System.Serializable]
[GeneratorMenu(menu = "Map/Project Coal", name = "Terrain Height Source")]
public class TerrainHeightSource : Generator, IOutlet<MatrixWorld>
{
    public MatrixAsset main;
    public MatrixAsset draft;
    public Vector2 offset;
    public Vector2 size;

    public override (string, int) GetCodeFileLine() => GetCodeFileLineBase();

    public float SampleHeight(float x, float z, bool isDraft)
    {
        var matrix = (isDraft ? draft : main).matrix;
        double u = System.Math.Clamp(((double)x - offset.x) / size.x, 0, 1);
        double v = System.Math.Clamp(((double)z - offset.y) / size.y, 0, 1);
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
        var result = new MatrixWorld(data.area.full.rect, data.area.full.worldPos,
            data.area.full.worldSize, data.globals.height);
        var active = data.area.active;
        var spacing = active.PixelSize;
        for (int z = result.rect.Min.z; z < result.rect.Max.z; z++)
        {
            if (stop != null && stop.stop) return;
            float worldZ = active.worldPos.z + (z - active.rect.offset.z) * spacing.z;
            for (int x = result.rect.Min.x; x < result.rect.Max.x; x++)
            {
                float worldX = active.worldPos.x + (x - active.rect.offset.x) * spacing.x;
                result[x, z] = SampleHeight(worldX, worldZ, data.isDraft);
            }
        }
        data.StoreProduct(this, result);
    }
}
