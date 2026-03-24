using UnityEngine;

public class PlayerAimController : MonoBehaviour, IAimProvider
{
    public FixedJoystick joystick;
    public Transform neckTransform;
    public float rotationSpeed = 15f;

    public Quaternion FinalAimRotation { get; private set; }
    public bool IsInputActive { get; private set; }

    private bool isAttacking;
    private bool isCharging;
    private Quaternion lockedAttackRotation;

    void Awake()
    {
        if (neckTransform) FinalAimRotation = neckTransform.rotation;

        PlayerEvents.OnPlayerAttackStarted += OnAttackStarted;
        PlayerEvents.OnPlayerAttackEnded += OnAttackEnded;
        PlayerEvents.OnFistChargeUpdate += OnFistChargeUpdate;
        PlayerEvents.OnFistAttackReleased += OnFistAttackReleased;
        PlayerEvents.OnWeaponSwapped += OnWeaponSwapped;
        PlayerEvents.OnPlayerAimReset += ResetAim;
    }

    void OnDestroy()
    {
        PlayerEvents.OnPlayerAttackStarted -= OnAttackStarted;
        PlayerEvents.OnPlayerAttackEnded -= OnAttackEnded;
        PlayerEvents.OnFistChargeUpdate -= OnFistChargeUpdate;
        PlayerEvents.OnFistAttackReleased -= OnFistAttackReleased;
        PlayerEvents.OnWeaponSwapped -= OnWeaponSwapped;
        PlayerEvents.OnPlayerAimReset -= ResetAim;
    }

    private void OnAttackStarted()
    {
        isAttacking = true;
        // Lock the rotation at the moment the attack is initiated
        lockedAttackRotation = FinalAimRotation;
    }

    private void OnAttackEnded()
    {
        isAttacking = false;
        isCharging = false;
    }

    private void OnFistChargeUpdate(float ratio) => isCharging = ratio > 0.1f;
    private void OnFistAttackReleased(float ratio) => isCharging = false;
    private void OnWeaponSwapped(PlayerAttackController.WeaponMode mode) => isCharging = false;

    void LateUpdate()
    {
        if (!joystick || !neckTransform) return;

        Vector2 dir = new Vector2(joystick.Horizontal, joystick.Vertical);
        IsInputActive = dir.magnitude > 0.15f;

        if (IsInputActive)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            FinalAimRotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }

        if (isAttacking)
        {
            neckTransform.rotation = lockedAttackRotation;
        }
        else
        {
            // 조이스틱을 놓은 순간 기를 모으고 있었다면 정면으로 돌아가지 않도록 가드
            if (!IsInputActive && isCharging)
            {
                neckTransform.rotation = FinalAimRotation; 
            }
            else
            {
                Quaternion target = IsInputActive ? FinalAimRotation : Quaternion.identity;
                neckTransform.rotation = Quaternion.Slerp(neckTransform.rotation, target, rotationSpeed * Time.deltaTime);
            }
        }
    }

    public void ResetAim()
    {
        FinalAimRotation = Quaternion.identity;
        if (neckTransform) neckTransform.rotation = Quaternion.identity;
    }
}