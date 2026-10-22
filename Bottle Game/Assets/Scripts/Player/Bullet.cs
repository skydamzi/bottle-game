using UnityEngine;

public class Bullet : MonoBehaviour, IDamageDealer
{
    private float _damage;
    private GameObject _owner;
    private bool _hasHit = false;
    private Poolable _poolable;
    private float _expiresAt;
    private bool _initialized;
    private RunAttackSnapshot _runAttack;
    private int _pierceRemaining;
    private int _leaseVersion;
    private readonly System.Collections.Generic.HashSet<int> _hitTargets = new System.Collections.Generic.HashSet<int>();

    void Awake()
    {
        _poolable = GetComponent<Poolable>();
    }

    void OnEnable()
    {
        _hasHit = false;
        _initialized = false;
    }

    public void Init(float damageValue, GameObject owner)
    {
        Init(damageValue, owner, 2f);
    }

    public void Init(float damageValue, GameObject owner, float lifetime)
    {
        // Older prefabs may acquire Poolable after Bullet.Awake.
        _poolable = GetComponent<Poolable>();
        _damage = damageValue;
        _owner = owner;
        _hasHit = false;
        _expiresAt = Time.time + Mathf.Max(0.01f, lifetime);
        _initialized = true;
        _leaseVersion++;
        _runAttack = default;
        _pierceRemaining = 0;
        _hitTargets.Clear();
    }

    void Update() { if (_initialized && Time.time >= _expiresAt) HandleDestruction(); }

    void OnDisable()
    {
        _initialized = false;
        _leaseVersion++;
        _owner = null;
        _damage = 0f;
        _runAttack = default;
        _hitTargets.Clear();
    }

    public float GetDamage() => _damage;
    public GameObject GetOwner() => _owner != null ? _owner : gameObject;

    public void SetRunAttack(RunAttackSnapshot attack)
    {
        _runAttack = attack;
        _pierceRemaining = attack.PierceCount;
    }

    public void Despawn() => HandleDestruction();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit || !_initialized) return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            if (_owner != null && other.transform.IsChildOf(_owner.transform)) return;
            MonsterStats monster = other.GetComponentInParent<MonsterStats>();
            int targetId = monster != null ? monster.GetInstanceID() : ((Component)damageable).GetInstanceID();
            if (!_hitTargets.Add(targetId)) return;
            int hitLease = _leaseVersion;
            RunAttackSnapshot hitAttack = _runAttack;
            bool finish = _pierceRemaining <= 0;
            if (finish) _hasHit = true;
            else _pierceRemaining--;
            damageable.OnDamage(_damage, this);
            // Damage observers may return/reuse this very pooled object synchronously.
            if (!_initialized || hitLease != _leaseVersion) return;
            if (hitAttack.Build != null) hitAttack.Build.NotifyBulletHit(hitAttack, damageable, this);
            if (!_initialized || hitLease != _leaseVersion) return;
            if (finish) HandleDestruction();
        }
    }

    private void HandleDestruction()
    {
        if (!_initialized) return;
        _initialized = false;
        _hasHit = true;
        if (_poolable != null) _poolable.ReturnToPool();
        else Destroy(gameObject);
    }
}
