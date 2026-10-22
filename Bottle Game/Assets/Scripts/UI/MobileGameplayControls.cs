using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class MobileGameplayControls : MonoBehaviour
{
    public ExpeditionManager expedition;
    public PlayerMovement player;
    private CanvasGroup group;
    private bool wasEnabled;

    void Awake() { group = GetComponent<CanvasGroup>(); }
    void OnEnable()
    {
        if (expedition != null) expedition.Changed += Refresh;
        Refresh();
    }
    void OnDisable()
    {
        if (expedition != null) expedition.Changed -= Refresh;
        if (wasEnabled && player != null) player.ResetInput();
        wasEnabled = false;
    }

    public void Refresh()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        bool enabled = expedition == null || expedition.State == ExpeditionState.Combat || expedition.State == ExpeditionState.BossCombat;
        group.interactable = group.blocksRaycasts = enabled;
        if (wasEnabled && !enabled && player != null) player.ResetInput();
        wasEnabled = enabled;
    }
}
