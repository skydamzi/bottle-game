using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public GameObject prefab;
        [Tooltip("최소 풀 개수")]
        public int size = 10;
    }

    public static PoolManager Instance;

    [Tooltip("게임 시작 전 풀 등록")]
    public List<Pool> pools = new List<Pool>();

    private Dictionary<GameObject, Queue<Poolable>> poolDictionary = new Dictionary<GameObject, Queue<Poolable>>();
    private Dictionary<GameObject, Transform> poolParents = new Dictionary<GameObject, Transform>();
    private readonly HashSet<Poolable> activeObjects = new HashSet<Poolable>();

    void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else 
        { 
            Destroy(gameObject); 
            return;
        }
    }

    void Start()
    {
        if (Instance != this || pools == null) return;
        foreach (Pool pool in pools)
        {
            PreparePool(pool.prefab, pool.size);
        }
    }

    public void PreparePool(GameObject prefab, int size)
    {
        if (prefab == null || poolDictionary.ContainsKey(prefab)) return;

        Transform poolParent = new GameObject($"Pool_{prefab.name}").transform;
        poolParent.SetParent(this.transform);

        poolParents.Add(prefab, poolParent);
        poolDictionary.Add(prefab, new Queue<Poolable>());

        for (int i = 0; i < size; i++)
        {
            poolDictionary[prefab].Enqueue(CreateNewObject(prefab));
        }
    }

    private Poolable CreateNewObject(GameObject prefab)
    {
        GameObject newObj = Instantiate(prefab, poolParents[prefab]);
        if (!newObj.TryGetComponent<Poolable>(out var poolable))
        {
            poolable = newObj.AddComponent<Poolable>();
        }
        poolable.Initialize(prefab, this);
        newObj.SetActive(false);
        poolable.SetLeased(false);
        return poolable;
    }

    public Poolable GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        // If the pool for this prefab doesn't exist, create it on the fly with a default size.
        if (!poolDictionary.ContainsKey(prefab)) 
        {
            Debug.LogWarning($"Pool for prefab '{prefab.name}' was not pre-warmed. Creating a default pool now.");
            PreparePool(prefab, 5);
        }

        // If the pool is empty, expand it by creating a new object.
        Poolable objToGet = null;
        while (poolDictionary[prefab].Count > 0 && objToGet == null)
        {
            Poolable candidate = poolDictionary[prefab].Dequeue();
            if (candidate != null && !activeObjects.Contains(candidate)) objToGet = candidate;
        }
        if (objToGet == null) objToGet = CreateNewObject(prefab);

        activeObjects.Add(objToGet);
        objToGet.SetLeased(true);
        objToGet.transform.SetPositionAndRotation(position, rotation);
        objToGet.gameObject.SetActive(true);
        return objToGet;
    }

    public void ReturnToPool(GameObject prefab, Poolable poolable)
    {
        if (poolable == null || poolable.manager != this || !activeObjects.Remove(poolable)) return;
        poolable.SetLeased(false);
        poolable.gameObject.SetActive(false);
        if (prefab == null || prefab != poolable.sourcePrefab || !poolDictionary.ContainsKey(prefab))
        { 
            Destroy(poolable.gameObject); 
            return; 
        }
        poolable.transform.SetParent(poolParents[prefab]);
        poolDictionary[prefab].Enqueue(poolable);
    }

    internal void Forget(Poolable obj) => activeObjects.Remove(obj);
    public int GetTotalActiveCount() => activeObjects.Count;
    void OnDestroy() { if (Instance == this) Instance = null; }
}
