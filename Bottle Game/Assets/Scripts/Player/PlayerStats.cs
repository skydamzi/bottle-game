using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 3f;
    public float attackDamage = 10f;
    // Serialized base values remain permanent; the exploration build is a separate layer.
    public RunBuild RunBuild
    {
        get
        {
            if (runBuild == null) runBuild = GetComponent<RunBuild>();
            return runBuild != null && runBuild.isActiveAndEnabled && runBuild.IsRunActive ? runBuild : null;
        }
    }
    public float EffectiveMaxHealth => Mathf.Max(1f, maxHealth + (RunBuild != null ? RunBuild.MaxHealthBonus : 0f));
    public float EffectiveAttackDamage => attackDamage * (RunBuild != null ? RunBuild.AttackDamageMultiplier : 1f);
    public float AttackSpeedMultiplier => RunBuild != null ? RunBuild.AttackSpeedMultiplier : 1f;
    [Header("피격")]
    [Min(0f)] public float hitStunDuration = 0.12f;
    [Min(0f)] public float knockbackSpeed;
    public bool IsDead => currentHealth <= 0f;
    public bool IsStunned => !IsDead && Time.time < stunnedUntil;
    public Vector2 KnockbackVelocity => IsStunned ? knockbackVelocity : Vector2.zero;
    public bool IsInvulnerable
    {
        get
        {
            if (dodge == null) dodge = GetComponent<PlayerDodgeController>();
            return dodge != null && dodge.IsInvulnerable;
        }
    }
    public event System.Action<float, float> OnHealthChanged;
    public event System.Action OnDied;
    private float stunnedUntil;
    private Vector2 knockbackVelocity;
    private PlayerDodgeController dodge;
    private RunBuild runBuild;

    void Awake()
    {
        currentHealth = maxHealth;
        dodge = GetComponent<PlayerDodgeController>();
    }

    public void OnDamage(float damage, IDamageDealer dealer)
    {
        if (IsDead || IsInvulnerable || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        if (RunBuild != null) damage *= RunBuild.DamageTakenMultiplier;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, EffectiveMaxHealth);
        GetComponent<PlayerAttackController>()?.CancelAttack();
        if (dodge == null) dodge = GetComponent<PlayerDodgeController>();
        dodge?.CancelDodge();
        stunnedUntil = Time.time + Mathf.Max(0f, hitStunDuration);
        GameObject owner = dealer?.GetOwner();
        knockbackVelocity = owner != null ? ((Vector2)(transform.position - owner.transform.position)).normalized * knockbackSpeed : Vector2.zero;
        OnHealthChanged?.Invoke(currentHealth, EffectiveMaxHealth);
        Debug.Log($"{dealer}에게 {damage}만큼 맞음. 체력 : {currentHealth} / {maxHealth}");
        
        if (currentHealth <= 0) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        currentHealth = Mathf.Min(EffectiveMaxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, EffectiveMaxHealth);
    }

    public void RefreshEffectiveHealth()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, EffectiveMaxHealth);
        OnHealthChanged?.Invoke(currentHealth, EffectiveMaxHealth);
    }

    public void ResetForExpedition()
    {
        GetComponent<PlayerAttackController>()?.ResetForExpedition();
        if (dodge == null) dodge = GetComponent<PlayerDodgeController>();
        dodge?.ResetForExpedition();
        stunnedUntil = 0f;
        knockbackVelocity = Vector2.zero;
        RunBuild?.ResetTransientEffects();
        currentHealth = EffectiveMaxHealth;
        OnHealthChanged?.Invoke(currentHealth, EffectiveMaxHealth);
    }

    private void Die()
    {
        GetComponent<PlayerAttackController>()?.CancelAttack();
        dodge?.CancelDodge();
        OnDied?.Invoke();
        Debug.Log("플레이어 사망");
    }
}
