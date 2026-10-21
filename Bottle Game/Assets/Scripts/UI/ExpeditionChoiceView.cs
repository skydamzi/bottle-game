using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ExpeditionChoiceView : MonoBehaviour
{
    public Button button;
    [Tooltip("UGUI Text or TextMeshProUGUI")]
    public Graphic title;
    public Graphic description;
    public Image icon;
    [Header("Optional rarity decoration")]
    public Image rarityAccent;
    [Tooltip("Leave off to preserve the imported card artwork. Use Rarity Accent for a separate border/badge.")]
    public bool tintBackgroundByRarity;
    [Range(0, 2)] public int choiceIndex;
    public event Action<int> Clicked;

    void OnEnable() { if (button != null) button.onClick.AddListener(OnClicked); }
    void OnDisable() { if (button != null) button.onClick.RemoveListener(OnClicked); }
    private void OnClicked() { if (isActiveAndEnabled && button != null && button.IsInteractable()) Clicked?.Invoke(choiceIndex); }

    public void Bind(string heading, string body, Sprite sprite, Color color)
    {
        SetText(title, heading);
        SetText(description, body);
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
        }
        gameObject.SetActive(true);
        if (rarityAccent != null) rarityAccent.color = color;
        if (tintBackgroundByRarity && button != null && button.image != null) button.image.color = color;
    }

    public static bool SetText(Graphic label, string value)
    {
        if (label is Text text && text.text != value) { text.text = value; return true; }
        if (label is TMP_Text tmp && tmp.text != value) { tmp.text = value; return true; }
        return false;
    }
}
