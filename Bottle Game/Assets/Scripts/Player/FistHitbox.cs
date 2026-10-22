using System.Collections.Generic;
using UnityEngine;

public class FistHitbox : MonoBehaviour, IDamageDealer
{
    private float _currentDamage;
    private Collider2D _collider;
    private PlayerStats _stats;

    private readonly HashSet<int> _hitTargets = new HashSet<int>();
    private bool _active;
    private int _activationVersion;

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _stats = GetComponentInParent<PlayerStats>();
        if (_collider != null) _collider.enabled = false;
    }

    public float GetDamage() => _currentDamage;
    public GameObject GetOwner() => _stats != null ? _stats.gameObject : gameObject;

    public void ActivateHitbox(float damage)
    {
        int activation = ++_activationVersion;
        RunBuild build = _stats != null ? _stats.RunBuild : null;
        RunAttackSnapshot attack = build != null ? build.CreateAttack(damage, false) : default;
        if (activation != _activationVersion || (build != null && !build.IsAttackCurrent(attack))) return;
        _currentDamage = build != null ? attack.Damage : damage;
        _hitTargets.Clear();
        _active = true;
        if (_collider != null) _collider.enabled = true;
    }

    public void DeactivateHitbox()
    {
        _activationVersion++;
        _active = false;
        if (_collider != null) _collider.enabled = false;
        _hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_active || !isActiveAndEnabled) return;
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            if (other.transform.IsChildOf(GetOwner().transform)) return;
            MonsterStats monster = other.GetComponentInParent<MonsterStats>();
            int targetId = monster != null ? monster.GetInstanceID() : ((Component)target).GetInstanceID();
            if (!_hitTargets.Add(targetId)) return;
            target.OnDamage(GetDamage(), this);
            
            Debug.Log($"{other.name}에게 핵펀치! 데미지: {GetDamage()}");
        }
    }

    void OnDisable() => DeactivateHitbox();
}
