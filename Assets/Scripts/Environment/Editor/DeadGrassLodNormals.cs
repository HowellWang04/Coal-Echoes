using System.IO;
using UnityEditor;
using UnityEngine;

// RockyDesert dead grass LOD0 ships with all normals pointing up, but LOD1 to LOD3
// carry geometric normals, so clumps turn dark brown at the first LOD switch.
// Point the lower LODs up as well so every LOD is lit the same way.
public class DeadGrassLodNormals : AssetPostprocessor
{
    void OnPostprocessModel(GameObject root)
    {
        string name = Path.GetFileNameWithoutExtension(assetPath);
        if (!name.StartsWith("SM_DeadGrass_") || !name.Contains("_LOD"))
            return;

        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = filter.sharedMesh;
            Vector3[] normals = new Vector3[mesh.vertexCount];
            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateTangents();
        }
    }
}
