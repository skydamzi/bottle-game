using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 3f;
    public float attackDamage = 10f;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void OnDamage(float damage, IDamageDealer dealer)
    {
        currentHealth -= damage;
        Debug.Log($"{dealer}에게 {damage}만큼 맞음. 체력 : {currentHealth} / {maxHealth}");
        
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        Debug.Log("플레이어 사망");
    }
}