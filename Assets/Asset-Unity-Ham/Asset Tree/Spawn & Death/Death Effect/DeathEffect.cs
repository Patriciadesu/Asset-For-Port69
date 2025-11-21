using UnityEngine;

public class DeathEffect : ObjectEffect
{
    public override void ApplyEffect( GameObject player)
    {
        if (player != null)
        {
            if(PlayerSpawnPointData.Instance != null)
            {
                player.transform.position = PlayerSpawnPointData.Instance.spawnPoint;
            }
            else
            {  
            player.SetActive(false);
            }
            Debug.Log($"{gameObject.name} triggered death effect - {player.gameObject.name} respawned!");
        }
    }
}