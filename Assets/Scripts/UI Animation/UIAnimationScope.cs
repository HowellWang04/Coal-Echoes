using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UIAnimationScope : MonoBehaviour
{
    [SerializeField] private UIAnimationProfile animationProfile;
    [SerializeField] private Button[] excludedButtons;

    public UIAnimationProfile AnimationProfile => animationProfile;

    private void Awake()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (animationProfile == null)
        {
            Debug.LogWarning("[UIAnimationScope] Animation Profile is not assigned.", this);
            return;
        }

        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
            RegisterButton(button);
    }

    public void RegisterButton(Button button)
    {
        if (button == null || IsExcluded(button)) return;

        var feedback = button.GetComponent<UIButtonTweenFeedback>();
        if (feedback == null)
            feedback = button.gameObject.AddComponent<UIButtonTweenFeedback>();

        feedback.Initialize(animationProfile);
    }

    public UITabTransition RegisterTab(GameObject tab)
    {
        var transition = tab.GetComponent<UITabTransition>();
        if (transition == null)
            transition = tab.AddComponent<UITabTransition>();

        transition.Initialize(animationProfile);
        return transition;
    }

    public UIPanelSlideTransition RegisterPanel(GameObject panel, RectTransform sidebarRoot, RectTransform tabsRoot)
    {
        var transition = panel.GetComponent<UIPanelSlideTransition>();
        if (transition == null)
            transition = panel.AddComponent<UIPanelSlideTransition>();

        transition.Initialize(animationProfile, sidebarRoot, tabsRoot);
        return transition;
    }

    private bool IsExcluded(Button button)
    {
        if (excludedButtons == null) return false;

        foreach (var excludedButton in excludedButtons)
            if (excludedButton == button)
                return true;

        return false;
    }
}
