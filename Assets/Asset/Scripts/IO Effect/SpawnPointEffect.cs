using UnityEngine;

public class SpawnPointEffect : ObjectEffect
{
    [SerializeField] private float yOffset = 1f;
    public override void ApplyEffect(Collision playerCollision)
    {
        PlayerController player = playerCollision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            Vector3 spawnPosition = transform.position;

            spawnPosition.y += yOffset;

            player.spawnPoint = spawnPosition;
            Debug.Log($"{gameObject.name} set spawn point at {spawnPosition}");
        }
    }
}