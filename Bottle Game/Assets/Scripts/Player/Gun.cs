using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform weaponSpawnPoint;
    public float bulletSpeed = 10f;
    public float bulletLifeTime = 2f;

    private GameObject _ownerObject;

    void Awake()
    {
        var stats = GetComponentInParent<PlayerStats>();
        _ownerObject = stats != null ? stats.gameObject : transform.root.gameObject;
    }

    public void Attack(Transform target, float damage)
    {
        TryAttack(target, damage);
    }

    public bool TryAttack(Transform target, float damage)
    {
        if (target == null || weaponSpawnPoint == null || PoolManager.Instance == null) return false;
        Poolable bulletObj = PoolManager.Instance.GetFromPool(bulletPrefab, weaponSpawnPoint.position, Quaternion.identity);
        if (bulletObj == null) return false;

        if (!bulletObj.TryGetComponent<Bullet>(out var bullet))
        {
            bulletObj.ReturnToPool();
            return false;
        }
        RunBuild build = _ownerObject != null ? _ownerObject.GetComponent<RunBuild>() : null;
        if (build != null && build.isActiveAndEnabled && build.IsRunActive)
        {
            RunAttackSnapshot attack = build.CreateAttack(damage, true);
            if (!build.IsAttackCurrent(attack))
            {
                bulletObj.ReturnToPool();
                return false;
            }
            bullet.Init(attack.Damage, _ownerObject, bulletLifeTime * build.ProjectileRangeMultiplier);
            bullet.SetRunAttack(attack);
        }
        else bullet.Init(damage, _ownerObject, bulletLifeTime);

        Vector2 direction = (target.position - weaponSpawnPoint.position).normalized;
        if (bulletObj.rb != null)
        {
            bulletObj.rb.velocity = direction * bulletSpeed;
        }

        return true;
    }
}
