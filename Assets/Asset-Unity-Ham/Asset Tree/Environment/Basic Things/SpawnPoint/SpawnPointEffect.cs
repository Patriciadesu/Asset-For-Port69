using Unity.VisualScripting;
using UnityEngine;

public class SpawnPointEffect : ObjectEffect
{
    [SerializeField] private float yOffset = 1f;
    
    public override void ApplyEffect(GameObject player)
    {
        if (player != null)
        {
            Vector3 spawnPosition = transform.position;

            spawnPosition.y += yOffset;

            if(PlayerSpawnPointData.Instance == null)
            {
                this.AddComponent<PlayerSpawnPointData>();
            }

            PlayerSpawnPointData.Instance.spawnPoint = spawnPosition;
            Debug.Log($"{gameObject.name} set spawn point for {player.gameObject.name} at {spawnPosition}");
        }
    }
}