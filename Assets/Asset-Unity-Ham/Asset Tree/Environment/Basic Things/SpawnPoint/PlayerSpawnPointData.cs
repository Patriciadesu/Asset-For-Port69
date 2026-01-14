using UnityEngine;

public class PlayerSpawnPointData : Singleton<PlayerSpawnPointData>
{
    public Vector3 spawnPoint;
    public void Start()
    {
        spawnPoint = GameObject.FindGameObjectWithTag("Player").transform.position;
    }
}