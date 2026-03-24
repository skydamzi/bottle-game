using System.Collections;
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
        var dealer = GetComponentInParent<IDamageDealer>();
        _ownerObject = dealer?.GetOwner();
    }

    public void Attack(Transform target, float damage)
    {
        Poolable bulletObj = PoolManager.Instance.GetFromPool(bulletPrefab, weaponSpawnPoint.position, Quaternion.identity);
        if (bulletObj == null) return;

        if (bulletObj.TryGetComponent<Bullet>(out var bullet))
        {
            bullet.Init(damage, _ownerObject);
        }

        Vector2 direction = (target.position - weaponSpawnPoint.position).normalized;
        if (bulletObj.rb != null)
        {
            bulletObj.rb.velocity = direction * bulletSpeed;
        }

        StartCoroutine(ReturnBulletAfterTime(bulletObj, bulletLifeTime));
    }

    private IEnumerator ReturnBulletAfterTime(Poolable bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (bullet != null && bullet.gameObject.activeSelf) 
            bullet.ReturnToPool();
    }
}