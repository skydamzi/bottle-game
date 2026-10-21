using UnityEngine;

[DefaultExecutionOrder(-50)]
public class PlayerMovement : MonoBehaviour
{
    public enum ControlMode { LegacyDualStick, SingleStickFlick }
    public ControlMode controlMode = ControlMode.LegacyDualStick;
    [Tooltip("Only the owning local player reads UI input. This is not network authority.")]
    public bool acceptsLocalInput = true;
    public FixedJoystick joystick;
    public float tiltAngle = 10f;
    public float tiltSpeed = 30f;
    public Rigidbody2D player_rb;
    public Animator player_ani;
    [Range(0f, 0.9f)] public float movementDeadZone = 0.2f;
    public bool proportionalSpeed;
    [Min(0f)] public float autoAttackDelay = 0.12f;
    public GameObject[] legacyActionUI;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LastMoveDirection { get; private set; } = Vector2.down;
    public bool HasMoveInput => MoveInput.magnitude > movementDeadZone;
    public bool IsSingleStick => controlMode == ControlMode.SingleStickFlick;
    public bool CanReadLocalInput => isActiveAndEnabled && acceptsLocalInput && hasFocus && !paused;
    public bool IsNeutralReady => CanReadLocalInput && !HasMoveInput && Time.time >= neutralSince + autoAttackDelay;
    public Vector2 AppliedVelocity { get; private set; }
    private PlayerStats playerStats;
    private PlayerAttackController attack;
    private PlayerAimController aim;
    private PlayerDodgeController dodge;
    private JoystickFlickDetector flick;
    private bool hasFocus = true, paused;
    private float neutralSince;
    private ControlMode appliedMode;
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[32];
    private Collider2D[] movementColliders;

    void OnEnable()
    {
        if (joystick != null) joystick.TouchCancelled += OnTouchCancelled;
    }

    private void OnTouchCancelled()
    {
        MoveInput = Vector2.zero;
        neutralSince = Time.time;
        attack?.CancelAttack();
        flick?.CancelGesture();
    }

    void Awake()
    {
        player_rb = GetComponent<Rigidbody2D>();
        player_ani = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();
        attack = GetComponent<PlayerAttackController>();
        aim = GetComponent<PlayerAimController>();
        dodge = GetComponent<PlayerDodgeController>();
        flick = GetComponent<JoystickFlickDetector>();
        movementColliders = GetComponentsInChildren<Collider2D>(true);
    }

    void Start() { ApplyMode(); ResetInput(); }

    void Update()
    {
        if (appliedMode != controlMode) ApplyMode();
        MoveInput = CanReadLocalInput && joystick != null ? joystick.Direction : Vector2.zero;
        if (HasMoveInput || !CanReadLocalInput || (dodge != null && dodge.IsDodging)) neutralSince = Time.time;
        if (HasMoveInput && (attack == null || !attack.isAttacking) && (dodge == null || !dodge.IsDodging))
            LastMoveDirection = MoveInput.normalized;
        if (!CanReadLocalInput) { attack?.CancelAttack(); dodge?.CancelDodge(); }
    }

    private void ApplyMode()
    {
        appliedMode = controlMode;
        attack?.CancelAttack();
        dodge?.CancelDodge();
        flick?.CancelGesture();
        ResetInput();
        if (!acceptsLocalInput) return;
        if (aim != null && aim.joystick != null) aim.joystick.gameObject.SetActive(!IsSingleStick);
        if (legacyActionUI != null)
            foreach (GameObject ui in legacyActionUI) if (ui != null) ui.SetActive(!IsSingleStick);
    }

    void FixedUpdate()
    {
        dodge?.TickPhysics();
        Vector2 velocity = Vector2.zero;
        if (playerStats != null && !playerStats.IsDead && CanReadLocalInput)
        {
            if (playerStats.IsStunned) velocity = playerStats.KnockbackVelocity;
            else if (dodge != null && dodge.IsDodging) velocity = dodge.Velocity;
            else if ((attack == null || !attack.isAttacking) && HasMoveInput)
            {
                float strength = IsSingleStick && proportionalSpeed ? Mathf.Clamp01(MoveInput.magnitude) : 1f;
                velocity = LastMoveDirection * playerStats.moveSpeed * strength;
            }
        }
        // Sweep solid foot colliders as well as keeping normal physics contacts active.
        // Triggers (hurtboxes/attacks) never stop a dodge.
        if (player_rb != null && velocity.sqrMagnitude > 0f)
        {
            float distance = velocity.magnitude * Time.fixedDeltaTime;
            float allowed = distance;
            foreach (Collider2D col in movementColliders)
            {
                if (col == null || col.isTrigger || !col.enabled || !col.gameObject.activeInHierarchy || col.attachedRigidbody != player_rb) continue;
                var filter = new ContactFilter2D();
                filter.SetLayerMask(Physics2D.GetLayerCollisionMask(col.gameObject.layer));
                filter.useTriggers = false;
                int count = col.Cast(velocity.normalized, filter, wallHits, distance + 0.01f);
                if (count == wallHits.Length) allowed = 0f;
                for (int i = 0; i < count; i++)
                    if (wallHits[i].collider != null && wallHits[i].normal.sqrMagnitude > 0f &&
                        Vector2.Dot(velocity, wallHits[i].normal) < -0.001f)
                        allowed = Mathf.Min(allowed, Mathf.Max(0f, wallHits[i].distance - 0.01f));
            }
            velocity *= distance > 0f ? allowed / distance : 0f;
        }
        AppliedVelocity = velocity;
        if (player_rb != null && player_rb.bodyType != RigidbodyType2D.Static) player_rb.velocity = velocity;
        bool moving = velocity.sqrMagnitude > 0.01f;
        Vector2 facing = dodge != null && dodge.IsDodging ? dodge.Direction : LastMoveDirection;
        if (player_ani != null && player_ani.runtimeAnimatorController != null)
        {
            player_ani.SetBool("isRunning", moving);
            // Keep the attack Blend Tree facing exactly as it was at launch.
            // Input is still sampled so held movement resumes after the punch.
            if (attack == null || !attack.isAttacking)
            {
                player_ani.SetFloat("InputX", facing.x);
                player_ani.SetFloat("InputY", facing.y);
            }
        }
        float targetZ = moving ? -facing.x * tiltAngle * Mathf.Abs(facing.x) : 0f;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetZ), Time.fixedDeltaTime * tiltSpeed);
    }

    public void ResetInput()
    {
        MoveInput = Vector2.zero;
        neutralSince = Time.time;
        joystick?.CancelInput();
        if (aim != null) aim.joystick?.CancelInput();
        flick?.CancelGesture();
    }

    void OnDisable()
    {
        if (joystick != null) joystick.TouchCancelled -= OnTouchCancelled;
        ResetInput();
        attack?.CancelAttack();
        dodge?.CancelDodge();
        AppliedVelocity = Vector2.zero;
        if (player_rb != null && player_rb.bodyType != RigidbodyType2D.Static) player_rb.velocity = Vector2.zero;
    }

    void OnApplicationFocus(bool focused) { hasFocus = focused; ResetInput(); if (!focused) { attack?.CancelAttack(); dodge?.CancelDodge(); } }
    void OnApplicationPause(bool value) { paused = value; ResetInput(); if (value) { attack?.CancelAttack(); dodge?.CancelDodge(); } }
}
