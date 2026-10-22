using System.Collections;
using UnityEngine;

public class NeckEffectController : MonoBehaviour
{
    public const float FistDuration = 0.65f;
    public Transform neckTransform;
    public Transform gloveTransform;
    public Transform neckCover; 
    public FistHitbox gloveHitbox;
    public float shakeIntensity;
    public bool isStretching { get; private set; }
    public bool IsFistAnimating { get; private set; }
    public bool IsFistAnimationAdvancing => IsFistAnimating && Time.frameCount - lastFistAnimationFrame <= 2;
    private int lastFistAnimationFrame;

    private Vector3 originalNeckPos;
    private Vector3 gloveOriginalScale;
    private Vector3 originalNeckCoverScale;
    private float initialGloveY; 
    private Animator anim;
    public Animator portraitAnim;
    private PlayerStats playerStats;
    private PlayerAttackController attack;
    private Vector3 originalNeckScale, originalGlovePos;
    private bool hasStarted;

    void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
        attack = GetComponentInParent<PlayerAttackController>();
        anim = GetComponentInParent<Animator>();
        if (neckTransform != null)
        {
            originalNeckPos = neckTransform.localPosition;
            originalNeckScale = neckTransform.localScale;
        }
        if (gloveTransform != null)
        {
            gloveOriginalScale = gloveTransform.localScale;
            originalGlovePos = gloveTransform.localPosition;
            initialGloveY = originalGlovePos.y;
        }
        if (neckCover != null) originalNeckCoverScale = neckCover.localScale;
    }

    void OnEnable()
    {
        PlayerEvents.OnGunFired += OnGunFired;
        PlayerEvents.OnFistAttackReleased += OnFistAttack;
        PlayerEvents.OnFistChargeUpdate += OnFistChargeUpdate;
        PlayerEvents.OnWeaponSwapped += OnWeaponSwapped;
        if (hasStarted) { ResetAnimation(anim); ResetAnimation(portraitAnim); }
    }

    void Start()
    {
        hasStarted = true;
        CancelEffects();
    }

    void OnDisable()
    {
        PlayerEvents.OnGunFired -= OnGunFired;
        PlayerEvents.OnFistAttackReleased -= OnFistAttack;
        PlayerEvents.OnFistChargeUpdate -= OnFistChargeUpdate;
        PlayerEvents.OnWeaponSwapped -= OnWeaponSwapped;
        attack?.CancelAttack();
        CancelEffects();
    }

    public void CancelEffects()
    {
        StopAllCoroutines();
        isStretching = false;
        IsFistAnimating = false;
        if (gloveHitbox != null) gloveHitbox.DeactivateHitbox();
        if (neckTransform != null)
        {
            neckTransform.localPosition = originalNeckPos;
            neckTransform.localScale = originalNeckScale;
            // Aim owns rotation, including the smooth return after a directional punch.
            // Reset only the stretch here; snapping rotation would bypass that return.
        }
        if (neckCover != null) neckCover.localScale = originalNeckCoverScale;
        if (gloveTransform != null)
        {
            gloveTransform.localPosition = originalGlovePos;
            gloveTransform.localScale = gloveOriginalScale;
            gloveTransform.gameObject.SetActive(false);
        }
        ResetAnimation(anim);
        ResetAnimation(portraitAnim);
    }

    private void ResetAnimation(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.ResetTrigger("attack");
        animator.SetFloat("animTime", 0f);
        if (animator.isActiveAndEnabled) animator.Play("Idle_Blend_Tree", 0, 0f);
    }

    private void UpdateNeckScale(Vector3 newScale)
    {
        if (neckTransform == null) return;
        neckTransform.localScale = newScale;
        if (neckCover != null && newScale.y > 0.01f)
        {
            neckCover.localScale = new Vector3(
                originalNeckCoverScale.x, 
                originalNeckCoverScale.y / newScale.y, 
                originalNeckCoverScale.z
            );
        }
    }

    private void OnGunFired(PlayerStats player)
    {
        if (player == null || player != playerStats || neckTransform == null) return;
        if (isStretching) return;
        StartCoroutine(QuickStretchRoutine());
    }

    private void OnFistAttack(PlayerStats player, float ratio)
    {
        if (player == null || player != playerStats) return;
        if (IsFistAnimating) return;
        CancelEffects();
        if (neckTransform == null) { attack?.CompleteAttack(); return; }
        neckTransform.localPosition = originalNeckPos;
        StartCoroutine(StretchRoutine(ratio, 4f));
    }

    private void OnFistChargeUpdate(PlayerStats player, float ratio)
    {
        if (player == null || player != playerStats || neckTransform == null) return;
        if (isStretching) return;

        UpdateNeckScale(new Vector3(1f + ratio * 0.1f, Mathf.Lerp(1f, 0.82f, ratio), 1f));

        if (ratio > 0.1f)
        {
            float s = ratio * shakeIntensity;
            neckTransform.localPosition = originalNeckPos + new Vector3(Random.Range(-s, s), Random.Range(-s, s), 0f);
        }

        if (gloveTransform)
            gloveTransform.localPosition = new Vector3(0, initialGloveY, 0);
    }

    private void OnWeaponSwapped(PlayerStats player, PlayerAttackController.WeaponMode mode)
    {
        if (player == null || player != playerStats || neckTransform == null) return;
        CancelEffects();
        UpdateNeckScale(Vector3.one);
        neckTransform.localPosition = originalNeckPos;
        if (gloveTransform)
            gloveTransform.localPosition = new Vector3(0, initialGloveY, 0);
    }

    private IEnumerator QuickStretchRoutine()
    {
        isStretching = true;
        Vector3 originalScale = Vector3.one;
        Vector3 stretchScale = new Vector3(1f, 1.5f, 1f);
        float duration = 0.05f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            UpdateNeckScale(Vector3.Lerp(originalScale, stretchScale, t / duration));
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            UpdateNeckScale(Vector3.Lerp(stretchScale, originalScale, t / duration));
            yield return null;
        }
        UpdateNeckScale(originalScale);
        isStretching = false;
    }

    private IEnumerator StretchRoutine(float ratio, float max)
    {
        isStretching = true;
        IsFistAnimating = true;
        lastFistAnimationFrame = Time.frameCount;

        if (gloveTransform) 
        {
            gloveTransform.localScale = Vector3.zero;
            gloveTransform.gameObject.SetActive(true);
        }

        if (gloveHitbox != null && playerStats != null)
        {
            gloveHitbox.ActivateHitbox(playerStats.EffectiveAttackDamage * (1f + ratio));
        }
        // Artifact feedback may synchronously cancel/end this attack.
        if (!IsFistAnimating) yield break;

        if (anim != null && anim.runtimeAnimatorController != null) anim.SetTrigger("attack");
        if (portraitAnim != null && portraitAnim.runtimeAnimatorController != null) portraitAnim.SetTrigger("attack");

        float gloveBoost = 1.2f + ratio * 1.5f; 
        Vector3 targetGloveScale = gloveOriginalScale * gloveBoost;
        Vector3 targetNeckScale = new Vector3(1f, max * (0.5f + ratio), 1f);
        float speed = playerStats != null ? playerStats.AttackSpeedMultiplier : 1f;
        float attackDuration = 0.25f / speed;

        yield return StartCoroutine(UpdateSyncWithAnim(targetNeckScale, Vector3.zero, targetGloveScale, attackDuration, 0f, 1f));
        float holdTime = 0f;
        while (holdTime < 0.15f / speed)
        {
            lastFistAnimationFrame = Time.frameCount;
            holdTime += Time.deltaTime;
            yield return null;
        }

        if (gloveHitbox != null) gloveHitbox.DeactivateHitbox();

        yield return StartCoroutine(UpdateSyncWithAnim(Vector3.one, targetGloveScale, Vector3.zero, attackDuration, 1f, 0f));

        if (gloveTransform) gloveTransform.gameObject.SetActive(false);
        ResetAnimation(anim);
        ResetAnimation(portraitAnim);

        isStretching = false; 
        IsFistAnimating = false;
        attack?.CompleteAttack();
    }

    private IEnumerator UpdateSyncWithAnim(Vector3 targetNeck, Vector3 fromGlove, Vector3 toGlove, float duration, float startAnimT, float endAnimT)
    {
        float t = 0;
        Vector3 startNeck = neckTransform.localScale;
        while (t < 1f)
        {
            lastFistAnimationFrame = Time.frameCount;
            t += Time.deltaTime / duration;
            float easedT = Mathf.SmoothStep(0, 1, t);
            UpdateNeckScale(Vector3.Lerp(startNeck, targetNeck, easedT));
            if (gloveTransform)
            {
                gloveTransform.localPosition = new Vector3(0, initialGloveY, 0);
                float sY = neckTransform.localScale.y;
                if (sY > 0.01f)
                {
                    Vector3 currentTargetGlove = Vector3.Lerp(fromGlove, toGlove, easedT);
                    gloveTransform.localScale = new Vector3(currentTargetGlove.x, currentTargetGlove.y / sY, currentTargetGlove.z);
                }
            }
            float currentAnimTime = Mathf.Lerp(startAnimT, endAnimT, easedT);
            if (anim != null && anim.runtimeAnimatorController != null) anim.SetFloat("animTime", currentAnimTime);
            if (portraitAnim != null && portraitAnim.runtimeAnimatorController != null) portraitAnim.SetFloat("animTime", currentAnimTime);
            yield return null;
        }
        UpdateNeckScale(targetNeck);
    }
}
