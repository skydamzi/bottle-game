using UnityEngine;

public class Bullet : MonoBehaviour, IDamageDealer
{
    private float _damage;
    private GameObject _owner;
    private bool _hasHit = false;
    private Poolable _poolable;

    void Awake()
    {
        _poolable = GetComponent<Poolable>();
    }

    void OnEnable()
    {
        _hasHit = false;
    }

    public void Init(float damageValue, GameObject owner)
    {
        _damage = damageValue;
        _owner = owner;
    }

    public float GetDamage() => _damage;
    public GameObject GetOwner() => _owner != null ? _owner : gameObject;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            if (other.gameObject == _owner) return;
            _hasHit = true;
            damageable.OnDamage(_damage, this);
            HandleDestruction();
        }
    }

    private void HandleDestruction()
    {
        if (_poolable != null) _poolable.ReturnToPool();
        else Destroy(gameObject);
    }
}