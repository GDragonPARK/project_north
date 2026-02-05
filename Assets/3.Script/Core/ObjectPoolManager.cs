using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional, depending on scene architecture
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                if (pool.prefab == null) continue;
                
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform); // Keep hierarchy clean
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        Queue<GameObject> objectPool = poolDictionary[tag];

        if (objectPool.Count == 0)
        {
            Debug.LogWarning($"Pool {tag} ran out of objects!");
            // Optional: Instantiate more or just return null
            return null; 
        }

        GameObject objectToSpawn = objectPool.Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Add back to queue (this implementation assumes auto-return or manual return isn't strict about "who" returns it, 
        // but typically you re-enqueue when it's "returned". 
        // Simple Ring Buffer style: Dequeue -> Enable -> Enqueue immediately?
        // NO, standard pattern is Dequeue -> Use -> Return later.
        // But for particles that auto-destroy/disable, we need a way to return them.
        
        // Alternative simple approach for fire-and-forget particles: 
        // Just re-enqueue immediately if we don't care about "Max active at once" strictly blocking, 
        // but that means we might grab an active particle.
        
        // Better: ReturnToPool method.
        // But particles often handle their own lifetime. 
        // We'll add a helper component to pooled objects if needed, OR just letting them Disable themselves 
        // and we check active state? No, Queue operations are O(1).
        
        // Let's use the layout where we expect the caller or the object itself to return.
        // For simplicity in this task, let's implement a "AutoReturn" component on the particle?
        // Or just let the pool manage "Active" state? 
        
        // Actually, for "Hit Particles", they usually last 1-2 seconds.
        // We can just add a simple Coroutine helper here to return it.
        
        // However, if we don't re-enqueue, we lose reference. 
        // Let's just modify the pool to be a list of "Available"? 
        // Or standard Queue. Logic: Dequeue -> Enable. 
        // When Done -> Disable -> Enqueue.
        
        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        objectToReturn.SetActive(false);
        objectToReturn.transform.SetParent(this.transform);
        
        if (poolDictionary.ContainsKey(tag))
        {
            poolDictionary[tag].Enqueue(objectToReturn);
        }
    }
}
