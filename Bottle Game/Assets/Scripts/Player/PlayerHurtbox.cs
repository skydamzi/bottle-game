using UnityEngine;

public class PlayerHurtbox : MonoBehaviour, IDamageable
{
    private PlayerStats _stats;

    void Awake()
    {
        _stats = GetComponentInParent<PlayerStats>();
    }

    public void OnDamage(float damage, IDamageDealer dealer)
    {
        _stats?.OnDamage(damage, dealer);
    }
}