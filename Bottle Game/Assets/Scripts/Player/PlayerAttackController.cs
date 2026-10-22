using UnityEngine;

[DefaultExecutionOrder(-20)]
public class PlayerAttackController : MonoBehaviour
{
    public enum WeaponMode { Gun, Fist }
    public WeaponMode currentMode = WeaponMode.Gun;
    public Transform neckTransform;
    public Animator anim;
    public float maxChargeTime = 2f;
    [Min(0f)] public float autoFistChargeTime;
    public bool isAttacking { get; private set; }
    public bool IsCharging => chargeTimer > 0f;
    public Quaternion LockedRotation { get; private set; }
    private PlayerStats playerStats;
    private IAimProvider aimProvider;
    private PlayerAimController aim;
    private PlayerMovement movement;
    private PlayerDodgeController dodge;
    private PlayerAutoTarget autoTarget;
    private NeckEffectController effects;
    private Gun gun;
    private float nextFireTime, chargeTimer, attackDeadline;
    private Transform aimTarget;

    void Awake()
    {
        gun = GetComponentInChildren<Gun>(true);
        aimProvider = GetComponent<IAimProvider>();
        aim = GetComponent<PlayerAimController>();
        playerStats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerMovement>();
        dodge = GetComponent<PlayerDodgeController>();
        autoTarget = GetComponent<PlayerAutoTarget>();
        effects = GetComponent<NeckEffectController>();
        aimTarget = new GameObject("GunAimTarget").transform;
        aimTarget.SetParent(transform);
    }

    void Start() { if (gun != null) gun.gameObject.SetActive(currentMode == WeaponMode.Gun); }
    void OnDisable() => CancelAttack();

    void Update()
    {
        if (playerStats == null || playerStats.IsDead || playerStats.IsStunned ||
            (movement != null && !movement.CanReadLocalInput) || (dodge != null && dodge.IsDodging))
        {
            CancelAttack();
            return;
        }
        if (movement != null && movement.IsSingleStick)
        {
            if (movement.HasMoveInput)
            {
                CancelAttack();
                return;
            }
            // Target selection is for starting the next attack. Losing a target must
            // not cut the current punch short before its retraction has finished.
            if (isAttacking) { CheckDeadline(); return; }
            Transform target = autoTarget != null && autoTarget.HasTarget ? autoTarget.CurrentTarget : null;
            if (target == null)
            {
                CancelAttack();
                return;
            }
            // Tracking stays active during movement, recovery and cooldowns.
            // Automatic gun fire depends on target validity, not visual rotation.
            if (!movement.IsNeutralReady) { CancelAttack(); return; }
            if (currentMode == WeaponMode.Gun) HandleGunAttack(target);
            else if (aim != null && !aim.IsAutoAimAligned) return;
            else if (autoFistChargeTime <= 0f) HandleRelease();
            else
            {
                HandleFistCharge();
                if (chargeTimer >= Mathf.Min(autoFistChargeTime, Mathf.Max(0.01f, maxChargeTime))) HandleRelease();
            }
            return;
        }
        if (isAttacking) { CheckDeadline(); return; }
        if (aimProvider == null) return;
        if (aimProvider.IsInputActive)
        {
            if (currentMode == WeaponMode.Gun) HandleGunAttack();
            else HandleFistCharge();
        }
        else if (currentMode == WeaponMode.Fist && IsCharging) HandleRelease();
    }

    private void CheckDeadline()
    {
        if (Time.time < attackDeadline) return;
        // Low frame rates can stretch coroutine timing. Only recover a stalled
        // animation; a healthy extension/hold/retraction must finish naturally.
        if (effects != null && effects.IsFistAnimationAdvancing) return;
        CancelAttack();
    }

    public void ToggleWeapon()
    {
        if (isAttacking || playerStats == null || playerStats.IsDead) return;
        SwapWeapon(currentMode == WeaponMode.Gun ? WeaponMode.Fist : WeaponMode.Gun);
    }

    void HandleGunAttack(Transform autoAimTarget = null)
    {
        if (Time.time < nextFireTime) return;
        if (gun != null && aimTarget != null && neckTransform != null)
        {
            // Legacy input uses the barrel direction. Auto fire uses the selected
            // enemy so cosmetic smoothing cannot delay shots or misdirect bullets.
            if (autoAimTarget == null) aimTarget.position = neckTransform.position + neckTransform.up * 10f;
            if (gun.TryAttack(autoAimTarget != null ? autoAimTarget : aimTarget, playerStats.EffectiveAttackDamage))
                PlayerEvents.RaiseGunFired(playerStats);
        }
        nextFireTime = Time.time + 0.1f / playerStats.AttackSpeedMultiplier;
    }

    void HandleFistCharge()
    {
        chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime * playerStats.AttackSpeedMultiplier, Mathf.Max(0.01f, maxChargeTime));
        PlayerEvents.RaiseFistChargeUpdate(playerStats, chargeTimer / Mathf.Max(0.01f, maxChargeTime));
    }

    void HandleRelease()
    {
        if (isAttacking || (effects != null && effects.IsFistAnimating)) return;
        if (effects == null || !effects.isActiveAndEnabled) { CancelAttack(); return; }
        float ratio = Mathf.Clamp01(chargeTimer / Mathf.Max(0.01f, maxChargeTime));
        chargeTimer = 0f;
        LockedRotation = aimProvider != null ? aimProvider.FinalAimRotation : Quaternion.identity;
        isAttacking = true;
        // Gameplay owns the timeout even if a visual coroutine is stopped externally.
        attackDeadline = Time.time + NeckEffectController.FistDuration / playerStats.AttackSpeedMultiplier + 0.2f;
        PlayerEvents.RaisePlayerAttackStarted(playerStats);
        PlayerEvents.RaiseFistAttackReleased(playerStats, ratio);
    }

    public void CompleteAttack()
    {
        if (effects != null && effects.IsFistAnimating) return;
        CancelAttack();
    }

    public void CancelAttack()
    {
        bool hadAttack = isAttacking || IsCharging;
        isAttacking = false;
        chargeTimer = 0f;
        if (effects != null && (hadAttack || effects.isStretching)) effects.CancelEffects();
        if (hadAttack && playerStats != null) PlayerEvents.RaisePlayerAttackEnded(playerStats);
    }

    public void SwapWeapon(WeaponMode mode)
    {
        CancelAttack();
        currentMode = mode;
        if (gun != null) gun.gameObject.SetActive(mode == WeaponMode.Gun);
        PlayerEvents.RaiseWeaponSwapped(playerStats, mode);
        PlayerEvents.RaisePlayerAimReset(playerStats);
    }

    public void ResetForExpedition()
    {
        CancelAttack();
        nextFireTime = 0f;
    }
}
