using UnityEngine;
using System.Collections.Generic;

public class EnemylifesourceCheck : MonoBehaviour
{
    public GameObject enemy;
    private List<GameObject> lifeSources = new List<GameObject>();

    void Start()
    {
        Destroyrange[] sources = FindObjectsOfType<Destroyrange>();
        foreach (Destroyrange src in sources)
        {
            lifeSources.Add(src.gameObject);
        }
        Debug.Log("Found " + lifeSources.Count + " Destroyrange objects.");
    }

    void Update()
    {
        lifeSources.RemoveAll(item => item == null);
        if(lifeSources.Count == 0)
        {
            Destroy(enemy);
        }
    }
}
