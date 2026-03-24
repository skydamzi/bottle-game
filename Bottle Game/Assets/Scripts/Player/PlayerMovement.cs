using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public FixedJoystick joystick;
    public float tiltAngle = 10f;
    public float tiltSpeed = 30f;
    Vector2 lastNonZeroMoveInput = Vector2.down;
    public Rigidbody2D player_rb;
    public Animator player_ani;
    private bool canMove = true;
    private PlayerStats playerStats;

    void Awake()
    {
        player_rb = GetComponent<Rigidbody2D>();
        player_ani = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();

        PlayerEvents.OnPlayerAttackStarted += OnPlayerAttackStarted;
        PlayerEvents.OnPlayerAttackEnded += OnPlayerAttackEnded;
    }

    void OnDestroy()
    {
        PlayerEvents.OnPlayerAttackStarted -= OnPlayerAttackStarted;
        PlayerEvents.OnPlayerAttackEnded -= OnPlayerAttackEnded;
    }

    private void OnPlayerAttackStarted() => canMove = false;
    private void OnPlayerAttackEnded() => canMove = true;

    void Update() => Movement();

    void Movement()
    {
        if (joystick == null) return;
        Vector2 input = new Vector2(joystick.Horizontal, joystick.Vertical);
        bool isMoving = input.magnitude > 0.2f;

        if (isMoving && canMove) lastNonZeroMoveInput = input.normalized;

        if (!canMove)
        {
            player_rb.velocity = Vector2.zero;
            player_ani.SetBool("isRunning", false);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * tiltSpeed);
            return;
        }

        float targetZ = isMoving ? -lastNonZeroMoveInput.x * tiltAngle * Mathf.Abs(lastNonZeroMoveInput.x) : 0;
        player_rb.velocity = isMoving ? lastNonZeroMoveInput * playerStats.moveSpeed : Vector2.zero;

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetZ), Time.deltaTime * tiltSpeed);
        player_ani.SetBool("isRunning", isMoving);
        player_ani.SetFloat("InputX", lastNonZeroMoveInput.x);
        player_ani.SetFloat("InputY", lastNonZeroMoveInput.y);
    }
}