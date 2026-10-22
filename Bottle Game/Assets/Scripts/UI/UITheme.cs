using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UIThemeRole { Panel, Overlay, HUD, PrimaryButton, SecondaryButton, RewardCard, RouteCard, Heading, Body, Accent, JoystickBase, JoystickHandle, WeaponButton, MenuButton, PortraitFrame }

[Serializable]
public class UIThemeStyle
{
    public Sprite sprite;
    public Image.Type imageType = Image.Type.Sliced;
    public bool applyTint;
    public Color tint = Color.white;
    public bool applyButtonStates;
    public Selectable.Transition transition = Selectable.Transition.ColorTint;
    public ColorBlock colors = ColorBlock.defaultColorBlock;
    public SpriteState spriteStates;
    [Min(0)] public int fontSize;
}

[CreateAssetMenu(menuName = "Bottle Game/UI/Theme", fileName = "UITheme")]
public class UITheme : ScriptableObject
{
    [Header("Fonts (optional; existing fonts/sizes are preserved when empty)")]
    public Font font;
    public TMP_FontAsset tmpFont;
    [Header("Artwork and button states")]
    public UIThemeStyle panel = new UIThemeStyle();
    public UIThemeStyle overlay = new UIThemeStyle();
    public UIThemeStyle hud = new UIThemeStyle();
    public UIThemeStyle primaryButton = new UIThemeStyle();
    public UIThemeStyle secondaryButton = new UIThemeStyle();
    public UIThemeStyle rewardCard = new UIThemeStyle();
    public UIThemeStyle routeCard = new UIThemeStyle();
    public UIThemeStyle heading = new UIThemeStyle();
    public UIThemeStyle body = new UIThemeStyle();
    public UIThemeStyle accent = new UIThemeStyle();
    [Header("Gameplay controls")]
    public UIThemeStyle joystickBase = new UIThemeStyle();
    public UIThemeStyle joystickHandle = new UIThemeStyle();
    public UIThemeStyle weaponButton = new UIThemeStyle();
    public UIThemeStyle menuButton = new UIThemeStyle();
    public UIThemeStyle portraitFrame = new UIThemeStyle();
    [Header("Reward rarity accents")]
    public Color common = new Color(0.21f, 0.26f, 0.28f);
    public Color uncommon = new Color(0.08f, 0.31f, 0.23f);
    public Color rare = new Color(0.12f, 0.23f, 0.42f);
    public Color legendary = new Color(0.42f, 0.3f, 0.1f);

    public UIThemeStyle GetStyle(UIThemeRole role)
    {
        switch (role)
        {
            case UIThemeRole.Overlay: return overlay;
            case UIThemeRole.HUD: return hud;
            case UIThemeRole.PrimaryButton: return primaryButton;
            case UIThemeRole.SecondaryButton: return secondaryButton;
            case UIThemeRole.RewardCard: return rewardCard;
            case UIThemeRole.RouteCard: return routeCard;
            case UIThemeRole.Heading: return heading;
            case UIThemeRole.Body: return body;
            case UIThemeRole.Accent: return accent;
            case UIThemeRole.JoystickBase: return joystickBase;
            case UIThemeRole.JoystickHandle: return joystickHandle;
            case UIThemeRole.WeaponButton: return weaponButton;
            case UIThemeRole.MenuButton: return menuButton;
            case UIThemeRole.PortraitFrame: return portraitFrame;
            default: return panel;
        }
    }

    public Color GetRarityColor(ArtifactRarity rarity)
    {
        switch (rarity)
        {
            case ArtifactRarity.Uncommon: return uncommon;
            case ArtifactRarity.Rare: return rare;
            case ArtifactRarity.Legendary: return legendary;
            default: return common;
        }
    }
}
