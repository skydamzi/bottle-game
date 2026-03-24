using UnityEngine;

public class PlayerAttackController : MonoBehaviour
{
    public enum WeaponMode { Gun, Fist }
    public WeaponMode currentMode = WeaponMode.Gun;
    public Transform neckTransform;
    public Animator anim;
    public float maxChargeTime = 2f;
    private PlayerStats playerStats;

    private IAimProvider aimProvider;
    private Gun gun;
    private float nextFireTime, chargeTimer;

    public bool isAttacking { get; private set; }
    public bool IsCharging => chargeTimer > 0;
    public Quaternion LockedRotation { get; private set; }
    private Transform aimTarget;

    void Start()
    {
        gun = GetComponentInChildren<Gun>(true);
        aimProvider = GetComponent<IAimProvider>();
        playerStats = GetComponent<PlayerStats>();

        PlayerEvents.OnPlayerAttackStarted += OnAttackStarted;
        PlayerEvents.OnPlayerAttackEnded += OnAttackEnded;

        if (gun)
        {
            gun.gameObject.SetActive(currentMode == WeaponMode.Gun);
        }

        if (aimTarget == null)
        {
            aimTarget = new GameObject("GunAimTarget").transform;
            aimTarget.SetParent(transform);
        }

        if (neckTransform) LockedRotation = neckTransform.rotation;
    }

    void OnDestroy()
    {
        PlayerEvents.OnPlayerAttackStarted -= OnAttackStarted;
        PlayerEvents.OnPlayerAttackEnded -= OnAttackEnded;
    }

    private void OnAttackStarted() => isAttacking = true;
    private void OnAttackEnded() => isAttacking = false;

    void Update()
    {
        if (isAttacking || aimProvider == null) return;

        if (aimProvider.IsInputActive)
        {
            if (currentMode == WeaponMode.Gun) HandleGunAttack();
            else HandleFistCharge();
        }
        else if (currentMode == WeaponMode.Fist && chargeTimer > 0)
        {
            LockedRotation = aimProvider.FinalAimRotation;
            HandleRelease();
        }
    }

    public void ToggleWeapon()
    {
        if (isAttacking) return;
        WeaponMode nextMode = (currentMode == WeaponMode.Gun) ? WeaponMode.Fist : WeaponMode.Gun;
        SwapWeapon(nextMode);
    }

    void HandleGunAttack()
    {
        if (Time.time >= nextFireTime) 
        { 
            if (gun != null && aimTarget != null && playerStats != null && neckTransform != null)
            {
                aimTarget.position = neckTransform.position + neckTransform.up * 10f;
                gun.Attack(aimTarget, playerStats.attackDamage);
                PlayerEvents.RaiseGunFired();
            }
            nextFireTime = Time.time + 0.1f; 
        }
    }

    void HandleFistCharge() 
    { 
        chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, maxChargeTime);
        PlayerEvents.RaiseFistChargeUpdate(chargeTimer / maxChargeTime);
    }

    void HandleRelease()
    {
        if (anim) anim.SetTrigger("attack");
        PlayerEvents.RaisePlayerAttackStarted();
        PlayerEvents.RaiseFistAttackReleased(chargeTimer / maxChargeTime);
        chargeTimer = 0;
    }

    public void SwapWeapon(WeaponMode mode) 
    { 
        currentMode = mode; 
        if (gun) gun.gameObject.SetActive(mode == WeaponMode.Gun); 
        chargeTimer = 0;
        PlayerEvents.RaiseWeaponSwapped(mode);
        PlayerEvents.RaisePlayerAimReset();
    }
}