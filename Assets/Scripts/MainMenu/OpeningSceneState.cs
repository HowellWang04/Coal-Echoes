using UnityEngine;

// While the main menu shows the factory as it was in 1980, the gate stands open with no chain
// and the motorcycle has not arrived yet. The present comes back while the opening camera
// looks at the sky, so the player first sees the locked gate when the camera comes down.
// A save with the chain already cut comes back with the gate still open.
public sealed class OpeningSceneState : MonoBehaviour
{
    [SerializeField] private GameObject motorcycle;
    [SerializeField] private ItemUsePoint gateChain;   // the chain and padlock
    [SerializeField] private Transform leftGateLeaf;
    [SerializeField] private Transform rightGateLeaf;
    [SerializeField, Range(0f, 120f)] private float openAngle = 90f;   // swings into the factory

    private bool prepared;
    private Quaternion leftClosed;
    private Quaternion rightClosed;

    // The scene is saved with the gate closed. Awake reads it before a loaded save opens the gate;
    // the edit-mode bakes call Prepare without Awake, while the gate is still closed.
    private void Awake() => ReadClosedPose();

    public void Prepare()
    {
        if (prepared) return;
        prepared = true;
        if (!gateChain.Used) ReadClosedPose();
        SetGateOpen(true);
        motorcycle.SetActive(false);
    }

    public void Restore()
    {
        if (!prepared) return;
        prepared = false;
        SetGateOpen(gateChain.Used);
        motorcycle.SetActive(true);
    }

    private void ReadClosedPose()
    {
        leftClosed = leftGateLeaf.localRotation;
        rightClosed = rightGateLeaf.localRotation;
    }

    // Same open pose as the leaves' SwingDoor after the chain is cut.
    private void SetGateOpen(bool open)
    {
        // The leaves hinge at the piers; opposite signs swing both free ends inward.
        float angle = open ? openAngle : 0f;
        leftGateLeaf.localRotation = leftClosed * Quaternion.Euler(0f, -angle, 0f);
        rightGateLeaf.localRotation = rightClosed * Quaternion.Euler(0f, angle, 0f);
        gateChain.gameObject.SetActive(!open);
    }

    private void OnDisable() => Restore();
}
