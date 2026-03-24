using UnityEngine;

public class MonsterCollisionDamage : MonoBehaviour, IDamageDealer
{
    private MonsterStats _stats;

    void Awake()
    {
        _stats = GetComponentInParent<MonsterStats>();
    }

    public float GetDamage() => _stats.attackDamage;
    public GameObject GetOwner() => _stats.gameObject;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var victim))
        {
            if (other.gameObject != _stats.gameObject)
            {
                victim.OnDamage(GetDamage(), this);
            }
        }
    }
}