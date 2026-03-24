using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip gunFireClip;
    public AudioClip fistAttackClip;
    public AudioClip weaponSwapClip;

    void Awake()
    {
        PlayerEvents.OnGunFired += OnGunFired;
        PlayerEvents.OnFistAttackReleased += OnFistAttackReleased;
        PlayerEvents.OnWeaponSwapped += OnWeaponSwapped;
    }

    void OnDestroy()
    {
        PlayerEvents.OnGunFired -= OnGunFired;
        PlayerEvents.OnFistAttackReleased -= OnFistAttackReleased;
        PlayerEvents.OnWeaponSwapped -= OnWeaponSwapped;
    }

    private void OnGunFired()
    {
        if (SoundManager.Instance != null && gunFireClip != null)
        {
            SoundManager.Instance.PlaySFX(gunFireClip);
        }
    }

    private void OnFistAttackReleased(float ratio)
    {
        if (SoundManager.Instance != null && fistAttackClip != null)
        {
            SoundManager.Instance.PlaySFX(fistAttackClip);
        }
    }

    private void OnWeaponSwapped(PlayerAttackController.WeaponMode mode)
    {
        if (SoundManager.Instance != null && weaponSwapClip != null)
        {
            SoundManager.Instance.PlaySFX(weaponSwapClip);
        }
    }
}
