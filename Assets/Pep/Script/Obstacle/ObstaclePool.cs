using UnityEngine;
using System.Collections.Generic;

public class ObstaclePool : MonoBehaviour
{
    public static ObstaclePool Instance;
    private Dictionary<string, Queue<ObstacleBase>> poolDictionary;

    private void Awake()
    {
        Instance = this;
        if (poolDictionary == null)
            poolDictionary = new Dictionary<string, Queue<ObstacleBase>>();
        Debug.Log("ObstaclePool initialized");
    }

    public void RegisterNewObstacle(string typeName, ObstacleBase prefab, int size)
    {
        if (poolDictionary.ContainsKey(typeName))
        {
            Debug.LogWarning($"Obstacle type '{typeName}' already registered!");
            return;
        }

        Queue<ObstacleBase> objectPool = new Queue<ObstacleBase>();
        for (int i = 0; i < size; i++)
        {
            ObstacleBase obj = Instantiate(prefab);
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(transform);
            objectPool.Enqueue(obj);
        }
        poolDictionary.Add(typeName, objectPool);
        Debug.Log($"✅ Registered {typeName} with {size} objects");
    }

    public ObstacleBase SpawnFromPool(string typeName, Vector3 position, Vector3 direction, float speed)
    {
        if (!poolDictionary.ContainsKey(typeName))
        {
            Debug.LogWarning($"Pool with type '{typeName}' doesn't exist. Available types: {string.Join(", ", poolDictionary.Keys)}");
            return null;
        }

        if (poolDictionary[typeName].Count == 0)
        {
            Debug.LogWarning($"No available objects in pool '{typeName}'");
            return null;
        }

        ObstacleBase objectToSpawn = poolDictionary[typeName].Dequeue();
        objectToSpawn.transform.position = position;
        objectToSpawn.gameObject.SetActive(true);
        objectToSpawn.Init(direction, speed);

        poolDictionary[typeName].Enqueue(objectToSpawn);

        return objectToSpawn;
    }

    public void ReturnToPool(string typeName, ObstacleBase obstacle)
    {
        if (poolDictionary.ContainsKey(typeName))
        {
            obstacle.gameObject.SetActive(false);
            obstacle.transform.SetParent(transform);
            poolDictionary[typeName].Enqueue(obstacle);
        }
    }

    public int GetPoolCount(string typeName)
    {
        if (poolDictionary == null)
            poolDictionary = new Dictionary<string, Queue<ObstacleBase>>();
        return poolDictionary.ContainsKey(typeName) ? poolDictionary[typeName].Count : 0;
    }

    [ContextMenu("Debug Pool Status")]
    void DebugPoolStatus()
    {
        if (poolDictionary == null)
            poolDictionary = new Dictionary<string, Queue<ObstacleBase>>();
        foreach (var pool in poolDictionary)
        {
            Debug.Log($"Pool '{pool.Key}': {pool.Value.Count} objects available");
        }
    }
}