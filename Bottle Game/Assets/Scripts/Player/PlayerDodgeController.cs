using UnityEngine;

[DefaultExecutionOrder(-40)]
public class PlayerDodgeController : MonoBehaviour
{
    public JoystickFlickDetector flickDetector;
    [Min(0f)] public float cooldown = 0.5f;
    [Min(0.01f)] public float duration = 0.3f;
    [Min(0f)] public float distance = 2.5f;
    [Min(0f)] public float invulnerableStart = 0.05f;
    [Min(0f)] public float invulnerableEnd = 0.25f;
    public bool showFeedback = true;
    public Color dodgeTint = new Color(0.35f, 1f, 1f, 1f);
    public bool IsDodging { get; private set; }
    public Vector2 Direction { get; private set; }
    public Vector2 Velocity => IsDodging ? Direction * dodgeSpeed : Vector2.zero;
    public bool IsInvulnerable => IsDodging && stats != null && !stats.IsDead &&
        Elapsed >= Mathf.Max(0f, invulnerableStart) && Elapsed < Mathf.Min(activeDuration, invulnerableEnd);
    private float Elapsed => Time.fixedTime - startedAt;
    private float startedAt, nextDodgeTime, activeDuration, dodgeSpeed;
    private PlayerStats stats;
    private PlayerMovement movement;
    private SpriteRenderer sprite;
    private Color originalColor;
    private bool tinted;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerMovement>();
        sprite = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (flickDetector == null) flickDetector = GetComponent<JoystickFlickDetector>();
        if (flickDetector != null) flickDetector.FlickDetected += OnFlick;
    }

    void OnDisable()
    {
        if (flickDetector != null) flickDetector.FlickDetected -= OnFlick;
        CancelDodge();
    }

    private void OnFlick(Vector2 direction) => TryDodge(direction);

    public bool TryDodge(Vector2 direction)
    {
        if (!isActiveAndEnabled || IsDodging || Time.time < nextDodgeTime || direction.sqrMagnitude < 0.001f) return false;
        if (stats == null || stats.IsDead || stats.IsStunned || movement == null || !movement.CanReadLocalInput || !movement.IsSingleStick) return false;
        GetComponent<PlayerAttackController>()?.CancelAttack();
        Direction = direction.normalized;
        activeDuration = Mathf.Max(Time.fixedDeltaTime, duration);
        dodgeSpeed = Mathf.Max(0f, distance) / activeDuration;
        // The first velocity step belongs to the next physics tick, not the last one.
        startedAt = Time.fixedTime + Time.fixedDeltaTime;
        nextDodgeTime = Time.time + Mathf.Max(0f, cooldown);
        IsDodging = true;
        stats.RunBuild?.NotifyDodgeStarted();
        if (showFeedback && sprite != null)
        {
            originalColor = sprite.color;
            sprite.color = dodgeTint;
            tinted = true;
        }
        return true;
    }

    public void TickPhysics()
    {
        if (IsDodging && (stats == null || stats.IsDead || stats.IsStunned || Elapsed >= activeDuration - 0.0001f)) CancelDodge();
    }

    public void CancelDodge()
    {
        IsDodging = false;
        if (tinted && sprite != null) sprite.color = originalColor;
        tinted = false;
    }

    public void ResetForExpedition()
    {
        CancelDodge();
        nextDodgeTime = 0f;
    }

    void OnApplicationFocus(bool focused) { if (!focused) CancelDodge(); }
    void OnApplicationPause(bool paused) { if (paused) CancelDodge(); }
}
