using UnityEngine;

public class PortraitDummy : MonoBehaviour
{
    private Animator ani;

    void Awake()
    {
        ani = GetComponent<Animator>();
        if (GetComponent<Rigidbody2D>()) 
            GetComponent<Rigidbody2D>().simulated = false;
    }

    public void SyncAnimation(bool isMoving, float x, float y)
    {
        if (ani == null) return;
        ani.SetBool("isRunning", isMoving);
        ani.SetFloat("InputX", x);
        ani.SetFloat("InputY", y);
    }

    public void PlayAttack()
    {
        if (ani) ani.SetTrigger("attack");
    }
}