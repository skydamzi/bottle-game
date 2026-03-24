using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    public float speed = 3f;
    [Header("Tilt Settings")]
    public float tiltAngle = 10f;
    public float tiltSpeed = 30f;

    private Transform player;
    private Rigidbody2D rb;
    private Animator monster_ani;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
        monster_ani = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * speed;

            bool isMoving = rb.velocity.magnitude > 0.1f;

            if(monster_ani != null)
            {
                monster_ani.SetBool("isRunning", isMoving);
                monster_ani.SetFloat("InputX", direction.x);
                monster_ani.SetFloat("InputY", direction.y);
            }

            float targetZ = isMoving ? -direction.x * tiltAngle * Mathf.Abs(direction.x) : 0;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetZ), Time.deltaTime * tiltSpeed);
        }
        else
        {
            rb.velocity = Vector2.zero;
            if(monster_ani != null)
            {
                monster_ani.SetBool("isRunning", false);
            }
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * tiltSpeed);
        }
    }
}
