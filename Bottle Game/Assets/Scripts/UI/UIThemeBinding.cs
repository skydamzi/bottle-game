using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach only to graphics that should follow the shared theme. Other artwork is untouched.
[DisallowMultipleComponent]
public class UIThemeBinding : MonoBehaviour
{
    public UIThemeRole role;
    public Image image;
    public Selectable selectable;
    [Tooltip("UGUI Text or TextMeshProUGUI. Decorative graphics do not need a label.")]
    public Graphic label;

    void Reset()
    {
        image = GetComponent<Image>();
        selectable = GetComponent<Selectable>();
        Graphic graphic = GetComponent<Graphic>();
        if (graphic is Text || graphic is TMP_Text) label = graphic;
    }

    public void Apply(UITheme theme)
    {
        if (theme == null) return;
        UIThemeStyle style = theme.GetStyle(role);
        if (style == null) return;
        if (image != null)
        {
            if (style.sprite != null) { image.sprite = style.sprite; image.type = style.imageType; }
            if (style.applyTint) image.color = style.tint;
        }
        if (selectable != null && style.applyButtonStates)
        {
            selectable.transition = style.transition;
            selectable.colors = style.colors;
            selectable.spriteState = style.spriteStates;
        }
        if (label == null) return;
        if (style.applyTint) label.color = style.tint;
        if (label is Text text)
        {
            if (theme.font != null) text.font = theme.font;
            if (style.fontSize > 0) text.fontSize = style.fontSize;
        }
        else if (label is TMP_Text tmp)
        {
            if (theme.tmpFont != null) tmp.font = theme.tmpFont;
            if (style.fontSize > 0) tmp.fontSize = style.fontSize;
        }
    }
}
