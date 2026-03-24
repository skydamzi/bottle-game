using System.Collections.Generic;
using UnityEngine;

public class FistHitbox : MonoBehaviour, IDamageDealer
{
    private float _currentDamage;
    private Collider2D _collider;
    private PlayerStats _stats;

    private readonly List<IDamageable> _hitTargets = new List<IDamageable>();

    void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _stats = GetComponentInParent<PlayerStats>();
        _collider.enabled = false;
    }

    public float GetDamage() => _currentDamage;
    public GameObject GetOwner() => _stats.gameObject;

    public void ActivateHitbox(float damage)
    {
        _currentDamage = damage;
        _hitTargets.Clear();
        _collider.enabled = true;
    }

    public void DeactivateHitbox()
    {
        _collider.enabled = false;
        _hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            if (other.gameObject == GetOwner()) return;
            if (_hitTargets.Contains(target)) return;

            _hitTargets.Add(target);     
            target.OnDamage(GetDamage(), this);
            
            Debug.Log($"{other.name}에게 핵펀치! 데미지: {GetDamage()}");
        }
    }
}