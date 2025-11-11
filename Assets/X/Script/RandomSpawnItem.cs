using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RandomSpawnItem : MonoBehaviour
{
    public List<GameObject> item = new List<GameObject>();
    public List<Transform> spawnpoint = new List<Transform>();

    private void Start()
    {
        int count = item.Count;

        for (int i = 0; i < count; i++)
        {
            int randomI = UnityEngine.Random.Range(0, item.Count);
            int randomS = UnityEngine.Random.Range(0, spawnpoint.Count);

            Vector3 pos = new Vector3(spawnpoint[randomS].position.x, spawnpoint[randomS].position.y, spawnpoint[randomS].position.z);

            Instantiate(item[randomI], pos, Quaternion.identity);
            item.RemoveAt(randomI);
            spawnpoint.RemoveAt(randomS);

        }

    }
}
