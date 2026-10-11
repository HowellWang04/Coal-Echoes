using TMPro;
using System.Collections;
using UnityEngine;

public class EchoPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private string popupMessage = "New Echo Discovered";
    [SerializeField] private float displayDuration = 2f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (contentText != null) contentText.text = popupMessage;
        if (popupRoot != null) popupRoot.SetActive(true);

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    public void Hide()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        hideRoutine = null;
        if (popupRoot != null) popupRoot.SetActive(false);
    }
}
