using UnityEngine;

[DefaultExecutionOrder(-3)]
public sealed class PlayerInputProvider : MonoBehaviour
{
    public PlayerControls Controls { get; private set; }

    private void Awake()
    {
        Controls = new PlayerControls();
    }

    private void OnEnable()
    {
        Controls.Enable();
    }

    private void OnDisable()
    {
        Controls.Disable();
    }

    private void OnDestroy()
    {
        Controls.Dispose();
    }
}
