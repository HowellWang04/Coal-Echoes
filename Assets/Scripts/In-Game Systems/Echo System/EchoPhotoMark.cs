using UnityEngine;
using UnityEngine.UI;

// Sits on Photo_Thumbnail_Prefab. Album, scrapbook top bar and scrapbook canvas all call Show()
// on photos that carry Echo texts: a slightly warmer frame plus the ECHO ink stamp.
public class EchoPhotoMark : MonoBehaviour
{
    [SerializeField] private Image frameImage;
    [SerializeField] private GameObject stamp;
    [SerializeField] private Color echoFrameColor = new Color(0.839f, 0.71f, 0.478f); // D6B57A, a touch warmer than the normal D8C397

    public Transform Stamp => stamp.transform;

    public void Show()
    {
        frameImage.color = echoFrameColor;
        stamp.SetActive(true);
    }
}
