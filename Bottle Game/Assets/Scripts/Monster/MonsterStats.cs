using UnityEngine;
using System;

public class MonsterStats : MonoBehaviour, IDamageable
{
    public event Action<float, float> OnHealthChanged;
    public float maxHealth = 50f;
    public float currentHealth;
    public float attackDamage = 5f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void OnDamage(float damage, IDamageDealer dealer)
    {
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"{dealer}에게 {damage}만큼 맞음. 체력 : {currentHealth} / {maxHealth}");
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}