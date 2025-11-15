using System.Collections.Generic;
using UnityEngine;

public partial class ItemCounter
{
    [Header("Random Spawn")]
    [SerializeField, Tooltip("Prefabs randomly spawned into available points.")]
    private List<GameObject> randomSpawnItems = new List<GameObject>();
    [SerializeField, Tooltip("Spawn points that items can occupy.")]
    private List<Transform> randomSpawnPoints = new List<Transform>();
    [SerializeField, Tooltip("Prevent multiple spawns when the component is re-enabled.")]
    private bool spawnOnlyOnce = true;

    private bool randomSpawnConsumed;

    partial void InitializeRandomSpawnModule()
    {
        if (spawnOnlyOnce && randomSpawnConsumed) return;
        if (randomSpawnItems == null || randomSpawnItems.Count == 0) return;
        if (randomSpawnPoints == null || randomSpawnPoints.Count == 0) return;

        SpawnRandomItems();
        randomSpawnConsumed = true;
    }

    private void SpawnRandomItems()
    {
        var availableItems = new List<GameObject>(randomSpawnItems);
        var availablePoints = new List<Transform>(randomSpawnPoints);
        int loopCount = Mathf.Min(availableItems.Count, availablePoints.Count);

        for (int i = 0; i < loopCount; i++)
        {
            int randomItemIndex = Random.Range(0, availableItems.Count);
            int randomPointIndex = Random.Range(0, availablePoints.Count);

            var prefab = availableItems[randomItemIndex];
            var point = availablePoints[randomPointIndex];

            if (prefab && point)
            {
                Object.Instantiate(prefab, point.position, point.rotation);
            }

            availableItems.RemoveAt(randomItemIndex);
            availablePoints.RemoveAt(randomPointIndex);
        }
    }
}

