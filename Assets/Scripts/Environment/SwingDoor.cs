using System.Collections;
using UnityEngine;

// Swings a hinged door open once, e.g. from an ItemUsePoint after the Lock Pick is used
// on the guardhouse lock. A saved open door comes back through SetOpen, without the swing.
public sealed class SwingDoor : MonoBehaviour
{
    [SerializeField] private Transform hinge;
    [SerializeField] private float openAngle = 100f;   // around the hinge's local Y
    [SerializeField, Min(0.01f)] private float duration = 1.4f;

    private bool opened;
    private Quaternion closedRotation;

    private void Awake()
    {
        closedRotation = hinge.localRotation;
        GameSaveManager.SavesCleared += Close;
    }

    private void OnDestroy()
    {
        GameSaveManager.SavesCleared -= Close;
    }

    // Back to the closed door of a new game. A door never opened is left alone, so the
    // Factory Gate keeps the open pose the main menu gives it.
    private void Close()
    {
        if (!opened) return;
        StopAllCoroutines();
        opened = false;
        hinge.localRotation = closedRotation;
    }

    public void Open()
    {
        if (opened) return;
        opened = true;
        StartCoroutine(Swing());
    }

    public void SetOpen()
    {
        if (opened) return;
        opened = true;
        hinge.localRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    private IEnumerator Swing()
    {
        Quaternion open = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            // Slow start as the latch gives, then the door drifts to a stop.
            hinge.localRotation = Quaternion.Slerp(closedRotation, open, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        hinge.localRotation = open;
    }
}
