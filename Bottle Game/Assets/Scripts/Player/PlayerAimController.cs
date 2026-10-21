using UnityEngine;

[DefaultExecutionOrder(-30)]
public class PlayerAimController : MonoBehaviour, IAimProvider
{
    public FixedJoystick joystick;
    public Transform neckTransform;
    public float rotationSpeed = 15f;
    public Quaternion FinalAimRotation { get; private set; } = Quaternion.identity;
    public bool IsInputActive { get; private set; }
    public bool IsAutoAimAligned => neckTransform != null && autoTarget != null &&
        Quaternion.Angle(neckTransform.rotation, FinalAimRotation) <= 1f;
    private bool isAttacking, isCharging;
    private Quaternion lockedAttackRotation;
    private PlayerStats playerStats;
    private PlayerMovement movement;
    private Transform autoTarget;
    private PlayerAutoTarget targeting;

    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerMovement>();
        targeting = GetComponent<PlayerAutoTarget>();
        if (neckTransform != null) FinalAimRotation = neckTransform.rotation;
    }

    void OnEnable()
    {
        if (joystick != null) joystick.TouchCancelled += OnTouchCancelled;
        PlayerEvents.OnPlayerAttackStarted += OnAttackStarted;
        PlayerEvents.OnPlayerAttackEnded += OnAttackEnded;
        PlayerEvents.OnFistChargeUpdate += OnFistChargeUpdate;
        PlayerEvents.OnWeaponSwapped += OnWeaponSwapped;
        PlayerEvents.OnPlayerAimReset += OnAimReset;
    }

    void OnDisable()
    {
        if (joystick != null) joystick.TouchCancelled -= OnTouchCancelled;
        PlayerEvents.OnPlayerAttackStarted -= OnAttackStarted;
        PlayerEvents.OnPlayerAttackEnded -= OnAttackEnded;
        PlayerEvents.OnFistChargeUpdate -= OnFistChargeUpdate;
        PlayerEvents.OnWeaponSwapped -= OnWeaponSwapped;
        PlayerEvents.OnPlayerAimReset -= OnAimReset;
        isAttacking = isCharging = IsInputActive = false;
        autoTarget = null;
    }

    private void OnAttackStarted(PlayerStats player)
    {
        if (player == null || player != playerStats) return;
        isAttacking = true;
        lockedAttackRotation = FinalAimRotation;
    }
    private void OnAttackEnded(PlayerStats player) { if (player != null && player == playerStats) isAttacking = isCharging = false; }
    private void OnFistChargeUpdate(PlayerStats player, float ratio) { if (player != null && player == playerStats) isCharging = true; }
    private void OnWeaponSwapped(PlayerStats player, PlayerAttackController.WeaponMode mode) { if (player != null && player == playerStats) isCharging = false; }
    private void OnAimReset(PlayerStats player) { if (player != null && player == playerStats) ResetAim(); }
    private void OnTouchCancelled()
    {
        IsInputActive = false;
        GetComponent<PlayerAttackController>()?.CancelAttack();
    }

    void Update()
    {
        IsInputActive = false;
        if (movement != null && movement.IsSingleStick)
        {
            SetAutoTarget(targeting != null && targeting.HasTarget ? targeting.CurrentTarget : null);
            return;
        }
        if (movement != null && !movement.CanReadLocalInput) return;
        Vector2 dir = joystick != null ? joystick.Direction : Vector2.zero;
        IsInputActive = dir.magnitude > 0.15f;
        if (IsInputActive) SetDirection(dir);
    }

    public void SetAutoTarget(Transform target)
    {
        autoTarget = target;
        IsInputActive = target != null;
        if (target == null) return;
        SetDirection(target.position - (neckTransform != null ? neckTransform.position : transform.position));
    }

    private void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        FinalAimRotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
    }

    void LateUpdate()
    {
        if (neckTransform == null) return;
        if (isAttacking) { neckTransform.rotation = lockedAttackRotation; return; }
        bool hasAutoTarget = movement != null && movement.IsSingleStick && autoTarget != null;
        if (hasAutoTarget)
        {
            SetDirection(autoTarget.position - neckTransform.position);
        }
        Quaternion target = hasAutoTarget || IsInputActive || isCharging ? FinalAimRotation : Quaternion.identity;
        neckTransform.rotation = Quaternion.Slerp(neckTransform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    public void ResetAim()
    {
        autoTarget = null;
        IsInputActive = false;
        FinalAimRotation = Quaternion.identity;
        if (neckTransform != null && (movement == null || !movement.IsSingleStick)) neckTransform.rotation = Quaternion.identity;
    }
}
