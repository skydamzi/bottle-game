using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip gunFireClip;
    public AudioClip fistAttackClip;
    public AudioClip weaponSwapClip;
    private PlayerStats playerStats;

    void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
    }

    void OnEnable()
    {
        PlayerEvents.OnGunFired += OnGunFired;
        PlayerEvents.OnFistAttackReleased += OnFistAttackReleased;
        PlayerEvents.OnWeaponSwapped += OnWeaponSwapped;
    }

    void OnDisable()
    {
        PlayerEvents.OnGunFired -= OnGunFired;
        PlayerEvents.OnFistAttackReleased -= OnFistAttackReleased;
        PlayerEvents.OnWeaponSwapped -= OnWeaponSwapped;
    }

    private void OnGunFired(PlayerStats player)
    {
        if (player == null || player != playerStats) return;
        if (SoundManager.Instance != null && gunFireClip != null)
        {
            SoundManager.Instance.PlaySFX(gunFireClip);
        }
    }

    private void OnFistAttackReleased(PlayerStats player, float ratio)
    {
        if (player == null || player != playerStats) return;
        if (SoundManager.Instance != null && fistAttackClip != null)
        {
            SoundManager.Instance.PlaySFX(fistAttackClip);
        }
    }

    private void OnWeaponSwapped(PlayerStats player, PlayerAttackController.WeaponMode mode)
    {
        if (player == null || player != playerStats) return;
        if (SoundManager.Instance != null && weaponSwapClip != null)
        {
            SoundManager.Instance.PlaySFX(weaponSwapClip);
        }
    }
}
