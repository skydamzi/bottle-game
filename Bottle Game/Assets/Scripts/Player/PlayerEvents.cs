using System;

public static class PlayerEvents
{
    // Player Attack States
    public static event Action<PlayerStats> OnPlayerAttackStarted;
    public static void RaisePlayerAttackStarted(PlayerStats player) => OnPlayerAttackStarted?.Invoke(player);

    public static event Action<PlayerStats> OnPlayerAttackEnded;
    public static void RaisePlayerAttackEnded(PlayerStats player) => OnPlayerAttackEnded?.Invoke(player);

    // Weapon Specific Actions
    public static event Action<PlayerStats> OnGunFired;
    public static void RaiseGunFired(PlayerStats player) => OnGunFired?.Invoke(player);

    public static event Action<PlayerStats, float> OnFistChargeUpdate;
    public static void RaiseFistChargeUpdate(PlayerStats player, float ratio) => OnFistChargeUpdate?.Invoke(player, ratio);

    public static event Action<PlayerStats, float> OnFistAttackReleased;
    public static void RaiseFistAttackReleased(PlayerStats player, float ratio) => OnFistAttackReleased?.Invoke(player, ratio);
    
    // Weapon Swapping
    public static event Action<PlayerStats, PlayerAttackController.WeaponMode> OnWeaponSwapped;
    public static void RaiseWeaponSwapped(PlayerStats player, PlayerAttackController.WeaponMode mode) => OnWeaponSwapped?.Invoke(player, mode);

    // Player General
    public static event Action<PlayerStats> OnPlayerAimReset;
    public static void RaisePlayerAimReset(PlayerStats player) => OnPlayerAimReset?.Invoke(player);
}
