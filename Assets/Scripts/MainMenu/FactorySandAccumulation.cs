using System.Collections.Generic;
using UnityEngine;

public sealed class FactorySandAccumulation : MonoBehaviour
{
    [SerializeField] private Transform[] dunes;
    [SerializeField, Min(0f)] private float depth = 6f;

    private sealed class Body
    {
        public Transform transform;
        public Vector3 restPosition;
    }

    private readonly List<Body> bodies = new List<Body>();
    public float Progress { get; private set; } = 1f;

    public void Prepare()
    {
        Restore();
        if (depth == 0f) return;

        // Player input is locked during the opening. Only the visible LOD meshes sink, so the
        // colliders and invisible blockers stay on the final dunes and never move.
        foreach (var dune in dunes)
            foreach (var lod in dune.GetComponent<LODGroup>().GetLODs())
                foreach (var renderer in lod.renderers)
                    bodies.Add(new Body { transform = renderer.transform, restPosition = renderer.transform.localPosition });

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

        // Depth exceeds the tallest dune, so at the start every dune is hidden under the ground.
        var offset = Vector3.down * (depth * (1f - Progress));
        foreach (var body in bodies) body.transform.localPosition = body.restPosition + offset;
    }

    public void Restore()
    {
        foreach (var body in bodies)
            if (body.transform != null) body.transform.localPosition = body.restPosition;
        bodies.Clear();
        Progress = 1f;
    }

    private void OnDisable() => Restore();
}
