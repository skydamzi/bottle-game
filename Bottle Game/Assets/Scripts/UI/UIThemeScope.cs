using UnityEngine;

[DisallowMultipleComponent]
public class UIThemeScope : MonoBehaviour
{
    [Tooltip("Drag a UITheme asset here. Empty styles preserve authored artwork and layout.")]
    public UITheme theme;

    void OnEnable() => Apply();

    public void Apply()
    {
        if (theme == null) return;
        foreach (UIThemeBinding binding in GetComponentsInChildren<UIThemeBinding>(true))
        {
            // A nested scope owns its own artwork, even if its theme is unassigned.
            if (binding.GetComponentInParent<UIThemeScope>(true) == this) binding.Apply(theme);
        }
    }
}
