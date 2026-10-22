using UnityEngine;

public class Poolable : MonoBehaviour
{
    public GameObject sourcePrefab;
    public Rigidbody2D rb;
    public Collider2D col;
    public bool IsLeased { get; private set; }
    internal PoolManager manager;
    private bool returning;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void ReturnToPool()
    {
        if (returning) return;
        returning = true;
        if (manager != null)
        {
            manager.ReturnToPool(sourcePrefab, this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    internal void Initialize(GameObject prefab, PoolManager owner)
    {
        sourcePrefab = prefab;
        manager = owner;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    internal void SetLeased(bool value)
    {
        IsLeased = value;
        returning = false;
        if (rb != null) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; }
    }

    void OnDestroy()
    {
        if (manager != null) manager.Forget(this);
    }
}
