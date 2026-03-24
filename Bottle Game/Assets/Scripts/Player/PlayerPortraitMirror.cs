using UnityEngine;

public class PlayerPortraitMirror : MonoBehaviour
{
    public Animator portraitAnim;
    private Animator mainAnim;

    void Start() => mainAnim = GetComponent<Animator>();

    void Update()
    {
        if (mainAnim == null || portraitAnim == null) return;

        float mainAnimTime = mainAnim.GetFloat("animTime");
        portraitAnim.SetFloat("animTime", mainAnimTime);
        portraitAnim.SetBool("isRunning", mainAnim.GetBool("isRunning"));

        portraitAnim.SetFloat("InputX", 0);
        portraitAnim.SetFloat("InputY", -1);

        AnimatorStateInfo mainState = mainAnim.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo portraitState = portraitAnim.GetCurrentAnimatorStateInfo(0);

        if (mainState.IsName("Attack_Blend_Tree"))
        {
            if (!portraitState.IsName("Attack_Blend_Tree"))
            {
                portraitAnim.SetTrigger("attack");
            }
        }

        else if (mainState.IsName("Idle_Blend_Tree") || mainState.IsName("Run_Blend_Tree"))
        {
            if (portraitState.IsName("Attack_Blend_Tree"))
            {
                portraitAnim.Play(mainState.fullPathHash, 0, 0f);
            }
        }
    }
}