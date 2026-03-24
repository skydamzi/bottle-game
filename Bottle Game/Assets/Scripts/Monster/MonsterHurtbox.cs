using UnityEngine;

public class MonsterHurtbox : MonoBehaviour, IDamageable
{
    private MonsterStats _stats;

    void Awake()
    {
        _stats = GetComponentInParent<MonsterStats>();
    }

    public void OnDamage(float damage, IDamageDealer dealer)
    {
        _stats?.OnDamage(damage, dealer);
    }
}