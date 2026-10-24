using System.Collections.Generic;
using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Right-click menu for elements placed on the ScrapBook canvas.
public class ScrapBookElementContextMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button deleteButton;

    [Header("Delete Effect")]
    // The album photo's dissolve; copied onto graphics that don't have one so every element deletes like an album photo.
    [SerializeField] private UIEffect dissolveTemplate;

    private GameObject currentElement;
    private UIAnimationProfile animationProfile;

    private void Awake()
    {
        animationProfile = GetComponentInParent<UIAnimationScope>().AnimationProfile;
        Hide();
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDelete);
    }

    public void Show(GameObject element, Vector2 screenPosition)
    {
        currentElement = element;

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            menuPanel.transform.SetAsLastSibling();
            menuPanel.GetComponent<RectTransform>().position = screenPosition;
        }
    }

    public void Hide()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        currentElement = null;
    }

    private void OnDelete()
    {
        if (currentElement != null) PlayDeleteEffect(currentElement);
        Hide();
    }

    // Same dissolve, duration and ease as an album photo deletion. TMP text dissolves through the
    // UIEffect "TextMeshPro Support (Unity 6)" sample shaders under Assets/Samples/UI Effect.
    private void PlayDeleteEffect(GameObject element)
    {
        var canvasGroup = element.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = element.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        var effects = new List<UIEffectBase>();
        foreach (var graphic in element.GetComponentsInChildren<Graphic>())
        {
            var effect = graphic.GetComponent<UIEffectBase>();
            if (effect == null)
            {
                var dissolve = graphic.gameObject.AddComponent<UIEffect>();
                dissolve.LoadPreset(dissolveTemplate);
                effect = dissolve;
            }
            effects.Add(effect);
        }

        DOTween.To(() => 0f, rate =>
            {
                foreach (var effect in effects)
                    effect.SetRate(rate, UIEffectTweener.CullingMask.Transition);
            }, 1f, animationProfile.photoDeleteDuration)
            .SetEase(animationProfile.photoDeleteEase)
            .SetUpdate(true)
            .SetLink(element)
            .OnComplete(() => Destroy(element));
    }

    private void Update()
    {
        // Click anywhere else to close the menu
        if (menuPanel != null && menuPanel.activeSelf)
        {
            var mouse = Mouse.current;
            if (mouse != null
                && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            {
                RectTransform rt = menuPanel.GetComponent<RectTransform>();
                if (!RectTransformUtility.RectangleContainsScreenPoint(rt, mouse.position.ReadValue()))
                    Hide();
            }
        }
    }
}
