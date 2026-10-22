using UnityEngine;
using System;

public class MonsterStats : MonoBehaviour, IDamageable
{
    public event Action<float, float> OnHealthChanged;
    public event Action<MonsterStats> OnDied;
    public float maxHealth = 50f;
    public float currentHealth;
    public float attackDamage = 5f;
    private bool died;
    public bool IsDead => died || currentHealth <= 0f;

    void Awake()
    {
        InitializeForSpawn();
    }

    public void InitializeForSpawn()
    {
        died = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void OnDamage(float damage, IDamageDealer dealer)
    {
        if (IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{dealer}에게 {damage}만큼 맞음. 체력 : {currentHealth} / {maxHealth}");
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (died) return;
        died = true;
        OnDied?.Invoke(this);
        Destroy(gameObject);
    }
}
