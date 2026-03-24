using System;

public static class PlayerEvents
{
    // Player Attack States
    public static event Action OnPlayerAttackStarted;
    public static void RaisePlayerAttackStarted() => OnPlayerAttackStarted?.Invoke();

    public static event Action OnPlayerAttackEnded;
    public static void RaisePlayerAttackEnded() => OnPlayerAttackEnded?.Invoke();

    // Weapon Specific Actions
    public static event Action OnGunFired;
    public static void RaiseGunFired() => OnGunFired?.Invoke();

    public static event Action<float> OnFistChargeUpdate;
    public static void RaiseFistChargeUpdate(float ratio) => OnFistChargeUpdate?.Invoke(ratio);

    public static event Action<float> OnFistAttackReleased;
    public static void RaiseFistAttackReleased(float ratio) => OnFistAttackReleased?.Invoke(ratio);
    
    // Weapon Swapping
    public static event Action<PlayerAttackController.WeaponMode> OnWeaponSwapped;
    public static void RaiseWeaponSwapped(PlayerAttackController.WeaponMode mode) => OnWeaponSwapped?.Invoke(mode);

    // Player General
    public static event Action OnPlayerAimReset;
    public static void RaisePlayerAimReset() => OnPlayerAimReset?.Invoke();
}
