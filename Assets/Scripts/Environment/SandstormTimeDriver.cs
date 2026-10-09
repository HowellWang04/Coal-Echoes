using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// 累计噪声空间位移，让材质中的风速调整只影响后续移动。
[ExecuteAlways]
[RequireComponent(typeof(LocalVolumetricFog))]
public sealed class SandstormTimeDriver : MonoBehaviour
{
    private static readonly int WindId = Shader.PropertyToID("_Wind");
    private static readonly int WindSpeedId = Shader.PropertyToID("_WindSpeed");
    private static readonly int WindOffsetId = Shader.PropertyToID("_SandstormWindOffset");

    private LocalVolumetricFog fog;
    private double previousEditorTime;

    private void OnEnable()
    {
        fog = GetComponent<LocalVolumetricFog>();
        previousEditorTime = Time.realtimeSinceStartupAsDouble;
    }

    private void Update()
    {
        double editorTime = Time.realtimeSinceStartupAsDouble;
        float deltaTime = Application.isPlaying ? Time.deltaTime : (float)(editorTime - previousEditorTime);
        previousEditorTime = editorTime;

        Material material = fog.parameters.materialMask;
        if (material == null)
            return;

        Vector3 offset = material.GetVector(WindOffsetId);
        Vector3 wind = material.GetVector(WindId);
        offset += wind * material.GetFloat(WindSpeedId) * deltaTime;
        material.SetVector(WindOffsetId, offset);
    }
}
