using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public sealed class OpeningSkyMotion : MonoBehaviour
{
    [SerializeField] private Volume sourceVolume;
    [SerializeField] private Volume skyVolume;
    [SerializeField] private float sweepAngle = 120f;

    private VolumeProfile profile;
    private HDRISky sky;
    private float targetRotation;
    public float Progress { get; private set; } = 1f;

    public void Prepare()
    {
        Restore();
        sourceVolume.sharedProfile.TryGet(out HDRISky original);
        targetRotation = original.rotation.value;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "Opening Sky Motion";
        profile.hideFlags = HideFlags.DontSave;
        sky = profile.Add<HDRISky>();
        // Environment lighting and sky-coloured fog follow the rotation, so nothing jumps when the override is removed.
        sky.rotation.Override(Mathf.Repeat(targetRotation - sweepAngle, 360f));
        skyVolume.sharedProfile = profile;
        skyVolume.enabled = true;
        Progress = 0f;
    }

    public void SetProgress(float progress)
    {
        Progress = Mathf.Clamp01(progress);
        if (Progress == 1f)
        {
            Restore();
            return;
        }

        sky.rotation.value = Mathf.Repeat(targetRotation - sweepAngle * (1f - Progress), 360f);
    }

    public void Restore()
    {
        if (profile == null) return;
        skyVolume.enabled = false;
        skyVolume.sharedProfile = null;
        CoreUtils.Destroy(sky);
        CoreUtils.Destroy(profile);
        sky = null;
        profile = null;
        Progress = 1f;
    }

    private void OnDisable() => Restore();
}
