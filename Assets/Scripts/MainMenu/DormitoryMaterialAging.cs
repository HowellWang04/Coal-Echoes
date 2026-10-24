using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class DormitoryMaterialAging : MonoBehaviour
{
    [Serializable]
    public sealed class MaterialPair
    {
        public Material aged;
        public Material clean;
    }

    [SerializeField] private Transform dormitory;
    [SerializeField] private MaterialPair[] materials;
    [SerializeField] private Shader blendShader;

    private sealed class Surface
    {
        public MaterialPair pair;
        public Material material;
        public RenderTexture color;
    }

    private readonly List<Surface> surfaces = new List<Surface>();
    private readonly Dictionary<Renderer, Material[]> originals = new Dictionary<Renderer, Material[]>();
    private Material blend;
    public float Progress { get; private set; } = 1f;

    public void Prepare()
    {
        Restore();
        blend = new Material(blendShader) { hideFlags = HideFlags.DontSave };
        foreach (var pair in materials)
        {
            var surface = new Surface
            {
                pair = pair,
                material = new Material(pair.aged) { hideFlags = HideFlags.DontSave }
            };
            var agedTexture = pair.aged.GetTexture("baseColorTexture");
            if (agedTexture != pair.clean.GetTexture("baseColorTexture"))
            {
                surface.color = new RenderTexture(agedTexture.width, agedTexture.height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    name = pair.aged.name + " Opening Color",
                    hideFlags = HideFlags.DontSave,
                    wrapMode = agedTexture.wrapMode,
                    filterMode = FilterMode.Trilinear,
                    useMipMap = true,
                    autoGenerateMips = true
                };
                surface.color.Create();
                surface.material.SetTexture("baseColorTexture", surface.color);
            }
            surfaces.Add(surface);
        }

        foreach (var renderer in dormitory.GetComponentsInChildren<Renderer>(true))
        {
            var before = renderer.sharedMaterials;
            var after = (Material[])before.Clone();
            bool changed = false;
            for (int i = 0; i < after.Length; i++)
            {
                foreach (var surface in surfaces)
                {
                    if (after[i] != surface.pair.aged) continue;
                    after[i] = surface.material;
                    changed = true;
                    break;
                }
            }
            if (!changed) continue;
            originals.Add(renderer, before);
            renderer.sharedMaterials = after;
        }
        SetProgress(0f);
    }

    public void SetProgress(float progress)
    {
        Progress = Mathf.Clamp01(progress);
        if (Progress == 1f)
        {
            Restore();
            return;
        }

        var previousTarget = RenderTexture.active;
        bool previousSrgb = GL.sRGBWrite;
        try
        {
            blend.SetFloat("_Progress", Progress);
            foreach (var surface in surfaces)
            {
                var clean = surface.pair.clean;
                var aged = surface.pair.aged;
                surface.material.SetColor("baseColorFactor", Color.Lerp(
                    clean.GetColor("baseColorFactor"), aged.GetColor("baseColorFactor"), Progress));
                surface.material.SetFloat("normalTexture_scale", Mathf.Lerp(
                    clean.GetFloat("normalTexture_scale"), aged.GetFloat("normalTexture_scale"), Progress));
                surface.material.SetFloat("roughnessFactor", Mathf.Lerp(
                    clean.GetFloat("roughnessFactor"), aged.GetFloat("roughnessFactor"), Progress));
                if (surface.color == null) continue;
                blend.SetTexture("_CleanTex", clean.GetTexture("baseColorTexture"));
                GL.sRGBWrite = true;
                Graphics.Blit(aged.GetTexture("baseColorTexture"), surface.color, blend);
            }
        }
        finally
        {
            GL.sRGBWrite = previousSrgb;
            RenderTexture.active = previousTarget;
        }
    }

    public void Restore()
    {
        foreach (var original in originals)
            if (original.Key != null) original.Key.sharedMaterials = original.Value;
        originals.Clear();
        foreach (var surface in surfaces)
        {
            CoreUtils.Destroy(surface.material);
            CoreUtils.Destroy(surface.color);
        }
        surfaces.Clear();
        CoreUtils.Destroy(blend);
        blend = null;
        Progress = 1f;
    }

    private void OnDisable() => Restore();
}
