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
    public List<Pool> pools;

    private Dictionary<GameObject, Queue<Poolable>> poolDictionary = new Dictionary<GameObject, Queue<Poolable>>();
    private Dictionary<GameObject, Transform> poolParents = new Dictionary<GameObject, Transform>();
    private int totalActiveCount = 0;

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
        }
    }

    void Start()
    {
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
            CreateNewObject(prefab);
        }
    }

    private Poolable CreateNewObject(GameObject prefab)
    {
        GameObject newObj = Instantiate(prefab, poolParents[prefab]);
        if (!newObj.TryGetComponent<Poolable>(out var poolable))
        {
            poolable = newObj.AddComponent<Poolable>();
        }
        poolable.sourcePrefab = prefab;
        newObj.SetActive(false);
        poolDictionary[prefab].Enqueue(poolable);
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
        Poolable objToGet = (poolDictionary[prefab].Count > 0) 
            ? poolDictionary[prefab].Dequeue() 
            : CreateNewObject(prefab);

        objToGet.transform.SetPositionAndRotation(position, rotation);
        objToGet.gameObject.SetActive(true);
        totalActiveCount++;
        return objToGet;
    }

    public void ReturnToPool(GameObject prefab, Poolable poolable)
    {
        if (prefab == null || !poolDictionary.ContainsKey(prefab)) 
        { 
            Destroy(poolable.gameObject); 
            return; 
        }
        if (!poolable.gameObject.activeSelf) return;

        poolable.gameObject.SetActive(false);
        poolable.transform.SetParent(poolParents[prefab]);
        poolDictionary[prefab].Enqueue(poolable);
        totalActiveCount--;
    }

    public int GetTotalActiveCount() => totalActiveCount;
}