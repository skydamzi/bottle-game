using UnityEngine;

public class Poolable : MonoBehaviour
{
    public GameObject sourcePrefab;
    public Rigidbody2D rb;
    public Collider2D col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void ReturnToPool()
    {
        if (sourcePrefab != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(sourcePrefab, this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}