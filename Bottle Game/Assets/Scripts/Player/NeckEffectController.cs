using System.Collections;
using UnityEngine;

public class NeckEffectController : MonoBehaviour
{
    public Transform neckTransform;
    public Transform gloveTransform;
    public Transform neckCover; 
    public FistHitbox gloveHitbox;
    public float shakeIntensity;
    public bool isStretching { get; private set; }

    private Vector3 originalNeckPos;
    private Vector3 gloveOriginalScale;
    private Vector3 originalNeckCoverScale;
    private float initialGloveY; 
    private Animator anim;
    public Animator portraitAnim;
    private PlayerStats playerStats;

    void Awake()
    {
        playerStats = GetComponentInParent<PlayerStats>();
        PlayerEvents.OnGunFired += OnGunFired;
        PlayerEvents.OnFistAttackReleased += OnFistAttack;
        PlayerEvents.OnFistChargeUpdate += OnFistChargeUpdate;
        PlayerEvents.OnWeaponSwapped += OnWeaponSwapped;
    }

    void Start()
    {
        anim = GetComponentInParent<Animator>();
        originalNeckPos = neckTransform.localPosition;

        if (gloveTransform)
        {
            gloveOriginalScale = gloveTransform.localScale;
            initialGloveY = gloveTransform.localPosition.y; 
            gloveTransform.gameObject.SetActive(false);
        }
        if (neckCover)
        {
            originalNeckCoverScale = neckCover.localScale;
        }
    }

    void OnDestroy()
    {
        PlayerEvents.OnGunFired -= OnGunFired;
        PlayerEvents.OnFistAttackReleased -= OnFistAttack;
        PlayerEvents.OnFistChargeUpdate -= OnFistChargeUpdate;
        PlayerEvents.OnWeaponSwapped -= OnWeaponSwapped;
    }

    private void UpdateNeckScale(Vector3 newScale)
    {
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

    private void OnGunFired()
    {
        if (isStretching) return;
        StartCoroutine(QuickStretchRoutine());
    }

    private void OnFistAttack(float ratio)
    {
        if (isStretching) return;
        neckTransform.localPosition = originalNeckPos;
        StartCoroutine(StretchRoutine(ratio, 4f));
    }

    private void OnFistChargeUpdate(float ratio)
    {
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

    private void OnWeaponSwapped(PlayerAttackController.WeaponMode mode)
    {
        if (isStretching) return;
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

        if (gloveTransform) 
        {
            gloveTransform.localScale = Vector3.zero;
            gloveTransform.gameObject.SetActive(true);
        }

        if (gloveHitbox != null && playerStats != null)
        {
            gloveHitbox.ActivateHitbox(playerStats.attackDamage * (1f + ratio));
        }

        if (anim) anim.SetTrigger("attack");
        if (portraitAnim) portraitAnim.SetTrigger("attack");

        float gloveBoost = 1.2f + ratio * 1.5f; 
        Vector3 targetGloveScale = gloveOriginalScale * gloveBoost;
        Vector3 targetNeckScale = new Vector3(1f, max * (0.5f + ratio), 1f);
        float attackDuration = 0.25f;

        yield return StartCoroutine(UpdateSyncWithAnim(targetNeckScale, Vector3.zero, targetGloveScale, attackDuration, 0f, 1f));
        yield return new WaitForSeconds(0.15f);

        if (gloveHitbox != null) gloveHitbox.DeactivateHitbox();

        yield return StartCoroutine(UpdateSyncWithAnim(Vector3.one, targetGloveScale, Vector3.zero, attackDuration, 1f, 0f));

        if (gloveTransform) gloveTransform.gameObject.SetActive(false);
        if (anim) { anim.SetFloat("animTime", 0f); anim.Play("Idle_Blend_Tree", 0, 0f); }
        if (portraitAnim) { portraitAnim.SetFloat("animTime", 0f); portraitAnim.Play("Idle_Blend_Tree", 0, 0f); }

        isStretching = false; 
        PlayerEvents.RaisePlayerAttackEnded();
    }

    private IEnumerator UpdateSyncWithAnim(Vector3 targetNeck, Vector3 fromGlove, Vector3 toGlove, float duration, float startAnimT, float endAnimT)
    {
        float t = 0;
        Vector3 startNeck = neckTransform.localScale;
        while (t < 1f)
        {
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
            if (anim) anim.SetFloat("animTime", currentAnimTime);
            if (portraitAnim) portraitAnim.SetFloat("animTime", currentAnimTime);
            yield return null;
        }
        UpdateNeckScale(targetNeck);
    }
}